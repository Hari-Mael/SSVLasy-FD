import asyncpg

from app.modules.access.domain.models import MemberIdentity, NewStaffUser, StaffUser
from app.shared.database import PostgresRepository
from app.shared.db_errors import translated_database_errors
from app.shared.errors import AppError
from app.shared.principal import Role

STAFF_COLUMNS = "id, email, display_name, password_hash, role, active"


def _to_staff_user(row: asyncpg.Record) -> StaffUser:
    return StaffUser(
        id=row["id"],
        email=row["email"],
        display_name=row["display_name"],
        role=Role(row["role"]),
        active=row["active"],
        password_hash=row["password_hash"],
    )


class PostgresStaffUserRepository(PostgresRepository):
    async def find_by_email(self, email: str) -> StaffUser | None:
        row = await self._connection.fetchrow(
            f"SELECT {STAFF_COLUMNS} FROM users WHERE lower(email) = $1", email
        )
        return _to_staff_user(row) if row is not None else None

    async def add(self, user: NewStaffUser) -> StaffUser:
        with translated_database_errors():
            row = await self._connection.fetchrow(
                f"INSERT INTO users (email, display_name, password_hash, role) "
                f"VALUES ($1, $2, $3, $4) RETURNING {STAFF_COLUMNS}",
                user.email,
                user.display_name,
                user.password_hash,
                user.role.value,
            )
        if row is None:
            raise AppError.invalid_state("Création du compte impossible")
        return _to_staff_user(row)


class PostgresMemberIdentityRepository(PostgresRepository):
    async def find_by_login(self, login: str) -> MemberIdentity | None:
        row = await self._connection.fetchrow(
            "SELECT m.id AS member_id, m.team_id, t.chef_id, m.password_hash "
            "FROM members m JOIN teams t ON t.id = m.team_id WHERE m.login = $1",
            login,
        )
        if row is None:
            return None
        return MemberIdentity(
            member_id=row["member_id"],
            team_id=row["team_id"],
            chef_id=row["chef_id"],
            password_hash=row["password_hash"],
        )
