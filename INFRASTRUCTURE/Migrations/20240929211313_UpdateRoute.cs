using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRoute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Routes_BillOfMaterialItems_BillOfMaterialItemId",
                table: "Routes");

            migrationBuilder.DropIndex(
                name: "IX_Routes_BillOfMaterialItemId",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "BillOfMaterialItemId",
                table: "Routes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BillOfMaterialItemId",
                table: "Routes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Routes_BillOfMaterialItemId",
                table: "Routes",
                column: "BillOfMaterialItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_BillOfMaterialItems_BillOfMaterialItemId",
                table: "Routes",
                column: "BillOfMaterialItemId",
                principalTable: "BillOfMaterialItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
