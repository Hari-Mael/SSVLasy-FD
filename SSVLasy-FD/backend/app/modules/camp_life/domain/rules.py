from dataclasses import dataclass, replace
from datetime import datetime, timedelta
from enum import StrEnum
from typing import Final

from app.shared.errors import AppError


class Resource(StrEnum):
    WOOD = "wood"
    WATER = "water"
    FOOD = "food"


class CampType(StrEnum):
    TSELATRA = "tselatra"
    LEHIBE = "lehibe"
    FENO = "feno"


class Weather(StrEnum):
    CHALEUR = "chaleur"
    PLUVIEUX = "pluvieux"
    VENTEUX = "venteux"
    CHAUD_HUMIDE = "chaud_humide"
    VENTEUX_HUMIDE = "venteux_humide"
    CHAUD_SEC = "chaud_sec"


class TeamStatus(StrEnum):
    NORMAL = "normal"
    BLOCKED = "blocked"
    NOTIFIED = "notified"
    SICK = "sick"


YIELD: Final[dict[Resource, int]] = {Resource.WOOD: 45, Resource.WATER: 30, Resource.FOOD: 15}
MEAL_COST: Final[dict[Resource, int]] = {Resource.WOOD: 10, Resource.WATER: 10, Resource.FOOD: 5}
WOOD_NIGHT_RESERVE: Final = 15
MEALS_PER_DAY: Final = 3
MEAL_ENERGY_GAIN: Final = 20
MISSED_MEAL_ENERGY_LOSS: Final = 25
MAX_MISSED_DAYS: Final = 2
NOTIFY_AFTER: Final = timedelta(minutes=15)
SICK_AFTER: Final = timedelta(minutes=30)
MAX_DAYS: Final[dict[CampType, int]] = {CampType.TSELATRA: 3, CampType.LEHIBE: 5, CampType.FENO: 11}
CAPACITY: Final[dict[CampType, dict[Resource, int]]] = {
    CampType.TSELATRA: {Resource.WOOD: 90, Resource.WATER: 60, Resource.FOOD: 30},
    CampType.LEHIBE: {Resource.WOOD: 135, Resource.WATER: 90, Resource.FOOD: 45},
    CampType.FENO: {Resource.WOOD: 180, Resource.WATER: 120, Resource.FOOD: 60},
}


@dataclass(frozen=True, slots=True)
class Stocks:
    wood: int = 0
    water: int = 0
    food: int = 0

    def amount(self, resource: Resource) -> int:
        return {Resource.WOOD: self.wood, Resource.WATER: self.water, Resource.FOOD: self.food}[
            resource
        ]

    def with_amount(self, resource: Resource, value: int) -> "Stocks":
        return replace(self, **{resource.value: value})


@dataclass(frozen=True, slots=True)
class CampState:
    stocks: Stocks = Stocks()
    missed_meal_days: int = 0
    blocked_since: datetime | None = None
    sick: bool = False


@dataclass(frozen=True, slots=True)
class DayResult:
    state: CampState
    meals: int
    missed_meals: int
    energy_delta: int


def collect(state: CampState, resource: Resource, camp_type: CampType) -> tuple[CampState, int]:
    if state.sick or state.blocked_since is not None:
        raise AppError.invalid_state("L'équipe est bloquée ou malade : contacter le chef")
    capacity = CAPACITY[camp_type][resource]
    current = state.stocks.amount(resource)
    gained = min(YIELD[resource], capacity - current)
    if gained <= 0:
        raise AppError.invalid_state("Stock plein pour cette ressource")
    return replace(state, stocks=state.stocks.with_amount(resource, current + gained)), gained


def meals_possible(stocks: Stocks) -> int:
    usable_wood = max(0, stocks.wood - WOOD_NIGHT_RESERVE)
    return min(
        MEALS_PER_DAY,
        usable_wood // MEAL_COST[Resource.WOOD],
        stocks.water // MEAL_COST[Resource.WATER],
        stocks.food // MEAL_COST[Resource.FOOD],
    )


def end_of_day(state: CampState, now: datetime) -> DayResult:
    meals = meals_possible(state.stocks)
    stocks = state.stocks
    for resource in Resource:
        stocks = stocks.with_amount(resource, stocks.amount(resource) - meals * MEAL_COST[resource])
    missed = MEALS_PER_DAY - meals
    missed_days = state.missed_meal_days + 1 if meals == 0 else 0
    blocked_since = state.blocked_since
    if missed_days >= MAX_MISSED_DAYS and blocked_since is None and not state.sick:
        blocked_since = now
    energy_delta = meals * MEAL_ENERGY_GAIN - missed * MISSED_MEAL_ENERGY_LOSS
    new_state = CampState(stocks, missed_days, blocked_since, state.sick)
    return DayResult(new_state, meals, missed, energy_delta)


def status(state: CampState, now: datetime) -> TeamStatus:
    if state.sick:
        return TeamStatus.SICK
    if state.blocked_since is None:
        return TeamStatus.NORMAL
    elapsed = now - state.blocked_since
    if elapsed >= SICK_AFTER:
        return TeamStatus.SICK
    if elapsed >= NOTIFY_AFTER:
        return TeamStatus.NOTIFIED
    return TeamStatus.BLOCKED


def sicken_if_overdue(state: CampState, now: datetime) -> CampState:
    if status(state, now) is TeamStatus.SICK and not state.sick:
        return replace(state, sick=True)
    return state


def unblock(state: CampState) -> CampState:
    return CampState(state.stocks, 0, None, False)
