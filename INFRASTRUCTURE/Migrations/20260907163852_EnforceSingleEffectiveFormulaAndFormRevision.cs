using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSingleEffectiveFormulaAndFormRevision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_FormulaRevisions_FormulaDefinitionId",
                table: "FormulaRevisions",
                column: "FormulaDefinitionId",
                unique: true,
                filter: "\"Status\" = 2 AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FormRevisions_FormId",
                table: "FormRevisions",
                column: "FormId",
                unique: true,
                filter: "\"Status\" = 2 AND \"DeletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FormulaRevisions_FormulaDefinitionId",
                table: "FormulaRevisions");

            migrationBuilder.DropIndex(
                name: "IX_FormRevisions_FormId",
                table: "FormRevisions");
        }
    }
}
