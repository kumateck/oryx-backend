using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <summary>
    /// Issuing a stock requisition set each issued lot's <c>QuantityAssigned</c> to 0, though the rest of the lot stayed on
    /// its shelves. The next transfer issued from such a lot was refused ("The recorded batch balance does not cover this
    /// issue"). A lot's assigned quantity is its stock on shelves in every store, so any lot recorded below that is raised
    /// to it. Lots at or above their shelf stock are left as they are. Data only; the model is unchanged.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261120090000_RepairLotAssignedFromShelves")]
    public partial class RepairLotAssignedFromShelves : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "MaterialBatches" AS lot
                SET "QuantityAssigned" = shelves.total
                FROM (
                    SELECT "MaterialBatchId", SUM("Quantity") AS total
                    FROM "ShelfMaterialBatches"
                    WHERE "DeletedAt" IS NULL
                    GROUP BY "MaterialBatchId"
                ) AS shelves
                WHERE shelves."MaterialBatchId" = lot."Id"
                  AND lot."DeletedAt" IS NULL
                  AND lot."QuantityAssigned" < shelves.total;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The zeroed values were wrong; there is nothing to restore.
        }
    }
}
