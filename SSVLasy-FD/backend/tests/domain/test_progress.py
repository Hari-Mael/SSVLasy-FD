from datetime import UTC, datetime, timedelta

import pytest

from app.modules.activities.domain.progress import (
    AttemptOutcome,
    Choice,
    Progress,
    ProgressStatus,
    choose,
    settle,
    start_attempt,
    submit,
)
from app.shared.errors import AppError

T0 = datetime(2026, 12, 1, 6, 0, tzinfo=UTC)


def at(minutes: int) -> datetime:
    return T0 + timedelta(minutes=minutes)


def wrong(progress: Progress, now: datetime) -> Progress:
    return submit(progress, False, 10, now).progress


def test_wrong_answers_impose_15_10_then_5_minute_waits() -> None:
    first = wrong(Progress(), T0)
    assert first.deadline_at == at(15)
    second = wrong(first, at(15))
    assert second.deadline_at == at(25)
    third = wrong(second, at(25))
    assert third.status is ProgressStatus.SKIPPED
    assert third.deadline_at is None


def test_cannot_answer_during_the_wait() -> None:
    first = wrong(Progress(), T0)
    with pytest.raises(AppError):
        start_attempt(first, at(14))
    assert start_attempt(first, at(15)).progress == first


def test_correct_answer_solves_and_awards_points_even_after_errors() -> None:
    first = wrong(Progress(), T0)
    resolution = submit(first, True, 25, at(20))
    assert resolution.outcome is AttemptOutcome.CORRECT
    assert resolution.progress.status is ProgressStatus.SOLVED
    assert resolution.progress.awarded_points == 25


def test_refaire_gives_one_retry_without_delay() -> None:
    first = wrong(Progress(), T0)
    refaire = choose(first, Choice.RETRY, at(1)).progress
    assert refaire.status is ProgressStatus.FREE_RETRY
    assert refaire.deadline_at is None
    assert start_attempt(refaire, at(1)).progress == refaire
    failed = wrong(refaire, at(2))
    assert failed.status is ProgressStatus.SKIP_ONLY


def test_after_failed_refaire_only_skip_remains() -> None:
    skip_only = wrong(choose(wrong(Progress(), T0), Choice.RETRY, at(1)).progress, at(2))
    with pytest.raises(AppError):
        start_attempt(skip_only, at(3))
    with pytest.raises(AppError):
        choose(skip_only, Choice.RETRY, at(3))
    skipped = choose(skip_only, Choice.SKIP, at(3)).progress
    assert skipped.status is ProgressStatus.SKIPPED


def test_skip_is_refused_before_any_error() -> None:
    with pytest.raises(AppError):
        choose(Progress(), Choice.SKIP, T0)


def test_settle_changes_nothing() -> None:
    first = wrong(Progress(), T0)
    assert settle(first, at(60)).progress == first
