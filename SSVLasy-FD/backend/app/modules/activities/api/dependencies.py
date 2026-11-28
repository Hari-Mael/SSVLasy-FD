from app.modules.activities.application.management import (
    ChangeActivityStatus,
    CreateActivity,
    ListActivities,
    RevealSolution,
)
from app.modules.activities.application.play import (
    ActivityGate,
    ChooseAfterFailure,
    ReadProgress,
    StartAttempt,
    SubmitAnswer,
)
from app.modules.activities.infrastructure.repositories import (
    PostgresActivityRepository,
    PostgresProgressRepository,
    PostgresScheduleRepository,
)
from app.modules.camp_life.infrastructure.repositories import PostgresTeamCampGate
from app.shared.api.dependencies import AuthenticatedConnection, ContainerDep


def create_activity(connection: AuthenticatedConnection, container: ContainerDep) -> CreateActivity:
    return CreateActivity(
        PostgresActivityRepository(connection),
        PostgresScheduleRepository(connection),
        container.signer,
        container.sealer,
    )


def change_activity_status(connection: AuthenticatedConnection) -> ChangeActivityStatus:
    return ChangeActivityStatus(
        PostgresActivityRepository(connection), PostgresScheduleRepository(connection)
    )


def list_activities(connection: AuthenticatedConnection) -> ListActivities:
    return ListActivities(
        PostgresActivityRepository(connection), PostgresProgressRepository(connection)
    )


def reveal_solution(connection: AuthenticatedConnection, container: ContainerDep) -> RevealSolution:
    return RevealSolution(PostgresActivityRepository(connection), container.sealer)


def _gate(connection: AuthenticatedConnection, container: ContainerDep) -> ActivityGate:
    return ActivityGate(
        PostgresActivityRepository(connection),
        PostgresScheduleRepository(connection),
        container.clock,
        PostgresTeamCampGate(connection),
    )


def start_attempt(connection: AuthenticatedConnection, container: ContainerDep) -> StartAttempt:
    return StartAttempt(
        _gate(connection, container), PostgresProgressRepository(connection), container.clock
    )


def submit_answer(connection: AuthenticatedConnection, container: ContainerDep) -> SubmitAnswer:
    return SubmitAnswer(
        _gate(connection, container),
        PostgresActivityRepository(connection),
        PostgresProgressRepository(connection),
        container.signer,
        container.clock,
    )


def choose_after_failure(
    connection: AuthenticatedConnection, container: ContainerDep
) -> ChooseAfterFailure:
    return ChooseAfterFailure(
        _gate(connection, container), PostgresProgressRepository(connection), container.clock
    )


def read_progress(connection: AuthenticatedConnection, container: ContainerDep) -> ReadProgress:
    return ReadProgress(
        PostgresActivityRepository(connection),
        PostgresProgressRepository(connection),
        container.clock,
    )
