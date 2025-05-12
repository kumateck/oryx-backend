using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddHolidayToShiftSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HolidayShiftSchedule");

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
    }
}
