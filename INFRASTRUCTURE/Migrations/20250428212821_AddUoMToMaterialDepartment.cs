using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddUoMToMaterialDepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UoMId",
                table: "MaterialDepartments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialDepartments_UoMId",
                table: "MaterialDepartments",
                column: "UoMId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialDepartments_UnitOfMeasures_UoMId",
                table: "MaterialDepartments",
                column: "UoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialDepartments_UnitOfMeasures_UoMId",
                table: "MaterialDepartments");

            migrationBuilder.DropIndex(
                name: "IX_MaterialDepartments_UoMId",
                table: "MaterialDepartments");

            migrationBuilder.DropColumn(
                name: "UoMId",
                table: "MaterialDepartments");
        }
    }
}
