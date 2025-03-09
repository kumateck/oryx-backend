using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Updated_MaterialBatch_PackageStyle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ContainerUoMId",
                table: "MaterialBatches",
                type: "uuid",
                nullable: true);

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
    }
}
