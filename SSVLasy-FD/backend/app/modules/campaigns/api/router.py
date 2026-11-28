from typing import Annotated
from uuid import UUID

from fastapi import APIRouter, Depends

from app.modules.campaigns.api.dependencies import (
    create_campaign,
    list_campaigns,
    transition_campaign,
)
from app.modules.campaigns.api.schemas import (
    CampaignResponse,
    CreateCampaignRequest,
    TransitionRequest,
)
from app.modules.campaigns.application.use_cases import (
    CreateCampaign,
    ListCampaigns,
    TransitionCampaign,
)
from app.shared.api.dependencies import ChefDep, PrincipalDep, requires
from app.shared.principal import Role

router = APIRouter(prefix="/api/v1/campaigns", tags=["campaigns"])


@router.post("", response_model=CampaignResponse, status_code=201)
async def post_campaign(
    body: CreateCampaignRequest,
    chef: ChefDep,
    use_case: Annotated[CreateCampaign, Depends(create_campaign)],
) -> CampaignResponse:
    campaign = await use_case.execute(chef, body.name, body.start_date, body.days, body.timezone)
    return CampaignResponse.from_domain(campaign)


@router.get("", response_model=list[CampaignResponse])
async def get_campaigns(
    _principal: PrincipalDep, use_case: Annotated[ListCampaigns, Depends(list_campaigns)]
) -> list[CampaignResponse]:
    return [CampaignResponse.from_domain(item) for item in await use_case.execute()]


@router.post(
    "/{campaign_id}/transitions",
    response_model=CampaignResponse,
    dependencies=[requires(Role.CHEF, Role.SUPER_ADMIN)],
)
async def post_transition(
    campaign_id: UUID,
    body: TransitionRequest,
    use_case: Annotated[TransitionCampaign, Depends(transition_campaign)],
) -> CampaignResponse:
    return CampaignResponse.from_domain(await use_case.execute(campaign_id, body.target))
