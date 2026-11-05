from fastapi import FastAPI
from pydantic import BaseModel

app = FastAPI(title="Manufacturing Agent", version="0.1.0")


class HealthResponse(BaseModel):
    status: str
    service: str


@app.get("/health", response_model=HealthResponse)
def health() -> HealthResponse:
    # Liveness must work without a model, backend, or production data.
    return HealthResponse(status="healthy", service="manufacturing-agent")
