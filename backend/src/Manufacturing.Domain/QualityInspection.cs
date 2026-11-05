namespace Manufacturing.Domain;

public enum InspectionStage { Spi, Aoi }

public sealed class QualityInspection
{
    public long Id { get; set; }
    public int ProductionLineId { get; set; }
    public int ProductionBatchId { get; set; }
    public int MachineId { get; set; }
    public required string UnitSerial { get; set; }
    public InspectionStage Stage { get; set; }
    public int Attempt { get; set; }
    public DateTimeOffset InspectedAt { get; set; }
    public bool Accepted { get; set; }
    // One primary defect per inspection; SPI values are percentages of nominal.
    public string? DefectCode { get; set; }
    public decimal? PasteVolumePercent { get; set; }
    public decimal? PasteHeightMicrometers { get; set; }
    public decimal? PasteAreaPercent { get; set; }
}
