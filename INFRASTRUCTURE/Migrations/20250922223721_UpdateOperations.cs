using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Action",
                table: "Operations");

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "Operations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Operations_DepartmentId",
                table: "Operations",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Operations_Departments_DepartmentId",
                table: "Operations",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Operations_Departments_DepartmentId",
                table: "Operations");

            migrationBuilder.DropIndex(
                name: "IX_Operations_DepartmentId",
                table: "Operations");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Operations");

            migrationBuilder.AddColumn<int>(
                name: "Action",
                table: "Operations",
                type: "integer",
                nullable: true);
        }
    }
}
