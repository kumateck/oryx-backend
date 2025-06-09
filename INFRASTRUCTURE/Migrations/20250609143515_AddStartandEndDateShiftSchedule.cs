using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddStartandEndDateShiftSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "ShiftSchedules");

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "ShiftSchedules",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "ShiftSchedules",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "ScheduleStatus",
                table: "ShiftSchedules",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "ShiftSchedules");

            migrationBuilder.DropColumn(
                name: "ScheduleStatus",
                table: "ShiftSchedules");

            migrationBuilder.AlterColumn<int>(
                name: "StartDate",
                table: "ShiftSchedules",
                type: "integer",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
            
            migrationBuilder.AddColumn<int>(
                name: "StartDate",
                table: "ShiftSchedules",
                type: "integer",
                nullable: true);
        }
    }
}
