using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Updated_ShipmentDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClearedAt",
                table: "ShipmentDocuments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ShipmentDocuments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "TransitStartedAt",
                table: "ShipmentDocuments",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClearedAt",
                table: "ShipmentDocuments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ShipmentDocuments");

            migrationBuilder.DropColumn(
                name: "TransitStartedAt",
                table: "ShipmentDocuments");
        }
    }
}
