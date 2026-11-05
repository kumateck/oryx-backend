using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddMigrationBatchNumberCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NewBatchNumber",
                table: "MigrationRetestDateChanges",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousBatchNumber",
                table: "MigrationRetestDateChanges",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewBatchNumber",
                table: "MigrationRetestDateChanges");

            migrationBuilder.DropColumn(
                name: "PreviousBatchNumber",
                table: "MigrationRetestDateChanges");
        }
    }
}
