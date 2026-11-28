import pytest

from app.modules.campaigns.domain.state import CampaignState, ensure_transition
from app.shared.errors import AppError

VALID = [
    (CampaignState.DRAFT, CampaignState.SCHEDULED),
    (CampaignState.SCHEDULED, CampaignState.RUNNING),
    (CampaignState.RUNNING, CampaignState.PAUSED),
    (CampaignState.PAUSED, CampaignState.RUNNING),
    (CampaignState.RUNNING, CampaignState.FINISHED),
    (CampaignState.PAUSED, CampaignState.FINISHED),
]

INVALID = [
    (CampaignState.DRAFT, CampaignState.RUNNING),
    (CampaignState.SCHEDULED, CampaignState.PAUSED),
    (CampaignState.FINISHED, CampaignState.RUNNING),
    (CampaignState.RUNNING, CampaignState.DRAFT),
]


@pytest.mark.parametrize(("current", "target"), VALID)
def test_valid_transitions_are_accepted(current: CampaignState, target: CampaignState) -> None:
    ensure_transition(current, target)


@pytest.mark.parametrize(("current", "target"), INVALID)
def test_invalid_transitions_are_refused(current: CampaignState, target: CampaignState) -> None:
    with pytest.raises(AppError):
        ensure_transition(current, target)
