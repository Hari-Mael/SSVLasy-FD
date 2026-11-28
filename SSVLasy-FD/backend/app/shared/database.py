from collections.abc import AsyncIterator
from contextlib import asynccontextmanager
from typing import Final, Self

import asyncpg

from app.shared.principal import Principal

CONTEXT_SQL: Final = (
    "SELECT set_config('app.role', $1, true), set_config('app.user_id', $2, true), "
    "set_config('app.chef_id', $3, true), set_config('app.team_id', $4, true)"
)
SYSTEM_ROLE: Final = "system"


def _context(principal: Principal | None) -> tuple[str, str, str, str]:
    if principal is None:
        return (SYSTEM_ROLE, "", "", "")
    return (
        principal.role.value,
        str(principal.id),
        str(principal.chef_id or ""),
        str(principal.team_id or ""),
    )


class Database:
    def __init__(self, pool: asyncpg.Pool) -> None:
        self._pool = pool

    @classmethod
    async def connect(cls, dsn: str) -> Self:
        pool = await asyncpg.create_pool(dsn, min_size=2, max_size=20)
        return cls(pool)

    async def close(self) -> None:
        await self._pool.close()

    @asynccontextmanager
    async def transaction(self, principal: Principal | None) -> AsyncIterator[asyncpg.Connection]:
        async with self._pool.acquire() as connection, connection.transaction():
            await connection.execute(CONTEXT_SQL, *_context(principal))
            yield connection


class PostgresRepository:
    def __init__(self, connection: asyncpg.Connection) -> None:
        self._connection = connection
