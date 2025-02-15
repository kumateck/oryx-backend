using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Added_Checklist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ChecklistId",
                table: "MaterialBatches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsChecked",
                table: "DistributedRequisitionMaterials",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Checklists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DistributedRequisitionMaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ShipmentInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManufacturerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificateOfAnalysisDelivered = table.Column<bool>(type: "boolean", nullable: false),
                    VisibleLabelling = table.Column<bool>(type: "boolean", nullable: false),
                    IntactnessStatus = table.Column<int>(type: "integer", nullable: false),
                    ConsignmentCarrierStatus = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Checklists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Checklists_DistributedRequisitionMaterials_DistributedRequi~",
                        column: x => x.DistributedRequisitionMaterialId,
                        principalTable: "DistributedRequisitionMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Checklists_Manufacturers_ManufacturerId",
                        column: x => x.ManufacturerId,
                        principalTable: "Manufacturers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Checklists_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Checklists_ShipmentInvoices_ShipmentInvoiceId",
                        column: x => x.ShipmentInvoiceId,
                        principalTable: "ShipmentInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Checklists_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Checklists_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Checklists_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Checklists_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Sr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    SrNumber = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    GrossWeight = table.Column<decimal>(type: "numeric", nullable: false),
                    UoMId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sr", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sr_MaterialBatches_MaterialBatchId",
                        column: x => x.MaterialBatchId,
                        principalTable: "MaterialBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sr_UnitOfMeasures_UoMId",
                        column: x => x.UoMId,
                        principalTable: "UnitOfMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sr_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Sr_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Sr_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatches_ChecklistId",
                table: "MaterialBatches",
                column: "ChecklistId");

            migrationBuilder.CreateIndex(
                name: "IX_Checklists_CreatedById",
                table: "Checklists",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Checklists_DistributedRequisitionMaterialId",
                table: "Checklists",
                column: "DistributedRequisitionMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_Checklists_LastDeletedById",
                table: "Checklists",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Checklists_LastUpdatedById",
                table: "Checklists",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Checklists_ManufacturerId",
                table: "Checklists",
                column: "ManufacturerId");

            migrationBuilder.CreateIndex(
                name: "IX_Checklists_MaterialId",
                table: "Checklists",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_Checklists_ShipmentInvoiceId",
                table: "Checklists",
                column: "ShipmentInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Checklists_SupplierId",
                table: "Checklists",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Sr_CreatedById",
                table: "Sr",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Sr_LastDeletedById",
                table: "Sr",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Sr_LastUpdatedById",
                table: "Sr",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Sr_MaterialBatchId",
                table: "Sr",
                column: "MaterialBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Sr_UoMId",
                table: "Sr",
                column: "UoMId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatches_Checklists_ChecklistId",
                table: "MaterialBatches",
                column: "ChecklistId",
                principalTable: "Checklists",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatches_Checklists_ChecklistId",
                table: "MaterialBatches");

            migrationBuilder.DropTable(
                name: "Checklists");

            migrationBuilder.DropTable(
                name: "Sr");

            migrationBuilder.DropIndex(
                name: "IX_MaterialBatches_ChecklistId",
                table: "MaterialBatches");

            migrationBuilder.DropColumn(
                name: "ChecklistId",
                table: "MaterialBatches");

            migrationBuilder.DropColumn(
                name: "IsChecked",
                table: "DistributedRequisitionMaterials");
        }
    }
}
