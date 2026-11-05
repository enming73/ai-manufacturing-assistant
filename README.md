# AI Manufacturing Assistant

A standalone SMT troubleshooting demonstration using only synthetic data. Not affiliated with any employer. Phase 2 adds a synthetic production dataset, read-only quality analytics API, and a German dashboard. AI diagnosis is not implemented.

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

Open <http://localhost:5173>. The German dashboard reads production analytics through nginx. Choose a line and UTC time window to inspect FPY, SPI anomalies, AOI defects, machine alarms and batch evidence. The visible demo date is **2026-10-15**. Host ports bind only to loopback. Override `FRONTEND_PORT`, `BACKEND_PORT`, and `AGENT_PORT` in `.env` if needed. PostgreSQL and Ollama are internal services with no published ports.

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

Compose applies the checked-in EF Core migration and transactionally seeds the deterministic scenario, including on an existing Phase 1 database volume. Repeated starts do not duplicate records. Line 3 falls from **98.5% to 94.2% hourly FPY**; its whole-window FPY is **96.3333%**. Increasing SPI anomalies, AOI solder defects and M03 nozzle alarms are correlated synthetic observations, not a proven cause.

Set `DATABASE_APPLY_MIGRATIONS=false` and `DEMO_SEED_ON_STARTUP=false` to disable automatic initialization. Read the [production model, FPY rules and API contract](docs/production-api.md) for details.

## Local development and tests

Use the .NET SDK from `global.json`, Node 24.16.0, Python 3.12, and uv 0.10.9. `dotnet tool restore` installs the pinned EF migration CLI.

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

To develop with a backend running on the host, set `ConnectionStrings__Postgres` to a reachable PostgreSQL connection string before `Database__ApplyMigrations=true Demo__SeedOnStartup=true dotnet run --project backend/src/Manufacturing.Api --urls http://localhost:8080` for a fresh synthetic demo database. Compose does not publish PostgreSQL; provide your own local PostgreSQL or an explicit local Compose port override. `npm run dev` proxies `/api` to localhost:8080. `uv run uvicorn app.main:app --port 8000` starts agent liveness independently from the `agent` directory.

Real PostgreSQL integration tests require `TEST_POSTGRES_CONNECTION`; without it they are explicitly skipped. Start the isolated local test database with `docker compose --env-file .env -f compose.test.yaml up -d --wait`, then follow the connection setup in [database tests](docs/production-api.md#tests). Tests create and drop a uniquely named temporary database, leaving the application demo database untouched.

After starting the application, verify all production endpoints and seeded totals:

```bash
python3 scripts/smoke-api.py
```

CI provides its own PostgreSQL service and runs all three test suites (including database integration tests), the frontend production build, and a five-service Compose smoke test with analytics consistency checks. See [architecture](docs/architecture.md) for boundaries and version choices.

Local Phase 2 test/build/Compose/browser results are recorded in [verification notes](docs/phase-2-verification.md).
