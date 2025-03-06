using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Updated_MaterialBatch_UoMs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ContainerUoMId",
                table: "MaterialBatches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfContainers",
                table: "MaterialBatches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityPerContainer",
                table: "MaterialBatches",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatches_ContainerUoMId",
                table: "MaterialBatches",
                column: "ContainerUoMId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatches_UnitOfMeasures_ContainerUoMId",
                table: "MaterialBatches",
                column: "ContainerUoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatches_UnitOfMeasures_ContainerUoMId",
                table: "MaterialBatches");

            migrationBuilder.DropIndex(
                name: "IX_MaterialBatches_ContainerUoMId",
                table: "MaterialBatches");

            migrationBuilder.DropColumn(
                name: "ContainerUoMId",
                table: "MaterialBatches");

            migrationBuilder.DropColumn(
                name: "NumberOfContainers",
                table: "MaterialBatches");

            migrationBuilder.DropColumn(
                name: "QuantityPerContainer",
                table: "MaterialBatches");
        }
    }
}
