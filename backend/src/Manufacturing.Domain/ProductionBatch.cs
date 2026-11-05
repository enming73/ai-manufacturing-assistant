namespace Manufacturing.Domain;

public sealed class ProductionBatch
{
    public int Id { get; set; }
    public int ProductionLineId { get; set; }
    public required string Code { get; set; }
    public required string ProductCode { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
    public int PlannedUnits { get; set; }
}
