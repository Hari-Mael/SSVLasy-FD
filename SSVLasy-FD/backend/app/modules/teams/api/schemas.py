from uuid import UUID

from pydantic import BaseModel, Field

from app.modules.teams.domain.models import EnrolledMember, Member, Team


class CreateTeamRequest(BaseModel):
    name: str = Field(min_length=1, max_length=60)


class TeamResponse(BaseModel):
    id: UUID
    chef_id: UUID
    name: str

    @classmethod
    def from_domain(cls, team: Team) -> "TeamResponse":
        return cls(id=team.id, chef_id=team.chef_id, name=team.name)


class AddMemberRequest(BaseModel):
    pseudonym: str = Field(min_length=1, max_length=40)
    password: str = Field(min_length=8, max_length=256)


class MemberResponse(BaseModel):
    id: UUID
    team_id: UUID
    pseudonym: str

    @classmethod
    def from_domain(cls, member: Member) -> "MemberResponse":
        return cls(id=member.id, team_id=member.team_id, pseudonym=member.pseudonym)


class EnrolledMemberResponse(MemberResponse):
    @classmethod
    def from_enrollment(cls, enrolled: EnrolledMember) -> "EnrolledMemberResponse":
        return cls(
            id=enrolled.member.id,
            team_id=enrolled.member.team_id,
            pseudonym=enrolled.member.pseudonym,
        )
