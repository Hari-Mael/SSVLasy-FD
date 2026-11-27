from datetime import date
from uuid import UUID

from pydantic import BaseModel, Field

from app.modules.campaigns.domain.models import Campaign
from app.modules.campaigns.domain.state import CampaignState
from app.shared.quotas import MAX_CAMPAIGN_DAYS, MIN_CAMPAIGN_DAYS


class CreateCampaignRequest(BaseModel):
    name: str = Field(min_length=1, max_length=100)
    start_date: date
    days: int = Field(ge=MIN_CAMPAIGN_DAYS, le=MAX_CAMPAIGN_DAYS)
    timezone: str | None = Field(default=None, max_length=64)


class TransitionRequest(BaseModel):
    target: CampaignState


class CampaignResponse(BaseModel):
    id: UUID
    owner_id: UUID
    name: str
    start_date: date
    days: int
    timezone: str
    state: CampaignState

    @classmethod
    def from_domain(cls, campaign: Campaign) -> "CampaignResponse":
        return cls(
            id=campaign.id,
            owner_id=campaign.owner_id,
            name=campaign.name,
            start_date=campaign.start_date,
            days=campaign.days,
            timezone=campaign.timezone,
            state=campaign.state,
        )
