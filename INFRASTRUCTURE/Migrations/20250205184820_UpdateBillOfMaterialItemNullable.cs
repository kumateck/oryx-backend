using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBillOfMaterialItemNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillOfMaterialItems_Materials_MaterialId",
                table: "BillOfMaterialItems");

            migrationBuilder.AlterColumn<Guid>(
                name: "MaterialId",
                table: "BillOfMaterialItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BillOfMaterialItems_Materials_MaterialId",
                table: "BillOfMaterialItems",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillOfMaterialItems_Materials_MaterialId",
                table: "BillOfMaterialItems");

            migrationBuilder.AlterColumn<Guid>(
                name: "MaterialId",
                table: "BillOfMaterialItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_BillOfMaterialItems_Materials_MaterialId",
                table: "BillOfMaterialItems",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id");
        }
    }
}
