from uuid import UUID

import asyncpg

from app.modules.teams.domain.models import Member, Team
from app.shared.database import PostgresRepository
from app.shared.db_errors import translated_database_errors
from app.shared.errors import AppError


def _to_team(row: asyncpg.Record) -> Team:
    return Team(id=row["id"], chef_id=row["chef_id"], name=row["name"])


def _to_member(row: asyncpg.Record) -> Member:
    return Member(id=row["id"], team_id=row["team_id"], pseudonym=row["pseudonym"])


class PostgresTeamRepository(PostgresRepository):
    async def add(self, chef_id: UUID, name: str) -> Team:
        with translated_database_errors():
            row = await self._connection.fetchrow(
                "INSERT INTO teams (chef_id, name) VALUES ($1, $2) RETURNING id, chef_id, name",
                chef_id,
                name,
            )
        if row is None:
            raise AppError.invalid_state("Création de l'équipe impossible")
        return _to_team(row)

    async def get(self, team_id: UUID) -> Team | None:
        row = await self._connection.fetchrow(
            "SELECT id, chef_id, name FROM teams WHERE id = $1", team_id
        )
        return _to_team(row) if row is not None else None

    async def list_visible(self) -> list[Team]:
        rows = await self._connection.fetch(
            "SELECT id, chef_id, name FROM teams ORDER BY created_at"
        )
        return [_to_team(row) for row in rows]

    async def count_for_chef(self, chef_id: UUID) -> int:
        count = await self._connection.fetchval(
            "SELECT count(*) FROM teams WHERE chef_id = $1", chef_id
        )
        return int(count)


class PostgresMemberRepository(PostgresRepository):
    async def add(self, team_id: UUID, pseudonym: str, login: str, password_hash: str) -> Member:
        with translated_database_errors():
            row = await self._connection.fetchrow(
                "INSERT INTO members (team_id, pseudonym, login, password_hash) "
                "VALUES ($1, $2, $3, $4) RETURNING id, team_id, pseudonym",
                team_id,
                pseudonym,
                login,
                password_hash,
            )
        if row is None:
            raise AppError.invalid_state("Création du membre impossible")
        return _to_member(row)

    async def list_by_team(self, team_id: UUID) -> list[Member]:
        rows = await self._connection.fetch(
            "SELECT id, team_id, pseudonym FROM members WHERE team_id = $1 ORDER BY created_at",
            team_id,
        )
        return [_to_member(row) for row in rows]

    async def count_in_team(self, team_id: UUID) -> int:
        count = await self._connection.fetchval(
            "SELECT count(*) FROM members WHERE team_id = $1", team_id
        )
        return int(count)
