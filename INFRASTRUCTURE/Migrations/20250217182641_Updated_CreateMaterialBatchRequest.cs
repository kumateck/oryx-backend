using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Updated_CreateMaterialBatchRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatches_UnitOfMeasures_UoMId",
                table: "MaterialBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_Sr_UnitOfMeasures_UoMId",
                table: "Sr");

            migrationBuilder.AlterColumn<Guid>(
                name: "UoMId",
                table: "Sr",
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
                name: "FK_Sr_UnitOfMeasures_UoMId",
                table: "Sr",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatches_UnitOfMeasures_UoMId",
                table: "MaterialBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_Sr_UnitOfMeasures_UoMId",
                table: "Sr");

            migrationBuilder.AlterColumn<Guid>(
                name: "UoMId",
                table: "Sr",
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
                name: "FK_Sr_UnitOfMeasures_UoMId",
                table: "Sr",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
