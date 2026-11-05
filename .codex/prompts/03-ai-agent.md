
Implement Phase 3.

Read AGENTS.md and inspect existing API contracts.

GOAL:
Implement a read-only manufacturing analysis
assistant using Python, LangGraph and Ollama.

ARCHITECTURE:
React -> ASP.NET Core -> FastAPI Agent
Agent -> approved ASP.NET Core read-only endpoints
Agent -> Ollama

TOOLS:
- get_production_metrics
- get_quality_trend
- get_equipment_alarms
- get_defect_statistics

REQUIREMENTS:
1. Define typed tool input schemas with Pydantic.
2. Validate production line IDs and time ranges.
3. Use an allowlist of backend API endpoints.
4. Never allow arbitrary SQL execution.
5. Never allow arbitrary HTTP URLs or shell commands.
6. Use request timeouts and bounded retries.
7. Implement bounded agent execution steps.
8. Add structured logs with correlation IDs.
9. Implement German-language responses.
10. Return structured diagnostic results.

OUTPUT SCHEMA:
- question
- line_id
- time_range
- observed_facts
- possible_causes
- recommended_checks
- evidence
- limitations

Each fact must include source information.
Hypotheses must be labeled as hypotheses.

FRONTEND:
- AI Chat panel
- Loading and error states
- Diagnostic answer sections
- Evidence references
- Display tool errors clearly

DEMO QUESTION:
"Warum ist die First Pass Yield der Linie 3
heute gesunken?"

In demo mode, "heute" must refer to
the configured demo date.

IMPORTANT:
Provide a deterministic analytics fallback when
Ollama is unavailable or the model cannot call tools.

Do not present fallback output as LLM-generated.
Do not claim that an alarm caused a defect.

TESTS:
- Tool argument validation
- Unauthorized tool requests
- Backend API failures
- Missing data
- Malformed model responses
- Deterministic fallback
- Evidence consistency

ACCEPTANCE:
The assistant produces a useful evidence-based
German analysis of the seeded Line 3 scenario.

Run tests and report results.
