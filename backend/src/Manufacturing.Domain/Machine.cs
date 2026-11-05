namespace Manufacturing.Domain;

public enum MachineKind { Spi, Placement, Aoi }

public sealed class Machine
{
    public int Id { get; set; }
    public int ProductionLineId { get; set; }
    public required string Code { get; set; }
    public MachineKind Kind { get; set; }
}
