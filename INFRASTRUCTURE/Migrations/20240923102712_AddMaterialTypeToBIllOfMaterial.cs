using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialTypeToBIllOfMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "BillOfMaterialItems");

            migrationBuilder.AlterColumn<int>(
                name: "Version",
                table: "ProductBillOfMaterials",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialTypeId",
                table: "BillOfMaterialItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillOfMaterialItems_MaterialTypeId",
                table: "BillOfMaterialItems",
                column: "MaterialTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_BillOfMaterialItems_MaterialTypes_MaterialTypeId",
                table: "BillOfMaterialItems",
                column: "MaterialTypeId",
                principalTable: "MaterialTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BillOfMaterialItems_MaterialTypes_MaterialTypeId",
                table: "BillOfMaterialItems");

            migrationBuilder.DropIndex(
                name: "IX_BillOfMaterialItems_MaterialTypeId",
                table: "BillOfMaterialItems");

            migrationBuilder.DropColumn(
                name: "MaterialTypeId",
                table: "BillOfMaterialItems");

            migrationBuilder.AlterColumn<int>(
                name: "Version",
                table: "ProductBillOfMaterials",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "BillOfMaterialItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
