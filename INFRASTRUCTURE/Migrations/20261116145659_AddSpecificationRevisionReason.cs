using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecificationRevisionReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RevisionReason",
                table: "ProductSpecifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevisionReason",
                table: "MaterialSpecifications",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RevisionReason",
                table: "ProductSpecifications");

            migrationBuilder.DropColumn(
                name: "RevisionReason",
                table: "MaterialSpecifications");
        }
    }
}
