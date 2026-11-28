from datetime import date
from uuid import UUID
from zoneinfo import ZoneInfo, ZoneInfoNotFoundError

from app.modules.campaigns.application.ports import CampaignRepository, StaffCensus
from app.modules.campaigns.domain.models import Campaign
from app.modules.campaigns.domain.state import CampaignState, ensure_transition
from app.shared.clock import Clock
from app.shared.errors import AppError
from app.shared.principal import Principal
from app.shared.quotas import MAX_CAMPAIGN_DAYS, MIN_CAMPAIGN_DAYS, MIN_CHEFS_TO_SCHEDULE


class CreateCampaign:
    def __init__(self, campaigns: CampaignRepository, default_timezone: str) -> None:
        self._campaigns = campaigns
        self._default_timezone = default_timezone

    async def execute(
        self, chef: Principal, name: str, start_date: date, days: int, timezone: str | None
    ) -> Campaign:
        if not MIN_CAMPAIGN_DAYS <= days <= MAX_CAMPAIGN_DAYS:
            raise AppError.validation(
                f"Une campagne dure entre {MIN_CAMPAIGN_DAYS} et {MAX_CAMPAIGN_DAYS} jours"
            )
        zone_name = timezone or self._default_timezone
        try:
            ZoneInfo(zone_name)
        except ZoneInfoNotFoundError as error:
            raise AppError.validation("Fuseau horaire inconnu") from error
        return await self._campaigns.add(chef.id, name.strip(), start_date, days, zone_name)


class ListCampaigns:
    def __init__(self, campaigns: CampaignRepository) -> None:
        self._campaigns = campaigns

    async def execute(self) -> list[Campaign]:
        return await self._campaigns.list_visible()


class TransitionCampaign:
    def __init__(self, campaigns: CampaignRepository, census: StaffCensus, clock: Clock) -> None:
        self._campaigns = campaigns
        self._census = census
        self._clock = clock

    async def execute(self, campaign_id: UUID, target: CampaignState) -> Campaign:
        campaign = await self._campaigns.get(campaign_id)
        if campaign is None:
            raise AppError.not_found("Campagne")
        ensure_transition(campaign.state, target)
        if target is CampaignState.SCHEDULED:
            await self._ensure_schedulable(campaign)
        return await self._campaigns.update_state(campaign.id, target)

    async def _ensure_schedulable(self, campaign: Campaign) -> None:
        chefs = await self._census.count_active_chefs()
        if chefs < MIN_CHEFS_TO_SCHEDULE:
            raise AppError.invalid_state(
                f"Au moins {MIN_CHEFS_TO_SCHEDULE} chefs actifs sont requis pour planifier"
            )
        today = self._clock.now().astimezone(ZoneInfo(campaign.timezone)).date()
        if campaign.start_date < today:
            raise AppError.invalid_state("La date de début est déjà passée")
