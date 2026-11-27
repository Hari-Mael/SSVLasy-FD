from dataclasses import dataclass

from app.shared.clock import Clock, SystemClock
from app.shared.config import Settings
from app.shared.database import Database
from app.shared.security import (
    AccessCodeService,
    AnswerSigner,
    PasswordService,
    SolutionSealer,
    TokenService,
)


@dataclass(frozen=True, slots=True)
class Container:
    settings: Settings
    database: Database
    clock: Clock
    passwords: PasswordService
    access_codes: AccessCodeService
    tokens: TokenService
    signer: AnswerSigner
    sealer: SolutionSealer


async def build_container(settings: Settings) -> Container:
    clock = SystemClock()
    return Container(
        settings=settings,
        database=await Database.connect(settings.database_url),
        clock=clock,
        passwords=PasswordService(),
        access_codes=AccessCodeService(),
        tokens=TokenService(
            settings.jwt_secret.get_secret_value(), settings.jwt_ttl_minutes, clock
        ),
        signer=AnswerSigner(settings.answer_hmac_secret.get_secret_value()),
        sealer=SolutionSealer(settings.content_encryption_key.get_secret_value()),
    )
