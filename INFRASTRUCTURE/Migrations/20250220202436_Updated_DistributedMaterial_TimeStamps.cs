using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class Updated_DistributedMaterial_TimeStamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CheckedAt",
                table: "DistributedRequisitionMaterials",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DistributedAt",
                table: "DistributedRequisitionMaterials",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GrnGeneratedAt",
                table: "DistributedRequisitionMaterials",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckedAt",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropColumn(
                name: "DistributedAt",
                table: "DistributedRequisitionMaterials");

            migrationBuilder.DropColumn(
                name: "GrnGeneratedAt",
                table: "DistributedRequisitionMaterials");
        }
    }
}
