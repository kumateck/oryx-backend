using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddFormulaSnapshotExecutionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MethodReference",
                table: "ResponseFormulaSnapshots",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultTargetsJson",
                table: "ResponseFormulaSnapshots",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MethodReference",
                table: "ResponseFormulaSnapshots");

            migrationBuilder.DropColumn(
                name: "ResultTargetsJson",
                table: "ResponseFormulaSnapshots");
        }
    }
}
