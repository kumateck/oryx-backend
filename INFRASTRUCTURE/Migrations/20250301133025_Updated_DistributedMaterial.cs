using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Updated_DistributedMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DistributedRequisitionMaterials_ShipmentInvoicesItems_Shipm~",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropIndex(
                name: "IX_DistributedRequisitionMaterials_ShipmentInvoiceItemId",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropColumn(
                name: "ShipmentInvoiceItemId",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.AddColumn<Guid>(
                name: "DistributedRequisitionMaterialId",
                table: "ShipmentInvoicesItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentInvoicesItems_DistributedRequisitionMaterialId",
                table: "ShipmentInvoicesItems",
                column: "DistributedRequisitionMaterialId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoicesItems_DistributedRequisitionMaterials_Distr~",
                table: "ShipmentInvoicesItems",
                column: "DistributedRequisitionMaterialId",
                principalTable: "DistributedRequisitionMaterials",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoicesItems_DistributedRequisitionMaterials_Distr~",
                table: "ShipmentInvoicesItems");

            migrationBuilder.DropIndex(
                name: "IX_ShipmentInvoicesItems_DistributedRequisitionMaterialId",
                table: "ShipmentInvoicesItems");

            migrationBuilder.DropColumn(
                name: "DistributedRequisitionMaterialId",
                table: "ShipmentInvoicesItems");

            migrationBuilder.AddColumn<Guid>(
                name: "ShipmentInvoiceItemId",
                table: "DistributedRequisitionMaterials",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DistributedRequisitionMaterials_ShipmentInvoiceItemId",
                table: "DistributedRequisitionMaterials",
                column: "ShipmentInvoiceItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedRequisitionMaterials_ShipmentInvoicesItems_Shipm~",
                table: "DistributedRequisitionMaterials",
                column: "ShipmentInvoiceItemId",
                principalTable: "ShipmentInvoicesItems",
                principalColumn: "Id");
        }
    }
}
