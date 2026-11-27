from dataclasses import dataclass
from uuid import UUID, uuid4

from app.modules.activities.application.ports import (
    ActivityRepository,
    ProgressRepository,
    ScheduleRepository,
)
from app.modules.activities.domain.activity import (
    Activity,
    ActivityDraft,
    ActivityInput,
    ActivityStatus,
    advance_status,
)
from app.modules.activities.domain.ciphers import answer_key, encode_message
from app.modules.activities.domain.progress import Progress
from app.modules.campaigns.domain.state import CampaignState
from app.shared.errors import AppError
from app.shared.principal import Principal, Role
from app.shared.security import AnswerSigner, SolutionSealer


@dataclass(frozen=True, slots=True)
class ActivityCard:
    activity: Activity
    encoded_text: str | None
    progress: Progress | None


class CreateActivity:
    def __init__(
        self,
        activities: ActivityRepository,
        schedules: ScheduleRepository,
        signer: AnswerSigner,
        sealer: SolutionSealer,
    ) -> None:
        self._activities = activities
        self._schedules = schedules
        self._signer = signer
        self._sealer = sealer

    async def execute(self, campaign_id: UUID, data: ActivityInput) -> Activity:
        schedule = await self._schedules.get(campaign_id)
        if schedule is None:
            raise AppError.not_found("Campagne")
        if schedule.state is CampaignState.FINISHED:
            raise AppError.invalid_state("La campagne est terminée")
        if not 1 <= data.day <= schedule.days:
            raise AppError.validation(f"Le jour doit être compris entre 1 et {schedule.days}")
        if data.slot_start >= data.slot_end:
            raise AppError.validation("Le créneau doit commencer avant de se terminer")
        encoded = encode_message(data.plain_text, data.cipher)
        activity_id = uuid4()
        draft = ActivityDraft(
            id=activity_id,
            title=data.title.strip(),
            day=data.day,
            slot_start=data.slot_start,
            slot_end=data.slot_end,
            points=data.points,
            cipher=encoded.cipher,
            encoded_text=encoded.encoded,
            answer_digest=self._signer.digest(activity_id, answer_key(data.plain_text)),
            solution_sealed=self._sealer.seal(data.plain_text.strip()),
        )
        return await self._activities.add(campaign_id, draft)


class ChangeActivityStatus:
    def __init__(self, activities: ActivityRepository, schedules: ScheduleRepository) -> None:
        self._activities = activities
        self._schedules = schedules

    async def execute(self, activity_id: UUID, target: ActivityStatus) -> Activity:
        activity = await self._activities.get(activity_id)
        if activity is None:
            raise AppError.not_found("Activité")
        advance_status(activity.status, target)
        if target is ActivityStatus.UNLOCKED:
            schedule = await self._schedules.get(activity.campaign_id)
            if schedule is None or schedule.state is not CampaignState.RUNNING:
                raise AppError.invalid_state("La campagne doit être en cours pour débloquer")
        return await self._activities.set_status(activity_id, target)


class ListActivities:
    def __init__(self, activities: ActivityRepository, progress: ProgressRepository) -> None:
        self._activities = activities
        self._progress = progress

    async def execute(self, principal: Principal, campaign_id: UUID) -> list[ActivityCard]:
        activities = await self._activities.list_for_campaign(campaign_id)
        is_member = principal.role is Role.MEMBER
        progress_by_activity: dict[UUID, Progress] = {}
        if is_member:
            progress_by_activity = await self._progress.list_for_team(
                campaign_id, principal.member_team_id()
            )
        return [
            ActivityCard(
                activity=activity,
                encoded_text=(
                    activity.encoded_text
                    if not is_member or activity.status is ActivityStatus.UNLOCKED
                    else None
                ),
                progress=progress_by_activity.get(activity.id),
            )
            for activity in activities
        ]


class RevealSolution:
    def __init__(self, activities: ActivityRepository, sealer: SolutionSealer) -> None:
        self._activities = activities
        self._sealer = sealer

    async def execute(self, activity_id: UUID) -> str:
        if await self._activities.get(activity_id) is None:
            raise AppError.not_found("Activité")
        return self._sealer.unseal(await self._activities.get_solution_sealed(activity_id))
