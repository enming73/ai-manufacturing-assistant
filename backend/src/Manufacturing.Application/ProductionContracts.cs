using Manufacturing.Domain;

namespace Manufacturing.Application;

public sealed record LineDto(int Id, string Name);
public sealed record LinesResponse(DateOnly DemoDate, int ScenarioLineId, TimeWindow DefaultWindow, IReadOnlyList<LineDto> Items);
public sealed record EvidenceResponse<T>(int LineId, TimeWindow Window, string Source, T Data);
public sealed record MetricsDto(int EligibleUnits, int FirstPassAcceptedUnits, decimal? FpyPercent,
    int SpiInspectedUnits, int SpiAnomalyUnits, decimal? SpiAnomalyPercent, int AlarmCount, decimal? AverageCycleTimeSeconds,
    decimal? AveragePasteVolumePercent, decimal? MaxPasteVolumePercent);
public sealed record TrendPoint(DateTimeOffset Timestamp, int EligibleUnits, int FirstPassAcceptedUnits,
    decimal? FpyPercent, int SpiInspectedUnits, int SpiAnomalyUnits);
public sealed record DefectDto(string Stage, string Code, int Count);
public sealed record AlarmDto(long Id, int MachineId, string MachineCode, int BatchId, string BatchCode,
    DateTimeOffset OccurredAt, string Code, string Severity, string Message);
public sealed record BatchDto(int Id, string Code, string ProductCode, DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt, int PlannedUnits, int EligibleUnits, int FirstPassAcceptedUnits, decimal? FpyPercent);

public interface IProductionReadStore
{
    Task<IReadOnlyList<LineDto>> LinesAsync(CancellationToken cancellationToken);
    Task<bool> LineExistsAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<QualityInspection>> InspectionsAsync(int lineId, TimeWindow window, CancellationToken cancellationToken);
    Task<IReadOnlyList<AlarmDto>> AlarmsAsync(int lineId, TimeWindow window, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductionBatch>> BatchesAsync(int lineId, TimeWindow window, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductionMetric>> MetricsAsync(int lineId, TimeWindow window, CancellationToken cancellationToken);
}
