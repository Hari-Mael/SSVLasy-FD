from typing import Annotated

from fastapi import APIRouter, Depends

from app.modules.access.api.dependencies import (
    authenticate_member,
    authenticate_staff,
    create_staff_user,
)
from app.modules.access.api.schemas import (
    CreateStaffUserRequest,
    MemberLoginRequest,
    StaffLoginRequest,
    StaffUserResponse,
    TokenResponse,
)
from app.modules.access.application.use_cases import (
    AuthenticateMember,
    AuthenticateStaff,
    CreateStaffUser,
)
from app.shared.api.dependencies import requires
from app.shared.principal import Role

router = APIRouter(prefix="/api/v1", tags=["access"])


@router.post("/auth/staff-login", response_model=TokenResponse)
async def staff_login(
    body: StaffLoginRequest, use_case: Annotated[AuthenticateStaff, Depends(authenticate_staff)]
) -> TokenResponse:
    token = await use_case.execute(body.email, body.password)
    return TokenResponse(access_token=token.value, role=token.role)


@router.post("/auth/member-login", response_model=TokenResponse)
async def member_login(
    body: MemberLoginRequest, use_case: Annotated[AuthenticateMember, Depends(authenticate_member)]
) -> TokenResponse:
    token = await use_case.execute(body.login, body.password)
    return TokenResponse(access_token=token.value, role=token.role)


@router.post(
    "/staff-users",
    response_model=StaffUserResponse,
    status_code=201,
    dependencies=[requires(Role.SUPER_ADMIN)],
)
async def add_staff_user(
    body: CreateStaffUserRequest,
    use_case: Annotated[CreateStaffUser, Depends(create_staff_user)],
) -> StaffUserResponse:
    user = await use_case.execute(body.email, body.display_name, body.password, body.role)
    return StaffUserResponse.from_domain(user)
