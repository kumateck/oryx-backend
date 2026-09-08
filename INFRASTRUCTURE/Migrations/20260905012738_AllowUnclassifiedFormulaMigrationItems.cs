using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AllowUnclassifiedFormulaMigrationItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_FormulaMigrationItem_Class",
                table: "FormulaMigrationItems");

            migrationBuilder.AlterColumn<int>(
                name: "MigrationClass",
                table: "FormulaMigrationItems",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.Sql(
                "ALTER TABLE \"FormulaMigrationItems\" " +
                "ALTER COLUMN \"MigrationClass\" DROP DEFAULT;");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FormulaMigrationItem_Class",
                table: "FormulaMigrationItems",
                sql: "\"MigrationClass\" IS NULL OR \"MigrationClass\" BETWEEN 0 AND 3");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_FormulaMigrationItem_Class",
                table: "FormulaMigrationItems");

            migrationBuilder.AlterColumn<int>(
                name: "MigrationClass",
                table: "FormulaMigrationItems",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_FormulaMigrationItem_Class",
                table: "FormulaMigrationItems",
                sql: "\"MigrationClass\" BETWEEN 0 AND 3");
        }
    }
}
