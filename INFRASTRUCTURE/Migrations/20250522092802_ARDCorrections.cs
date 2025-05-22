using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class ARDCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MaterialStandardTestProceduresId",
                table: "AnalyticalRawData",
                newName: "MaterialStandardTestProcedureId");

            migrationBuilder.RenameIndex(
                name: "IX_AnalyticalRawData_MaterialStandardTestProceduresId",
                table: "AnalyticalRawData",
                newName: "IX_AnalyticalRawData_MaterialStandardTestProcedureId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MaterialStandardTestProcedureId",
                table: "AnalyticalRawData",
                newName: "MaterialStandardTestProceduresId");

            migrationBuilder.RenameIndex(
                name: "IX_AnalyticalRawData_MaterialStandardTestProcedureId",
                table: "AnalyticalRawData",
                newName: "IX_AnalyticalRawData_MaterialStandardTestProceduresId");
        }
    }
}
