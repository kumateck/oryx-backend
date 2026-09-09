using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class HardenStpDocumentControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StpDocumentVersions_StpDocumentId",
                table: "StpDocumentVersions");

            migrationBuilder.CreateIndex(
                name: "IX_StpDocumentVersions_StpDocumentId_VersionNumber",
                table: "StpDocumentVersions",
                columns: new[] { "StpDocumentId", "VersionNumber" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_StpDocumentVersions_Source",
                table: "StpDocumentVersions",
                sql: "\"Source\" IN (0, 1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StpDocumentSignatures_Action",
                table: "StpDocumentSignatures",
                sql: "\"Action\" IN (0, 1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StpDocuments_OwnerType",
                table: "StpDocuments",
                sql: "\"OwnerType\" IN ('MaterialStandardTestProcedure', 'ProductStandardTestProcedure')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StpDocuments_Status",
                table: "StpDocuments",
                sql: "\"Status\" IN (0, 1, 2, 3)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StpDocumentVersions_StpDocumentId_VersionNumber",
                table: "StpDocumentVersions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StpDocumentVersions_Source",
                table: "StpDocumentVersions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StpDocumentSignatures_Action",
                table: "StpDocumentSignatures");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StpDocuments_OwnerType",
                table: "StpDocuments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StpDocuments_Status",
                table: "StpDocuments");

            migrationBuilder.CreateIndex(
                name: "IX_StpDocumentVersions_StpDocumentId",
                table: "StpDocumentVersions",
                column: "StpDocumentId");
        }
    }
}
