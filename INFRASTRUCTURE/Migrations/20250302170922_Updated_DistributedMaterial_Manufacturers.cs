using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Updated_DistributedMaterial_Manufacturers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DistributedRequisitionMaterials_Manufacturers_ManufacturerId",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropIndex(
                name: "IX_DistributedRequisitionMaterials_ManufacturerId",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropColumn(
                name: "ManufacturerId",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.AddColumn<Guid>(
                name: "DistributedRequisitionMaterialId",
                table: "Manufacturers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Manufacturers_DistributedRequisitionMaterialId",
                table: "Manufacturers",
                column: "DistributedRequisitionMaterialId");

            migrationBuilder.AddForeignKey(
                name: "FK_Manufacturers_DistributedRequisitionMaterials_DistributedRe~",
                table: "Manufacturers",
                column: "DistributedRequisitionMaterialId",
                principalTable: "DistributedRequisitionMaterials",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Manufacturers_DistributedRequisitionMaterials_DistributedRe~",
                table: "Manufacturers");

            migrationBuilder.DropIndex(
                name: "IX_Manufacturers_DistributedRequisitionMaterialId",
                table: "Manufacturers");

            migrationBuilder.DropColumn(
                name: "DistributedRequisitionMaterialId",
                table: "Manufacturers");

            migrationBuilder.AddColumn<Guid>(
                name: "ManufacturerId",
                table: "DistributedRequisitionMaterials",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DistributedRequisitionMaterials_ManufacturerId",
                table: "DistributedRequisitionMaterials",
                column: "ManufacturerId");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributedRequisitionMaterials_Manufacturers_ManufacturerId",
                table: "DistributedRequisitionMaterials",
                column: "ManufacturerId",
                principalTable: "Manufacturers",
                principalColumn: "Id");
        }
    }
}
