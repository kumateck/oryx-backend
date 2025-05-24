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
                table: "MaterialAnalyticalRawData",
                newName: "MaterialStandardTestProcedureId");

            migrationBuilder.RenameIndex(
                name: "IX_AnalyticalRawData_MaterialStandardTestProceduresId",
                table: "MaterialAnalyticalRawData",
                newName: "IX_AnalyticalRawData_MaterialStandardTestProcedureId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MaterialStandardTestProcedureId",
                table: "MaterialAnalyticalRawData",
                newName: "MaterialStandardTestProceduresId");

            migrationBuilder.RenameIndex(
                name: "IX_AnalyticalRawData_MaterialStandardTestProcedureId",
                table: "MaterialAnalyticalRawData",
                newName: "IX_AnalyticalRawData_MaterialStandardTestProceduresId");
        }
    }
}
