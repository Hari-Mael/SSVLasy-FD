from dataclasses import dataclass, field
from datetime import UTC, datetime
from uuid import UUID, uuid4

import pytest

from app.modules.camp_life.application.ports import CampCampaign
from app.modules.camp_life.application.use_cases import (
    AdvanceDay,
    CollectResource,
    UnblockTeam,
)
from app.modules.camp_life.domain.rules import CampState, CampType, Resource, Stocks, Weather
from app.shared.errors import AppError, ErrorCode
from app.shared.principal import Principal, Role

NOW = datetime(2026, 12, 1, 18, 0, tzinfo=UTC)


class FixedClock:
    def now(self) -> datetime:
        return NOW


@dataclass
class FakeCampRepository:
    campaigns: dict[UUID, CampCampaign]
    teams: dict[UUID, UUID]
    states: dict[UUID, CampState] = field(default_factory=dict)
    actions: set[tuple[UUID, int, Resource]] = field(default_factory=set)
    energy: dict[UUID, int] = field(default_factory=dict)

    async def campaign(self, campaign_id: UUID) -> CampCampaign | None:
        return self.campaigns.get(campaign_id)

    async def running_campaign_of(self, owner_id: UUID) -> CampCampaign | None:
        return next(
            (c for c in self.campaigns.values() if c.owner_id == owner_id and c.running), None
        )

    async def set_day(self, campaign_id: UUID, day: int, weather: Weather) -> None:
        self.campaigns[campaign_id] = _replace(
            self.campaigns[campaign_id], current_day=day, weather=weather
        )

    async def set_weather(self, campaign_id: UUID, weather: Weather) -> None:
        self.campaigns[campaign_id] = _replace(self.campaigns[campaign_id], weather=weather)

    async def set_camp_type(self, campaign_id: UUID, camp_type: CampType) -> None:
        self.campaigns[campaign_id] = _replace(self.campaigns[campaign_id], camp_type=camp_type)

    async def team_ids_of_owner(self, owner_id: UUID) -> list[UUID]:
        return [team for team, owner in self.teams.items() if owner == owner_id]

    async def team_owner(self, team_id: UUID) -> UUID | None:
        return self.teams.get(team_id)

    async def state(self, team_id: UUID) -> CampState:
        return self.states.get(team_id, CampState())

    async def save_state(self, team_id: UUID, state: CampState) -> None:
        self.states[team_id] = state

    async def actions_today(self, team_id: UUID, campaign_id: UUID, day: int) -> set[Resource]:
        return {r for (t, d, r) in self.actions if t == team_id and d == day}

    async def add_action(
        self, team_id: UUID, campaign_id: UUID, day: int, resource: Resource
    ) -> None:
        self.actions.add((team_id, day, resource))

    async def apply_energy(self, team_id: UUID, delta: int) -> None:
        self.energy[team_id] = self.energy.get(team_id, 100) + delta


def _replace(campaign: CampCampaign, **changes: object) -> CampCampaign:
    from dataclasses import replace

    return replace(campaign, **changes)


def setup() -> tuple[FakeCampRepository, UUID, UUID, UUID]:
    chef, team, campaign_id = uuid4(), uuid4(), uuid4()
    campaign = CampCampaign(campaign_id, chef, 3, 1, CampType.TSELATRA, Weather.CHALEUR, True)
    return FakeCampRepository({campaign_id: campaign}, {team: chef}), chef, team, campaign_id


async def test_each_collection_is_allowed_once_per_day() -> None:
    repository, _, team, _ = setup()
    member = Principal(id=uuid4(), role=Role.MEMBER, chef_id=uuid4(), team_id=team)
    use_case = CollectResource(repository, FixedClock())
    assert await use_case.execute(member, Resource.WOOD) == 45
    with pytest.raises(AppError):
        await use_case.execute(member, Resource.WOOD)
    assert await use_case.execute(member, Resource.WATER) == 30


async def test_blocked_team_cannot_collect() -> None:
    repository, _, team, _ = setup()
    repository.states[team] = CampState(blocked_since=NOW)
    member = Principal(id=uuid4(), role=Role.MEMBER, chef_id=uuid4(), team_id=team)
    with pytest.raises(AppError):
        await CollectResource(repository, FixedClock()).execute(member, Resource.FOOD)


async def test_next_day_runs_meals_and_changes_weather() -> None:
    repository, chef, team, campaign_id = setup()
    repository.states[team] = CampState(Stocks(wood=45, water=30, food=15))
    principal = Principal(id=chef, role=Role.CHEF)
    day, weather = await AdvanceDay(repository, FixedClock(), lambda: Weather.PLUVIEUX).execute(
        principal, campaign_id
    )
    assert (day, weather) == (2, Weather.PLUVIEUX)
    assert repository.states[team].stocks == Stocks(wood=15, water=0, food=0)
    assert repository.energy[team] == 100 + 3 * 20


async def test_only_the_owner_or_super_admin_can_advance_the_day() -> None:
    repository, _, _, campaign_id = setup()
    stranger = Principal(id=uuid4(), role=Role.CHEF)
    with pytest.raises(AppError) as refused:
        await AdvanceDay(repository, FixedClock(), lambda: Weather.CHALEUR).execute(
            stranger, campaign_id
        )
    assert refused.value.code is ErrorCode.FORBIDDEN


async def test_only_the_team_chef_or_super_admin_can_unblock() -> None:
    repository, chef, team, _ = setup()
    repository.states[team] = CampState(blocked_since=NOW)
    other = Principal(id=uuid4(), role=Role.CHEF)
    with pytest.raises(AppError):
        await UnblockTeam(repository).execute(other, team)
    await UnblockTeam(repository).execute(Principal(id=chef, role=Role.CHEF), team)
    assert repository.states[team].blocked_since is None
