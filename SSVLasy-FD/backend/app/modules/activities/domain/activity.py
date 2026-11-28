from dataclasses import dataclass
from datetime import date, time
from enum import StrEnum
from typing import Final
from uuid import UUID

from app.modules.activities.domain.ciphers import CipherSpec
from app.modules.campaigns.domain.state import CampaignState
from app.shared.errors import AppError

DAY_END: Final = time(17, 30)
NIGHT_START: Final = time(21, 0)


class ActivityStatus(StrEnum):
    DRAFT = "draft"
    PUBLISHED = "published"
    UNLOCKED = "unlocked"


STATUS_FLOW: Final[dict[ActivityStatus, ActivityStatus]] = {
    ActivityStatus.DRAFT: ActivityStatus.PUBLISHED,
    ActivityStatus.PUBLISHED: ActivityStatus.UNLOCKED,
}


def advance_status(current: ActivityStatus, target: ActivityStatus) -> ActivityStatus:
    if STATUS_FLOW.get(current) is not target:
        raise AppError.invalid_state(
            f"Passage de l'activité de « {current.value} » à « {target.value} » impossible"
        )
    return target


@dataclass(frozen=True, slots=True)
class Activity:
    id: UUID
    campaign_id: UUID
    title: str
    day: int
    slot_start: time
    slot_end: time
    points: int
    cipher: CipherSpec
    encoded_text: str
    status: ActivityStatus

    @property
    def is_night(self) -> bool:
        return self.slot_start >= NIGHT_START


@dataclass(frozen=True, slots=True)
class ActivityInput:
    title: str
    day: int
    slot_start: time
    slot_end: time
    points: int
    cipher: CipherSpec
    plain_text: str


@dataclass(frozen=True, slots=True)
class ActivityDraft:
    id: UUID
    title: str
    day: int
    slot_start: time
    slot_end: time
    points: int
    cipher: CipherSpec
    encoded_text: str
    answer_digest: str
    solution_sealed: bytes


@dataclass(frozen=True, slots=True)
class CampaignSchedule:
    owner_id: UUID
    state: CampaignState
    start_date: date
    days: int
    timezone: str
