from uuid import UUID

import asyncpg

from app.modules.activities.domain.activity import (
    DAY_END,
    Activity,
    ActivityDraft,
    ActivityStatus,
    CampaignSchedule,
)
from app.modules.activities.domain.ciphers import CipherKind, CipherSpec
from app.modules.activities.domain.progress import AttemptOutcome, Progress, ProgressStatus
from app.modules.campaigns.domain.state import CampaignState
from app.shared.database import PostgresRepository
from app.shared.db_errors import translated_database_errors
from app.shared.errors import AppError

ACTIVITY_COLUMNS = (
    "id, campaign_id, title, day, slot_start, slot_end, points, "
    "cipher, cipher_shift, cipher_key, encoded_text, status"
)
PROGRESS_COLUMNS = "status, attempts_used, running, deadline_at, awarded_points"


def _to_activity(row: asyncpg.Record) -> Activity:
    return Activity(
        id=row["id"],
        campaign_id=row["campaign_id"],
        title=row["title"],
        day=row["day"],
        slot_start=row["slot_start"],
        slot_end=row["slot_end"],
        points=row["points"],
        cipher=CipherSpec(CipherKind(row["cipher"]), row["cipher_shift"], row["cipher_key"]),
        encoded_text=row["encoded_text"],
        status=ActivityStatus(row["status"]),
    )


def _to_progress(row: asyncpg.Record) -> Progress:
    return Progress(
        status=ProgressStatus(row["status"]),
        attempts_used=row["attempts_used"],
        running=row["running"],
        deadline_at=row["deadline_at"],
        awarded_points=row["awarded_points"],
    )


class PostgresActivityRepository(PostgresRepository):
    async def add(self, campaign_id: UUID, draft: ActivityDraft) -> Activity:
        with translated_database_errors():
            row = await self._connection.fetchrow(
                f"INSERT INTO activities (id, campaign_id, title, day, slot_start, slot_end, "
                f"points, cipher, cipher_shift, cipher_key, encoded_text, answer_digest, "
                f"solution_sealed) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13) "
                f"RETURNING {ACTIVITY_COLUMNS}",
                draft.id,
                campaign_id,
                draft.title,
                draft.day,
                draft.slot_start,
                draft.slot_end,
                draft.points,
                draft.cipher.kind.value,
                draft.cipher.shift,
                draft.cipher.key,
                draft.encoded_text,
                draft.answer_digest,
                draft.solution_sealed,
            )
        if row is None:
            raise AppError.invalid_state("Création de l'activité impossible")
        return _to_activity(row)

    async def get(self, activity_id: UUID) -> Activity | None:
        row = await self._connection.fetchrow(
            f"SELECT {ACTIVITY_COLUMNS} FROM activities WHERE id = $1", activity_id
        )
        return _to_activity(row) if row is not None else None

    async def list_for_campaign(self, campaign_id: UUID) -> list[Activity]:
        rows = await self._connection.fetch(
            f"SELECT {ACTIVITY_COLUMNS} FROM activities WHERE campaign_id = $1 "
            f"ORDER BY day, slot_start",
            campaign_id,
        )
        return [_to_activity(row) for row in rows]

    async def set_status(self, activity_id: UUID, status: ActivityStatus) -> Activity:
        row = await self._connection.fetchrow(
            f"UPDATE activities SET status = $2 WHERE id = $1 RETURNING {ACTIVITY_COLUMNS}",
            activity_id,
            status.value,
        )
        if row is None:
            raise AppError.not_found("Activité")
        return _to_activity(row)

    async def get_answer_digest(self, activity_id: UUID) -> str:
        digest = await self._connection.fetchval(
            "SELECT answer_digest FROM activities WHERE id = $1", activity_id
        )
        if digest is None:
            raise AppError.not_found("Activité")
        return str(digest)

    async def get_solution_sealed(self, activity_id: UUID) -> bytes:
        sealed = await self._connection.fetchval(
            "SELECT solution_sealed FROM activities WHERE id = $1", activity_id
        )
        if sealed is None:
            raise AppError.not_found("Activité")
        return bytes(sealed)


class PostgresScheduleRepository(PostgresRepository):
    async def get(self, campaign_id: UUID) -> CampaignSchedule | None:
        row = await self._connection.fetchrow(
            "SELECT owner_id, state, start_date, days, timezone FROM campaigns WHERE id = $1",
            campaign_id,
        )
        if row is None:
            return None
        return CampaignSchedule(
            owner_id=row["owner_id"],
            state=CampaignState(row["state"]),
            start_date=row["start_date"],
            days=row["days"],
            timezone=row["timezone"],
        )


class PostgresProgressRepository(PostgresRepository):
    async def lock(self, activity_id: UUID, team_id: UUID) -> Progress:
        with translated_database_errors():
            await self._connection.execute(
                "INSERT INTO team_activity (activity_id, team_id) VALUES ($1, $2) "
                "ON CONFLICT DO NOTHING",
                activity_id,
                team_id,
            )
            row = await self._connection.fetchrow(
                f"SELECT {PROGRESS_COLUMNS} FROM team_activity "
                f"WHERE activity_id = $1 AND team_id = $2 FOR UPDATE",
                activity_id,
                team_id,
            )
        if row is None:
            raise AppError.not_found("Progression")
        return _to_progress(row)

    async def save(self, activity_id: UUID, team_id: UUID, progress: Progress) -> None:
        await self._connection.execute(
            "UPDATE team_activity SET status = $3, attempts_used = $4, running = $5, "
            "deadline_at = $6, awarded_points = $7, updated_at = now() "
            "WHERE activity_id = $1 AND team_id = $2",
            activity_id,
            team_id,
            progress.status.value,
            progress.attempts_used,
            progress.running,
            progress.deadline_at,
            progress.awarded_points,
        )

    async def log(
        self, activity_id: UUID, team_id: UUID, number: int, outcome: AttemptOutcome
    ) -> None:
        await self._connection.execute(
            "INSERT INTO attempts (activity_id, team_id, number, outcome) VALUES ($1, $2, $3, $4)",
            activity_id,
            team_id,
            number,
            outcome.value,
        )

    async def count_unsolved_daytime(self, campaign_id: UUID, day: int, team_id: UUID) -> int:
        return int(
            await self._connection.fetchval(
                "SELECT count(*) FROM activities a "
                "LEFT JOIN team_activity p ON p.activity_id = a.id AND p.team_id = $3 "
                "WHERE a.campaign_id = $1 AND a.day = $2 AND a.status <> 'draft' "
                "AND a.slot_end <= $4 AND coalesce(p.status::text, 'open') <> 'solved'",
                campaign_id,
                day,
                team_id,
                DAY_END,
            )
        )

    async def list_for_team(self, campaign_id: UUID, team_id: UUID) -> dict[UUID, Progress]:
        rows = await self._connection.fetch(
            f"SELECT p.activity_id, p.{PROGRESS_COLUMNS.replace(', ', ', p.')} "
            f"FROM team_activity p JOIN activities a ON a.id = p.activity_id "
            f"WHERE a.campaign_id = $1 AND p.team_id = $2",
            campaign_id,
            team_id,
        )
        return {row["activity_id"]: _to_progress(row) for row in rows}
