using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddProductAssayReporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssayReportingSnapshotJson",
                table: "Responses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssayReportingRulesJson",
                table: "ProductSpecifications",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssayReportingSnapshotJson",
                table: "Responses");

            migrationBuilder.DropColumn(
                name: "AssayReportingRulesJson",
                table: "ProductSpecifications");
        }
    }
}
