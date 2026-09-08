using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddFormulaSubmissionSetIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ResponseFormulaSubmissionSets_ResponseId_SetHash",
                table: "ResponseFormulaSubmissionSets",
                columns: new[] { "ResponseId", "SetHash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ResponseFormulaSubmissionSets_ResponseId_SetHash",
                table: "ResponseFormulaSubmissionSets");
        }
    }
}
