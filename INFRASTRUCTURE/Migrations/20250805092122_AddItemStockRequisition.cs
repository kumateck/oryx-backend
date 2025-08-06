using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddItemStockRequisition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Vendors_VendorId",
                table: "Items");

            migrationBuilder.RenameColumn(
                name: "VendorId",
                table: "Items",
                newName: "ItemStockRequisitionId");

            migrationBuilder.RenameIndex(
                name: "IX_Items_VendorId",
                table: "Items",
                newName: "IX_Items_ItemStockRequisitionId");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Items",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ItemStockRequisitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequisitionNo = table.Column<string>(type: "text", nullable: true),
                    RequisitionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequestedById = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Justification = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemStockRequisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemStockRequisitions_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemStockRequisitions_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemStockRequisitions_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemStockRequisitions_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ItemStockRequisitions_users_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VendorItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    LastUpdatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDeletedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorItem_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VendorItem_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VendorItem_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VendorItem_users_LastDeletedById",
                        column: x => x.LastDeletedById,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VendorItem_users_LastUpdatedById",
                        column: x => x.LastUpdatedById,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemStockRequisitions_CreatedById",
                table: "ItemStockRequisitions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemStockRequisitions_DepartmentId",
                table: "ItemStockRequisitions",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemStockRequisitions_LastDeletedById",
                table: "ItemStockRequisitions",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemStockRequisitions_LastUpdatedById",
                table: "ItemStockRequisitions",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ItemStockRequisitions_RequestedById",
                table: "ItemStockRequisitions",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_VendorItem_CreatedById",
                table: "VendorItem",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_VendorItem_ItemId",
                table: "VendorItem",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorItem_LastDeletedById",
                table: "VendorItem",
                column: "LastDeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_VendorItem_LastUpdatedById",
                table: "VendorItem",
                column: "LastUpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_VendorItem_VendorId",
                table: "VendorItem",
                column: "VendorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_ItemStockRequisitions_ItemStockRequisitionId",
                table: "Items",
                column: "ItemStockRequisitionId",
                principalTable: "ItemStockRequisitions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_ItemStockRequisitions_ItemStockRequisitionId",
                table: "Items");

            migrationBuilder.DropTable(
                name: "ItemStockRequisitions");

            migrationBuilder.DropTable(
                name: "VendorItem");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Items");

            migrationBuilder.RenameColumn(
                name: "ItemStockRequisitionId",
                table: "Items",
                newName: "VendorId");

            migrationBuilder.RenameIndex(
                name: "IX_Items_ItemStockRequisitionId",
                table: "Items",
                newName: "IX_Items_VendorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Vendors_VendorId",
                table: "Items",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id");
        }
    }
}
