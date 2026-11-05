import asyncio

from httpx import ASGITransport, AsyncClient

from app.main import app


def test_health_without_model_or_backend(monkeypatch):
    monkeypatch.setenv("OLLAMA_BASE_URL", "http://unreachable.invalid")
    monkeypatch.setenv("BACKEND_BASE_URL", "http://unreachable.invalid")

    async def request_health():
        async with AsyncClient(transport=ASGITransport(app=app), base_url="http://test") as client:
            return await client.get("/health")

    response = asyncio.run(request_health())
    assert response.status_code == 200
    assert response.json() == {"status": "healthy", "service": "manufacturing-agent"}
