from app.modules.campaigns.application.use_cases import (
    CreateCampaign,
    ListCampaigns,
    TransitionCampaign,
)
from app.modules.campaigns.infrastructure.repositories import (
    PostgresCampaignRepository,
    PostgresStaffCensus,
)
from app.shared.api.dependencies import AuthenticatedConnection, ContainerDep


def create_campaign(connection: AuthenticatedConnection, container: ContainerDep) -> CreateCampaign:
    return CreateCampaign(
        PostgresCampaignRepository(connection), container.settings.default_timezone
    )


def list_campaigns(connection: AuthenticatedConnection) -> ListCampaigns:
    return ListCampaigns(PostgresCampaignRepository(connection))


def transition_campaign(
    connection: AuthenticatedConnection, container: ContainerDep
) -> TransitionCampaign:
    return TransitionCampaign(
        PostgresCampaignRepository(connection),
        PostgresStaffCensus(container.database),
        container.clock,
    )
