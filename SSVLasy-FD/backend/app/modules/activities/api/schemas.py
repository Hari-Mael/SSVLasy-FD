from datetime import datetime, time
from uuid import UUID

from pydantic import BaseModel, Field

from app.modules.activities.application.management import ActivityCard
from app.modules.activities.application.play import SubmissionResult
from app.modules.activities.domain.activity import Activity, ActivityStatus
from app.modules.activities.domain.ciphers import CipherKind
from app.modules.activities.domain.progress import AttemptOutcome, Choice, Progress, ProgressStatus


class CreateActivityRequest(BaseModel):
    title: str = Field(min_length=1, max_length=120)
    day: int = Field(ge=1, le=11)
    slot_start: time
    slot_end: time
    points: int = Field(ge=1, le=1000)
    cipher: CipherKind
    cipher_shift: int | None = None
    cipher_key: str | None = Field(default=None, max_length=64)
    plain_text: str = Field(min_length=1, max_length=500)


class AnswerRequest(BaseModel):
    answer: str = Field(min_length=1, max_length=500)


class ChoiceRequest(BaseModel):
    choice: Choice


class ProgressResponse(BaseModel):
    status: ProgressStatus
    attempts_used: int
    running: bool
    deadline_at: datetime | None
    awarded_points: int

    @classmethod
    def from_domain(cls, progress: Progress) -> "ProgressResponse":
        return cls(
            status=progress.status,
            attempts_used=progress.attempts_used,
            running=progress.running,
            deadline_at=progress.deadline_at,
            awarded_points=progress.awarded_points,
        )


class SubmissionResponse(BaseModel):
    outcome: AttemptOutcome
    progress: ProgressResponse

    @classmethod
    def from_domain(cls, result: SubmissionResult) -> "SubmissionResponse":
        return cls(outcome=result.outcome, progress=ProgressResponse.from_domain(result.progress))


class ActivityResponse(BaseModel):
    id: UUID
    campaign_id: UUID
    title: str
    day: int
    slot_start: time
    slot_end: time
    points: int
    cipher: CipherKind
    status: ActivityStatus
    encoded_text: str | None
    progress: ProgressResponse | None

    @classmethod
    def from_card(cls, card: ActivityCard) -> "ActivityResponse":
        activity = card.activity
        return cls(
            id=activity.id,
            campaign_id=activity.campaign_id,
            title=activity.title,
            day=activity.day,
            slot_start=activity.slot_start,
            slot_end=activity.slot_end,
            points=activity.points,
            cipher=activity.cipher.kind,
            status=activity.status,
            encoded_text=card.encoded_text,
            progress=ProgressResponse.from_domain(card.progress) if card.progress else None,
        )

    @classmethod
    def from_activity(cls, activity: Activity) -> "ActivityResponse":
        return cls.from_card(ActivityCard(activity, activity.encoded_text, None))


class SolutionResponse(BaseModel):
    plain_text: str
