from enum import StrEnum
from typing import Final, Self


class ErrorCode(StrEnum):
    UNAUTHENTICATED = "unauthenticated"
    FORBIDDEN = "forbidden"
    NOT_FOUND = "not_found"
    CONFLICT = "conflict"
    QUOTA_EXCEEDED = "quota_exceeded"
    INVALID_STATE = "invalid_state"
    VALIDATION = "validation_error"
    RATE_LIMITED = "rate_limited"


HTTP_STATUS: Final[dict[ErrorCode, int]] = {
    ErrorCode.UNAUTHENTICATED: 401,
    ErrorCode.FORBIDDEN: 403,
    ErrorCode.NOT_FOUND: 404,
    ErrorCode.CONFLICT: 409,
    ErrorCode.QUOTA_EXCEEDED: 409,
    ErrorCode.INVALID_STATE: 409,
    ErrorCode.VALIDATION: 422,
    ErrorCode.RATE_LIMITED: 429,
}


class AppError(Exception):
    def __init__(self, code: ErrorCode, message: str) -> None:
        super().__init__(message)
        self.code = code
        self.message = message

    @property
    def status_code(self) -> int:
        return HTTP_STATUS[self.code]

    @classmethod
    def unauthenticated(cls, message: str = "Authentification requise") -> Self:
        return cls(ErrorCode.UNAUTHENTICATED, message)

    @classmethod
    def forbidden(cls, message: str = "Action non autorisée") -> Self:
        return cls(ErrorCode.FORBIDDEN, message)

    @classmethod
    def not_found(cls, entity: str) -> Self:
        return cls(ErrorCode.NOT_FOUND, f"{entity} introuvable")

    @classmethod
    def conflict(cls, message: str) -> Self:
        return cls(ErrorCode.CONFLICT, message)

    @classmethod
    def quota(cls, message: str) -> Self:
        return cls(ErrorCode.QUOTA_EXCEEDED, message)

    @classmethod
    def invalid_state(cls, message: str) -> Self:
        return cls(ErrorCode.INVALID_STATE, message)

    @classmethod
    def validation(cls, message: str) -> Self:
        return cls(ErrorCode.VALIDATION, message)
