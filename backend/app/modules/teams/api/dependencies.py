from app.modules.teams.application.use_cases import AddMember, CreateTeam, ListMembers, ListTeams
from app.modules.teams.infrastructure.repositories import (
    PostgresMemberRepository,
    PostgresTeamRepository,
)
from app.shared.api.dependencies import AuthenticatedConnection, ContainerDep


def create_team(connection: AuthenticatedConnection) -> CreateTeam:
    return CreateTeam(PostgresTeamRepository(connection))


def list_teams(connection: AuthenticatedConnection) -> ListTeams:
    return ListTeams(PostgresTeamRepository(connection))


def add_member(connection: AuthenticatedConnection, container: ContainerDep) -> AddMember:
    return AddMember(
        PostgresTeamRepository(connection),
        PostgresMemberRepository(connection),
        container.passwords,
    )


def list_members(connection: AuthenticatedConnection) -> ListMembers:
    return ListMembers(PostgresTeamRepository(connection), PostgresMemberRepository(connection))
