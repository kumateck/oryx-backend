using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBinCardInformation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Manufacturer",
                table: "ProductBinCardInformation",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Supplier",
                table: "ProductBinCardInformation",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Manufacturer",
                table: "BinCardInformation",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Supplier",
                table: "BinCardInformation",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Manufacturer",
                table: "ProductBinCardInformation");

            migrationBuilder.DropColumn(
                name: "Supplier",
                table: "ProductBinCardInformation");

            migrationBuilder.DropColumn(
                name: "Manufacturer",
                table: "BinCardInformation");

            migrationBuilder.DropColumn(
                name: "Supplier",
                table: "BinCardInformation");
        }
    }
}
