using Manufacturing.Domain;
using Microsoft.EntityFrameworkCore;

namespace Manufacturing.Infrastructure;

public sealed class ManufacturingDbContext(DbContextOptions<ManufacturingDbContext> options) : DbContext(options)
{
    public DbSet<ProductionLine> ProductionLines => Set<ProductionLine>();
    public DbSet<Machine> Machines => Set<Machine>();
    public DbSet<ProductionBatch> ProductionBatches => Set<ProductionBatch>();
    public DbSet<QualityInspection> QualityInspections => Set<QualityInspection>();
    public DbSet<EquipmentAlarm> EquipmentAlarms => Set<EquipmentAlarm>();
    public DbSet<ProductionMetric> ProductionMetrics => Set<ProductionMetric>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.HasPostgresExtension("vector");
        model.Entity<ProductionLine>(entity =>
        {
            entity.HasKey(row => row.Id);
            entity.Property(row => row.Id).ValueGeneratedNever();
            entity.Property(row => row.Name).HasMaxLength(80);
            entity.HasIndex(row => row.Name).IsUnique();
        });
        model.Entity<Machine>(entity =>
        {
            entity.HasKey(row => row.Id);
            entity.Property(row => row.Id).ValueGeneratedNever();
            entity.HasAlternateKey(row => new { row.Id, row.ProductionLineId });
            entity.Property(row => row.Code).HasMaxLength(40);
            entity.Property(row => row.Kind).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(row => row.Code).IsUnique();
            entity.HasOne<ProductionLine>().WithMany().HasForeignKey(row => row.ProductionLineId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint("CK_Machine_Kind", "\"Kind\" IN ('Spi', 'Placement', 'Aoi')"));
        });
        model.Entity<ProductionBatch>(entity =>
        {
            entity.HasKey(row => row.Id);
            entity.Property(row => row.Id).ValueGeneratedNever();
            entity.HasAlternateKey(row => new { row.Id, row.ProductionLineId });
            entity.Property(row => row.Code).HasMaxLength(80);
            entity.Property(row => row.ProductCode).HasMaxLength(80);
            entity.HasIndex(row => row.Code).IsUnique();
            entity.HasOne<ProductionLine>().WithMany().HasForeignKey(row => row.ProductionLineId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Batch_Window", "\"StartedAt\" < \"CompletedAt\"");
                table.HasCheckConstraint("CK_Batch_PlannedUnits", "\"PlannedUnits\" > 0");
            });
        });
        model.Entity<QualityInspection>(entity =>
        {
            entity.HasKey(row => row.Id);
            entity.Property(row => row.Id).ValueGeneratedNever();
            entity.Property(row => row.UnitSerial).HasMaxLength(80);
            entity.Property(row => row.Stage).HasConversion<string>().HasMaxLength(10);
            entity.Property(row => row.DefectCode).HasMaxLength(80);
            entity.Property(row => row.PasteVolumePercent).HasPrecision(8, 3);
            entity.Property(row => row.PasteHeightMicrometers).HasPrecision(8, 3);
            entity.Property(row => row.PasteAreaPercent).HasPrecision(8, 3);
            entity.HasIndex(row => new { row.ProductionBatchId, row.UnitSerial, row.Stage, row.Attempt }).IsUnique();
            entity.HasIndex(row => new { row.ProductionLineId, row.InspectedAt, row.Stage, row.Attempt });
            entity.HasOne<ProductionBatch>().WithMany().HasForeignKey(row => new { row.ProductionBatchId, row.ProductionLineId })
                .HasPrincipalKey(row => new { row.Id, row.ProductionLineId }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Machine>().WithMany().HasForeignKey(row => new { row.MachineId, row.ProductionLineId })
                .HasPrincipalKey(row => new { row.Id, row.ProductionLineId }).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Inspection_Attempt", "\"Attempt\" >= 1");
                table.HasCheckConstraint("CK_Inspection_Result", "(\"Accepted\" AND \"DefectCode\" IS NULL) OR (NOT \"Accepted\" AND \"DefectCode\" IS NOT NULL)");
                table.HasCheckConstraint("CK_Inspection_Measurements", "(\"Stage\" = 'Spi' AND \"PasteVolumePercent\" > 0 AND \"PasteVolumePercent\" IS NOT NULL AND \"PasteHeightMicrometers\" > 0 AND \"PasteHeightMicrometers\" IS NOT NULL AND \"PasteAreaPercent\" > 0 AND \"PasteAreaPercent\" IS NOT NULL) OR (\"Stage\" = 'Aoi' AND \"PasteVolumePercent\" IS NULL AND \"PasteHeightMicrometers\" IS NULL AND \"PasteAreaPercent\" IS NULL)");
            });
        });
        model.Entity<EquipmentAlarm>(entity =>
        {
            entity.HasKey(row => row.Id);
            entity.Property(row => row.Id).ValueGeneratedNever();
            entity.Property(row => row.Code).HasMaxLength(80);
            entity.Property(row => row.Severity).HasMaxLength(20);
            entity.Property(row => row.Message).HasMaxLength(300);
            entity.HasIndex(row => new { row.ProductionLineId, row.OccurredAt });
            entity.HasOne<Machine>().WithMany().HasForeignKey(row => new { row.MachineId, row.ProductionLineId })
                .HasPrincipalKey(row => new { row.Id, row.ProductionLineId }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ProductionBatch>().WithMany().HasForeignKey(row => new { row.ProductionBatchId, row.ProductionLineId })
                .HasPrincipalKey(row => new { row.Id, row.ProductionLineId }).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<ProductionMetric>(entity =>
        {
            entity.HasKey(row => row.Id);
            entity.Property(row => row.Id).ValueGeneratedNever();
            entity.Property(row => row.Name).HasMaxLength(80);
            entity.Property(row => row.Unit).HasMaxLength(20);
            entity.Property(row => row.Value).HasPrecision(12, 3);
            entity.HasIndex(row => new { row.ProductionLineId, row.RecordedAt });
            entity.HasOne<Machine>().WithMany().HasForeignKey(row => new { row.MachineId, row.ProductionLineId })
                .HasPrincipalKey(row => new { row.Id, row.ProductionLineId }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ProductionBatch>().WithMany().HasForeignKey(row => new { row.ProductionBatchId, row.ProductionLineId })
                .HasPrincipalKey(row => new { row.Id, row.ProductionLineId }).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table => table.HasCheckConstraint("CK_Metric_Value", "\"Value\" >= 0"));
        });
    }
}
