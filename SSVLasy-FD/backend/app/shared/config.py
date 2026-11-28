from functools import lru_cache

from pydantic import Field, SecretStr
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", env_prefix="SCOUT_", extra="ignore")

    database_url: str
    migration_database_url: str | None = None
    jwt_secret: SecretStr
    jwt_ttl_minutes: int = Field(default=720, ge=5)
    answer_hmac_secret: SecretStr
    content_encryption_key: SecretStr
    default_timezone: str = "Indian/Antananarivo"


@lru_cache
def get_settings() -> Settings:
    return Settings()
