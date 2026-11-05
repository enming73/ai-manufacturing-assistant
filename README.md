# AI Manufacturing Assistant

A standalone SMT troubleshooting demonstration using only synthetic data. Not affiliated with any employer. Phase 1 provides a runnable service foundation; diagnosis and production datasets are not implemented.

## Start with Docker

Requires Docker Engine with Compose v2+ and internet access for the first image build. No local SDKs, GPU, or Ollama model are needed.

```bash
cp .env.example .env
# Edit .env: set POSTGRES_PASSWORD to a unique password (hex characters recommended).
docker compose config -q
docker compose up --build --wait --wait-timeout 180
```

If your Docker configuration references a missing `docker-credential-desktop.exe`, fix your host credential-helper configuration or use a temporary configuration for these public images:

```bash
mkdir -p /tmp/manufacturing-docker-config
docker --config /tmp/manufacturing-docker-config compose up --build --wait --wait-timeout 180
```

Open <http://localhost:5173>. The German status page fetches `/api/health` through nginx and shows actual API/database readiness. Host ports bind only to loopback. Override `FRONTEND_PORT`, `BACKEND_PORT`, and `AGENT_PORT` in `.env` if needed. PostgreSQL and Ollama are internal services with no published ports.

```bash
curl --fail http://localhost:8080/api/health
curl --fail http://localhost:5173/api/health
curl --fail http://localhost:8000/health
docker compose exec postgres psql -U manufacturing -d manufacturing -c "SELECT extversion FROM pg_extension WHERE extname = 'vector';"
docker compose ps
docker compose logs backend agent
```

API health returns HTTP 200 with `status: healthy` and `checks.postgres: healthy` after a successful `SELECT 1`; dependency failure returns 503 with a sanitized body. Agent health is liveness only and never calls Ollama. Ollama's health check lists installed models without downloading any.

```bash
docker compose down
# Destructive reset of demonstration volumes, only if desired:
docker compose down --volumes
```

The vector extension initialization runs only on first creation of the database volume. Phase 1 has no domain tables, migrations, seeded measurements, or fixed-date scenario yet.

## Local development and tests

Use the .NET SDK from `global.json`, Node 24.16.0, Python 3.12, and uv 0.10.9.

```bash
dotnet test backend/Manufacturing.slnx --configuration Release
cd frontend
npm ci
npm test
npm run build
cd ../agent
uv sync --frozen
uv run --frozen pytest
```

To develop with a backend running on the host, set `ConnectionStrings__Postgres` to a reachable PostgreSQL connection string before `dotnet run --project backend/src/Manufacturing.Api --urls http://localhost:8080`. Compose does not publish PostgreSQL; provide your own local PostgreSQL or an explicit local Compose port override. `npm run dev` proxies `/api` to localhost:8080. `uv run uvicorn app.main:app --port 8000` starts agent liveness independently from the `agent` directory.

CI runs all three test suites, the frontend production build, and a five-service Compose smoke test. See [architecture](docs/architecture.md) for boundaries and version choices.
