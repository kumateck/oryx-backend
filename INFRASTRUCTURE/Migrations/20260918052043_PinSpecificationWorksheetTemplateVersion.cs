using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class PinSpecificationWorksheetTemplateVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QcSpecificationWorksheetLinks_WorksheetTemplateId",
                table: "QcSpecificationWorksheetLinks");

            migrationBuilder.AddColumn<int>(
                name: "WorksheetTemplateVersion",
                table: "QcSpecificationWorksheetLinks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_QcSpecificationWorksheetLinks_WorksheetTemplateId_Worksheet~",
                table: "QcSpecificationWorksheetLinks",
                columns: new[] { "WorksheetTemplateId", "WorksheetTemplateVersion" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QcSpecificationWorksheetLinks_WorksheetTemplateId_Worksheet~",
                table: "QcSpecificationWorksheetLinks");

            migrationBuilder.DropColumn(
                name: "WorksheetTemplateVersion",
                table: "QcSpecificationWorksheetLinks");

            migrationBuilder.CreateIndex(
                name: "IX_QcSpecificationWorksheetLinks_WorksheetTemplateId",
                table: "QcSpecificationWorksheetLinks",
                column: "WorksheetTemplateId");
        }
    }
}
