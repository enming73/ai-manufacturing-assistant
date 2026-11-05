
Implement Phase 1 of the AI Manufacturing Assistant.

Read AGENTS.md before making changes.

GOAL:
Create a runnable monorepo with a minimal vertical slice.

TASKS:
1. Create a .NET solution with:
   - Manufacturing.Api
   - Manufacturing.Application
   - Manufacturing.Domain
   - Manufacturing.Infrastructure
   - Manufacturing.Api.Tests

2. Create React + TypeScript + Vite frontend.

3. Create Python FastAPI agent service.

4. Create PostgreSQL with pgvector extension.

5. Create Dockerfiles and compose.yaml for:
   - frontend
   - backend
   - agent
   - postgres
   - ollama

6. Add health endpoints:
   - GET /api/health
   - GET /health on agent

7. Add environment configuration and .env.example.

8. Implement a minimal frontend page that fetches
   backend health status.

9. Add initial README and architecture documentation.

10. Add minimal tests and GitHub Actions CI.

ACCEPTANCE CRITERIA:
- docker compose config succeeds.
- docker compose up --build starts the services.
- Frontend is accessible from the browser.
- Frontend can reach the .NET API.
- Backend connects to PostgreSQL.
- Agent health endpoint works.
- No Ollama model download is required for health checks.
- All available tests pass.

Do not implement AI diagnosis yet.
Do not add real manufacturing data.

Run tests and report exact results.
