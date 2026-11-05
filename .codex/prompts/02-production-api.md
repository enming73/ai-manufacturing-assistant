
Implement Phase 2.

Read AGENTS.md and inspect the existing code.

GOAL:
Build a realistic synthetic SMT manufacturing
data model and read-only quality analytics API.

DOMAIN:
- ProductionLine
- Machine
- ProductionBatch
- QualityInspection
- EquipmentAlarm
- ProductionMetric

REQUIREMENTS:
1. Use EF Core and PostgreSQL.
2. Implement database migrations.
3. Implement reproducible synthetic data seeding.
4. Use a fixed demo date: 2026-10-15.
5. Model realistic relationships between production
   lines, machines, batches and inspections.
6. Include SPI and AOI inspection measurements.
7. Include machine alarm events.
8. Define FPY as first-pass accepted units divided
   by eligible units, using explicit counting rules.
9. Avoid duplicate counting and division by zero.
10. Provide time-window and production-line filters.

REST ENDPOINTS:
- GET /api/lines
- GET /api/lines/{id}/metrics
- GET /api/lines/{id}/quality-trend
- GET /api/lines/{id}/alarms
- GET /api/lines/{id}/defects
- GET /api/lines/{id}/batches

SCENARIO:
For Line 3, create a reproducible decline in FPY
from approximately 98.5% to 94.2%.

Also create:
- Increasing solder defects in AOI results
- Abnormal solder paste measurements in SPI
- Nozzle alarms on machine M03

Make it clear that these are correlated observations,
not a proven causal chain.

FRONTEND:
- Line selector
- FPY KPI
- Quality trend chart
- Defect distribution
- Equipment alarm table
- Visible demo date

TESTS:
- FPY calculation
- Empty time window
- Duplicate inspections
- Invalid date range
- Line not found
- API integration tests

ACCEPTANCE:
The dashboard must display consistent values
derived from seeded database records.

Do not hardcode KPI values in the frontend.
Do not implement AI yet.

Run tests and report results.
