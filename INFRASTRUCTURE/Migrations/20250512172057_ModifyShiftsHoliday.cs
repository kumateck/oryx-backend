using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class ModifyShiftsHoliday : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Shifts",
                table: "Holidays");

            migrationBuilder.AddColumn<Guid>(
                name: "HolidayId",
                table: "ShiftSchedules",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShiftSchedules_HolidayId",
                table: "ShiftSchedules",
                column: "HolidayId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShiftSchedules_Holidays_HolidayId",
                table: "ShiftSchedules",
                column: "HolidayId",
                principalTable: "Holidays",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShiftSchedules_Holidays_HolidayId",
                table: "ShiftSchedules");

            migrationBuilder.DropIndex(
                name: "IX_ShiftSchedules_HolidayId",
                table: "ShiftSchedules");

            migrationBuilder.DropColumn(
                name: "HolidayId",
                table: "ShiftSchedules");

            migrationBuilder.AddColumn<List<Guid>>(
                name: "Shifts",
                table: "Holidays",
                type: "uuid[]",
                nullable: true);
        }
    }
}
