using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class FixProductionOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductionOrderCode",
                table: "ProductionOrders");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "ProductionOrders",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Code",
                table: "ProductionOrders");

            migrationBuilder.AddColumn<string>(
                name: "ProductionOrderCode",
                table: "ProductionOrders",
                type: "text",
                nullable: true);
        }
    }
}
