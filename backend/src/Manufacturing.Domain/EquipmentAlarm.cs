namespace Manufacturing.Domain;

public sealed class EquipmentAlarm
{
    public long Id { get; set; }
    public int ProductionLineId { get; set; }
    public int ProductionBatchId { get; set; }
    public int MachineId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public required string Code { get; set; }
    public required string Severity { get; set; }
    public required string Message { get; set; }
}
