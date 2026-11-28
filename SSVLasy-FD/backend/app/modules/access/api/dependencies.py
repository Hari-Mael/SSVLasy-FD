from app.modules.access.application.use_cases import (
    AuthenticateMember,
    AuthenticateStaff,
    CreateStaffUser,
)
from app.modules.access.infrastructure.repositories import (
    PostgresMemberIdentityRepository,
    PostgresStaffUserRepository,
)
from app.shared.api.dependencies import AuthenticatedConnection, ContainerDep, SystemConnection
from app.shared.clock import SystemClock
from app.shared.rate_limit import LoginLimiter

_LOGIN_LIMITER = LoginLimiter(SystemClock())


def authenticate_staff(connection: SystemConnection, container: ContainerDep) -> AuthenticateStaff:
    return AuthenticateStaff(
        PostgresStaffUserRepository(connection),
        container.passwords,
        container.tokens,
        _LOGIN_LIMITER,
    )


def authenticate_member(
    connection: SystemConnection, container: ContainerDep
) -> AuthenticateMember:
    return AuthenticateMember(
        PostgresMemberIdentityRepository(connection),
        container.passwords,
        container.tokens,
        _LOGIN_LIMITER,
    )


def create_staff_user(
    connection: AuthenticatedConnection, container: ContainerDep
) -> CreateStaffUser:
    return CreateStaffUser(PostgresStaffUserRepository(connection), container.passwords)
