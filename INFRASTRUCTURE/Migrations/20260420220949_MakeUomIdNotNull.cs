using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class MakeUomIdNotNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatches_UnitOfMeasures_UoMId",
                table: "MaterialBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialDepartments_UnitOfMeasures_UoMId",
                table: "MaterialDepartments");

            migrationBuilder.AlterColumn<Guid>(
                name: "UoMId",
                table: "MaterialDepartments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UoMId",
                table: "MaterialBatches",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatches_UnitOfMeasures_UoMId",
                table: "MaterialBatches",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialDepartments_UnitOfMeasures_UoMId",
                table: "MaterialDepartments",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatches_UnitOfMeasures_UoMId",
                table: "MaterialBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialDepartments_UnitOfMeasures_UoMId",
                table: "MaterialDepartments");

            migrationBuilder.AlterColumn<Guid>(
                name: "UoMId",
                table: "MaterialDepartments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "UoMId",
                table: "MaterialBatches",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatches_UnitOfMeasures_UoMId",
                table: "MaterialBatches",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialDepartments_UnitOfMeasures_UoMId",
                table: "MaterialDepartments",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");
        }
    }
}
