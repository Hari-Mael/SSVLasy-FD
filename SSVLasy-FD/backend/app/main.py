from collections.abc import AsyncIterator
from contextlib import asynccontextmanager
from typing import cast

from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse

from app.modules.access.api.router import router as access_router
from app.modules.activities.api.router import router as activities_router
from app.modules.camp_life.api.router import router as camp_life_router
from app.modules.campaigns.api.router import router as campaigns_router
from app.modules.teams.api.router import router as teams_router
from app.shared.config import get_settings
from app.shared.container import build_container
from app.shared.errors import AppError


@asynccontextmanager
async def lifespan(application: FastAPI) -> AsyncIterator[None]:
    container = await build_container(get_settings())
    application.state.container = container
    try:
        yield
    finally:
        await container.database.close()


async def handle_app_error(_request: Request, error: Exception) -> JSONResponse:
    app_error = cast(AppError, error)
    return JSONResponse(
        status_code=app_error.status_code,
        content={"error": {"code": app_error.code.value, "message": app_error.message}},
    )


def create_app() -> FastAPI:
    application = FastAPI(title="Scout Camp API", version="0.1.0", lifespan=lifespan)
    application.add_exception_handler(AppError, handle_app_error)
    for router in (
        access_router,
        teams_router,
        campaigns_router,
        activities_router,
        camp_life_router,
    ):
        application.include_router(router)

    @application.get("/healthz")
    async def healthz() -> dict[str, str]:
        return {"status": "ok"}

    return application


app = create_app()
