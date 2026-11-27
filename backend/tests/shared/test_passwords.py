from app.shared.security import PasswordService


def test_password_round_trip() -> None:
    service = PasswordService()
    stored = service.hash("correct horse battery staple")
    assert service.verify(stored, "correct horse battery staple")
    assert not service.verify(stored, "wrong password value")


def test_unknown_user_never_verifies() -> None:
    assert not PasswordService().verify(None, "anything at all")
