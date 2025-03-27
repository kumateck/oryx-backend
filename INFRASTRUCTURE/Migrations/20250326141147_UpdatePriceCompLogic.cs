using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePriceCompLogic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderItem_Currencies_CurrencyId",
                table: "PurchaseOrderItem");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderItem_Materials_MaterialId",
                table: "PurchaseOrderItem");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderItem_PurchaseOrders_PurchaseOrderId",
                table: "PurchaseOrderItem");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderItem_UnitOfMeasures_UoMId",
                table: "PurchaseOrderItem");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderItem_users_CreatedById",
                table: "PurchaseOrderItem");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderItem_users_LastDeletedById",
                table: "PurchaseOrderItem");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderItem_users_LastUpdatedById",
                table: "PurchaseOrderItem");

            migrationBuilder.DropForeignKey(
                name: "FK_RevisedPurchaseOrderItem_RevisedPurchaseOrders_RevisedPurch~",
                table: "RevisedPurchaseOrderItem");

            migrationBuilder.DropForeignKey(
                name: "FK_RevisedPurchaseOrders_PurchaseOrders_PurchaseOrderId",
                table: "RevisedPurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_RevisedPurchaseOrders_users_CreatedById",
                table: "RevisedPurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_RevisedPurchaseOrders_users_LastDeletedById",
                table: "RevisedPurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_RevisedPurchaseOrders_users_LastUpdatedById",
                table: "RevisedPurchaseOrders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RevisedPurchaseOrders",
                table: "RevisedPurchaseOrders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PurchaseOrderItem",
                table: "PurchaseOrderItem");

            migrationBuilder.DropColumn(
                name: "Processed",
                table: "SupplierQuotations");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "RevisedPurchaseOrders");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "RevisedPurchaseOrders");

            migrationBuilder.DropColumn(
                name: "DeliveryDate",
                table: "RevisedPurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ExpectedDeliveryDate",
                table: "RevisedPurchaseOrders");

            migrationBuilder.DropColumn(
                name: "RequestDate",
                table: "RevisedPurchaseOrders");

            migrationBuilder.DropColumn(
                name: "SentAt",
                table: "RevisedPurchaseOrders");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "RevisedPurchaseOrders");

            migrationBuilder.RenameTable(
                name: "RevisedPurchaseOrders",
                newName: "RevisedPurchaseOrder");

            migrationBuilder.RenameTable(
                name: "PurchaseOrderItem",
                newName: "PurchaseOrderItems");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "RevisedPurchaseOrder",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedById",
                table: "RevisedPurchaseOrder",
                newName: "UoMId");

            migrationBuilder.RenameColumn(
                name: "LastDeletedById",
                table: "RevisedPurchaseOrder",
                newName: "PurchaseOrderItemId");

            migrationBuilder.RenameColumn(
                name: "CreatedById",
                table: "RevisedPurchaseOrder",
                newName: "MaterialId");

            migrationBuilder.RenameIndex(
                name: "IX_RevisedPurchaseOrders_PurchaseOrderId",
                table: "RevisedPurchaseOrder",
                newName: "IX_RevisedPurchaseOrder_PurchaseOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_RevisedPurchaseOrders_LastUpdatedById",
                table: "RevisedPurchaseOrder",
                newName: "IX_RevisedPurchaseOrder_UoMId");

            migrationBuilder.RenameIndex(
                name: "IX_RevisedPurchaseOrders_LastDeletedById",
                table: "RevisedPurchaseOrder",
                newName: "IX_RevisedPurchaseOrder_PurchaseOrderItemId");

            migrationBuilder.RenameIndex(
                name: "IX_RevisedPurchaseOrders_CreatedById",
                table: "RevisedPurchaseOrder",
                newName: "IX_RevisedPurchaseOrder_MaterialId");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrderItem_UoMId",
                table: "PurchaseOrderItems",
                newName: "IX_PurchaseOrderItems_UoMId");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrderItem_PurchaseOrderId",
                table: "PurchaseOrderItems",
                newName: "IX_PurchaseOrderItems_PurchaseOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrderItem_MaterialId",
                table: "PurchaseOrderItems",
                newName: "IX_PurchaseOrderItems_MaterialId");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrderItem_LastUpdatedById",
                table: "PurchaseOrderItems",
                newName: "IX_PurchaseOrderItems_LastUpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrderItem_LastDeletedById",
                table: "PurchaseOrderItems",
                newName: "IX_PurchaseOrderItems_LastDeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrderItem_CurrencyId",
                table: "PurchaseOrderItems",
                newName: "IX_PurchaseOrderItems_CurrencyId");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrderItem_CreatedById",
                table: "PurchaseOrderItems",
                newName: "IX_PurchaseOrderItems_CreatedById");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "SupplierQuotationItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "PurchaseOrderId",
                table: "RevisedPurchaseOrder",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "CurrencyId",
                table: "RevisedPurchaseOrder",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "RevisedPurchaseOrder",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "RevisedPurchaseOrder",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_RevisedPurchaseOrder",
                table: "RevisedPurchaseOrder",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PurchaseOrderItems",
                table: "PurchaseOrderItems",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_RevisedPurchaseOrder_CurrencyId",
                table: "RevisedPurchaseOrder",
                column: "CurrencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderItems_Currencies_CurrencyId",
                table: "PurchaseOrderItems",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderItems_Materials_MaterialId",
                table: "PurchaseOrderItems",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId",
                table: "PurchaseOrderItems",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderItems_UnitOfMeasures_UoMId",
                table: "PurchaseOrderItems",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderItems_users_CreatedById",
                table: "PurchaseOrderItems",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderItems_users_LastDeletedById",
                table: "PurchaseOrderItems",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderItems_users_LastUpdatedById",
                table: "PurchaseOrderItems",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RevisedPurchaseOrder_Currencies_CurrencyId",
                table: "RevisedPurchaseOrder",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RevisedPurchaseOrder_Materials_MaterialId",
                table: "RevisedPurchaseOrder",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RevisedPurchaseOrder_PurchaseOrderItems_PurchaseOrderItemId",
                table: "RevisedPurchaseOrder",
                column: "PurchaseOrderItemId",
                principalTable: "PurchaseOrderItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RevisedPurchaseOrder_PurchaseOrders_PurchaseOrderId",
                table: "RevisedPurchaseOrder",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RevisedPurchaseOrder_UnitOfMeasures_UoMId",
                table: "RevisedPurchaseOrder",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RevisedPurchaseOrderItem_RevisedPurchaseOrder_RevisedPurcha~",
                table: "RevisedPurchaseOrderItem",
                column: "RevisedPurchaseOrderId",
                principalTable: "RevisedPurchaseOrder",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderItems_Currencies_CurrencyId",
                table: "PurchaseOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderItems_Materials_MaterialId",
                table: "PurchaseOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId",
                table: "PurchaseOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderItems_UnitOfMeasures_UoMId",
                table: "PurchaseOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderItems_users_CreatedById",
                table: "PurchaseOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderItems_users_LastDeletedById",
                table: "PurchaseOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderItems_users_LastUpdatedById",
                table: "PurchaseOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_RevisedPurchaseOrder_Currencies_CurrencyId",
                table: "RevisedPurchaseOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_RevisedPurchaseOrder_Materials_MaterialId",
                table: "RevisedPurchaseOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_RevisedPurchaseOrder_PurchaseOrderItems_PurchaseOrderItemId",
                table: "RevisedPurchaseOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_RevisedPurchaseOrder_PurchaseOrders_PurchaseOrderId",
                table: "RevisedPurchaseOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_RevisedPurchaseOrder_UnitOfMeasures_UoMId",
                table: "RevisedPurchaseOrder");

            migrationBuilder.DropForeignKey(
                name: "FK_RevisedPurchaseOrderItem_RevisedPurchaseOrder_RevisedPurcha~",
                table: "RevisedPurchaseOrderItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RevisedPurchaseOrder",
                table: "RevisedPurchaseOrder");

            migrationBuilder.DropIndex(
                name: "IX_RevisedPurchaseOrder_CurrencyId",
                table: "RevisedPurchaseOrder");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PurchaseOrderItems",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "SupplierQuotationItems");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "RevisedPurchaseOrder");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "RevisedPurchaseOrder");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "RevisedPurchaseOrder");

            migrationBuilder.RenameTable(
                name: "RevisedPurchaseOrder",
                newName: "RevisedPurchaseOrders");

            migrationBuilder.RenameTable(
                name: "PurchaseOrderItems",
                newName: "PurchaseOrderItem");

            migrationBuilder.RenameColumn(
                name: "UoMId",
                table: "RevisedPurchaseOrders",
                newName: "LastUpdatedById");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "RevisedPurchaseOrders",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "PurchaseOrderItemId",
                table: "RevisedPurchaseOrders",
                newName: "LastDeletedById");

            migrationBuilder.RenameColumn(
                name: "MaterialId",
                table: "RevisedPurchaseOrders",
                newName: "CreatedById");

            migrationBuilder.RenameIndex(
                name: "IX_RevisedPurchaseOrder_UoMId",
                table: "RevisedPurchaseOrders",
                newName: "IX_RevisedPurchaseOrders_LastUpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_RevisedPurchaseOrder_PurchaseOrderItemId",
                table: "RevisedPurchaseOrders",
                newName: "IX_RevisedPurchaseOrders_LastDeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_RevisedPurchaseOrder_PurchaseOrderId",
                table: "RevisedPurchaseOrders",
                newName: "IX_RevisedPurchaseOrders_PurchaseOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_RevisedPurchaseOrder_MaterialId",
                table: "RevisedPurchaseOrders",
                newName: "IX_RevisedPurchaseOrders_CreatedById");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrderItems_UoMId",
                table: "PurchaseOrderItem",
                newName: "IX_PurchaseOrderItem_UoMId");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrderItems_PurchaseOrderId",
                table: "PurchaseOrderItem",
                newName: "IX_PurchaseOrderItem_PurchaseOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrderItems_MaterialId",
                table: "PurchaseOrderItem",
                newName: "IX_PurchaseOrderItem_MaterialId");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrderItems_LastUpdatedById",
                table: "PurchaseOrderItem",
                newName: "IX_PurchaseOrderItem_LastUpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrderItems_LastDeletedById",
                table: "PurchaseOrderItem",
                newName: "IX_PurchaseOrderItem_LastDeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrderItems_CurrencyId",
                table: "PurchaseOrderItem",
                newName: "IX_PurchaseOrderItem_CurrencyId");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrderItems_CreatedById",
                table: "PurchaseOrderItem",
                newName: "IX_PurchaseOrderItem_CreatedById");

            migrationBuilder.AddColumn<bool>(
                name: "Processed",
                table: "SupplierQuotations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "PurchaseOrderId",
                table: "RevisedPurchaseOrders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "RevisedPurchaseOrders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "RevisedPurchaseOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryDate",
                table: "RevisedPurchaseOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedDeliveryDate",
                table: "RevisedPurchaseOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestDate",
                table: "RevisedPurchaseOrders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "SentAt",
                table: "RevisedPurchaseOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "RevisedPurchaseOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_RevisedPurchaseOrders",
                table: "RevisedPurchaseOrders",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PurchaseOrderItem",
                table: "PurchaseOrderItem",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderItem_Currencies_CurrencyId",
                table: "PurchaseOrderItem",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderItem_Materials_MaterialId",
                table: "PurchaseOrderItem",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderItem_PurchaseOrders_PurchaseOrderId",
                table: "PurchaseOrderItem",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderItem_UnitOfMeasures_UoMId",
                table: "PurchaseOrderItem",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderItem_users_CreatedById",
                table: "PurchaseOrderItem",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderItem_users_LastDeletedById",
                table: "PurchaseOrderItem",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderItem_users_LastUpdatedById",
                table: "PurchaseOrderItem",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RevisedPurchaseOrderItem_RevisedPurchaseOrders_RevisedPurch~",
                table: "RevisedPurchaseOrderItem",
                column: "RevisedPurchaseOrderId",
                principalTable: "RevisedPurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RevisedPurchaseOrders_PurchaseOrders_PurchaseOrderId",
                table: "RevisedPurchaseOrders",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RevisedPurchaseOrders_users_CreatedById",
                table: "RevisedPurchaseOrders",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RevisedPurchaseOrders_users_LastDeletedById",
                table: "RevisedPurchaseOrders",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RevisedPurchaseOrders_users_LastUpdatedById",
                table: "RevisedPurchaseOrders",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");
        }
    }
}
