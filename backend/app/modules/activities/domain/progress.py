from dataclasses import dataclass, replace
from datetime import datetime, timedelta
from enum import StrEnum
from typing import Final

from app.shared.errors import AppError

# Délai d'attente après la 1re, 2e et 3e erreur (règle du jeu).
ATTEMPT_COOLDOWN_MINUTES: Final[tuple[int, ...]] = (15, 10, 5)
MAX_ATTEMPTS: Final = len(ATTEMPT_COOLDOWN_MINUTES)


class ProgressStatus(StrEnum):
    OPEN = "open"
    FREE_RETRY = "free_retry"
    SKIP_ONLY = "skip_only"
    SOLVED = "solved"
    SKIPPED = "skipped"


class AttemptOutcome(StrEnum):
    CORRECT = "correct"
    WRONG = "wrong"
    EXPIRED = "expired"


class Choice(StrEnum):
    RETRY = "retry"
    SKIP = "skip"


@dataclass(frozen=True, slots=True)
class Progress:
    """État d'une équipe sur une activité.

    `deadline_at` est la fin du délai d'attente après une erreur (None si aucun délai).
    `running` n'est plus utilisé : il reste en base pour compatibilité et vaut toujours False.
    """

    status: ProgressStatus = ProgressStatus.OPEN
    attempts_used: int = 0
    running: bool = False
    deadline_at: datetime | None = None
    awarded_points: int = 0


@dataclass(frozen=True, slots=True)
class Transition:
    progress: Progress
    outcome: AttemptOutcome | None


@dataclass(frozen=True, slots=True)
class Resolution:
    progress: Progress
    outcome: AttemptOutcome


def settle(progress: Progress, now: datetime) -> Transition:
    """Le délai d'attente se purge seul : aucune transition d'état n'est nécessaire."""
    return Transition(progress, None)


def _ensure_can_answer(progress: Progress, now: datetime) -> None:
    if progress.status in (ProgressStatus.SOLVED, ProgressStatus.SKIPPED):
        raise AppError.invalid_state("Activité terminée pour cette équipe")
    if progress.status is ProgressStatus.SKIP_ONLY:
        raise AppError.invalid_state("Après un échec en « refaire », seul « passer » est possible")
    if progress.deadline_at is not None and now < progress.deadline_at:
        remaining = int((progress.deadline_at - now).total_seconds() // 60) + 1
        raise AppError.invalid_state(f"Délai d'attente : encore {remaining} min")


def start_attempt(progress: Progress, now: datetime) -> Transition:
    """Vérifie qu'une réponse peut être envoyée maintenant. Ne modifie rien."""
    _ensure_can_answer(progress, now)
    return Transition(progress, None)


def submit(progress: Progress, is_correct: bool, points: int, now: datetime) -> Resolution:
    _ensure_can_answer(progress, now)
    attempts = progress.attempts_used + 1
    if is_correct:
        solved = replace(
            progress,
            status=ProgressStatus.SOLVED,
            attempts_used=attempts,
            deadline_at=None,
            awarded_points=points,
        )
        return Resolution(solved, AttemptOutcome.CORRECT)
    if progress.status is ProgressStatus.FREE_RETRY:
        return Resolution(
            replace(
                progress, status=ProgressStatus.SKIP_ONLY, attempts_used=attempts, deadline_at=None
            ),
            AttemptOutcome.WRONG,
        )
    if attempts >= MAX_ATTEMPTS:
        closed = replace(
            progress, status=ProgressStatus.SKIPPED, attempts_used=attempts, deadline_at=None
        )
        return Resolution(closed, AttemptOutcome.WRONG)
    cooldown = now + timedelta(minutes=ATTEMPT_COOLDOWN_MINUTES[attempts - 1])
    return Resolution(
        replace(progress, status=ProgressStatus.OPEN, attempts_used=attempts, deadline_at=cooldown),
        AttemptOutcome.WRONG,
    )


def choose(progress: Progress, choice: Choice, now: datetime) -> Transition:
    if progress.attempts_used == 0:
        raise AppError.invalid_state("Aucune erreur : pas de choix à faire")
    if choice is Choice.RETRY and progress.status is ProgressStatus.OPEN:
        return Transition(
            replace(progress, status=ProgressStatus.FREE_RETRY, deadline_at=None), None
        )
    skippable = {ProgressStatus.OPEN, ProgressStatus.FREE_RETRY, ProgressStatus.SKIP_ONLY}
    if choice is Choice.SKIP and progress.status in skippable:
        return Transition(replace(progress, status=ProgressStatus.SKIPPED, deadline_at=None), None)
    raise AppError.invalid_state("Ce choix n'est pas disponible à ce stade")
