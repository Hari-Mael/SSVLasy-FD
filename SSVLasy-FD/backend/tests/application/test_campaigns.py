from datetime import date
from uuid import uuid4

import pytest

from app.modules.campaigns.application.use_cases import CreateCampaign, TransitionCampaign
from app.modules.campaigns.domain.models import Campaign
from app.modules.campaigns.domain.state import CampaignState
from app.shared.errors import AppError
from app.shared.principal import Principal, Role
from tests.application.fakes import FakeCampaignRepository, FakeCensus, FixedClock, utc

CHEF = Principal(id=uuid4(), role=Role.CHEF)
ZONE = "Indian/Antananarivo"


def draft(start: date) -> Campaign:
    return Campaign(uuid4(), CHEF.id, "Camp", start, 5, ZONE, CampaignState.DRAFT)


async def test_campaign_length_is_bounded() -> None:
    use_case = CreateCampaign(FakeCampaignRepository(), ZONE)
    with pytest.raises(AppError):
        await use_case.execute(CHEF, "Camp", date(2026, 12, 1), 0, None)
    with pytest.raises(AppError):
        await use_case.execute(CHEF, "Camp", date(2026, 12, 1), 12, None)
    await use_case.execute(CHEF, "Camp", date(2026, 12, 1), 1, None)
    campaign = await use_case.execute(CHEF, "Camp", date(2026, 12, 1), 11, None)
    assert campaign.timezone == ZONE


async def test_unknown_timezone_is_refused() -> None:
    use_case = CreateCampaign(FakeCampaignRepository(), ZONE)
    with pytest.raises(AppError):
        await use_case.execute(CHEF, "Camp", date(2026, 12, 1), 5, "Mars/Olympus")


async def test_scheduling_requires_enough_active_chefs() -> None:
    repository = FakeCampaignRepository(draft(date(2026, 12, 1)))
    clock = FixedClock(utc(2026, 11, 25, 9))
    with pytest.raises(AppError):
        await TransitionCampaign(repository, FakeCensus(5), clock).execute(
            repository.campaign.id if repository.campaign else uuid4(), CampaignState.SCHEDULED
        )
    scheduled = await TransitionCampaign(repository, FakeCensus(6), clock).execute(
        repository.campaign.id if repository.campaign else uuid4(), CampaignState.SCHEDULED
    )
    assert scheduled.state is CampaignState.SCHEDULED


async def test_scheduling_refuses_a_start_date_in_the_past() -> None:
    campaign = draft(date(2026, 11, 20))
    repository = FakeCampaignRepository(campaign)
    clock = FixedClock(utc(2026, 11, 25, 9))
    with pytest.raises(AppError):
        await TransitionCampaign(repository, FakeCensus(10), clock).execute(
            campaign.id, CampaignState.SCHEDULED
        )


async def test_invalid_transition_is_refused_before_any_check() -> None:
    campaign = draft(date(2026, 12, 1))
    repository = FakeCampaignRepository(campaign)
    clock = FixedClock(utc(2026, 11, 25, 9))
    with pytest.raises(AppError):
        await TransitionCampaign(repository, FakeCensus(10), clock).execute(
            campaign.id, CampaignState.RUNNING
        )


async def test_unknown_campaign_is_reported() -> None:
    repository = FakeCampaignRepository(None)
    clock = FixedClock(utc(2026, 11, 25, 9))
    with pytest.raises(AppError):
        await TransitionCampaign(repository, FakeCensus(10), clock).execute(
            uuid4(), CampaignState.SCHEDULED
        )
