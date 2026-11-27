from typing import Protocol
from uuid import UUID

from app.modules.teams.domain.models import Member, Team


class TeamRepository(Protocol):
    async def add(self, chef_id: UUID, name: str) -> Team: ...

    async def get(self, team_id: UUID) -> Team | None: ...

    async def list_visible(self) -> list[Team]: ...

    async def count_for_chef(self, chef_id: UUID) -> int: ...


class MemberRepository(Protocol):
    async def add(
        self, team_id: UUID, pseudonym: str, login: str, password_hash: str
    ) -> Member: ...

    async def list_by_team(self, team_id: UUID) -> list[Member]: ...

    async def count_in_team(self, team_id: UUID) -> int: ...
