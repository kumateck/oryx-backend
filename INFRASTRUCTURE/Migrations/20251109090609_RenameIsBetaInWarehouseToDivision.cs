using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class RenameIsBetaInWarehouseToDivision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBeta",
                table: "Warehouses");

            migrationBuilder.AddColumn<int>(
                name: "Division",
                table: "Warehouses",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Division",
                table: "Warehouses");

            migrationBuilder.AddColumn<bool>(
                name: "IsBeta",
                table: "Warehouses",
                type: "boolean",
                nullable: true);
        }
    }
}
