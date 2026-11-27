from datetime import date
from uuid import UUID

import asyncpg

from app.modules.campaigns.domain.models import Campaign
from app.modules.campaigns.domain.state import CampaignState
from app.shared.database import Database, PostgresRepository
from app.shared.db_errors import translated_database_errors
from app.shared.errors import AppError

CAMPAIGN_COLUMNS = "id, owner_id, name, start_date, days, timezone, state"


def _to_campaign(row: asyncpg.Record) -> Campaign:
    return Campaign(
        id=row["id"],
        owner_id=row["owner_id"],
        name=row["name"],
        start_date=row["start_date"],
        days=row["days"],
        timezone=row["timezone"],
        state=CampaignState(row["state"]),
    )


class PostgresCampaignRepository(PostgresRepository):
    async def add(
        self, owner_id: UUID, name: str, start_date: date, days: int, timezone: str
    ) -> Campaign:
        with translated_database_errors():
            row = await self._connection.fetchrow(
                f"INSERT INTO campaigns (owner_id, name, start_date, days, timezone) "
                f"VALUES ($1, $2, $3, $4, $5) RETURNING {CAMPAIGN_COLUMNS}",
                owner_id,
                name,
                start_date,
                days,
                timezone,
            )
        if row is None:
            raise AppError.invalid_state("Création de la campagne impossible")
        return _to_campaign(row)

    async def get(self, campaign_id: UUID) -> Campaign | None:
        row = await self._connection.fetchrow(
            f"SELECT {CAMPAIGN_COLUMNS} FROM campaigns WHERE id = $1", campaign_id
        )
        return _to_campaign(row) if row is not None else None

    async def list_visible(self) -> list[Campaign]:
        rows = await self._connection.fetch(
            f"SELECT {CAMPAIGN_COLUMNS} FROM campaigns ORDER BY start_date DESC"
        )
        return [_to_campaign(row) for row in rows]

    async def update_state(self, campaign_id: UUID, state: CampaignState) -> Campaign:
        row = await self._connection.fetchrow(
            f"UPDATE campaigns SET state = $2 WHERE id = $1 RETURNING {CAMPAIGN_COLUMNS}",
            campaign_id,
            state.value,
        )
        if row is None:
            raise AppError.not_found("Campagne")
        return _to_campaign(row)


class PostgresStaffCensus:
    def __init__(self, database: Database) -> None:
        self._database = database

    async def count_active_chefs(self) -> int:
        async with self._database.transaction(None) as connection:
            return int(
                await connection.fetchval(
                    "SELECT count(*) FROM users WHERE role = 'chef' AND active"
                )
            )
