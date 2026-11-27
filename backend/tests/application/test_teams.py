from uuid import uuid4

import pytest

from app.modules.teams.application.use_cases import AddMember, CreateTeam, ListMembers
from app.shared.errors import AppError, ErrorCode
from app.shared.principal import Principal, Role
from app.shared.quotas import MEMBERS_PER_TEAM, TEAMS_PER_CHEF
from app.shared.security import PasswordService
from tests.application.fakes import FakeMemberRepository, FakeTeamRepository

CHEF = Principal(id=uuid4(), role=Role.CHEF)


async def test_chef_cannot_exceed_the_team_quota() -> None:
    use_case = CreateTeam(FakeTeamRepository())
    for index in range(TEAMS_PER_CHEF):
        await use_case.execute(CHEF, f"Équipe {index}")
    with pytest.raises(AppError) as error:
        await use_case.execute(CHEF, "Une de trop")
    assert error.value.code is ErrorCode.QUOTA_EXCEEDED


async def test_team_quota_is_counted_per_chef() -> None:
    teams = FakeTeamRepository()
    for index in range(TEAMS_PER_CHEF):
        await CreateTeam(teams).execute(CHEF, f"Équipe {index}")
    other = Principal(id=uuid4(), role=Role.CHEF)
    assert (await CreateTeam(teams).execute(other, "Autre")).chef_id == other.id


async def test_team_cannot_exceed_the_member_quota() -> None:
    teams = FakeTeamRepository()
    team = await CreateTeam(teams).execute(CHEF, "Loups")
    use_case = AddMember(teams, FakeMemberRepository(), PasswordService())
    for index in range(MEMBERS_PER_TEAM):
        await use_case.execute(team.id, f"Scout {index}", "motdepasse-long")
    with pytest.raises(AppError) as error:
        await use_case.execute(team.id, "Un de trop", "motdepasse-long")
    assert error.value.code is ErrorCode.QUOTA_EXCEEDED


async def test_member_account_stores_normalized_login_and_a_hashed_password() -> None:
    teams = FakeTeamRepository()
    members = FakeMemberRepository()
    passwords = PasswordService()
    team = await CreateTeam(teams).execute(CHEF, "Loups")
    enrolled = await AddMember(teams, members, passwords).execute(
        team.id, "  Renard  Rouge ", "motdepasse-long"
    )
    assert enrolled.member.pseudonym == "Renard Rouge"
    assert members.logins == ["renard rouge"]
    assert members.password_hashes[0] != "motdepasse-long"
    assert passwords.verify(members.password_hashes[0], "motdepasse-long")


async def test_member_password_must_be_long_enough() -> None:
    teams = FakeTeamRepository()
    team = await CreateTeam(teams).execute(CHEF, "Loups")
    with pytest.raises(AppError):
        await AddMember(teams, FakeMemberRepository(), PasswordService()).execute(
            team.id, "Renard", "court"
        )


async def test_unknown_team_is_reported() -> None:
    teams = FakeTeamRepository()
    with pytest.raises(AppError) as error:
        await AddMember(teams, FakeMemberRepository(), PasswordService()).execute(
            uuid4(), "X", "motdepasse-long"
        )
    assert error.value.code is ErrorCode.NOT_FOUND
    with pytest.raises(AppError):
        await ListMembers(teams, FakeMemberRepository()).execute(uuid4())
