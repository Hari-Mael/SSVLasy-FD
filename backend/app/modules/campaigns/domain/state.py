from enum import StrEnum
from typing import Final

from app.shared.errors import AppError


class CampaignState(StrEnum):
    DRAFT = "draft"
    SCHEDULED = "scheduled"
    RUNNING = "running"
    PAUSED = "paused"
    FINISHED = "finished"


ALLOWED_TRANSITIONS: Final[dict[CampaignState, frozenset[CampaignState]]] = {
    CampaignState.DRAFT: frozenset({CampaignState.SCHEDULED}),
    CampaignState.SCHEDULED: frozenset({CampaignState.RUNNING}),
    CampaignState.RUNNING: frozenset({CampaignState.PAUSED, CampaignState.FINISHED}),
    CampaignState.PAUSED: frozenset({CampaignState.RUNNING, CampaignState.FINISHED}),
    CampaignState.FINISHED: frozenset(),
}


def ensure_transition(current: CampaignState, target: CampaignState) -> None:
    if target not in ALLOWED_TRANSITIONS[current]:
        raise AppError.invalid_state(
            f"Passage de la campagne de « {current.value} » à « {target.value} » impossible"
        )
