using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddPackDescriptionToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrimaryPackDescription",
                table: "Products",
                type: "character varying(1000000)",
                maxLength: 1000000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryPackDescription",
                table: "Products",
                type: "character varying(1000000)",
                maxLength: 1000000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TertiaryPackDescription",
                table: "Products",
                type: "character varying(1000000)",
                maxLength: 1000000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrimaryPackDescription",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SecondaryPackDescription",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TertiaryPackDescription",
                table: "Products");
        }
    }
}
