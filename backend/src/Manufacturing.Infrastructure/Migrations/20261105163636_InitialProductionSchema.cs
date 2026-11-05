using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manufacturing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialProductionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "ProductionLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionLines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Machines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    ProductionLineId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Machines", x => x.Id);
                    table.UniqueConstraint("AK_Machines_Id_ProductionLineId", x => new { x.Id, x.ProductionLineId });
                    table.CheckConstraint("CK_Machine_Kind", "\"Kind\" IN ('Spi', 'Placement', 'Aoi')");
                    table.ForeignKey(
                        name: "FK_Machines_ProductionLines_ProductionLineId",
                        column: x => x.ProductionLineId,
                        principalTable: "ProductionLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    ProductionLineId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PlannedUnits = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionBatches", x => x.Id);
                    table.UniqueConstraint("AK_ProductionBatches_Id_ProductionLineId", x => new { x.Id, x.ProductionLineId });
                    table.CheckConstraint("CK_Batch_PlannedUnits", "\"PlannedUnits\" > 0");
                    table.CheckConstraint("CK_Batch_Window", "\"StartedAt\" < \"CompletedAt\"");
                    table.ForeignKey(
                        name: "FK_ProductionBatches_ProductionLines_ProductionLineId",
                        column: x => x.ProductionLineId,
                        principalTable: "ProductionLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentAlarms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ProductionLineId = table.Column<int>(type: "integer", nullable: false),
                    ProductionBatchId = table.Column<int>(type: "integer", nullable: false),
                    MachineId = table.Column<int>(type: "integer", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentAlarms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentAlarms_Machines_MachineId_ProductionLineId",
                        columns: x => new { x.MachineId, x.ProductionLineId },
                        principalTable: "Machines",
                        principalColumns: new[] { "Id", "ProductionLineId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EquipmentAlarms_ProductionBatches_ProductionBatchId_Product~",
                        columns: x => new { x.ProductionBatchId, x.ProductionLineId },
                        principalTable: "ProductionBatches",
                        principalColumns: new[] { "Id", "ProductionLineId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionMetrics",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ProductionLineId = table.Column<int>(type: "integer", nullable: false),
                    ProductionBatchId = table.Column<int>(type: "integer", nullable: false),
                    MachineId = table.Column<int>(type: "integer", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Value = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionMetrics", x => x.Id);
                    table.CheckConstraint("CK_Metric_Value", "\"Value\" >= 0");
                    table.ForeignKey(
                        name: "FK_ProductionMetrics_Machines_MachineId_ProductionLineId",
                        columns: x => new { x.MachineId, x.ProductionLineId },
                        principalTable: "Machines",
                        principalColumns: new[] { "Id", "ProductionLineId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionMetrics_ProductionBatches_ProductionBatchId_Produ~",
                        columns: x => new { x.ProductionBatchId, x.ProductionLineId },
                        principalTable: "ProductionBatches",
                        principalColumns: new[] { "Id", "ProductionLineId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QualityInspections",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ProductionLineId = table.Column<int>(type: "integer", nullable: false),
                    ProductionBatchId = table.Column<int>(type: "integer", nullable: false),
                    MachineId = table.Column<int>(type: "integer", nullable: false),
                    UnitSerial = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Stage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Attempt = table.Column<int>(type: "integer", nullable: false),
                    InspectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Accepted = table.Column<bool>(type: "boolean", nullable: false),
                    DefectCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    PasteVolumePercent = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: true),
                    PasteHeightMicrometers = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: true),
                    PasteAreaPercent = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityInspections", x => x.Id);
                    table.CheckConstraint("CK_Inspection_Attempt", "\"Attempt\" >= 1");
                    table.CheckConstraint("CK_Inspection_Measurements", "(\"Stage\" = 'Spi' AND \"PasteVolumePercent\" > 0 AND \"PasteVolumePercent\" IS NOT NULL AND \"PasteHeightMicrometers\" > 0 AND \"PasteHeightMicrometers\" IS NOT NULL AND \"PasteAreaPercent\" > 0 AND \"PasteAreaPercent\" IS NOT NULL) OR (\"Stage\" = 'Aoi' AND \"PasteVolumePercent\" IS NULL AND \"PasteHeightMicrometers\" IS NULL AND \"PasteAreaPercent\" IS NULL)");
                    table.CheckConstraint("CK_Inspection_Result", "(\"Accepted\" AND \"DefectCode\" IS NULL) OR (NOT \"Accepted\" AND \"DefectCode\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_QualityInspections_Machines_MachineId_ProductionLineId",
                        columns: x => new { x.MachineId, x.ProductionLineId },
                        principalTable: "Machines",
                        principalColumns: new[] { "Id", "ProductionLineId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityInspections_ProductionBatches_ProductionBatchId_Prod~",
                        columns: x => new { x.ProductionBatchId, x.ProductionLineId },
                        principalTable: "ProductionBatches",
                        principalColumns: new[] { "Id", "ProductionLineId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentAlarms_MachineId_ProductionLineId",
                table: "EquipmentAlarms",
                columns: new[] { "MachineId", "ProductionLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentAlarms_ProductionBatchId_ProductionLineId",
                table: "EquipmentAlarms",
                columns: new[] { "ProductionBatchId", "ProductionLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentAlarms_ProductionLineId_OccurredAt",
                table: "EquipmentAlarms",
                columns: new[] { "ProductionLineId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Machines_Code",
                table: "Machines",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Machines_ProductionLineId",
                table: "Machines",
                column: "ProductionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatches_Code",
                table: "ProductionBatches",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatches_ProductionLineId",
                table: "ProductionBatches",
                column: "ProductionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionLines_Name",
                table: "ProductionLines",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionMetrics_MachineId_ProductionLineId",
                table: "ProductionMetrics",
                columns: new[] { "MachineId", "ProductionLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionMetrics_ProductionBatchId_ProductionLineId",
                table: "ProductionMetrics",
                columns: new[] { "ProductionBatchId", "ProductionLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionMetrics_ProductionLineId_RecordedAt",
                table: "ProductionMetrics",
                columns: new[] { "ProductionLineId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_MachineId_ProductionLineId",
                table: "QualityInspections",
                columns: new[] { "MachineId", "ProductionLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_ProductionBatchId_ProductionLineId",
                table: "QualityInspections",
                columns: new[] { "ProductionBatchId", "ProductionLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_ProductionBatchId_UnitSerial_Stage_Attem~",
                table: "QualityInspections",
                columns: new[] { "ProductionBatchId", "UnitSerial", "Stage", "Attempt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_ProductionLineId_InspectedAt_Stage_Attem~",
                table: "QualityInspections",
                columns: new[] { "ProductionLineId", "InspectedAt", "Stage", "Attempt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EquipmentAlarms");

            migrationBuilder.DropTable(
                name: "ProductionMetrics");

            migrationBuilder.DropTable(
                name: "QualityInspections");

            migrationBuilder.DropTable(
                name: "Machines");

            migrationBuilder.DropTable(
                name: "ProductionBatches");

            migrationBuilder.DropTable(
                name: "ProductionLines");
        }
    }
}
