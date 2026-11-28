from dataclasses import dataclass
from enum import StrEnum
from uuid import UUID

from app.shared.errors import AppError


class Role(StrEnum):
    SUPER_ADMIN = "super_admin"
    CHEF = "chef"
    MEMBER = "member"


@dataclass(frozen=True, slots=True)
class Principal:
    id: UUID
    role: Role
    chef_id: UUID | None = None
    team_id: UUID | None = None

    def member_team_id(self) -> UUID:
        if self.role is not Role.MEMBER or self.team_id is None:
            raise AppError.forbidden("Réservé aux membres d'une équipe")
        return self.team_id
