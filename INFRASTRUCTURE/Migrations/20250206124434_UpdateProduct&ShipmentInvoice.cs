using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductShipmentInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoiceItem_Manufacturers_ManufacturerId",
                table: "ShipmentInvoiceItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoiceItem_Materials_MaterialId",
                table: "ShipmentInvoiceItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoiceItem_ShipmentInvoices_ShipmentInvoiceId",
                table: "ShipmentInvoiceItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoiceItem_UnitOfMeasures_UoMId",
                table: "ShipmentInvoiceItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoiceItem_users_CreatedById",
                table: "ShipmentInvoiceItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoiceItem_users_LastDeletedById",
                table: "ShipmentInvoiceItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoiceItem_users_LastUpdatedById",
                table: "ShipmentInvoiceItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ShipmentInvoiceItem",
                table: "ShipmentInvoiceItem");

            migrationBuilder.RenameTable(
                name: "ShipmentInvoiceItem",
                newName: "ShipmentInvoicesItems");

            migrationBuilder.RenameColumn(
                name: "PackingErrorMargin",
                table: "Products",
                newName: "PackingExcessMargin");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoiceItem_UoMId",
                table: "ShipmentInvoicesItems",
                newName: "IX_ShipmentInvoicesItems_UoMId");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoiceItem_ShipmentInvoiceId",
                table: "ShipmentInvoicesItems",
                newName: "IX_ShipmentInvoicesItems_ShipmentInvoiceId");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoiceItem_MaterialId",
                table: "ShipmentInvoicesItems",
                newName: "IX_ShipmentInvoicesItems_MaterialId");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoiceItem_ManufacturerId",
                table: "ShipmentInvoicesItems",
                newName: "IX_ShipmentInvoicesItems_ManufacturerId");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoiceItem_LastUpdatedById",
                table: "ShipmentInvoicesItems",
                newName: "IX_ShipmentInvoicesItems_LastUpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoiceItem_LastDeletedById",
                table: "ShipmentInvoicesItems",
                newName: "IX_ShipmentInvoicesItems_LastDeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoiceItem_CreatedById",
                table: "ShipmentInvoicesItems",
                newName: "IX_ShipmentInvoicesItems_CreatedById");

            migrationBuilder.AddColumn<DateTime>(
                name: "ShipmentArrived",
                table: "ShipmentInvoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ShipmentInvoicesItems",
                table: "ShipmentInvoicesItems",
                column: "Id");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoicesItems_Manufacturers_ManufacturerId",
                table: "ShipmentInvoicesItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentInvoicesItems_Materials_MaterialId",
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

            migrationBuilder.DropColumn(
                name: "ShipmentArrived",
                table: "ShipmentInvoices");

            migrationBuilder.RenameTable(
                name: "ShipmentInvoicesItems",
                newName: "ShipmentInvoiceItem");

            migrationBuilder.RenameColumn(
                name: "PackingExcessMargin",
                table: "Products",
                newName: "PackingErrorMargin");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoicesItems_UoMId",
                table: "ShipmentInvoiceItem",
                newName: "IX_ShipmentInvoiceItem_UoMId");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoicesItems_ShipmentInvoiceId",
                table: "ShipmentInvoiceItem",
                newName: "IX_ShipmentInvoiceItem_ShipmentInvoiceId");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoicesItems_MaterialId",
                table: "ShipmentInvoiceItem",
                newName: "IX_ShipmentInvoiceItem_MaterialId");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoicesItems_ManufacturerId",
                table: "ShipmentInvoiceItem",
                newName: "IX_ShipmentInvoiceItem_ManufacturerId");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoicesItems_LastUpdatedById",
                table: "ShipmentInvoiceItem",
                newName: "IX_ShipmentInvoiceItem_LastUpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoicesItems_LastDeletedById",
                table: "ShipmentInvoiceItem",
                newName: "IX_ShipmentInvoiceItem_LastDeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_ShipmentInvoicesItems_CreatedById",
                table: "ShipmentInvoiceItem",
                newName: "IX_ShipmentInvoiceItem_CreatedById");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ShipmentInvoiceItem",
                table: "ShipmentInvoiceItem",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoiceItem_Manufacturers_ManufacturerId",
                table: "ShipmentInvoiceItem",
                column: "ManufacturerId",
                principalTable: "Manufacturers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoiceItem_Materials_MaterialId",
                table: "ShipmentInvoiceItem",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoiceItem_ShipmentInvoices_ShipmentInvoiceId",
                table: "ShipmentInvoiceItem",
                column: "ShipmentInvoiceId",
                principalTable: "ShipmentInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoiceItem_UnitOfMeasures_UoMId",
                table: "ShipmentInvoiceItem",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoiceItem_users_CreatedById",
                table: "ShipmentInvoiceItem",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoiceItem_users_LastDeletedById",
                table: "ShipmentInvoiceItem",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentInvoiceItem_users_LastUpdatedById",
                table: "ShipmentInvoiceItem",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");
        }
    }
}
