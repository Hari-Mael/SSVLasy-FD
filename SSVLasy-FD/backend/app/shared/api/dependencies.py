from collections.abc import AsyncIterator, Callable
from typing import Annotated, cast

import asyncpg
from fastapi import Depends, Request, params
from fastapi.security import HTTPAuthorizationCredentials, HTTPBearer

from app.shared.container import Container
from app.shared.errors import AppError
from app.shared.principal import Principal, Role

bearer = HTTPBearer(auto_error=False)


def get_container(request: Request) -> Container:
    container: Container = request.app.state.container
    return container


ContainerDep = Annotated[Container, Depends(get_container)]


def current_principal(
    container: ContainerDep,
    credentials: Annotated[HTTPAuthorizationCredentials | None, Depends(bearer)],
) -> Principal:
    if credentials is None:
        raise AppError.unauthenticated()
    return container.tokens.decode(credentials.credentials)


PrincipalDep = Annotated[Principal, Depends(current_principal)]


def require_roles(*roles: Role) -> Callable[[PrincipalDep], Principal]:
    def dependency(principal: PrincipalDep) -> Principal:
        if principal.role not in roles:
            raise AppError.forbidden()
        return principal

    return dependency


def requires(*roles: Role) -> params.Depends:
    return cast(params.Depends, Depends(require_roles(*roles)))


ChefDep = Annotated[Principal, Depends(require_roles(Role.CHEF))]
MemberDep = Annotated[Principal, Depends(require_roles(Role.MEMBER))]


async def authenticated_connection(
    container: ContainerDep, principal: PrincipalDep
) -> AsyncIterator[asyncpg.Connection]:
    async with container.database.transaction(principal) as connection:
        yield connection


async def system_connection(container: ContainerDep) -> AsyncIterator[asyncpg.Connection]:
    async with container.database.transaction(None) as connection:
        yield connection


AuthenticatedConnection = Annotated[asyncpg.Connection, Depends(authenticated_connection)]
SystemConnection = Annotated[asyncpg.Connection, Depends(system_connection)]
