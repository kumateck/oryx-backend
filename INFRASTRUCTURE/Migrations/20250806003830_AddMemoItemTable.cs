using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddMemoItemTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MemoItem_Items_ItemId",
                table: "MemoItem");

            migrationBuilder.DropForeignKey(
                name: "FK_MemoItem_MarketRequisitionVendors_MarketRequisitionVendorId",
                table: "MemoItem");

            migrationBuilder.DropForeignKey(
                name: "FK_MemoItem_Memos_MemoId",
                table: "MemoItem");

            migrationBuilder.DropForeignKey(
                name: "FK_MemoItem_UnitOfMeasures_UoMId",
                table: "MemoItem");

            migrationBuilder.DropForeignKey(
                name: "FK_MemoItem_VendorQuotationItems_VendorQuotationItemId",
                table: "MemoItem");

            migrationBuilder.DropForeignKey(
                name: "FK_MemoItem_users_CreatedById",
                table: "MemoItem");

            migrationBuilder.DropForeignKey(
                name: "FK_MemoItem_users_LastDeletedById",
                table: "MemoItem");

            migrationBuilder.DropForeignKey(
                name: "FK_MemoItem_users_LastUpdatedById",
                table: "MemoItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MemoItem",
                table: "MemoItem");

            migrationBuilder.RenameTable(
                name: "MemoItem",
                newName: "MemoItems");

            migrationBuilder.RenameIndex(
                name: "IX_MemoItem_VendorQuotationItemId",
                table: "MemoItems",
                newName: "IX_MemoItems_VendorQuotationItemId");

            migrationBuilder.RenameIndex(
                name: "IX_MemoItem_UoMId",
                table: "MemoItems",
                newName: "IX_MemoItems_UoMId");

            migrationBuilder.RenameIndex(
                name: "IX_MemoItem_MemoId",
                table: "MemoItems",
                newName: "IX_MemoItems_MemoId");

            migrationBuilder.RenameIndex(
                name: "IX_MemoItem_MarketRequisitionVendorId",
                table: "MemoItems",
                newName: "IX_MemoItems_MarketRequisitionVendorId");

            migrationBuilder.RenameIndex(
                name: "IX_MemoItem_LastUpdatedById",
                table: "MemoItems",
                newName: "IX_MemoItems_LastUpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_MemoItem_LastDeletedById",
                table: "MemoItems",
                newName: "IX_MemoItems_LastDeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_MemoItem_ItemId",
                table: "MemoItems",
                newName: "IX_MemoItems_ItemId");

            migrationBuilder.RenameIndex(
                name: "IX_MemoItem_CreatedById",
                table: "MemoItems",
                newName: "IX_MemoItems_CreatedById");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MemoItems",
                table: "MemoItems",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MemoItems_Items_ItemId",
                table: "MemoItems",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MemoItems_MarketRequisitionVendors_MarketRequisitionVendorId",
                table: "MemoItems",
                column: "MarketRequisitionVendorId",
                principalTable: "MarketRequisitionVendors",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MemoItems_Memos_MemoId",
                table: "MemoItems",
                column: "MemoId",
                principalTable: "Memos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MemoItems_UnitOfMeasures_UoMId",
                table: "MemoItems",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MemoItems_VendorQuotationItems_VendorQuotationItemId",
                table: "MemoItems",
                column: "VendorQuotationItemId",
                principalTable: "VendorQuotationItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MemoItems_users_CreatedById",
                table: "MemoItems",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MemoItems_users_LastDeletedById",
                table: "MemoItems",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MemoItems_users_LastUpdatedById",
                table: "MemoItems",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MemoItems_Items_ItemId",
                table: "MemoItems");

            migrationBuilder.DropForeignKey(
                name: "FK_MemoItems_MarketRequisitionVendors_MarketRequisitionVendorId",
                table: "MemoItems");

            migrationBuilder.DropForeignKey(
                name: "FK_MemoItems_Memos_MemoId",
                table: "MemoItems");

            migrationBuilder.DropForeignKey(
                name: "FK_MemoItems_UnitOfMeasures_UoMId",
                table: "MemoItems");

            migrationBuilder.DropForeignKey(
                name: "FK_MemoItems_VendorQuotationItems_VendorQuotationItemId",
                table: "MemoItems");

            migrationBuilder.DropForeignKey(
                name: "FK_MemoItems_users_CreatedById",
                table: "MemoItems");

            migrationBuilder.DropForeignKey(
                name: "FK_MemoItems_users_LastDeletedById",
                table: "MemoItems");

            migrationBuilder.DropForeignKey(
                name: "FK_MemoItems_users_LastUpdatedById",
                table: "MemoItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MemoItems",
                table: "MemoItems");

            migrationBuilder.RenameTable(
                name: "MemoItems",
                newName: "MemoItem");

            migrationBuilder.RenameIndex(
                name: "IX_MemoItems_VendorQuotationItemId",
                table: "MemoItem",
                newName: "IX_MemoItem_VendorQuotationItemId");

            migrationBuilder.RenameIndex(
                name: "IX_MemoItems_UoMId",
                table: "MemoItem",
                newName: "IX_MemoItem_UoMId");

            migrationBuilder.RenameIndex(
                name: "IX_MemoItems_MemoId",
                table: "MemoItem",
                newName: "IX_MemoItem_MemoId");

            migrationBuilder.RenameIndex(
                name: "IX_MemoItems_MarketRequisitionVendorId",
                table: "MemoItem",
                newName: "IX_MemoItem_MarketRequisitionVendorId");

            migrationBuilder.RenameIndex(
                name: "IX_MemoItems_LastUpdatedById",
                table: "MemoItem",
                newName: "IX_MemoItem_LastUpdatedById");

            migrationBuilder.RenameIndex(
                name: "IX_MemoItems_LastDeletedById",
                table: "MemoItem",
                newName: "IX_MemoItem_LastDeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_MemoItems_ItemId",
                table: "MemoItem",
                newName: "IX_MemoItem_ItemId");

            migrationBuilder.RenameIndex(
                name: "IX_MemoItems_CreatedById",
                table: "MemoItem",
                newName: "IX_MemoItem_CreatedById");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MemoItem",
                table: "MemoItem",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MemoItem_Items_ItemId",
                table: "MemoItem",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MemoItem_MarketRequisitionVendors_MarketRequisitionVendorId",
                table: "MemoItem",
                column: "MarketRequisitionVendorId",
                principalTable: "MarketRequisitionVendors",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MemoItem_Memos_MemoId",
                table: "MemoItem",
                column: "MemoId",
                principalTable: "Memos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MemoItem_UnitOfMeasures_UoMId",
                table: "MemoItem",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MemoItem_VendorQuotationItems_VendorQuotationItemId",
                table: "MemoItem",
                column: "VendorQuotationItemId",
                principalTable: "VendorQuotationItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MemoItem_users_CreatedById",
                table: "MemoItem",
                column: "CreatedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MemoItem_users_LastDeletedById",
                table: "MemoItem",
                column: "LastDeletedById",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MemoItem_users_LastUpdatedById",
                table: "MemoItem",
                column: "LastUpdatedById",
                principalTable: "users",
                principalColumn: "Id");
        }
    }
}
