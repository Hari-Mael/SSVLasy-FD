from datetime import UTC, datetime, timedelta

import pytest

from app.modules.camp_life.domain.rules import (
    CampState,
    CampType,
    Resource,
    Stocks,
    TeamStatus,
    collect,
    end_of_day,
    meals_possible,
    sicken_if_overdue,
    status,
    unblock,
)
from app.shared.errors import AppError

T0 = datetime(2026, 12, 1, 18, 0, tzinfo=UTC)


def full_stocks() -> Stocks:
    return Stocks(wood=60, water=40, food=20)


def test_collect_adds_the_yield_capped_by_capacity() -> None:
    state, gained = collect(CampState(), Resource.WOOD, CampType.TSELATRA)
    assert gained == 45 and state.stocks.wood == 45
    almost_full = CampState(Stocks(wood=80))
    state, gained = collect(almost_full, Resource.WOOD, CampType.TSELATRA)
    assert gained == 10 and state.stocks.wood == 90
    with pytest.raises(AppError):
        collect(state, Resource.WOOD, CampType.TSELATRA)


def test_blocked_or_sick_teams_cannot_collect() -> None:
    blocked = CampState(blocked_since=T0)
    with pytest.raises(AppError):
        collect(blocked, Resource.WATER, CampType.TSELATRA)
    with pytest.raises(AppError):
        collect(CampState(sick=True), Resource.FOOD, CampType.TSELATRA)


def test_night_reserve_of_fifteen_wood_is_not_used_for_meals() -> None:
    assert meals_possible(Stocks(wood=30, water=60, food=30)) == 1
    assert meals_possible(Stocks(wood=45, water=60, food=30)) == 3


def test_three_meals_consume_their_cost() -> None:
    result = end_of_day(CampState(full_stocks()), T0)
    assert result.meals == 3
    assert result.missed_meals == 0
    assert result.state.stocks == Stocks(wood=30, water=10, food=5)
    assert result.energy_delta == 3 * 20


def test_two_days_without_any_meal_block_the_team() -> None:
    state = CampState()
    first = end_of_day(state, T0)
    assert first.state.missed_meal_days == 1 and first.state.blocked_since is None
    second = end_of_day(first.state, T0)
    assert second.state.blocked_since == T0


def test_blocked_team_is_notified_then_becomes_sick_after_thirty_minutes() -> None:
    blocked = CampState(blocked_since=T0)
    assert status(blocked, T0 + timedelta(minutes=5)) is TeamStatus.BLOCKED
    assert status(blocked, T0 + timedelta(minutes=15)) is TeamStatus.NOTIFIED
    assert status(blocked, T0 + timedelta(minutes=29)) is TeamStatus.NOTIFIED
    assert status(blocked, T0 + timedelta(minutes=30)) is TeamStatus.SICK
    assert sicken_if_overdue(blocked, T0 + timedelta(minutes=30)).sick


def test_chief_unblock_resets_the_counters() -> None:
    state = unblock(CampState(Stocks(1, 2, 3), 2, T0, True))
    assert state == CampState(Stocks(1, 2, 3), 0, None, False)
