import asyncio
import getpass
import sys

from app.modules.access.application.use_cases import CreateStaffUser
from app.modules.access.infrastructure.repositories import PostgresStaffUserRepository
from app.shared.config import get_settings
from app.shared.container import build_container
from app.shared.principal import Role


async def run(email: str, display_name: str, password: str) -> None:
    container = await build_container(get_settings())
    try:
        async with container.database.transaction(None) as connection:
            use_case = CreateStaffUser(PostgresStaffUserRepository(connection), container.passwords)
            user = await use_case.execute(email, display_name, password, Role.SUPER_ADMIN)
        print(user.id)
    finally:
        await container.database.close()


def main() -> None:
    if len(sys.argv) != 3:
        raise SystemExit("usage : python -m app.create_super_admin EMAIL NOM")
    asyncio.run(run(sys.argv[1], sys.argv[2], getpass.getpass("Mot de passe : ")))


if __name__ == "__main__":
    main()
