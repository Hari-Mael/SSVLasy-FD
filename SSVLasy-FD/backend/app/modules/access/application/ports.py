from typing import Protocol

from app.modules.access.domain.models import MemberIdentity, NewStaffUser, StaffUser


class StaffUserRepository(Protocol):
    async def find_by_email(self, email: str) -> StaffUser | None: ...

    async def add(self, user: NewStaffUser) -> StaffUser: ...


class MemberIdentityRepository(Protocol):
    async def find_by_login(self, login: str) -> MemberIdentity | None: ...
