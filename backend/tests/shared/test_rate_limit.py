from datetime import timedelta

import pytest

from app.shared.errors import AppError, ErrorCode
from app.shared.rate_limit import LoginLimiter
from tests.application.fakes import FixedClock, utc


def test_repeated_failures_lock_the_identifier_until_the_window_passes() -> None:
    clock = FixedClock(utc(2026, 12, 1, 6, 0))
    limiter = LoginLimiter(clock)
    for _ in range(5):
        limiter.ensure_allowed("staff:chef@exemple.mg")
        limiter.record_failure("staff:chef@exemple.mg")
    with pytest.raises(AppError) as error:
        limiter.ensure_allowed("staff:chef@exemple.mg")
    assert error.value.code is ErrorCode.RATE_LIMITED
    clock.moment = clock.moment + timedelta(minutes=16)
    limiter.ensure_allowed("staff:chef@exemple.mg")


def test_a_success_resets_the_counter_and_other_identifiers_are_independent() -> None:
    clock = FixedClock(utc(2026, 12, 1, 6, 0))
    limiter = LoginLimiter(clock)
    for _ in range(4):
        limiter.record_failure("staff:a")
    limiter.reset("staff:a")
    limiter.ensure_allowed("staff:a")
    for _ in range(5):
        limiter.record_failure("staff:b")
    limiter.ensure_allowed("staff:a")
