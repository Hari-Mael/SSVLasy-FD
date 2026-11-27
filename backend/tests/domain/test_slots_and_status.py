from datetime import UTC, date, datetime, time

import pytest

from app.modules.activities.domain.activity import ActivityStatus, advance_status
from app.modules.activities.domain.slots import slot_is_open, slot_window
from app.shared.errors import AppError

START = date(2026, 12, 1)
ZONE = "Indian/Antananarivo"


def test_slot_window_uses_the_campaign_timezone_and_day_offset() -> None:
    opening, closing = slot_window(START, ZONE, 2, time(9, 0), time(10, 0))
    assert opening.astimezone(UTC) == datetime(2026, 12, 2, 6, 0, tzinfo=UTC)
    assert closing.astimezone(UTC) == datetime(2026, 12, 2, 7, 0, tzinfo=UTC)


def test_slot_is_open_only_inside_the_window() -> None:
    inside = datetime(2026, 12, 1, 6, 30, tzinfo=UTC)
    before = datetime(2026, 12, 1, 5, 59, tzinfo=UTC)
    after = datetime(2026, 12, 1, 7, 1, tzinfo=UTC)
    assert slot_is_open(START, ZONE, 1, time(9, 0), time(10, 0), inside)
    assert not slot_is_open(START, ZONE, 1, time(9, 0), time(10, 0), before)
    assert not slot_is_open(START, ZONE, 1, time(9, 0), time(10, 0), after)


def test_activity_status_only_moves_forward() -> None:
    draft, published = ActivityStatus.DRAFT, ActivityStatus.PUBLISHED
    assert advance_status(draft, published) is published
    assert advance_status(published, ActivityStatus.UNLOCKED) is ActivityStatus.UNLOCKED
    with pytest.raises(AppError):
        advance_status(ActivityStatus.DRAFT, ActivityStatus.UNLOCKED)
    with pytest.raises(AppError):
        advance_status(ActivityStatus.UNLOCKED, ActivityStatus.PUBLISHED)
