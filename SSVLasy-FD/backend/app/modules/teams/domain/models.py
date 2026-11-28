from dataclasses import dataclass
from uuid import UUID


@dataclass(frozen=True, slots=True)
class Team:
    id: UUID
    chef_id: UUID
    name: str


@dataclass(frozen=True, slots=True)
class Member:
    id: UUID
    team_id: UUID
    pseudonym: str


@dataclass(frozen=True, slots=True)
class EnrolledMember:
    member: Member
