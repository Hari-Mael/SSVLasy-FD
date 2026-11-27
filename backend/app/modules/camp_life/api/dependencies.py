import secrets

from app.modules.camp_life.application.use_cases import (
    AdvanceDay,
    CollectResource,
    ReadMyCamp,
    SetCampType,
    SetWeather,
    UnblockTeam,
)
from app.modules.camp_life.domain.rules import Weather
from app.modules.camp_life.infrastructure.repositories import PostgresCampRepository
from app.shared.api.dependencies import AuthenticatedConnection, ContainerDep


def _random_weather() -> Weather:
    return secrets.choice(list(Weather))


def collect_resource(
    connection: AuthenticatedConnection, container: ContainerDep
) -> CollectResource:
    return CollectResource(PostgresCampRepository(connection), container.clock)


def read_my_camp(connection: AuthenticatedConnection, container: ContainerDep) -> ReadMyCamp:
    return ReadMyCamp(PostgresCampRepository(connection), container.clock)


def advance_day(connection: AuthenticatedConnection, container: ContainerDep) -> AdvanceDay:
    return AdvanceDay(PostgresCampRepository(connection), container.clock, _random_weather)


def set_weather(connection: AuthenticatedConnection) -> SetWeather:
    return SetWeather(PostgresCampRepository(connection))


def set_camp_type(connection: AuthenticatedConnection) -> SetCampType:
    return SetCampType(PostgresCampRepository(connection))


def unblock_team(connection: AuthenticatedConnection) -> UnblockTeam:
    return UnblockTeam(PostgresCampRepository(connection))
