using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Updated_DistributedRequisitionMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DistributedRequisitionMaterials_ShipmentInvoicesItems_Shipm~",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_DistributedRequisitionMaterials_ShipmentInvoicesItems_Ship~1",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedRequisitionMaterials_ShipmentInvoicesItems_Shipm~",
                table: "DistributedRequisitionMaterials",
                column: "ShipmentInvoiceItemId",
                principalTable: "ShipmentInvoicesItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedRequisitionMaterials_ShipmentInvoices_ShipmentIn~",
                table: "DistributedRequisitionMaterials",
                column: "ShipmentInvoiceId",
                principalTable: "ShipmentInvoices",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DistributedRequisitionMaterials_ShipmentInvoicesItems_Shipm~",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropForeignKey(
                name: "FK_DistributedRequisitionMaterials_ShipmentInvoices_ShipmentIn~",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedRequisitionMaterials_ShipmentInvoicesItems_Shipm~",
                table: "DistributedRequisitionMaterials",
                column: "ShipmentInvoiceId",
                principalTable: "ShipmentInvoicesItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedRequisitionMaterials_ShipmentInvoicesItems_Ship~1",
                table: "DistributedRequisitionMaterials",
                column: "ShipmentInvoiceItemId",
                principalTable: "ShipmentInvoicesItems",
                principalColumn: "Id");
        }
    }
}
