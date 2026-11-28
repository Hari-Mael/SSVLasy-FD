from typing import Final
from uuid import UUID

from app.modules.teams.application.ports import MemberRepository, TeamRepository
from app.modules.teams.domain.models import EnrolledMember, Member, Team
from app.shared.errors import AppError
from app.shared.principal import Principal
from app.shared.quotas import (
    MEMBER_QUOTA_MESSAGE,
    MEMBERS_PER_TEAM,
    TEAM_QUOTA_MESSAGE,
    TEAMS_PER_CHEF,
)
from app.shared.security import PasswordService


class CreateTeam:
    def __init__(self, teams: TeamRepository) -> None:
        self._teams = teams

    async def execute(self, chef: Principal, name: str) -> Team:
        if await self._teams.count_for_chef(chef.id) >= TEAMS_PER_CHEF:
            raise AppError.quota(TEAM_QUOTA_MESSAGE)
        return await self._teams.add(chef.id, name.strip())


class ListTeams:
    def __init__(self, teams: TeamRepository) -> None:
        self._teams = teams

    async def execute(self) -> list[Team]:
        return await self._teams.list_visible()


MIN_MEMBER_PASSWORD_LENGTH: Final = 8


def member_login(pseudonym: str) -> str:
    return " ".join(pseudonym.split()).lower()


class AddMember:
    def __init__(
        self, teams: TeamRepository, members: MemberRepository, passwords: PasswordService
    ) -> None:
        self._teams = teams
        self._members = members
        self._passwords = passwords

    async def execute(self, team_id: UUID, pseudonym: str, password: str) -> EnrolledMember:
        if await self._teams.get(team_id) is None:
            raise AppError.not_found("Équipe")
        if await self._members.count_in_team(team_id) >= MEMBERS_PER_TEAM:
            raise AppError.quota(MEMBER_QUOTA_MESSAGE)
        clean_name = " ".join(pseudonym.split())
        if not clean_name:
            raise AppError.validation("Le pseudonyme est obligatoire")
        if len(password) < MIN_MEMBER_PASSWORD_LENGTH:
            raise AppError.validation(
                f"Le mot de passe doit contenir au moins {MIN_MEMBER_PASSWORD_LENGTH} caractères"
            )
        member = await self._members.add(
            team_id, clean_name, member_login(clean_name), self._passwords.hash(password)
        )
        return EnrolledMember(member)


class ListMembers:
    def __init__(self, teams: TeamRepository, members: MemberRepository) -> None:
        self._teams = teams
        self._members = members

    async def execute(self, team_id: UUID) -> list[Member]:
        if await self._teams.get(team_id) is None:
            raise AppError.not_found("Équipe")
        return await self._members.list_by_team(team_id)
