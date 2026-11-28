from typing import Final

MAX_SUPER_ADMINS: Final = 10
MAX_CHEFS: Final = 100
MIN_CHEFS_TO_SCHEDULE: Final = 6
TEAMS_PER_CHEF: Final = 3
MEMBERS_PER_TEAM: Final = 5
MAX_ACTIVITIES_PER_DAY: Final = 7
MIN_CAMPAIGN_DAYS: Final = 1
MAX_CAMPAIGN_DAYS: Final = 11

TEAM_QUOTA_MESSAGE: Final = f"Un chef ne peut avoir que {TEAMS_PER_CHEF} équipes"
MEMBER_QUOTA_MESSAGE: Final = f"Une équipe ne peut avoir que {MEMBERS_PER_TEAM} membres"
SUPER_ADMIN_QUOTA_MESSAGE: Final = f"Maximum de {MAX_SUPER_ADMINS} super admins atteint"
CHEF_QUOTA_MESSAGE: Final = f"Maximum de {MAX_CHEFS} chefs atteint"
ACTIVITY_DAY_QUOTA_MESSAGE: Final = (
    f"Maximum de {MAX_ACTIVITIES_PER_DAY} activités par jour atteint"
)
