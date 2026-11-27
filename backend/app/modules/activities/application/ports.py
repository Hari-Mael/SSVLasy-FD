from datetime import datetime
from typing import Protocol
from uuid import UUID

from app.modules.activities.domain.activity import (
    Activity,
    ActivityDraft,
    ActivityStatus,
    CampaignSchedule,
)
from app.modules.activities.domain.progress import AttemptOutcome, Progress


class ActivityRepository(Protocol):
    async def add(self, campaign_id: UUID, draft: ActivityDraft) -> Activity: ...

    async def get(self, activity_id: UUID) -> Activity | None: ...

    async def list_for_campaign(self, campaign_id: UUID) -> list[Activity]: ...

    async def set_status(self, activity_id: UUID, status: ActivityStatus) -> Activity: ...

    async def get_answer_digest(self, activity_id: UUID) -> str: ...

    async def get_solution_sealed(self, activity_id: UUID) -> bytes: ...


class TeamCampGate(Protocol):
    async def blocking_reason(self, team_id: UUID, now: datetime) -> str | None: ...


class ScheduleRepository(Protocol):
    async def get(self, campaign_id: UUID) -> CampaignSchedule | None: ...


class ProgressRepository(Protocol):
    async def lock(self, activity_id: UUID, team_id: UUID) -> Progress: ...

    async def save(self, activity_id: UUID, team_id: UUID, progress: Progress) -> None: ...

    async def log(
        self, activity_id: UUID, team_id: UUID, number: int, outcome: AttemptOutcome
    ) -> None: ...

    async def count_unsolved_daytime(self, campaign_id: UUID, day: int, team_id: UUID) -> int: ...

    async def list_for_team(self, campaign_id: UUID, team_id: UUID) -> dict[UUID, Progress]: ...
