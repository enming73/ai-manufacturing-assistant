using Manufacturing.Application;
using Manufacturing.Domain;
using Microsoft.EntityFrameworkCore;

namespace Manufacturing.Infrastructure;

public sealed class ProductionReadStore(ManufacturingDbContext db) : IProductionReadStore
{
    public async Task<IReadOnlyList<LineDto>> LinesAsync(CancellationToken ct) => await db.ProductionLines.AsNoTracking()
        .OrderBy(row => row.Id).Select(row => new LineDto(row.Id, row.Name)).ToArrayAsync(ct);

    public Task<bool> LineExistsAsync(int id, CancellationToken ct) => db.ProductionLines.AnyAsync(row => row.Id == id, ct);

    public async Task<IReadOnlyList<QualityInspection>> InspectionsAsync(int lineId, TimeWindow window, CancellationToken ct)
        => await db.QualityInspections.AsNoTracking()
            .Where(row => row.ProductionLineId == lineId && row.Attempt == 1 && row.InspectedAt >= window.From && row.InspectedAt < window.To)
            .ToArrayAsync(ct);

    public async Task<IReadOnlyList<AlarmDto>> AlarmsAsync(int lineId, TimeWindow window, CancellationToken ct)
        => await (from alarm in db.EquipmentAlarms.AsNoTracking()
                  join machine in db.Machines on alarm.MachineId equals machine.Id
                  join batch in db.ProductionBatches on alarm.ProductionBatchId equals batch.Id
                  where alarm.ProductionLineId == lineId && alarm.OccurredAt >= window.From && alarm.OccurredAt < window.To
                  orderby alarm.OccurredAt, alarm.Id
                  select new AlarmDto(alarm.Id, machine.Id, machine.Code, batch.Id, batch.Code,
                      alarm.OccurredAt, alarm.Code, alarm.Severity, alarm.Message)).ToArrayAsync(ct);

    public async Task<IReadOnlyList<ProductionBatch>> BatchesAsync(int lineId, TimeWindow window, CancellationToken ct)
        => await db.ProductionBatches.AsNoTracking()
            .Where(row => row.ProductionLineId == lineId && row.StartedAt < window.To && row.CompletedAt > window.From)
            .OrderBy(row => row.StartedAt).ThenBy(row => row.Id).ToArrayAsync(ct);

    public async Task<IReadOnlyList<ProductionMetric>> MetricsAsync(int lineId, TimeWindow window, CancellationToken ct)
        => await db.ProductionMetrics.AsNoTracking()
            .Where(row => row.ProductionLineId == lineId && row.RecordedAt >= window.From && row.RecordedAt < window.To)
            .OrderBy(row => row.RecordedAt).ToArrayAsync(ct);
}
