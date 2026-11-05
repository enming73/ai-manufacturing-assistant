namespace Manufacturing.Domain;

// A raw equipment measurement, never a stored/hand-entered FPY KPI.
public sealed class ProductionMetric
{
    public long Id { get; set; }
    public int ProductionLineId { get; set; }
    public int ProductionBatchId { get; set; }
    public int MachineId { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
    public required string Name { get; set; }
    public decimal Value { get; set; }
    public required string Unit { get; set; }
}
