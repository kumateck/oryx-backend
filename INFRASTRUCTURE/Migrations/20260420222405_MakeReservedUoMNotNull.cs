using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class MakeReservedUoMNotNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatchReservedQuantities_UnitOfMeasures_UoMId",
                table: "MaterialBatchReservedQuantities");

            migrationBuilder.AlterColumn<Guid>(
                name: "UoMId",
                table: "MaterialBatchReservedQuantities",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatchReservedQuantities_UnitOfMeasures_UoMId",
                table: "MaterialBatchReservedQuantities",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatchReservedQuantities_UnitOfMeasures_UoMId",
                table: "MaterialBatchReservedQuantities");

            migrationBuilder.AlterColumn<Guid>(
                name: "UoMId",
                table: "MaterialBatchReservedQuantities",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatchReservedQuantities_UnitOfMeasures_UoMId",
                table: "MaterialBatchReservedQuantities",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");
        }
    }
}
