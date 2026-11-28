from dataclasses import dataclass
from datetime import date
from uuid import UUID

from app.modules.campaigns.domain.state import CampaignState


@dataclass(frozen=True, slots=True)
class Campaign:
    id: UUID
    owner_id: UUID
    name: str
    start_date: date
    days: int
    timezone: str
    state: CampaignState
