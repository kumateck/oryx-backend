using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class FinalizeRoutineWorksheet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "WorksheetFinalizedAt",
                table: "RoutineTracks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorksheetFinalizedById",
                table: "RoutineTracks",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorksheetFinalizedAt",
                table: "RoutineTracks");

            migrationBuilder.DropColumn(
                name: "WorksheetFinalizedById",
                table: "RoutineTracks");
        }
    }
}
