using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddYieldToFinishedGoodsAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "UnitsPerPack",
                table: "ProductionOrderProductQuantity",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "YieldQuantity",
                table: "ProductionOrderProductQuantity",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitsPerPack",
                table: "AllocateProductQuantity",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "YieldQuantity",
                table: "AllocateProductQuantity",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnitsPerPack",
                table: "ProductionOrderProductQuantity");

            migrationBuilder.DropColumn(
                name: "YieldQuantity",
                table: "ProductionOrderProductQuantity");

            migrationBuilder.DropColumn(
                name: "UnitsPerPack",
                table: "AllocateProductQuantity");

            migrationBuilder.DropColumn(
                name: "YieldQuantity",
                table: "AllocateProductQuantity");
        }
    }
}
