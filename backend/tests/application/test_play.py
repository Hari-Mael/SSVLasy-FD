from datetime import date, time
from uuid import uuid4

import pytest

from app.modules.activities.application.play import (
    ActivityGate,
    ChooseAfterFailure,
    StartAttempt,
    SubmitAnswer,
)
from app.modules.activities.domain.activity import Activity, ActivityStatus, CampaignSchedule
from app.modules.activities.domain.ciphers import CipherKind, CipherSpec
from app.modules.activities.domain.progress import AttemptOutcome, Choice, ProgressStatus
from app.modules.campaigns.domain.state import CampaignState
from app.shared.errors import AppError, ErrorCode
from app.shared.principal import Principal, Role
from app.shared.security import AnswerSigner
from tests.application.fakes import (
    FakeActivityRepository,
    FakeProgressRepository,
    FakeScheduleRepository,
    FixedClock,
    utc,
)

TEAM_ID = uuid4()
MEMBER = Principal(id=uuid4(), role=Role.MEMBER, chef_id=uuid4(), team_id=TEAM_ID)
SIGNER = AnswerSigner("test-secret")
ZONE = "Indian/Antananarivo"


def build_activity(
    status: ActivityStatus = ActivityStatus.UNLOCKED,
    slot_start: time = time(9, 0),
    slot_end: time = time(10, 0),
) -> Activity:
    return Activity(
        id=uuid4(),
        campaign_id=uuid4(),
        title="Message du feu",
        day=1,
        slot_start=slot_start,
        slot_end=slot_end,
        points=20,
        cipher=CipherSpec(CipherKind.MORSE),
        encoded_text="... --- ...",
        status=status,
    )


class NoBlockingGate:
    async def blocking_reason(self, team_id, now):  # type: ignore[no-untyped-def]
        return None


class Scenario:
    def __init__(
        self,
        activity: Activity,
        moment_hour: int = 6,
        moment_minute: int = 30,
        state: CampaignState = CampaignState.RUNNING,
        unsolved: int = 0,
    ) -> None:
        self.activity = activity
        self.clock = FixedClock(utc(2026, 12, 1, moment_hour, moment_minute))
        self.progress = FakeProgressRepository(unsolved)
        activities = FakeActivityRepository(activity, SIGNER.digest(activity.id, "SOS"))
        schedules = FakeScheduleRepository(
            CampaignSchedule(uuid4(), state, date(2026, 12, 1), 5, ZONE)
        )
        self.gate = ActivityGate(activities, schedules, self.clock, NoBlockingGate())
        self.start = StartAttempt(self.gate, self.progress, self.clock)
        self.submit = SubmitAnswer(self.gate, activities, self.progress, SIGNER, self.clock)
        self.choose = ChooseAfterFailure(self.gate, self.progress, self.clock)


async def test_team_solves_with_a_noisy_but_correct_answer() -> None:
    scenario = Scenario(build_activity())
    await scenario.start.execute(MEMBER, scenario.activity.id)
    result = await scenario.submit.execute(MEMBER, scenario.activity.id, " s.o.s ")
    assert result.outcome is AttemptOutcome.CORRECT
    assert result.progress.awarded_points == 20
    assert scenario.progress.logged == [(1, AttemptOutcome.CORRECT)]


async def test_wrong_answer_starts_a_wait_and_is_logged() -> None:
    scenario = Scenario(build_activity())
    await scenario.start.execute(MEMBER, scenario.activity.id)
    result = await scenario.submit.execute(MEMBER, scenario.activity.id, "FEU")
    assert result.outcome is AttemptOutcome.WRONG
    assert result.progress.attempts_used == 1
    assert result.progress.deadline_at is not None
    assert scenario.progress.logged == [(1, AttemptOutcome.WRONG)]
    with pytest.raises(AppError):
        await scenario.start.execute(MEMBER, scenario.activity.id)


async def test_refaire_after_one_failure_allows_one_retry_without_wait() -> None:
    scenario = Scenario(build_activity())
    await scenario.start.execute(MEMBER, scenario.activity.id)
    await scenario.submit.execute(MEMBER, scenario.activity.id, "FEU")
    progress = await scenario.choose.execute(MEMBER, scenario.activity.id, Choice.RETRY)
    assert progress.status is ProgressStatus.FREE_RETRY
    assert progress.deadline_at is None


async def test_activity_outside_its_slot_is_refused() -> None:
    scenario = Scenario(build_activity(), moment_hour=9)
    with pytest.raises(AppError) as error:
        await scenario.start.execute(MEMBER, scenario.activity.id)
    assert error.value.code is ErrorCode.INVALID_STATE


async def test_locked_activity_is_refused() -> None:
    scenario = Scenario(build_activity(status=ActivityStatus.PUBLISHED))
    with pytest.raises(AppError):
        await scenario.start.execute(MEMBER, scenario.activity.id)


async def test_paused_campaign_blocks_play() -> None:
    scenario = Scenario(build_activity(), state=CampaignState.PAUSED)
    with pytest.raises(AppError):
        await scenario.start.execute(MEMBER, scenario.activity.id)


async def test_staff_cannot_play() -> None:
    scenario = Scenario(build_activity())
    chef = Principal(id=uuid4(), role=Role.CHEF)
    with pytest.raises(AppError) as error:
        await scenario.start.execute(chef, scenario.activity.id)
    assert error.value.code is ErrorCode.FORBIDDEN


async def test_night_activity_is_closed_to_teams_with_unsolved_daytime_tasks() -> None:
    night = build_activity(slot_start=time(21, 0), slot_end=time(22, 0))
    blocked = Scenario(night, moment_hour=18, moment_minute=30, unsolved=1)
    with pytest.raises(AppError) as error:
        await blocked.start.execute(MEMBER, night.id)
    assert error.value.code is ErrorCode.FORBIDDEN
    open_scenario = Scenario(night, moment_hour=18, moment_minute=30, unsolved=0)
    progress = await open_scenario.start.execute(MEMBER, night.id)
    assert progress.status is ProgressStatus.OPEN
    assert progress.attempts_used == 0
