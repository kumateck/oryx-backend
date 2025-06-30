using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class RefactorDepartmentNameToDepartmentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepartmentName",
                table: "StaffRequisitions");

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "StaffRequisitions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_StaffRequisitions_DepartmentId",
                table: "StaffRequisitions",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_StaffRequisitions_Departments_DepartmentId",
                table: "StaffRequisitions",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StaffRequisitions_Departments_DepartmentId",
                table: "StaffRequisitions");

            migrationBuilder.DropIndex(
                name: "IX_StaffRequisitions_DepartmentId",
                table: "StaffRequisitions");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "StaffRequisitions");

            migrationBuilder.AddColumn<string>(
                name: "DepartmentName",
                table: "StaffRequisitions",
                type: "text",
                nullable: true);
        }
    }
}
