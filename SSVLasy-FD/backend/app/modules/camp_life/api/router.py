from typing import Annotated
from uuid import UUID

from fastapi import APIRouter, Depends
from pydantic import BaseModel

from app.modules.camp_life.api.dependencies import (
    advance_day,
    collect_resource,
    read_my_camp,
    set_camp_type,
    set_weather,
    unblock_team,
)
from app.modules.camp_life.application.use_cases import (
    AdvanceDay,
    CampView,
    CollectResource,
    ReadMyCamp,
    SetCampType,
    SetWeather,
    UnblockTeam,
)
from app.modules.camp_life.domain.rules import CampType, Resource, TeamStatus, Weather
from app.shared.api.dependencies import MemberDep, PrincipalDep, requires
from app.shared.principal import Role

router = APIRouter(prefix="/api/v1/camp", tags=["vie du camp"])


class CollectRequest(BaseModel):
    resource: Resource


class CollectResponse(BaseModel):
    gained: int


class CampViewResponse(BaseModel):
    campaign_id: UUID
    day: int
    weather: Weather
    camp_type: CampType
    wood: int
    water: int
    food: int
    status: TeamStatus
    actions_done: list[Resource]

    @classmethod
    def from_view(cls, view: CampView) -> "CampViewResponse":
        return cls(
            campaign_id=view.campaign_id,
            day=view.day,
            weather=view.weather,
            camp_type=view.camp_type,
            wood=view.wood,
            water=view.water,
            food=view.food,
            status=view.status,
            actions_done=view.actions_done,
        )


class WeatherRequest(BaseModel):
    weather: Weather


class CampTypeRequest(BaseModel):
    camp_type: CampType


class NextDayResponse(BaseModel):
    day: int
    weather: Weather


@router.post("/collect", response_model=CollectResponse)
async def post_collect(
    body: CollectRequest,
    member: MemberDep,
    use_case: Annotated[CollectResource, Depends(collect_resource)],
) -> CollectResponse:
    return CollectResponse(gained=await use_case.execute(member, body.resource))


@router.get("/me", response_model=CampViewResponse)
async def get_my_camp(
    member: MemberDep,
    use_case: Annotated[ReadMyCamp, Depends(read_my_camp)],
) -> CampViewResponse:
    return CampViewResponse.from_view(await use_case.execute(member))


@router.post(
    "/campaigns/{campaign_id}/next-day",
    response_model=NextDayResponse,
    dependencies=[requires(Role.CHEF, Role.SUPER_ADMIN)],
)
async def post_next_day(
    campaign_id: UUID,
    principal: PrincipalDep,
    use_case: Annotated[AdvanceDay, Depends(advance_day)],
) -> NextDayResponse:
    day, weather = await use_case.execute(principal, campaign_id)
    return NextDayResponse(day=day, weather=weather)


@router.put(
    "/campaigns/{campaign_id}/weather",
    dependencies=[requires(Role.CHEF, Role.SUPER_ADMIN)],
)
async def put_weather(
    campaign_id: UUID,
    body: WeatherRequest,
    principal: PrincipalDep,
    use_case: Annotated[SetWeather, Depends(set_weather)],
) -> dict[str, str]:
    await use_case.execute(principal, campaign_id, body.weather)
    return {"status": "ok"}


@router.put(
    "/campaigns/{campaign_id}/camp-type",
    dependencies=[requires(Role.CHEF, Role.SUPER_ADMIN)],
)
async def put_camp_type(
    campaign_id: UUID,
    body: CampTypeRequest,
    principal: PrincipalDep,
    use_case: Annotated[SetCampType, Depends(set_camp_type)],
) -> dict[str, str]:
    await use_case.execute(principal, campaign_id, body.camp_type)
    return {"status": "ok"}


@router.post(
    "/teams/{team_id}/unblock",
    dependencies=[requires(Role.CHEF, Role.SUPER_ADMIN)],
)
async def post_unblock(
    team_id: UUID,
    principal: PrincipalDep,
    use_case: Annotated[UnblockTeam, Depends(unblock_team)],
) -> dict[str, str]:
    await use_case.execute(principal, team_id)
    return {"status": "ok"}
