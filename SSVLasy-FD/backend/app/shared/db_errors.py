from collections.abc import Iterator
from contextlib import contextmanager
from typing import Final

import asyncpg

from app.shared.errors import AppError, ErrorCode
from app.shared.quotas import (
    ACTIVITY_DAY_QUOTA_MESSAGE,
    CHEF_QUOTA_MESSAGE,
    MEMBER_QUOTA_MESSAGE,
    SUPER_ADMIN_QUOTA_MESSAGE,
    TEAM_QUOTA_MESSAGE,
)

RAISED_ERRORS: Final[dict[str, tuple[ErrorCode, str]]] = {
    "QUOTA_USERS_SUPER_ADMIN": (ErrorCode.QUOTA_EXCEEDED, SUPER_ADMIN_QUOTA_MESSAGE),
    "QUOTA_USERS_CHEF": (ErrorCode.QUOTA_EXCEEDED, CHEF_QUOTA_MESSAGE),
    "QUOTA_TEAMS_PER_CHEF": (ErrorCode.QUOTA_EXCEEDED, TEAM_QUOTA_MESSAGE),
    "QUOTA_MEMBERS_PER_TEAM": (ErrorCode.QUOTA_EXCEEDED, MEMBER_QUOTA_MESSAGE),
    "QUOTA_ACTIVITIES_PER_DAY": (ErrorCode.QUOTA_EXCEEDED, ACTIVITY_DAY_QUOTA_MESSAGE),
    "LAST_SUPER_ADMIN": (
        ErrorCode.INVALID_STATE,
        "Le dernier super admin actif ne peut pas être retiré",
    ),
    "OWNER_NOT_CHEF": (ErrorCode.VALIDATION, "Le propriétaire doit être un chef actif"),
}
FALLBACK_RAISED: Final = (ErrorCode.INVALID_STATE, "Opération refusée par la base de données")


@contextmanager
def translated_database_errors() -> Iterator[None]:
    try:
        yield
    except asyncpg.UniqueViolationError as error:
        raise AppError.conflict("Cette valeur existe déjà") from error
    except asyncpg.RaiseError as error:
        code, message = RAISED_ERRORS.get(error.message, FALLBACK_RAISED)
        raise AppError(code, message) from error
    except asyncpg.ForeignKeyViolationError as error:
        raise AppError.not_found("Ressource liée") from error
    except asyncpg.CheckViolationError as error:
        raise AppError.validation("Valeur hors des limites autorisées") from error
    except asyncpg.InsufficientPrivilegeError as error:
        raise AppError.forbidden() from error
