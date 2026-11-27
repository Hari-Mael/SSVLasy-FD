from datetime import date
from typing import Protocol
from uuid import UUID

from app.modules.campaigns.domain.models import Campaign
from app.modules.campaigns.domain.state import CampaignState


class CampaignRepository(Protocol):
    async def add(
        self, owner_id: UUID, name: str, start_date: date, days: int, timezone: str
    ) -> Campaign: ...

    async def get(self, campaign_id: UUID) -> Campaign | None: ...

    async def list_visible(self) -> list[Campaign]: ...

    async def update_state(self, campaign_id: UUID, state: CampaignState) -> Campaign: ...


class StaffCensus(Protocol):
    async def count_active_chefs(self) -> int: ...
