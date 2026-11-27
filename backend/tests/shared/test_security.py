from datetime import UTC, datetime, timedelta
from uuid import uuid4

import jwt
import pytest
from cryptography.fernet import Fernet

from app.shared.errors import AppError, ErrorCode
from app.shared.principal import Principal, Role
from app.shared.security import AccessCodeService, AnswerSigner, SolutionSealer, TokenService
from tests.application.fakes import FixedClock

SECRET = "x" * 32


def make_tokens(ttl_minutes: int = 60) -> tuple[TokenService, FixedClock]:
    clock = FixedClock(datetime.now(UTC))
    return TokenService(SECRET, ttl_minutes, clock), clock


def test_member_token_round_trips_with_its_scope() -> None:
    tokens, _ = make_tokens()
    principal = Principal(uuid4(), Role.MEMBER, chef_id=uuid4(), team_id=uuid4())
    assert tokens.decode(tokens.issue(principal)) == principal


def test_staff_token_has_no_team_scope() -> None:
    tokens, _ = make_tokens()
    principal = Principal(uuid4(), Role.CHEF)
    decoded = tokens.decode(tokens.issue(principal))
    assert decoded.team_id is None
    assert decoded.chef_id is None


def test_expired_token_is_refused() -> None:
    tokens, clock = make_tokens(ttl_minutes=5)
    clock.moment = clock.moment - timedelta(days=1)
    token = tokens.issue(Principal(uuid4(), Role.CHEF))
    with pytest.raises(AppError) as error:
        tokens.decode(token)
    assert error.value.code is ErrorCode.UNAUTHENTICATED


def test_token_signed_with_another_secret_is_refused() -> None:
    tokens, _ = make_tokens()
    forged = jwt.encode({"sub": str(uuid4()), "role": "super_admin"}, "y" * 32, algorithm="HS256")
    with pytest.raises(AppError):
        tokens.decode(forged)


def test_garbage_token_is_refused() -> None:
    tokens, _ = make_tokens()
    with pytest.raises(AppError):
        tokens.decode("not-a-token")


def test_answer_signer_is_bound_to_the_activity() -> None:
    signer = AnswerSigner("secret")
    activity_id = uuid4()
    digest = signer.digest(activity_id, "SOS")
    assert signer.matches(digest, activity_id, "SOS")
    assert not signer.matches(digest, activity_id, "FEU")
    assert not signer.matches(digest, uuid4(), "SOS")


def test_sealed_solution_can_be_read_back_but_not_with_another_key() -> None:
    sealer = SolutionSealer(Fernet.generate_key().decode())
    sealed = sealer.seal("Rendez-vous au grand chêne")
    assert sealer.unseal(sealed) == "Rendez-vous au grand chêne"
    other = SolutionSealer(Fernet.generate_key().decode())
    with pytest.raises(AppError):
        other.unseal(sealed)


def test_access_codes_are_unique_and_hashed_deterministically() -> None:
    codes = AccessCodeService()
    first, second = codes.generate(), codes.generate()
    assert first != second
    assert len(first) >= 16
    assert codes.digest(first) == codes.digest(first)
    assert codes.digest(first) != codes.digest(second)
