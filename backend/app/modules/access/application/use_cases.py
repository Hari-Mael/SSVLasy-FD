from dataclasses import dataclass

from app.modules.access.application.ports import MemberIdentityRepository, StaffUserRepository
from app.modules.access.domain.models import MIN_PASSWORD_LENGTH, NewStaffUser, StaffUser
from app.shared.errors import AppError
from app.shared.principal import Principal, Role
from app.shared.rate_limit import LoginLimiter
from app.shared.security import PasswordService, TokenService


@dataclass(frozen=True, slots=True)
class AccessToken:
    value: str
    role: Role


class AuthenticateStaff:
    def __init__(
        self,
        users: StaffUserRepository,
        passwords: PasswordService,
        tokens: TokenService,
        limiter: LoginLimiter,
    ) -> None:
        self._users = users
        self._passwords = passwords
        self._tokens = tokens
        self._limiter = limiter

    async def execute(self, email: str, password: str) -> AccessToken:
        key = "staff:" + email.strip().lower()
        self._limiter.ensure_allowed(key)
        user = await self._users.find_by_email(email.strip().lower())
        password_hash = user.password_hash if user is not None and user.active else None
        if user is None or not self._passwords.verify(password_hash, password):
            self._limiter.record_failure(key)
            raise AppError.unauthenticated("Identifiants invalides")
        self._limiter.reset(key)
        return AccessToken(self._tokens.issue(Principal(id=user.id, role=user.role)), user.role)


class AuthenticateMember:
    def __init__(
        self,
        identities: MemberIdentityRepository,
        passwords: PasswordService,
        tokens: TokenService,
        limiter: LoginLimiter,
    ) -> None:
        self._identities = identities
        self._passwords = passwords
        self._tokens = tokens
        self._limiter = limiter

    async def execute(self, login: str, password: str) -> AccessToken:
        clean_login = " ".join(login.split()).lower()
        key = "member:" + clean_login
        self._limiter.ensure_allowed(key)
        identity = await self._identities.find_by_login(clean_login)
        if identity is None or not self._passwords.verify(identity.password_hash, password):
            self._limiter.record_failure(key)
            raise AppError.unauthenticated("Identifiant ou mot de passe invalide")
        self._limiter.reset(key)
        principal = Principal(
            id=identity.member_id,
            role=Role.MEMBER,
            chef_id=identity.chef_id,
            team_id=identity.team_id,
        )
        return AccessToken(self._tokens.issue(principal), Role.MEMBER)


class CreateStaffUser:
    def __init__(self, users: StaffUserRepository, passwords: PasswordService) -> None:
        self._users = users
        self._passwords = passwords

    async def execute(self, email: str, display_name: str, password: str, role: Role) -> StaffUser:
        if role is Role.MEMBER:
            raise AppError.validation("Un membre n'a pas de compte d'équipe d'encadrement")
        if len(password) < MIN_PASSWORD_LENGTH:
            raise AppError.validation(
                f"Le mot de passe doit contenir au moins {MIN_PASSWORD_LENGTH} caractères"
            )
        return await self._users.add(
            NewStaffUser(
                email=email.strip().lower(),
                display_name=display_name.strip(),
                password_hash=self._passwords.hash(password),
                role=role,
            )
        )
