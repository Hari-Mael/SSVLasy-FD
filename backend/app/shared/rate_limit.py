from datetime import datetime, timedelta
from typing import Final

from app.shared.clock import Clock
from app.shared.errors import AppError, ErrorCode

MAX_LOGIN_FAILURES: Final = 5
LOGIN_WINDOW: Final = timedelta(minutes=15)


class LoginLimiter:
    """Limite les échecs de connexion par identifiant sur une fenêtre glissante.

    Mémoire du processus : suffisant pour un seul serveur. Plusieurs instances
    demanderont de déplacer ce compteur en base ou dans un cache partagé.
    """

    def __init__(
        self,
        clock: Clock,
        max_failures: int = MAX_LOGIN_FAILURES,
        window: timedelta = LOGIN_WINDOW,
    ) -> None:
        self._clock = clock
        self._max_failures = max_failures
        self._window = window
        self._failures: dict[str, list[datetime]] = {}

    def _recent(self, key: str) -> list[datetime]:
        now = self._clock.now()
        recent = [moment for moment in self._failures.get(key, []) if now - moment < self._window]
        if recent:
            self._failures[key] = recent
        else:
            self._failures.pop(key, None)
        return recent

    def ensure_allowed(self, key: str) -> None:
        if len(self._recent(key)) >= self._max_failures:
            raise AppError(
                ErrorCode.RATE_LIMITED,
                "Trop de tentatives de connexion : réessayez dans quelques minutes",
            )

    def record_failure(self, key: str) -> None:
        self._recent(key)
        self._failures.setdefault(key, []).append(self._clock.now())

    def reset(self, key: str) -> None:
        self._failures.pop(key, None)
