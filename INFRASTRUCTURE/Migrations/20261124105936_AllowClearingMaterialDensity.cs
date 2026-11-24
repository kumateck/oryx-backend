using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AllowClearingMaterialDensity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Density",
                table: "MaterialDensityMeasurements",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<string>(
                name: "PreviousDepartmentDensitiesJson",
                table: "MaterialDensityMeasurements",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // An older schema cannot represent an explicit clear. Never fabricate a zero
            // measurement or discard the recorded legacy-link evidence during rollback.
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM "MaterialDensityMeasurements"
                        WHERE "Density" IS NULL OR "PreviousDepartmentDensitiesJson" IS NOT NULL) THEN
                        RAISE EXCEPTION 'Cannot roll back material-density audit schema after a density clear';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropColumn(
                name: "PreviousDepartmentDensitiesJson",
                table: "MaterialDensityMeasurements");

            migrationBuilder.AlterColumn<decimal>(
                name: "Density",
                table: "MaterialDensityMeasurements",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);
        }
    }
}
