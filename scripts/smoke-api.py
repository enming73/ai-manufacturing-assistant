"""Verify seeded production analytics through the running frontend proxy (stdlib only)."""
import json
import sys
from urllib.request import urlopen

base_url = sys.argv[1] if len(sys.argv) > 1 else "http://localhost:5173"


def get(path):
    with urlopen(base_url + path, timeout=15) as response:
        assert response.status == 200, path
        return json.load(response)


lines = get("/api/lines")
assert lines["demoDate"] == "2026-10-15"
assert [line["id"] for line in lines["items"]] == [1, 2, 3]
results = {kind: get("/api/lines/3/" + kind) for kind in
           ("metrics", "quality-trend", "defects", "alarms", "batches")}
assert all(result["lineId"] == 3 and result["window"] == lines["defaultWindow"] for result in results.values())
metrics = results["metrics"]["data"]
trend = results["quality-trend"]["data"]
assert metrics["eligibleUnits"] == 6000
assert metrics["firstPassAcceptedUnits"] == 5780
assert metrics["fpyPercent"] == 96.3333
assert trend[0]["fpyPercent"] == 98.5 and trend[-1]["fpyPercent"] == 94.2
assert sum(point["firstPassAcceptedUnits"] for point in trend) == metrics["firstPassAcceptedUnits"]
assert sum(batch["eligibleUnits"] for batch in results["batches"]["data"]) == metrics["eligibleUnits"]
assert sum(defect["count"] for defect in results["defects"]["data"] if defect["stage"] == "AOI") == 220
assert metrics["spiAnomalyUnits"] == 350
assert metrics["maxPasteVolumePercent"] > 125
assert len(results["alarms"]["data"]) == metrics["alarmCount"] == 16
assert all(alarm["machineCode"] == "M03" for alarm in results["alarms"]["data"])
print("Production API smoke passed: 6 endpoints, seeded FPY/SPI/alarms and batch totals consistent.")
