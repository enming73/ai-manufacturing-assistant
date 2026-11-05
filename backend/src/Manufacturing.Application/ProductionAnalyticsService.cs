using Manufacturing.Domain;

namespace Manufacturing.Application;

public sealed class ProductionAnalyticsService(IProductionReadStore store)
{
    public async Task<MetricsDto> MetricsAsync(int lineId, TimeWindow window, CancellationToken ct)
    {
        var inspections = await store.InspectionsAsync(lineId, window, ct);
        var result = QualityAnalytics.Yield(inspections, window);
        var spi = QualityAnalytics.FirstPass(inspections, InspectionStage.Spi, window);
        var anomalies = spi.Count(row => !row.Accepted);
        var volumes = spi.Where(row => row.PasteVolumePercent.HasValue).Select(row => row.PasteVolumePercent!.Value).ToArray();
        var alarms = await store.AlarmsAsync(lineId, window, ct);
        var samples = (await store.MetricsAsync(lineId, window, ct))
            .Where(row => row.Name == "CycleTime" && row.Unit == "s").ToArray();
        return new(result.EligibleUnits, result.FirstPassAcceptedUnits, result.FpyPercent,
            spi.Count, anomalies, QualityAnalytics.Percent(anomalies, spi.Count), alarms.Count,
            samples.Length == 0 ? null : Math.Round(samples.Average(row => row.Value), 3),
            volumes.Length == 0 ? null : Math.Round(volumes.Average(), 3),
            volumes.Length == 0 ? null : volumes.Max());
    }

    public async Task<IReadOnlyList<TrendPoint>> TrendAsync(int lineId, TimeWindow window, CancellationToken ct)
    {
        var inspections = await store.InspectionsAsync(lineId, window, ct);
        var aoi = QualityAnalytics.FirstPass(inspections, InspectionStage.Aoi, window);
        var spi = QualityAnalytics.FirstPass(inspections, InspectionStage.Spi, window);
        var points = new List<TrendPoint>();
        var hour = new DateTimeOffset(window.From.Year, window.From.Month, window.From.Day, window.From.Hour, 0, 0, TimeSpan.Zero);
        for (; hour < window.To; hour = hour.AddHours(1))
        {
            var aoiRows = aoi.Where(row => row.InspectedAt >= hour && row.InspectedAt < hour.AddHours(1)).ToArray();
            var spiRows = spi.Where(row => row.InspectedAt >= hour && row.InspectedAt < hour.AddHours(1)).ToArray();
            var accepted = aoiRows.Count(row => row.Accepted);
            points.Add(new(hour, aoiRows.Length, accepted, QualityAnalytics.Percent(accepted, aoiRows.Length),
                spiRows.Length, spiRows.Count(row => !row.Accepted)));
        }
        return points;
    }

    public async Task<IReadOnlyList<DefectDto>> DefectsAsync(int lineId, TimeWindow window, CancellationToken ct)
    {
        var inspections = await store.InspectionsAsync(lineId, window, ct);
        return new[] { InspectionStage.Spi, InspectionStage.Aoi }
            .SelectMany(stage => QualityAnalytics.FirstPass(inspections, stage, window))
            .Where(row => !row.Accepted && row.DefectCode is not null)
            .GroupBy(row => new { row.Stage, row.DefectCode })
            .Select(group => new DefectDto(group.Key.Stage == InspectionStage.Aoi ? "AOI" : "SPI", group.Key.DefectCode!, group.Count()))
            .OrderBy(row => row.Stage).ThenByDescending(row => row.Count).ThenBy(row => row.Code).ToArray();
    }

    public Task<IReadOnlyList<AlarmDto>> AlarmsAsync(int lineId, TimeWindow window, CancellationToken ct)
        => store.AlarmsAsync(lineId, window, ct);

    public async Task<IReadOnlyList<BatchDto>> BatchesAsync(int lineId, TimeWindow window, CancellationToken ct)
    {
        var batches = await store.BatchesAsync(lineId, window, ct);
        var inspections = await store.InspectionsAsync(lineId, window, ct);
        return batches.Select(batch =>
        {
            var result = QualityAnalytics.Yield(inspections.Where(row => row.ProductionBatchId == batch.Id), window);
            return new BatchDto(batch.Id, batch.Code, batch.ProductCode, batch.StartedAt, batch.CompletedAt,
                batch.PlannedUnits, result.EligibleUnits, result.FirstPassAcceptedUnits, result.FpyPercent);
        }).ToArray();
    }
}
