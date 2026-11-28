from datetime import datetime
from uuid import UUID

import asyncpg

from app.modules.camp_life.application.ports import CampCampaign
from app.modules.camp_life.domain.rules import (
    CampState,
    CampType,
    Resource,
    Stocks,
    TeamStatus,
    Weather,
    sicken_if_overdue,
    status,
)
from app.shared.database import PostgresRepository


def _campaign(row: asyncpg.Record) -> CampCampaign:
    return CampCampaign(
        id=row["id"],
        owner_id=row["owner_id"],
        days=row["days"],
        current_day=row["current_day"],
        camp_type=CampType(row["camp_type"]),
        weather=Weather(row["weather"]),
        running=row["state"] == "running",
    )


_CAMPAIGN_COLUMNS = "id, owner_id, days, current_day, camp_type, weather, state FROM campaigns"


class PostgresCampRepository(PostgresRepository):
    async def campaign(self, campaign_id: UUID) -> CampCampaign | None:
        row = await self._connection.fetchrow(
            f"SELECT {_CAMPAIGN_COLUMNS} WHERE id = $1", campaign_id
        )
        return _campaign(row) if row else None

    async def running_campaign_of(self, owner_id: UUID) -> CampCampaign | None:
        row = await self._connection.fetchrow(
            f"SELECT {_CAMPAIGN_COLUMNS} WHERE owner_id = $1 AND state = 'running'", owner_id
        )
        return _campaign(row) if row else None

    async def set_day(self, campaign_id: UUID, day: int, weather: Weather) -> None:
        await self._connection.execute(
            "UPDATE campaigns SET current_day = $2, weather = $3 WHERE id = $1",
            campaign_id,
            day,
            weather.value,
        )

    async def set_weather(self, campaign_id: UUID, weather: Weather) -> None:
        await self._connection.execute(
            "UPDATE campaigns SET weather = $2 WHERE id = $1", campaign_id, weather.value
        )

    async def set_camp_type(self, campaign_id: UUID, camp_type: CampType) -> None:
        await self._connection.execute(
            "UPDATE campaigns SET camp_type = $2 WHERE id = $1", campaign_id, camp_type.value
        )

    async def team_ids_of_owner(self, owner_id: UUID) -> list[UUID]:
        rows = await self._connection.fetch("SELECT id FROM teams WHERE chef_id = $1", owner_id)
        return [row["id"] for row in rows]

    async def team_owner(self, team_id: UUID) -> UUID | None:
        owner = await self._connection.fetchval("SELECT chef_id FROM teams WHERE id = $1", team_id)
        return owner if isinstance(owner, UUID) else None

    async def state(self, team_id: UUID) -> CampState:
        row = await self._connection.fetchrow(
            "SELECT wood, water, food, missed_meal_days, blocked_since, sick "
            "FROM team_camp_state WHERE team_id = $1",
            team_id,
        )
        if row is None:
            return CampState()
        return CampState(
            stocks=Stocks(row["wood"], row["water"], row["food"]),
            missed_meal_days=row["missed_meal_days"],
            blocked_since=row["blocked_since"],
            sick=row["sick"],
        )

    async def save_state(self, team_id: UUID, state: CampState) -> None:
        await self._connection.execute(
            "INSERT INTO team_camp_state (team_id, wood, water, food, missed_meal_days, "
            "blocked_since, sick) VALUES ($1, $2, $3, $4, $5, $6, $7) "
            "ON CONFLICT (team_id) DO UPDATE SET wood = $2, water = $3, food = $4, "
            "missed_meal_days = $5, blocked_since = $6, sick = $7",
            team_id,
            state.stocks.wood,
            state.stocks.water,
            state.stocks.food,
            state.missed_meal_days,
            state.blocked_since,
            state.sick,
        )

    async def actions_today(self, team_id: UUID, campaign_id: UUID, day: int) -> set[Resource]:
        rows = await self._connection.fetch(
            "SELECT resource FROM team_day_actions "
            "WHERE team_id = $1 AND campaign_id = $2 AND day = $3",
            team_id,
            campaign_id,
            day,
        )
        return {Resource(row["resource"]) for row in rows}

    async def add_action(
        self, team_id: UUID, campaign_id: UUID, day: int, resource: Resource
    ) -> None:
        await self._connection.execute(
            "INSERT INTO team_day_actions (team_id, campaign_id, day, resource) "
            "VALUES ($1, $2, $3, $4)",
            team_id,
            campaign_id,
            day,
            resource.value,
        )

    async def apply_energy(self, team_id: UUID, delta: int) -> None:
        await self._connection.execute(
            "UPDATE members SET energy = GREATEST(0, LEAST(100, energy + $2)) WHERE team_id = $1",
            team_id,
            delta,
        )


class PostgresTeamCampGate(PostgresRepository):
    async def blocking_reason(self, team_id: UUID, now: datetime) -> str | None:
        repository = PostgresCampRepository(self._connection)
        state = await repository.state(team_id)
        corrected = sicken_if_overdue(state, now)
        if corrected != state:
            await repository.save_state(team_id, corrected)
        current = status(corrected, now)
        if current is TeamStatus.NORMAL:
            return None
        if current is TeamStatus.SICK:
            return "L'équipe est malade : contacter le chef"
        if current is TeamStatus.NOTIFIED:
            return "L'équipe est bloquée : contacter le chef pour débloquer"
        return "L'équipe est bloquée : en attente du chef"
