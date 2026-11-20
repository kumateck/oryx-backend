using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <summary>
    /// Unstarting a batch now releases its batch number. Batches unstarted earlier kept it (for example NL006 L006K005),
    /// so it could not be issued again. A batch that has been unstarted and has neither a live run nor a numbered live
    /// BMR has its number cleared; the unstart repair's before-snapshot still records the released number.
    /// Data only; the model is unchanged.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261120120000_ReleaseUnstartedBatchNumbers")]
    public partial class ReleaseUnstartedBatchNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "ProductionScheduleProducts" AS batch
                SET "BatchNumber" = NULL
                WHERE batch."BatchNumber" IS NOT NULL
                  AND EXISTS (SELECT 1 FROM "ProductionUnstartRepairs" repair WHERE repair."BatchId" = batch."Id")
                  AND NOT EXISTS (SELECT 1 FROM "ProductionActivities" run
                                  WHERE run."ProductionScheduleProductId" = batch."Id" AND run."DeletedAt" IS NULL)
                  AND NOT EXISTS (SELECT 1 FROM "BatchManufacturingRecords" record
                                  WHERE record."ProductionScheduleProductId" = batch."Id" AND record."DeletedAt" IS NULL
                                    AND NULLIF(TRIM(record."BatchNumber"), '') IS NOT NULL);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The released numbers stay recorded on each unstart repair; they are not put back on the batches.
        }
    }
}
