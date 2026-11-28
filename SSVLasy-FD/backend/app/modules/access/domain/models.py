from dataclasses import dataclass
from typing import Final
from uuid import UUID

from app.shared.principal import Role

MIN_PASSWORD_LENGTH: Final = 12


@dataclass(frozen=True, slots=True)
class StaffUser:
    id: UUID
    email: str
    display_name: str
    role: Role
    active: bool
    password_hash: str


@dataclass(frozen=True, slots=True)
class NewStaffUser:
    email: str
    display_name: str
    password_hash: str
    role: Role


@dataclass(frozen=True, slots=True)
class MemberIdentity:
    member_id: UUID
    team_id: UUID
    chef_id: UUID
    password_hash: str | None = None
