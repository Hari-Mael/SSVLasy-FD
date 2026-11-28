from collections.abc import Callable
from dataclasses import dataclass
from uuid import UUID

from app.modules.activities.application.ports import (
    ActivityRepository,
    ProgressRepository,
    ScheduleRepository,
    TeamCampGate,
)
from app.modules.activities.domain.activity import Activity, ActivityStatus, CampaignSchedule
from app.modules.activities.domain.ciphers import answer_key
from app.modules.activities.domain.progress import (
    AttemptOutcome,
    Choice,
    Progress,
    Transition,
    choose,
    settle,
    start_attempt,
    submit,
)
from app.modules.activities.domain.slots import slot_is_open
from app.modules.campaigns.domain.state import CampaignState
from app.shared.clock import Clock
from app.shared.errors import AppError
from app.shared.principal import Principal
from app.shared.security import AnswerSigner


@dataclass(frozen=True, slots=True)
class OpenActivity:
    activity: Activity
    schedule: CampaignSchedule
    team_id: UUID


@dataclass(frozen=True, slots=True)
class SubmissionResult:
    outcome: AttemptOutcome
    progress: Progress


class ActivityGate:
    def __init__(
        self,
        activities: ActivityRepository,
        schedules: ScheduleRepository,
        clock: Clock,
        camp: TeamCampGate,
    ) -> None:
        self._activities = activities
        self._schedules = schedules
        self._clock = clock
        self._camp = camp

    async def open(self, principal: Principal, activity_id: UUID) -> OpenActivity:
        team_id = principal.member_team_id()
        activity = await self._activities.get(activity_id)
        if activity is None:
            raise AppError.not_found("Activité")
        schedule = await self._schedules.get(activity.campaign_id)
        if schedule is None:
            raise AppError.not_found("Campagne")
        if schedule.state is not CampaignState.RUNNING:
            raise AppError.invalid_state("La campagne n'est pas en cours")
        if activity.status is not ActivityStatus.UNLOCKED:
            raise AppError.invalid_state("L'activité n'est pas débloquée")
        inside_slot = slot_is_open(
            schedule.start_date,
            schedule.timezone,
            activity.day,
            activity.slot_start,
            activity.slot_end,
            self._clock.now(),
        )
        if not inside_slot:
            raise AppError.invalid_state("Hors du créneau de l'activité")
        reason = await self._camp.blocking_reason(team_id, self._clock.now())
        if reason is not None:
            raise AppError.invalid_state(reason)
        return OpenActivity(activity, schedule, team_id)


class ProgressStore:
    def __init__(self, progress: ProgressRepository) -> None:
        self._progress = progress

    async def commit(
        self,
        activity_id: UUID,
        team_id: UUID,
        before: Progress,
        after: Progress,
        outcome: AttemptOutcome | None,
    ) -> Progress:
        if outcome is not None:
            await self._progress.log(activity_id, team_id, after.attempts_used, outcome)
        await self._progress.save(activity_id, team_id, after)
        return after

    async def apply(
        self, activity_id: UUID, team_id: UUID, step: Callable[[Progress], Transition]
    ) -> Progress:
        before = await self._progress.lock(activity_id, team_id)
        transition = step(before)
        return await self.commit(
            activity_id, team_id, before, transition.progress, transition.outcome
        )


class StartAttempt:
    def __init__(self, gate: ActivityGate, progress: ProgressRepository, clock: Clock) -> None:
        self._gate = gate
        self._progress = progress
        self._store = ProgressStore(progress)
        self._clock = clock

    async def execute(self, principal: Principal, activity_id: UUID) -> Progress:
        opened = await self._gate.open(principal, activity_id)
        activity = opened.activity
        if activity.is_night:
            unsolved = await self._progress.count_unsolved_daytime(
                activity.campaign_id, activity.day, opened.team_id
            )
            if unsolved > 0:
                raise AppError.forbidden(
                    "Activité de nuit réservée aux équipes ayant terminé les tâches du jour"
                )
        return await self._store.apply(
            activity_id, opened.team_id, lambda before: start_attempt(before, self._clock.now())
        )


class SubmitAnswer:
    def __init__(
        self,
        gate: ActivityGate,
        activities: ActivityRepository,
        progress: ProgressRepository,
        signer: AnswerSigner,
        clock: Clock,
    ) -> None:
        self._gate = gate
        self._activities = activities
        self._progress = progress
        self._store = ProgressStore(progress)
        self._signer = signer
        self._clock = clock

    async def execute(
        self, principal: Principal, activity_id: UUID, answer: str
    ) -> SubmissionResult:
        opened = await self._gate.open(principal, activity_id)
        expected = await self._activities.get_answer_digest(activity_id)
        correct = self._signer.matches(expected, activity_id, answer_key(answer))
        before = await self._progress.lock(activity_id, opened.team_id)
        resolution = submit(before, correct, opened.activity.points, self._clock.now())
        progress = await self._store.commit(
            activity_id, opened.team_id, before, resolution.progress, resolution.outcome
        )
        return SubmissionResult(resolution.outcome, progress)


class ChooseAfterFailure:
    def __init__(self, gate: ActivityGate, progress: ProgressRepository, clock: Clock) -> None:
        self._gate = gate
        self._store = ProgressStore(progress)
        self._clock = clock

    async def execute(self, principal: Principal, activity_id: UUID, choice: Choice) -> Progress:
        opened = await self._gate.open(principal, activity_id)
        return await self._store.apply(
            activity_id, opened.team_id, lambda before: choose(before, choice, self._clock.now())
        )


class ReadProgress:
    def __init__(
        self, activities: ActivityRepository, progress: ProgressRepository, clock: Clock
    ) -> None:
        self._activities = activities
        self._store = ProgressStore(progress)
        self._clock = clock

    async def execute(self, principal: Principal, activity_id: UUID) -> Progress:
        team_id = principal.member_team_id()
        if await self._activities.get(activity_id) is None:
            raise AppError.not_found("Activité")
        return await self._store.apply(
            activity_id, team_id, lambda before: settle(before, self._clock.now())
        )
