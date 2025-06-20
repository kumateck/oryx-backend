using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddDescriptionToFormField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Response",
                table: "FormFields");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "FormFields",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "FormFields");

            migrationBuilder.AddColumn<string>(
                name: "Response",
                table: "FormFields",
                type: "character varying(1000000)",
                maxLength: 1000000,
                nullable: true);
        }
    }
}
