from typing import Literal
from uuid import UUID

from pydantic import BaseModel, EmailStr, Field

from app.modules.access.domain.models import MIN_PASSWORD_LENGTH, StaffUser
from app.shared.principal import Role


class StaffLoginRequest(BaseModel):
    email: EmailStr
    password: str = Field(min_length=1, max_length=256)


class MemberLoginRequest(BaseModel):
    login: str = Field(min_length=1, max_length=40)
    password: str = Field(min_length=1, max_length=256)


class TokenResponse(BaseModel):
    access_token: str
    token_type: Literal["bearer"] = "bearer"
    role: Role


class CreateStaffUserRequest(BaseModel):
    email: EmailStr
    display_name: str = Field(min_length=1, max_length=80)
    password: str = Field(min_length=MIN_PASSWORD_LENGTH, max_length=256)
    role: Literal[Role.SUPER_ADMIN, Role.CHEF]


class StaffUserResponse(BaseModel):
    id: UUID
    email: str
    display_name: str
    role: Role
    active: bool

    @classmethod
    def from_domain(cls, user: StaffUser) -> "StaffUserResponse":
        return cls(
            id=user.id,
            email=user.email,
            display_name=user.display_name,
            role=user.role,
            active=user.active,
        )
