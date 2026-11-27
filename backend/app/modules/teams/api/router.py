from typing import Annotated
from uuid import UUID

from fastapi import APIRouter, Depends

from app.modules.teams.api.dependencies import add_member, create_team, list_members, list_teams
from app.modules.teams.api.schemas import (
    AddMemberRequest,
    CreateTeamRequest,
    EnrolledMemberResponse,
    MemberResponse,
    TeamResponse,
)
from app.modules.teams.application.use_cases import AddMember, CreateTeam, ListMembers, ListTeams
from app.shared.api.dependencies import ChefDep, requires
from app.shared.principal import Role

router = APIRouter(prefix="/api/v1/teams", tags=["teams"])


@router.post("", response_model=TeamResponse, status_code=201)
async def post_team(
    body: CreateTeamRequest, chef: ChefDep, use_case: Annotated[CreateTeam, Depends(create_team)]
) -> TeamResponse:
    return TeamResponse.from_domain(await use_case.execute(chef, body.name))


@router.get(
    "", response_model=list[TeamResponse], dependencies=[requires(Role.CHEF, Role.SUPER_ADMIN)]
)
async def get_teams(
    use_case: Annotated[ListTeams, Depends(list_teams)],
) -> list[TeamResponse]:
    return [TeamResponse.from_domain(team) for team in await use_case.execute()]


@router.post(
    "/{team_id}/members",
    response_model=EnrolledMemberResponse,
    status_code=201,
    dependencies=[requires(Role.CHEF, Role.SUPER_ADMIN)],
)
async def post_member(
    team_id: UUID,
    body: AddMemberRequest,
    use_case: Annotated[AddMember, Depends(add_member)],
) -> EnrolledMemberResponse:
    return EnrolledMemberResponse.from_enrollment(
        await use_case.execute(team_id, body.pseudonym, body.password)
    )


@router.get(
    "/{team_id}/members",
    response_model=list[MemberResponse],
    dependencies=[requires(Role.CHEF, Role.SUPER_ADMIN)],
)
async def get_members(
    team_id: UUID, use_case: Annotated[ListMembers, Depends(list_members)]
) -> list[MemberResponse]:
    return [MemberResponse.from_domain(member) for member in await use_case.execute(team_id)]
