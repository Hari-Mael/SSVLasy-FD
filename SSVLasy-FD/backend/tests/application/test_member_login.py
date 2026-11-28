from dataclasses import dataclass
from uuid import UUID, uuid4

import pytest

from app.modules.access.application.use_cases import AuthenticateMember
from app.modules.access.domain.models import MemberIdentity
from app.shared.errors import AppError, ErrorCode
from app.shared.principal import Role
from app.shared.rate_limit import LoginLimiter
from app.shared.security import PasswordService, TokenService
from tests.application.fakes import FixedClock, utc

SECRET = "x" * 48


@dataclass
class FakeIdentityRepository:
    identities: dict[str, MemberIdentity]

    async def find_by_login(self, login: str) -> MemberIdentity | None:
        return self.identities.get(login)


def build(password_hash: str | None) -> tuple[AuthenticateMember, UUID]:
    member_id = uuid4()
    identity = MemberIdentity(member_id, uuid4(), uuid4(), password_hash)
    clock = FixedClock(utc(2026, 12, 1, 6, 0))
    use_case = AuthenticateMember(
        FakeIdentityRepository({"renard rouge": identity}),
        PasswordService(),
        TokenService(SECRET, 60, clock),
        LoginLimiter(clock),
    )
    return use_case, member_id


async def test_member_logs_in_with_pseudonym_and_password() -> None:
    passwords = PasswordService()
    use_case, _ = build(passwords.hash("motdepasse-long"))
    token = await use_case.execute("  Renard   Rouge ", "motdepasse-long")
    assert token.role is Role.MEMBER


async def test_wrong_password_is_refused_and_members_without_password_cannot_log_in() -> None:
    use_case, _ = build(PasswordService().hash("motdepasse-long"))
    with pytest.raises(AppError) as wrong:
        await use_case.execute("renard rouge", "mauvais-mot-de-passe")
    assert wrong.value.code is ErrorCode.UNAUTHENTICATED
    legacy, _ = build(None)
    with pytest.raises(AppError):
        await legacy.execute("renard rouge", "motdepasse-long")


async def test_repeated_failures_are_rate_limited() -> None:
    use_case, _ = build(PasswordService().hash("motdepasse-long"))
    for _ in range(5):
        with pytest.raises(AppError):
            await use_case.execute("renard rouge", "faux-mot-de-passe")
    with pytest.raises(AppError) as limited:
        await use_case.execute("renard rouge", "motdepasse-long")
    assert limited.value.code is ErrorCode.RATE_LIMITED
