import hashlib
import hmac
import secrets
from datetime import timedelta
from typing import Final
from uuid import UUID

import jwt
from argon2 import PasswordHasher
from argon2.exceptions import InvalidHashError, VerificationError
from cryptography.fernet import Fernet, InvalidToken

from app.shared.clock import Clock
from app.shared.errors import AppError
from app.shared.principal import Principal, Role

JWT_ALGORITHM: Final = "HS256"
ACCESS_CODE_BYTES: Final = 12


class PasswordService:
    def __init__(self) -> None:
        self._hasher = PasswordHasher()
        self._decoy = self._hasher.hash(secrets.token_urlsafe(16))

    def hash(self, password: str) -> str:
        return self._hasher.hash(password)

    def verify(self, password_hash: str | None, password: str) -> bool:
        try:
            self._hasher.verify(password_hash or self._decoy, password)
        except (VerificationError, InvalidHashError):
            return False
        return password_hash is not None


class AccessCodeService:
    def generate(self) -> str:
        return secrets.token_urlsafe(ACCESS_CODE_BYTES)

    def digest(self, code: str) -> str:
        return hashlib.sha256(code.encode("utf-8")).hexdigest()


def _optional_uuid(value: object) -> UUID | None:
    return UUID(str(value)) if value else None


class TokenService:
    def __init__(self, secret: str, ttl_minutes: int, clock: Clock) -> None:
        self._secret = secret
        self._ttl = timedelta(minutes=ttl_minutes)
        self._clock = clock

    def issue(self, principal: Principal) -> str:
        issued_at = self._clock.now()
        claims: dict[str, object] = {
            "sub": str(principal.id),
            "role": principal.role.value,
            "chef": str(principal.chef_id) if principal.chef_id else None,
            "team": str(principal.team_id) if principal.team_id else None,
            "iat": issued_at,
            "exp": issued_at + self._ttl,
        }
        return jwt.encode(claims, self._secret, algorithm=JWT_ALGORITHM)

    def decode(self, token: str) -> Principal:
        try:
            claims: dict[str, object] = jwt.decode(
                token,
                self._secret,
                algorithms=[JWT_ALGORITHM],
                options={"require": ["exp", "sub", "role"]},
            )
            return Principal(
                id=UUID(str(claims["sub"])),
                role=Role(str(claims["role"])),
                chef_id=_optional_uuid(claims.get("chef")),
                team_id=_optional_uuid(claims.get("team")),
            )
        except (jwt.PyJWTError, ValueError) as error:
            raise AppError.unauthenticated("Jeton invalide ou expiré") from error


class AnswerSigner:
    def __init__(self, secret: str) -> None:
        self._secret = secret.encode("utf-8")

    def digest(self, activity_id: UUID, answer_key: str) -> str:
        message = f"{activity_id}:{answer_key}".encode()
        return hmac.new(self._secret, message, hashlib.sha256).hexdigest()

    def matches(self, expected: str, activity_id: UUID, answer_key: str) -> bool:
        return hmac.compare_digest(expected, self.digest(activity_id, answer_key))


class SolutionSealer:
    def __init__(self, key: str) -> None:
        self._fernet = Fernet(key.encode("utf-8"))

    def seal(self, text: str) -> bytes:
        return self._fernet.encrypt(text.encode("utf-8"))

    def unseal(self, sealed: bytes) -> str:
        try:
            return self._fernet.decrypt(sealed).decode("utf-8")
        except InvalidToken as error:
            raise AppError.invalid_state("Contenu illisible avec la clé actuelle") from error
