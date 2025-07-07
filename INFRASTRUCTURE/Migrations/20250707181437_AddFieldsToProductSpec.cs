using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldsToProductSpec : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Claim",
                table: "ProductSpecifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Label",
                table: "ProductSpecifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackingStyle",
                table: "ProductSpecifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShelfLife",
                table: "ProductSpecifications",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Claim",
                table: "ProductSpecifications");

            migrationBuilder.DropColumn(
                name: "Label",
                table: "ProductSpecifications");

            migrationBuilder.DropColumn(
                name: "PackingStyle",
                table: "ProductSpecifications");

            migrationBuilder.DropColumn(
                name: "ShelfLife",
                table: "ProductSpecifications");
        }
    }
}
