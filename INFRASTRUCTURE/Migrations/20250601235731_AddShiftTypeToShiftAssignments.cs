using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftTypeToShiftAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ShiftTypeId",
                table: "ShiftAssignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAssignments_ShiftTypeId",
                table: "ShiftAssignments",
                column: "ShiftTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShiftAssignments_ShiftTypes_ShiftTypeId",
                table: "ShiftAssignments",
                column: "ShiftTypeId",
                principalTable: "ShiftTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShiftAssignments_ShiftTypes_ShiftTypeId",
                table: "ShiftAssignments");

            migrationBuilder.DropIndex(
                name: "IX_ShiftAssignments_ShiftTypeId",
                table: "ShiftAssignments");

            migrationBuilder.DropColumn(
                name: "ShiftTypeId",
                table: "ShiftAssignments");
        }
    }
}
