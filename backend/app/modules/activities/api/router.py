from typing import Annotated
from uuid import UUID

from fastapi import APIRouter, Depends

from app.modules.activities.api.dependencies import (
    change_activity_status,
    choose_after_failure,
    create_activity,
    list_activities,
    read_progress,
    reveal_solution,
    start_attempt,
    submit_answer,
)
from app.modules.activities.api.schemas import (
    ActivityResponse,
    AnswerRequest,
    ChoiceRequest,
    CreateActivityRequest,
    ProgressResponse,
    SolutionResponse,
    SubmissionResponse,
)
from app.modules.activities.application.management import (
    ChangeActivityStatus,
    CreateActivity,
    ListActivities,
    RevealSolution,
)
from app.modules.activities.application.play import (
    ChooseAfterFailure,
    ReadProgress,
    StartAttempt,
    SubmitAnswer,
)
from app.modules.activities.domain.activity import ActivityInput, ActivityStatus
from app.modules.activities.domain.ciphers import CipherSpec
from app.shared.api.dependencies import MemberDep, PrincipalDep, requires
from app.shared.principal import Role

router = APIRouter(prefix="/api/v1", tags=["activities"])
STAFF = requires(Role.CHEF, Role.SUPER_ADMIN)


@router.post(
    "/campaigns/{campaign_id}/activities",
    response_model=ActivityResponse,
    status_code=201,
    dependencies=[STAFF],
)
async def post_activity(
    campaign_id: UUID,
    body: CreateActivityRequest,
    use_case: Annotated[CreateActivity, Depends(create_activity)],
) -> ActivityResponse:
    activity = await use_case.execute(
        campaign_id,
        ActivityInput(
            title=body.title,
            day=body.day,
            slot_start=body.slot_start,
            slot_end=body.slot_end,
            points=body.points,
            cipher=CipherSpec(body.cipher, body.cipher_shift, body.cipher_key),
            plain_text=body.plain_text,
        ),
    )
    return ActivityResponse.from_activity(activity)


@router.get("/campaigns/{campaign_id}/activities", response_model=list[ActivityResponse])
async def get_activities(
    campaign_id: UUID,
    principal: PrincipalDep,
    use_case: Annotated[ListActivities, Depends(list_activities)],
) -> list[ActivityResponse]:
    cards = await use_case.execute(principal, campaign_id)
    return [ActivityResponse.from_card(card) for card in cards]


@router.post(
    "/activities/{activity_id}/publish", response_model=ActivityResponse, dependencies=[STAFF]
)
async def publish_activity(
    activity_id: UUID,
    use_case: Annotated[ChangeActivityStatus, Depends(change_activity_status)],
) -> ActivityResponse:
    activity = await use_case.execute(activity_id, ActivityStatus.PUBLISHED)
    return ActivityResponse.from_activity(activity)


@router.post(
    "/activities/{activity_id}/unlock", response_model=ActivityResponse, dependencies=[STAFF]
)
async def unlock_activity(
    activity_id: UUID,
    use_case: Annotated[ChangeActivityStatus, Depends(change_activity_status)],
) -> ActivityResponse:
    activity = await use_case.execute(activity_id, ActivityStatus.UNLOCKED)
    return ActivityResponse.from_activity(activity)


@router.get(
    "/activities/{activity_id}/solution", response_model=SolutionResponse, dependencies=[STAFF]
)
async def get_solution(
    activity_id: UUID, use_case: Annotated[RevealSolution, Depends(reveal_solution)]
) -> SolutionResponse:
    return SolutionResponse(plain_text=await use_case.execute(activity_id))


@router.post("/activities/{activity_id}/attempts", response_model=ProgressResponse)
async def post_attempt(
    activity_id: UUID, member: MemberDep, use_case: Annotated[StartAttempt, Depends(start_attempt)]
) -> ProgressResponse:
    return ProgressResponse.from_domain(await use_case.execute(member, activity_id))


@router.post("/activities/{activity_id}/answers", response_model=SubmissionResponse)
async def post_answer(
    activity_id: UUID,
    body: AnswerRequest,
    member: MemberDep,
    use_case: Annotated[SubmitAnswer, Depends(submit_answer)],
) -> SubmissionResponse:
    return SubmissionResponse.from_domain(await use_case.execute(member, activity_id, body.answer))


@router.post("/activities/{activity_id}/choices", response_model=ProgressResponse)
async def post_choice(
    activity_id: UUID,
    body: ChoiceRequest,
    member: MemberDep,
    use_case: Annotated[ChooseAfterFailure, Depends(choose_after_failure)],
) -> ProgressResponse:
    return ProgressResponse.from_domain(await use_case.execute(member, activity_id, body.choice))


@router.get("/activities/{activity_id}/progress", response_model=ProgressResponse)
async def get_progress(
    activity_id: UUID, member: MemberDep, use_case: Annotated[ReadProgress, Depends(read_progress)]
) -> ProgressResponse:
    return ProgressResponse.from_domain(await use_case.execute(member, activity_id))
