using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddFormulaRevisionAuthoringPayload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthoringPayloadHash",
                table: "FormulaRevisions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthoringPayloadJson",
                table: "FormulaRevisions",
                type: "text",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_FormulaRevision_AuthoringPayload",
                table: "FormulaRevisions",
                sql: "(\"AuthoringPayloadJson\" IS NULL AND \"AuthoringPayloadHash\" IS NULL) OR (\"AuthoringPayloadJson\" IS NOT NULL AND octet_length(\"AuthoringPayloadJson\") BETWEEN 1 AND 2097152 AND \"AuthoringPayloadHash\" ~ '^[a-f0-9]{64}$')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_FormulaRevision_AuthoringPayload",
                table: "FormulaRevisions");

            migrationBuilder.DropColumn(
                name: "AuthoringPayloadHash",
                table: "FormulaRevisions");

            migrationBuilder.DropColumn(
                name: "AuthoringPayloadJson",
                table: "FormulaRevisions");
        }
    }
}
