from dataclasses import dataclass
from typing import Protocol
from uuid import UUID

from app.modules.camp_life.domain.rules import CampState, CampType, Resource, Weather


@dataclass(frozen=True, slots=True)
class CampCampaign:
    id: UUID
    owner_id: UUID
    days: int
    current_day: int
    camp_type: CampType
    weather: Weather
    running: bool


class CampRepository(Protocol):
    async def campaign(self, campaign_id: UUID) -> CampCampaign | None: ...

    async def running_campaign_of(self, owner_id: UUID) -> CampCampaign | None: ...

    async def set_day(self, campaign_id: UUID, day: int, weather: Weather) -> None: ...

    async def set_weather(self, campaign_id: UUID, weather: Weather) -> None: ...

    async def set_camp_type(self, campaign_id: UUID, camp_type: CampType) -> None: ...

    async def team_ids_of_owner(self, owner_id: UUID) -> list[UUID]: ...

    async def team_owner(self, team_id: UUID) -> UUID | None: ...

    async def state(self, team_id: UUID) -> CampState: ...

    async def save_state(self, team_id: UUID, state: CampState) -> None: ...

    async def actions_today(self, team_id: UUID, campaign_id: UUID, day: int) -> set[Resource]: ...

    async def add_action(
        self, team_id: UUID, campaign_id: UUID, day: int, resource: Resource
    ) -> None: ...

    async def apply_energy(self, team_id: UUID, delta: int) -> None: ...
