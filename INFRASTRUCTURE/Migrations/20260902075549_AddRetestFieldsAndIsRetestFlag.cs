using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddRetestFieldsAndIsRetestFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RetestNotes",
                table: "MaterialBatches",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RetestReason",
                table: "MaterialBatches",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRetest",
                table: "AnalyticalTestRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RetestNotes",
                table: "MaterialBatches");

            migrationBuilder.DropColumn(
                name: "RetestReason",
                table: "MaterialBatches");

            migrationBuilder.DropColumn(
                name: "IsRetest",
                table: "AnalyticalTestRequests");
        }
    }
}
