using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDIstributionStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DistributedRequisitionMaterials_Suppliers_SupplierId",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_Manufacturers_DistributedRequisitionMaterials_DistributedRe~",
                table: "Manufacturers");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoiceItems_DistributedRequisitionMaterials_Distri~",
                table: "ShipmentInvoiceItems");

            migrationBuilder.DropIndex(
                name: "IX_ShipmentInvoiceItems_DistributedRequisitionMaterialId",
                table: "ShipmentInvoiceItems");

            migrationBuilder.DropIndex(
                name: "IX_Manufacturers_DistributedRequisitionMaterialId",
                table: "Manufacturers");

            migrationBuilder.DropIndex(
                name: "IX_DistributedRequisitionMaterials_SupplierId",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropColumn(
                name: "DistributedRequisitionMaterialId",
                table: "ShipmentInvoiceItems");

            migrationBuilder.DropColumn(
                name: "DistributedRequisitionMaterialId",
                table: "Manufacturers");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.CreateTable(
                name: "MaterialItemDistributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DistributedRequisitionMaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentInvoiceItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialItemDistributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialItemDistributions_DistributedRequisitionMaterials_D~",
                        column: x => x.DistributedRequisitionMaterialId,
                        principalTable: "DistributedRequisitionMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialItemDistributions_ShipmentInvoiceItems_ShipmentInvo~",
                        column: x => x.ShipmentInvoiceItemId,
                        principalTable: "ShipmentInvoiceItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialItemDistributions_DistributedRequisitionMaterialId",
                table: "MaterialItemDistributions",
                column: "DistributedRequisitionMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialItemDistributions_ShipmentInvoiceItemId",
                table: "MaterialItemDistributions",
                column: "ShipmentInvoiceItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaterialItemDistributions");

            migrationBuilder.AddColumn<Guid>(
                name: "DistributedRequisitionMaterialId",
                table: "ShipmentInvoiceItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DistributedRequisitionMaterialId",
                table: "Manufacturers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "DistributedRequisitionMaterials",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoiceItems_DistributedRequisitionMaterialId",
                table: "ShipmentInvoiceItems",
                column: "DistributedRequisitionMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_Manufacturers_DistributedRequisitionMaterialId",
                table: "Manufacturers",
                column: "DistributedRequisitionMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributedRequisitionMaterials_SupplierId",
                table: "DistributedRequisitionMaterials",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedRequisitionMaterials_Suppliers_SupplierId",
                table: "DistributedRequisitionMaterials",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Manufacturers_DistributedRequisitionMaterials_DistributedRe~",
                table: "Manufacturers",
                column: "DistributedRequisitionMaterialId",
                principalTable: "DistributedRequisitionMaterials",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoiceItems_DistributedRequisitionMaterials_Distri~",
                table: "ShipmentInvoiceItems",
                column: "DistributedRequisitionMaterialId",
                principalTable: "DistributedRequisitionMaterials",
                principalColumn: "Id");
        }
    }
}
