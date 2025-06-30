using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftScheduletoShiftType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.CreateTable(
                name: "ShiftScheduleShiftType",
                columns: table => new
                {
                    ShiftSchedulesId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShiftTypesId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftScheduleShiftType", x => new { x.ShiftSchedulesId, x.ShiftTypesId });
                    table.ForeignKey(
                        name: "FK_ShiftScheduleShiftType_ShiftSchedules_ShiftSchedulesId",
                        column: x => x.ShiftSchedulesId,
                        principalTable: "ShiftSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShiftScheduleShiftType_ShiftTypes_ShiftTypesId",
                        column: x => x.ShiftTypesId,
                        principalTable: "ShiftTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShiftScheduleShiftType_ShiftTypesId",
                table: "ShiftScheduleShiftType",
                column: "ShiftTypesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShiftScheduleShiftType");

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
    }
}
