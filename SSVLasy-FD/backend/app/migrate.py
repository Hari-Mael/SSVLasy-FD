import asyncio
from pathlib import Path

import asyncpg

from app.shared.config import get_settings

MIGRATIONS_DIRECTORY = Path(__file__).resolve().parent.parent / "migrations"
BOOKKEEPING_SQL = (
    "CREATE TABLE IF NOT EXISTS schema_migrations ("
    "name text PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now())"
)


async def run(dsn: str, directory: Path) -> list[str]:
    connection = await asyncpg.connect(dsn)
    try:
        await connection.execute(BOOKKEEPING_SQL)
        rows = await connection.fetch("SELECT name FROM schema_migrations")
        applied = {row["name"] for row in rows}
        executed: list[str] = []
        for path in sorted(directory.glob("*.sql")):
            if path.name in applied:
                continue
            async with connection.transaction():
                await connection.execute(path.read_text(encoding="utf-8"))
                await connection.execute(
                    "INSERT INTO schema_migrations (name) VALUES ($1)", path.name
                )
            executed.append(path.name)
        return executed
    finally:
        await connection.close()


def main() -> None:
    settings = get_settings()
    dsn = settings.migration_database_url or settings.database_url
    executed = asyncio.run(run(dsn, MIGRATIONS_DIRECTORY))
    for name in executed:
        print(name)


if __name__ == "__main__":
    main()
