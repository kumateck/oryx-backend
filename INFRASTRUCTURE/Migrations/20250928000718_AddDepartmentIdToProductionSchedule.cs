using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentIdToProductionSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "ProductionSchedules",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSchedules_DepartmentId",
                table: "ProductionSchedules",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionSchedules_Departments_DepartmentId",
                table: "ProductionSchedules",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionSchedules_Departments_DepartmentId",
                table: "ProductionSchedules");

            migrationBuilder.DropIndex(
                name: "IX_ProductionSchedules_DepartmentId",
                table: "ProductionSchedules");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "ProductionSchedules");
        }
    }
}
