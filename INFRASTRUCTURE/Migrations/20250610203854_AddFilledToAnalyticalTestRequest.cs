using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddFilledToAnalyticalTestRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Category",
                table: "AnalyticalTestRequests",
                newName: "State");

            migrationBuilder.AddColumn<string>(
                name: "Filled",
                table: "AnalyticalTestRequests",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Filled",
                table: "AnalyticalTestRequests");

            migrationBuilder.RenameColumn(
                name: "State",
                table: "AnalyticalTestRequests",
                newName: "Category");
        }
    }
}
