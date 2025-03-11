using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Updated_MaterialBatch_PackageStyle_v2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ContainerPackageStyleId",
                table: "MaterialBatches",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialBatches_ContainerPackageStyleId",
                table: "MaterialBatches",
                column: "ContainerPackageStyleId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialBatches_PackageStyles_ContainerPackageStyleId",
                table: "MaterialBatches",
                column: "ContainerPackageStyleId",
                principalTable: "PackageStyles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialBatches_PackageStyles_ContainerPackageStyleId",
                table: "MaterialBatches");

            migrationBuilder.DropIndex(
                name: "IX_MaterialBatches_ContainerPackageStyleId",
                table: "MaterialBatches");

            migrationBuilder.DropColumn(
                name: "ContainerPackageStyleId",
                table: "MaterialBatches");
        }
    }
}
