using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddDensityUoMToMaterialDepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DensityUoMId",
                table: "MaterialDepartments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialDepartments_DensityUoMId",
                table: "MaterialDepartments",
                column: "DensityUoMId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialDepartments_UnitOfMeasures_DensityUoMId",
                table: "MaterialDepartments",
                column: "DensityUoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialDepartments_UnitOfMeasures_DensityUoMId",
                table: "MaterialDepartments");

            migrationBuilder.DropIndex(
                name: "IX_MaterialDepartments_DensityUoMId",
                table: "MaterialDepartments");

            migrationBuilder.DropColumn(
                name: "DensityUoMId",
                table: "MaterialDepartments");
        }
    }
}
