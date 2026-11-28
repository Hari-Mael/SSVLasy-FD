from collections.abc import Callable
from dataclasses import dataclass
from uuid import UUID

from app.modules.camp_life.application.ports import CampCampaign, CampRepository
from app.modules.camp_life.domain.rules import (
    MAX_DAYS,
    CampType,
    Resource,
    TeamStatus,
    Weather,
    collect,
    end_of_day,
    sicken_if_overdue,
    status,
    unblock,
)
from app.shared.clock import Clock
from app.shared.errors import AppError
from app.shared.principal import Principal, Role


@dataclass(frozen=True, slots=True)
class CampView:
    campaign_id: UUID
    day: int
    weather: Weather
    camp_type: CampType
    wood: int
    water: int
    food: int
    status: TeamStatus
    actions_done: list[Resource]


def _may_manage(principal: Principal, campaign: CampCampaign) -> bool:
    return principal.role is Role.SUPER_ADMIN or (
        principal.role is Role.CHEF and principal.id == campaign.owner_id
    )


async def _campaign_or_fail(repository: CampRepository, campaign_id: UUID) -> CampCampaign:
    campaign = await repository.campaign(campaign_id)
    if campaign is None:
        raise AppError.not_found("Campagne")
    return campaign


class CollectResource:
    def __init__(self, repository: CampRepository, clock: Clock) -> None:
        self._repository = repository
        self._clock = clock

    async def execute(self, principal: Principal, resource: Resource) -> int:
        team_id = principal.member_team_id()
        owner = await self._repository.team_owner(team_id)
        if owner is None:
            raise AppError.not_found("Équipe")
        campaign = await self._repository.running_campaign_of(owner)
        if campaign is None:
            raise AppError.invalid_state("Aucune campagne en cours")
        done = await self._repository.actions_today(team_id, campaign.id, campaign.current_day)
        if resource in done:
            raise AppError.invalid_state("Cette collecte a déjà été faite aujourd'hui")
        state = await self._repository.state(team_id)
        new_state, gained = collect(state, resource, campaign.camp_type)
        await self._repository.save_state(team_id, new_state)
        await self._repository.add_action(team_id, campaign.id, campaign.current_day, resource)
        return gained


class AdvanceDay:
    def __init__(
        self,
        repository: CampRepository,
        clock: Clock,
        choose_weather: Callable[[], Weather],
    ) -> None:
        self._repository = repository
        self._clock = clock
        self._choose_weather = choose_weather

    async def execute(self, principal: Principal, campaign_id: UUID) -> tuple[int, Weather]:
        campaign = await _campaign_or_fail(self._repository, campaign_id)
        if not _may_manage(principal, campaign):
            raise AppError.forbidden("Réservé au chef ou au Super Admin de la campagne")
        if not campaign.running:
            raise AppError.invalid_state("La campagne n'est pas en cours")
        if campaign.current_day >= campaign.days:
            raise AppError.invalid_state("Dernier jour atteint")
        now = self._clock.now()
        for team_id in await self._repository.team_ids_of_owner(campaign.owner_id):
            result = end_of_day(await self._repository.state(team_id), now)
            await self._repository.save_state(team_id, result.state)
            await self._repository.apply_energy(team_id, result.energy_delta)
        weather = self._choose_weather()
        next_day = campaign.current_day + 1
        await self._repository.set_day(campaign_id, next_day, weather)
        return next_day, weather


class SetWeather:
    def __init__(self, repository: CampRepository) -> None:
        self._repository = repository

    async def execute(self, principal: Principal, campaign_id: UUID, weather: Weather) -> None:
        campaign = await _campaign_or_fail(self._repository, campaign_id)
        if not _may_manage(principal, campaign):
            raise AppError.forbidden("Réservé au chef ou au Super Admin de la campagne")
        await self._repository.set_weather(campaign_id, weather)


class SetCampType:
    def __init__(self, repository: CampRepository) -> None:
        self._repository = repository

    async def execute(self, principal: Principal, campaign_id: UUID, camp_type: CampType) -> None:
        campaign = await _campaign_or_fail(self._repository, campaign_id)
        if not _may_manage(principal, campaign):
            raise AppError.forbidden("Réservé au chef ou au Super Admin de la campagne")
        if campaign.days > MAX_DAYS[camp_type]:
            raise AppError.validation(
                f"Ce type de camp dure au maximum {MAX_DAYS[camp_type]} jours"
            )
        await self._repository.set_camp_type(campaign_id, camp_type)


class UnblockTeam:
    def __init__(self, repository: CampRepository) -> None:
        self._repository = repository

    async def execute(self, principal: Principal, team_id: UUID) -> None:
        owner = await self._repository.team_owner(team_id)
        if owner is None:
            raise AppError.not_found("Équipe")
        if principal.role is not Role.SUPER_ADMIN and principal.id != owner:
            raise AppError.forbidden("Seul le chef de l'équipe ou le Super Admin peut la débloquer")
        await self._repository.save_state(team_id, unblock(await self._repository.state(team_id)))


class ReadMyCamp:
    def __init__(self, repository: CampRepository, clock: Clock) -> None:
        self._repository = repository
        self._clock = clock

    async def execute(self, principal: Principal) -> CampView:
        team_id = principal.member_team_id()
        owner = await self._repository.team_owner(team_id)
        if owner is None:
            raise AppError.not_found("Équipe")
        campaign = await self._repository.running_campaign_of(owner)
        if campaign is None:
            raise AppError.invalid_state("Aucune campagne en cours")
        now = self._clock.now()
        state = await self._repository.state(team_id)
        corrected = sicken_if_overdue(state, now)
        if corrected != state:
            await self._repository.save_state(team_id, corrected)
        return CampView(
            campaign_id=campaign.id,
            day=campaign.current_day,
            weather=campaign.weather,
            camp_type=campaign.camp_type,
            wood=corrected.stocks.wood,
            water=corrected.stocks.water,
            food=corrected.stocks.food,
            status=status(corrected, now),
            actions_done=sorted(
                await self._repository.actions_today(team_id, campaign.id, campaign.current_day),
                key=str,
            ),
        )
