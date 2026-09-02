using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueProductResponseStep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "Responses"
                        WHERE "BatchManufacturingRecordId" IS NOT NULL
                          AND "ProductionActivityStepId" IS NOT NULL
                        GROUP BY "BatchManufacturingRecordId", "ProductionActivityStepId"
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Duplicate product responses exist for a BMR production step; reconcile them before applying this migration.';
                    END IF;
                END $$;
                """
            );

            migrationBuilder.CreateIndex(
                name: "IX_Responses_BatchManufacturingRecordId_ProductionActivityStep~",
                table: "Responses",
                columns: new[] { "BatchManufacturingRecordId", "ProductionActivityStepId" },
                unique: true,
                filter: "\"BatchManufacturingRecordId\" IS NOT NULL AND \"ProductionActivityStepId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Responses_BatchManufacturingRecordId_ProductionActivityStep~",
                table: "Responses");

        }
    }
}
