from dataclasses import replace
from datetime import UTC, date, datetime
from uuid import UUID, uuid4

from app.modules.activities.domain.activity import Activity, CampaignSchedule
from app.modules.activities.domain.progress import AttemptOutcome, Progress
from app.modules.campaigns.domain.models import Campaign
from app.modules.campaigns.domain.state import CampaignState
from app.modules.teams.domain.models import Member, Team


class FixedClock:
    def __init__(self, moment: datetime) -> None:
        self.moment = moment

    def now(self) -> datetime:
        return self.moment


def utc(year: int, month: int, day: int, hour: int, minute: int = 0) -> datetime:
    return datetime(year, month, day, hour, minute, tzinfo=UTC)


class FakeTeamRepository:
    def __init__(self, teams: list[Team] | None = None) -> None:
        self.teams = teams or []

    async def add(self, chef_id: UUID, name: str) -> Team:
        team = Team(uuid4(), chef_id, name)
        self.teams.append(team)
        return team

    async def get(self, team_id: UUID) -> Team | None:
        return next((team for team in self.teams if team.id == team_id), None)

    async def list_visible(self) -> list[Team]:
        return list(self.teams)

    async def count_for_chef(self, chef_id: UUID) -> int:
        return sum(1 for team in self.teams if team.chef_id == chef_id)


class FakeMemberRepository:
    def __init__(self) -> None:
        self.members: list[Member] = []
        self.logins: list[str] = []
        self.password_hashes: list[str] = []

    async def add(self, team_id: UUID, pseudonym: str, login: str, password_hash: str) -> Member:
        member = Member(uuid4(), team_id, pseudonym)
        self.members.append(member)
        self.logins.append(login)
        self.password_hashes.append(password_hash)
        return member

    async def list_by_team(self, team_id: UUID) -> list[Member]:
        return [member for member in self.members if member.team_id == team_id]

    async def count_in_team(self, team_id: UUID) -> int:
        return len(await self.list_by_team(team_id))


class FakeCampaignRepository:
    def __init__(self, campaign: Campaign | None = None) -> None:
        self.campaign = campaign

    async def add(
        self, owner_id: UUID, name: str, start_date: date, days: int, timezone: str
    ) -> Campaign:
        self.campaign = Campaign(
            uuid4(), owner_id, name, start_date, days, timezone, CampaignState.DRAFT
        )
        return self.campaign

    async def get(self, campaign_id: UUID) -> Campaign | None:
        return self.campaign

    async def list_visible(self) -> list[Campaign]:
        return [self.campaign] if self.campaign else []

    async def update_state(self, campaign_id: UUID, state: CampaignState) -> Campaign:
        assert self.campaign is not None
        self.campaign = replace(self.campaign, state=state)
        return self.campaign


class FakeCensus:
    def __init__(self, chefs: int) -> None:
        self.chefs = chefs

    async def count_active_chefs(self) -> int:
        return self.chefs


class FakeActivityRepository:
    def __init__(self, activity: Activity, digest: str) -> None:
        self.activity = activity
        self.digest = digest

    async def get(self, activity_id: UUID) -> Activity | None:
        return self.activity if self.activity.id == activity_id else None

    async def get_answer_digest(self, activity_id: UUID) -> str:
        return self.digest


class FakeScheduleRepository:
    def __init__(self, schedule: CampaignSchedule) -> None:
        self.schedule = schedule

    async def get(self, campaign_id: UUID) -> CampaignSchedule | None:
        return self.schedule


class FakeProgressRepository:
    def __init__(self, unsolved: int = 0) -> None:
        self.rows: dict[tuple[UUID, UUID], Progress] = {}
        self.logged: list[tuple[int, AttemptOutcome]] = []
        self.unsolved = unsolved

    async def lock(self, activity_id: UUID, team_id: UUID) -> Progress:
        return self.rows.get((activity_id, team_id), Progress())

    async def save(self, activity_id: UUID, team_id: UUID, progress: Progress) -> None:
        self.rows[(activity_id, team_id)] = progress

    async def log(
        self, activity_id: UUID, team_id: UUID, number: int, outcome: AttemptOutcome
    ) -> None:
        self.logged.append((number, outcome))

    async def count_unsolved_daytime(self, campaign_id: UUID, day: int, team_id: UUID) -> int:
        return self.unsolved
