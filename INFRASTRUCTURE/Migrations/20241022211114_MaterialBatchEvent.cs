using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class MaterialBatchEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatchConsumptions_users_ConsumedById",
                table: "MaterialBatchConsumptions");

            migrationBuilder.DropColumn(
                name: "DateConsumed",
                table: "MaterialBatchConsumptions");

            migrationBuilder.RenameColumn(
                name: "QuantityConsumed",
                table: "MaterialBatchConsumptions",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "ConsumedById",
                table: "MaterialBatchConsumptions",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_MaterialBatchConsumptions_ConsumedById",
                table: "MaterialBatchConsumptions",
                newName: "IX_MaterialBatchConsumptions_UserId");

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "MaterialBatchConsumptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatchConsumptions_users_UserId",
                table: "MaterialBatchConsumptions",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatchConsumptions_users_UserId",
                table: "MaterialBatchConsumptions");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "MaterialBatchConsumptions");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "MaterialBatchConsumptions",
                newName: "ConsumedById");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "MaterialBatchConsumptions",
                newName: "QuantityConsumed");

            migrationBuilder.RenameIndex(
                name: "IX_MaterialBatchConsumptions_UserId",
                table: "MaterialBatchConsumptions",
                newName: "IX_MaterialBatchConsumptions_ConsumedById");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateConsumed",
                table: "MaterialBatchConsumptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatchConsumptions_users_ConsumedById",
                table: "MaterialBatchConsumptions",
                column: "ConsumedById",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
