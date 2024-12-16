using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class FromLocationNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatchMovements_WarehouseLocations_FromLocationId",
                table: "MaterialBatchMovements");

            migrationBuilder.AlterColumn<Guid>(
                name: "FromLocationId",
                table: "MaterialBatchMovements",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatchMovements_WarehouseLocations_FromLocationId",
                table: "MaterialBatchMovements",
                column: "FromLocationId",
                principalTable: "WarehouseLocations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatchMovements_WarehouseLocations_FromLocationId",
                table: "MaterialBatchMovements");

            migrationBuilder.AlterColumn<Guid>(
                name: "FromLocationId",
                table: "MaterialBatchMovements",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatchMovements_WarehouseLocations_FromLocationId",
                table: "MaterialBatchMovements",
                column: "FromLocationId",
                principalTable: "WarehouseLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
