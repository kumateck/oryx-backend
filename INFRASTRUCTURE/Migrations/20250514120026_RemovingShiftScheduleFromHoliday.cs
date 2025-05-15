using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class RemovingShiftScheduleFromHoliday : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HolidayShiftSchedule");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartTime",
                table: "ShiftSchedules",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(TimeOnly),
                oldType: "time without time zone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<TimeOnly>(
                name: "StartTime",
                table: "ShiftSchedules",
                type: "time without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateTable(
                name: "HolidayShiftSchedule",
                columns: table => new
                {
                    HolidaysId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShiftSchedulesId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HolidayShiftSchedule", x => new { x.HolidaysId, x.ShiftSchedulesId });
                    table.ForeignKey(
                        name: "FK_HolidayShiftSchedule_Holidays_HolidaysId",
                        column: x => x.HolidaysId,
                        principalTable: "Holidays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HolidayShiftSchedule_ShiftSchedules_ShiftSchedulesId",
                        column: x => x.ShiftSchedulesId,
                        principalTable: "ShiftSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HolidayShiftSchedule_ShiftSchedulesId",
                table: "HolidayShiftSchedule",
                column: "ShiftSchedulesId");
        }
    }
}
