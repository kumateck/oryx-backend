using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateShiftSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShiftTypeIds",
                table: "ShiftSchedules");

            migrationBuilder.AddColumn<Guid>(
                name: "ShiftScheduleId",
                table: "ShiftTypes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShiftTypes_ShiftScheduleId",
                table: "ShiftTypes",
                column: "ShiftScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShiftTypes_ShiftSchedules_ShiftScheduleId",
                table: "ShiftTypes",
                column: "ShiftScheduleId",
                principalTable: "ShiftSchedules",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShiftTypes_ShiftSchedules_ShiftScheduleId",
                table: "ShiftTypes");

            migrationBuilder.DropIndex(
                name: "IX_ShiftTypes_ShiftScheduleId",
                table: "ShiftTypes");

            migrationBuilder.DropColumn(
                name: "ShiftScheduleId",
                table: "ShiftTypes");

            migrationBuilder.AddColumn<List<Guid>>(
                name: "ShiftTypeIds",
                table: "ShiftSchedules",
                type: "uuid[]",
                nullable: true);
        }
    }
}
