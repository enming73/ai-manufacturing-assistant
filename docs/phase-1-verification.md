# Phase 1 verification

Executed locally during implementation. The host had Node 24.16.0, Python 3.12.3, Docker Engine 28.1.1, and Compose 5.1.4. A temporary .NET 10.0.403 SDK was installed under `/tmp`; temporary SDK/package cache directories were used because the home directory is read-only in the execution sandbox.

| Command | Result |
| --- | --- |
| `DOTNET_CLI_HOME=/tmp/manufacturing-dotnet-home NUGET_PACKAGES=/tmp/manufacturing-nuget /tmp/manufacturing-dotnet/dotnet test backend/Manufacturing.slnx --configuration Release --no-restore` | 2 passed, 0 failed, 0 skipped |
| `npm test` (frontend) | 3 passed, 0 failed |
| `npm run build` (frontend) | TypeScript and Vite production build succeeded |
| `npm audit` (frontend) | 0 vulnerabilities |
| `uv run --project agent --frozen --cache-dir /tmp/manufacturing-uv-cache pytest agent/tests` | 1 passed, no warnings |
| `docker --config /tmp/manufacturing-docker-config compose config -q` | Exit 0 |
| `docker --config /tmp/manufacturing-docker-config compose up --build --wait --wait-timeout 180` | Exit 0; all five services healthy |
| `curl --fail --silent http://localhost:8080/api/health` | HTTP 200; API and PostgreSQL healthy |
| `curl --fail --silent http://localhost:5173/api/health` | HTTP 200; same healthy API response through frontend proxy |
| `curl --fail --silent http://localhost:8000/health` | HTTP 200; agent healthy |
| `curl --fail --silent http://localhost:5173/` | HTTP 200; frontend HTML with built JS/CSS assets |
| `docker --config /tmp/manufacturing-docker-config compose exec -T postgres psql -U manufacturing -d manufacturing -c "SELECT extversion FROM pg_extension WHERE extname = 'vector';"` | pgvector 0.8.2 installed |
| `docker --config /tmp/manufacturing-docker-config compose exec -T ollama ollama list` | Empty model list; no model download needed |
| `git diff --check` | Exit 0 |

The temporary Docker configuration bypasses the host's missing Windows credential helper without changing the host configuration. A generated database password is stored only in the ignored local `.env`. Services remain running for local review.

The initial runs exposed backend test configuration timing and a missing frontend CSS declaration; both were fixed before the successful runs above. Python pins were subsequently updated and their test rerun without the initial deprecation warning. CI was added but has not been executed on GitHub. Frontend verification used component tests and HTTP smoke checks, not an automated browser session. AI diagnosis, manufacturing data, and domain migrations remain intentionally deferred to later phases.
