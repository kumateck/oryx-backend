using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class RefactorNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Departments_DepartmentId",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_Items_InventoryTypes_ItemTypeId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_DepartmentId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_ItemTypeId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "InitialStockQuantity",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "InventoryTypeId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ItemTypeId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "MaterialName",
                table: "Items");

            migrationBuilder.RenameColumn(
                name: "Remarks",
                table: "Items",
                newName: "Name");

            migrationBuilder.AddColumn<int>(
                name: "Store",
                table: "Items",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Store",
                table: "Items");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Items",
                newName: "Remarks");

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "Items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "InitialStockQuantity",
                table: "Items",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "InventoryTypeId",
                table: "Items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ItemTypeId",
                table: "Items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaterialName",
                table: "Items",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_DepartmentId",
                table: "Items",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_ItemTypeId",
                table: "Items",
                column: "ItemTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Departments_DepartmentId",
                table: "Items",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Items_InventoryTypes_ItemTypeId",
                table: "Items",
                column: "ItemTypeId",
                principalTable: "InventoryTypes",
                principalColumn: "Id");
        }
    }
}
