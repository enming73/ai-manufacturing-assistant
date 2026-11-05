using Manufacturing.Application;
using Manufacturing.Domain;
using Microsoft.EntityFrameworkCore;

namespace Manufacturing.Infrastructure;

public static class DemoSeeder
{
    public static async Task SeedAsync(ManufacturingDbContext db, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Serialize this opt-in demo seed, including across concurrent application instances.
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20261015)", ct);
        if (await db.ProductionBatches.AnyAsync(row => row.Code == "DEMO-L3-20261015-11", ct))
        {
            await transaction.CommitAsync(ct);
            return;
        }
        if (await db.ProductionLines.AnyAsync(ct))
            throw new InvalidOperationException("Demo seeding requires empty domain tables. Disable Demo:SeedOnStartup for existing datasets.");

        var inspections = new List<QualityInspection>();
        long inspectionId = 0, alarmId = 0, metricId = 0;
        int[] line3Failures = [15, 24, 32, 41, 50, 58];
        int[] line3SpiAnomalies = [20, 35, 50, 65, 80, 100];
        int[] line3Alarms = [1, 1, 2, 3, 4, 5];
        for (var lineId = 1; lineId <= 3; lineId++)
        {
            db.ProductionLines.Add(new ProductionLine { Id = lineId, Name = $"Line {lineId}" });
            var spiMachine = lineId * 10 + 1;
            var placementMachine = lineId * 10 + 2;
            var aoiMachine = lineId * 10 + 3;
            db.Machines.AddRange(
                new Machine { Id = spiMachine, ProductionLineId = lineId, Code = $"SPI{lineId:00}", Kind = MachineKind.Spi },
                new Machine { Id = placementMachine, ProductionLineId = lineId, Code = $"M{lineId:00}", Kind = MachineKind.Placement },
                new Machine { Id = aoiMachine, ProductionLineId = lineId, Code = $"AOI{lineId:00}", Kind = MachineKind.Aoi });
            for (var hourIndex = 0; hourIndex < 6; hourIndex++)
            {
                var start = DemoScenario.DefaultWindow.From.AddHours(hourIndex);
                var batchId = lineId * 100 + hourIndex;
                var failures = lineId == 3 ? line3Failures[hourIndex] : (lineId == 1 ? 10 : 12);
                var spiAnomalies = lineId == 3 ? line3SpiAnomalies[hourIndex] : 10;
                db.ProductionBatches.Add(new ProductionBatch
                {
                    Id = batchId, ProductionLineId = lineId, Code = $"DEMO-L{lineId}-20261015-{start.Hour:00}",
                    ProductCode = "CTRL-PCB-A", StartedAt = start, CompletedAt = start.AddHours(1), PlannedUnits = 1000
                });
                for (var unit = 1; unit <= 1000; unit++)
                {
                    var timestamp = start.AddSeconds((unit - 1) * 3);
                    var spiAccepted = unit > spiAnomalies;
                    var accepted = unit > failures;
                    var defect = accepted ? null : unit % 5 == 0 ? "COMPONENT_OFFSET"
                        : unit % 3 == 0 ? "SOLDER_BRIDGE" : "INSUFFICIENT_SOLDER";
                    inspections.Add(new QualityInspection
                    {
                        Id = ++inspectionId, ProductionLineId = lineId, ProductionBatchId = batchId,
                        MachineId = spiMachine, UnitSerial = $"U{unit:0000}", Stage = InspectionStage.Spi,
                        Attempt = 1, InspectedAt = timestamp, Accepted = spiAccepted,
                        DefectCode = spiAccepted ? null : "PASTE_VOLUME_HIGH",
                        PasteVolumePercent = spiAccepted ? 98m + unit % 5 : 135m + unit % 11,
                        PasteHeightMicrometers = spiAccepted ? 118m + unit % 5 : 155m + unit % 7,
                        PasteAreaPercent = spiAccepted ? 97m + unit % 5 : 126m + unit % 9
                    });
                    inspections.Add(new QualityInspection
                    {
                        Id = ++inspectionId, ProductionLineId = lineId, ProductionBatchId = batchId,
                        MachineId = aoiMachine, UnitSerial = $"U{unit:0000}", Stage = InspectionStage.Aoi,
                        Attempt = 1, InspectedAt = timestamp.AddSeconds(30), Accepted = accepted, DefectCode = defect
                    });
                    if (!accepted)
                        inspections.Add(new QualityInspection
                        {
                            Id = ++inspectionId, ProductionLineId = lineId, ProductionBatchId = batchId,
                            MachineId = aoiMachine, UnitSerial = $"U{unit:0000}", Stage = InspectionStage.Aoi,
                            Attempt = 2, InspectedAt = timestamp.AddMinutes(15), Accepted = true
                        });
                }
                db.ProductionMetrics.Add(new ProductionMetric
                {
                    Id = ++metricId, ProductionLineId = lineId, ProductionBatchId = batchId,
                    MachineId = placementMachine, RecordedAt = start.AddMinutes(30), Name = "CycleTime",
                    Value = 2.8m + (lineId == 3 ? hourIndex * 0.07m : 0), Unit = "s"
                });
                var alarmCount = lineId == 3 ? line3Alarms[hourIndex] : lineId == 2 && hourIndex == 1 ? 1 : 0;
                for (var alarm = 0; alarm < alarmCount; alarm++)
                    db.EquipmentAlarms.Add(new EquipmentAlarm
                    {
                        Id = ++alarmId, ProductionLineId = lineId, ProductionBatchId = batchId,
                        MachineId = placementMachine, OccurredAt = start.AddMinutes(10 + alarm * 8),
                        Code = lineId == 3 ? "NOZZLE_PICKUP_FAILURE" : "FEEDER_LOW", Severity = "Warning",
                        Message = lineId == 3 ? "Aufnahmefehler an der Bestückungsdüse (synthetisch)." : "Materialvorrat niedrig (synthetisch)."
                    });
            }
        }
        db.QualityInspections.AddRange(inspections);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        db.ChangeTracker.Clear();
    }
}
