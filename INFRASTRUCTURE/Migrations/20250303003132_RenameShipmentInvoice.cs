using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class RenameShipmentInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoicesItems_DistributedRequisitionMaterials_Distr~",
                table: "ShipmentInvoicesItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoicesItems_Manufacturers_ManufacturerId",
                table: "ShipmentInvoicesItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoicesItems_Materials_MaterialId",
                table: "ShipmentInvoicesItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoicesItems_PurchaseOrders_PurchaseOrderId",
                table: "ShipmentInvoicesItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoicesItems_ShipmentInvoices_ShipmentInvoiceId",
                table: "ShipmentInvoicesItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoicesItems_UnitOfMeasures_UoMId",
                table: "ShipmentInvoicesItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoicesItems_users_CreatedById",
                table: "ShipmentInvoicesItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoicesItems_users_LastDeletedById",
                table: "ShipmentInvoicesItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoicesItems_users_LastUpdatedById",
                table: "ShipmentInvoicesItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ShipmentInvoicesItems",
                table: "ShipmentInvoicesItems");

            migrationBuilder.RenameTable(
                name: "ShipmentInvoicesItems",
                newName: "ShipmentInvoiceItems");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoicesItems_UoMId",
                table: "ShipmentInvoiceItems",
                newName: "IX_ShipmentInvoiceItems_UoMId");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoicesItems_ShipmentInvoiceId",
                table: "ShipmentInvoiceItems",
                newName: "IX_ShipmentInvoiceItems_ShipmentInvoiceId");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoicesItems_PurchaseOrderId",
                table: "ShipmentInvoiceItems",
                newName: "IX_ShipmentInvoiceItems_PurchaseOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoicesItems_MaterialId",
                table: "ShipmentInvoiceItems",
                newName: "IX_ShipmentInvoiceItems_MaterialId");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoicesItems_ManufacturerId",
                table: "ShipmentInvoiceItems",
                newName: "IX_ShipmentInvoiceItems_ManufacturerId");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoicesItems_LastUpdatedById",
                table: "ShipmentInvoiceItems",
                newName: "IX_ShipmentInvoiceItems_LastUpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoicesItems_LastDeletedById",
                table: "ShipmentInvoiceItems",
                newName: "IX_ShipmentInvoiceItems_LastDeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoicesItems_DistributedRequisitionMaterialId",
                table: "ShipmentInvoiceItems",
                newName: "IX_ShipmentInvoiceItems_DistributedRequisitionMaterialId");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoicesItems_CreatedById",
                table: "ShipmentInvoiceItems",
                newName: "IX_ShipmentInvoiceItems_CreatedById");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ShipmentInvoiceItems",
                table: "ShipmentInvoiceItems",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoiceItems_DistributedRequisitionMaterials_Distri~",
                table: "ShipmentInvoiceItems",
                column: "DistributedRequisitionMaterialId",
                principalTable: "DistributedRequisitionMaterials",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoiceItems_Manufacturers_ManufacturerId",
                table: "ShipmentInvoiceItems",
                column: "ManufacturerId",
                principalTable: "Manufacturers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoiceItems_Materials_MaterialId",
                table: "ShipmentInvoiceItems",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoiceItems_PurchaseOrders_PurchaseOrderId",
                table: "ShipmentInvoiceItems",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoiceItems_ShipmentInvoices_ShipmentInvoiceId",
                table: "ShipmentInvoiceItems",
                column: "ShipmentInvoiceId",
                principalTable: "ShipmentInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoiceItems_UnitOfMeasures_UoMId",
                table: "ShipmentInvoiceItems",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoiceItems_users_CreatedById",
                table: "ShipmentInvoiceItems",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoiceItems_users_LastDeletedById",
                table: "ShipmentInvoiceItems",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoiceItems_users_LastUpdatedById",
                table: "ShipmentInvoiceItems",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoiceItems_DistributedRequisitionMaterials_Distri~",
                table: "ShipmentInvoiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoiceItems_Manufacturers_ManufacturerId",
                table: "ShipmentInvoiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoiceItems_Materials_MaterialId",
                table: "ShipmentInvoiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoiceItems_PurchaseOrders_PurchaseOrderId",
                table: "ShipmentInvoiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoiceItems_ShipmentInvoices_ShipmentInvoiceId",
                table: "ShipmentInvoiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoiceItems_UnitOfMeasures_UoMId",
                table: "ShipmentInvoiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoiceItems_users_CreatedById",
                table: "ShipmentInvoiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoiceItems_users_LastDeletedById",
                table: "ShipmentInvoiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoiceItems_users_LastUpdatedById",
                table: "ShipmentInvoiceItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ShipmentInvoiceItems",
                table: "ShipmentInvoiceItems");

            migrationBuilder.RenameTable(
                name: "ShipmentInvoiceItems",
                newName: "ShipmentInvoicesItems");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoiceItems_UoMId",
                table: "ShipmentInvoicesItems",
                newName: "IX_ShipmentInvoicesItems_UoMId");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoiceItems_ShipmentInvoiceId",
                table: "ShipmentInvoicesItems",
                newName: "IX_ShipmentInvoicesItems_ShipmentInvoiceId");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoiceItems_PurchaseOrderId",
                table: "ShipmentInvoicesItems",
                newName: "IX_ShipmentInvoicesItems_PurchaseOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoiceItems_MaterialId",
                table: "ShipmentInvoicesItems",
                newName: "IX_ShipmentInvoicesItems_MaterialId");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoiceItems_ManufacturerId",
                table: "ShipmentInvoicesItems",
                newName: "IX_ShipmentInvoicesItems_ManufacturerId");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoiceItems_LastUpdatedById",
                table: "ShipmentInvoicesItems",
                newName: "IX_ShipmentInvoicesItems_LastUpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoiceItems_LastDeletedById",
                table: "ShipmentInvoicesItems",
                newName: "IX_ShipmentInvoicesItems_LastDeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoiceItems_DistributedRequisitionMaterialId",
                table: "ShipmentInvoicesItems",
                newName: "IX_ShipmentInvoicesItems_DistributedRequisitionMaterialId");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoiceItems_CreatedById",
                table: "ShipmentInvoicesItems",
                newName: "IX_ShipmentInvoicesItems_CreatedById");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ShipmentInvoicesItems",
                table: "ShipmentInvoicesItems",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoicesItems_DistributedRequisitionMaterials_Distr~",
                table: "ShipmentInvoicesItems",
                column: "DistributedRequisitionMaterialId",
                principalTable: "DistributedRequisitionMaterials",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoicesItems_Manufacturers_ManufacturerId",
                table: "ShipmentInvoicesItems",
                column: "ManufacturerId",
                principalTable: "Manufacturers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoicesItems_Materials_MaterialId",
                table: "ShipmentInvoicesItems",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoicesItems_PurchaseOrders_PurchaseOrderId",
                table: "ShipmentInvoicesItems",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoicesItems_ShipmentInvoices_ShipmentInvoiceId",
                table: "ShipmentInvoicesItems",
                column: "ShipmentInvoiceId",
                principalTable: "ShipmentInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoicesItems_UnitOfMeasures_UoMId",
                table: "ShipmentInvoicesItems",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoicesItems_users_CreatedById",
                table: "ShipmentInvoicesItems",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoicesItems_users_LastDeletedById",
                table: "ShipmentInvoicesItems",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoicesItems_users_LastUpdatedById",
                table: "ShipmentInvoicesItems",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");
        }
    }
}
