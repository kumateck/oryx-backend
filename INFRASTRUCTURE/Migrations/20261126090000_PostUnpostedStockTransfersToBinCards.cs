using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <summary>
    /// Stock transfers between departments moved shelf stock but, before the transfer ledger was deployed, wrote no
    /// bin-card rows. The sending store's bin card kept the stock and the receiving store's card went negative as it issued
    /// stock it had never received (Vanilla Powder R-03506220: Beta +130 kg, Tablet −130 kg against their shelves).
    ///
    /// For every transfer batch that is not already on the ledger this posts, with the same descriptions the live code uses:
    /// <list type="bullet">
    /// <item>an issue in the source store at the time the transfer was issued, and</item>
    /// <item>a receipt in the destination store at the time it was received (its receipt time, else the recorded
    /// warehouse movement, else the transfer's last update). Transfers still awaiting receipt get only the issue.</item>
    /// </list>
    /// Quantities are in the lot's unit, as the live code posts them. Each posted row is logged in
    /// <c>_BinCardTransferBackfill</c>, which also stops a second run posting it again. Saved balances of the affected
    /// store ledgers are then recomputed in date order where all their rows share one unit; the bin card endpoint replays
    /// balances itself, so this only keeps the stored column honest. Data only; the model is unchanged.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261126090000_PostUnpostedStockTransfersToBinCards")]
    public partial class PostUnpostedStockTransfersToBinCards : Migration
    {
        public const string Sql = """
            CREATE TABLE IF NOT EXISTS "_BinCardTransferBackfill" (
                "BinCardInformationId" uuid PRIMARY KEY,
                "HoldingMaterialTransferBatchId" uuid NOT NULL,
                "Side" text NOT NULL,
                "PostedAt" timestamp with time zone NOT NULL DEFAULT now(),
                UNIQUE ("HoldingMaterialTransferBatchId", "Side")
            );

            CREATE TEMP TABLE _transfer_postings ON COMMIT DROP AS
            SELECT hb."Id" AS held_id, 'issue' AS side, hb."SourceWarehouseId" AS warehouse_id, hb."MaterialBatchId" AS lot_id,
                   lot."UoMId" AS unit_id, 0::numeric AS received, hb."Quantity" AS issued,
                   'Stock transfer ' || COALESCE(st."Code", h."Id"::text) || ': issued to ' || dw."Name" AS description,
                   COALESCE(src."IssuedAt", h."CreatedAt") AS at, COALESCE(src."IssuedById", h."CreatedById") AS actor
            FROM "HoldingMaterialTransferBatches" hb
            JOIN "HoldingMaterialTransfers" h ON h."Id" = hb."HoldingMaterialTransferId"
            JOIN "MaterialBatches" lot ON lot."Id" = hb."MaterialBatchId"
            JOIN "Warehouses" dw ON dw."Id" = hb."DestinationWarehouseId"
            LEFT JOIN "StockTransfers" st ON st."Id" = h."StockTransferId"
            LEFT JOIN "StockTransferSources" src ON src."Id" = h."StockTransferSourceId"
            WHERE h."DeletedAt" IS NULL AND hb."Quantity" > 0
            UNION ALL
            SELECT hb."Id", 'receipt', hb."DestinationWarehouseId", hb."MaterialBatchId", lot."UoMId", hb."Quantity", 0::numeric,
                   'Stock transfer ' || COALESCE(st."Code", h."Id"::text) || ': received from ' || sw."Name",
                   COALESCE(hb."ReceivedAt",
                       (SELECT MIN(m."MovedAt") FROM "MassMaterialBatchMovements" m
                        WHERE m."DeletedAt" IS NULL AND m."BatchId" = hb."MaterialBatchId" AND m."FromWarehouseId" = hb."SourceWarehouseId"
                          AND m."ToWarehouseId" = hb."DestinationWarehouseId" AND m."MovedAt" >= h."CreatedAt"),
                       h."UpdatedAt", h."CreatedAt"),
                   COALESCE(hb."ReceivedById", h."LastUpdatedById", h."CreatedById")
            FROM "HoldingMaterialTransferBatches" hb
            JOIN "HoldingMaterialTransfers" h ON h."Id" = hb."HoldingMaterialTransferId"
            JOIN "MaterialBatches" lot ON lot."Id" = hb."MaterialBatchId"
            JOIN "Warehouses" sw ON sw."Id" = hb."SourceWarehouseId"
            LEFT JOIN "StockTransfers" st ON st."Id" = h."StockTransferId"
            WHERE h."DeletedAt" IS NULL AND hb."Quantity" > 0 AND (hb."ReceivedAt" IS NOT NULL OR h."Status" = 1);

            -- Skip what is already posted: by this backfill, or by the live transfer ledger with the same description.
            DELETE FROM _transfer_postings p
            WHERE EXISTS (SELECT 1 FROM "_BinCardTransferBackfill" b WHERE b."HoldingMaterialTransferBatchId" = p.held_id AND b."Side" = p.side)
               OR EXISTS (SELECT 1 FROM "BinCardInformation" c WHERE c."DeletedAt" IS NULL AND c."MaterialBatchId" = p.lot_id
                          AND c."WarehouseId" = p.warehouse_id AND c."Description" = p.description);

            ALTER TABLE _transfer_postings ADD COLUMN card_id uuid;
            UPDATE _transfer_postings SET card_id = gen_random_uuid();

            INSERT INTO "BinCardInformation" ("Id", "MaterialBatchId", "Description", "WayBill", "ArNumber", "Supplier", "Manufacturer",
                "QuantityReceived", "QuantityIssued", "BalanceQuantity", "UoMId", "WarehouseId", "CreatedAt", "CreatedById")
            SELECT p.card_id, p.lot_id, LEFT(p.description, 500), NULL,
                   COALESCE((SELECT s."ArNumber" FROM "MaterialSamplings" s WHERE s."MaterialBatchId" = p.lot_id ORDER BY s."CreatedAt" DESC LIMIT 1), first."ArNumber"),
                   first."Supplier", first."Manufacturer", p.received, p.issued, 0, p.unit_id, p.warehouse_id, p.at, p.actor
            FROM _transfer_postings p
            LEFT JOIN LATERAL (SELECT c."ArNumber", c."Supplier", c."Manufacturer" FROM "BinCardInformation" c
                               WHERE c."MaterialBatchId" = p.lot_id AND c."DeletedAt" IS NULL
                               ORDER BY c."CreatedAt", c."Id" LIMIT 1) first ON true;

            INSERT INTO "_BinCardTransferBackfill" ("BinCardInformationId", "HoldingMaterialTransferBatchId", "Side")
            SELECT card_id, held_id, side FROM _transfer_postings;

            -- Recompute saved balances of each touched store ledger (material + warehouse) whose rows share one unit.
            WITH touched AS (
                SELECT DISTINCT lot."MaterialId" AS material_id, p.warehouse_id
                FROM _transfer_postings p JOIN "MaterialBatches" lot ON lot."Id" = p.lot_id
            ), card_rows AS (
                SELECT c."Id", t.material_id, t.warehouse_id, COALESCE(c."UoMId", lot."UoMId") AS unit_id,
                       SUM(c."QuantityReceived" - c."QuantityIssued") OVER (PARTITION BY t.material_id, t.warehouse_id
                           ORDER BY c."CreatedAt", c."Id" ROWS UNBOUNDED PRECEDING) AS balance
                FROM "BinCardInformation" c
                JOIN "MaterialBatches" lot ON lot."Id" = c."MaterialBatchId"
                JOIN touched t ON t.material_id = lot."MaterialId" AND t.warehouse_id = c."WarehouseId"
                WHERE c."DeletedAt" IS NULL
            ), single_unit AS (
                SELECT material_id, warehouse_id FROM card_rows GROUP BY 1, 2 HAVING COUNT(DISTINCT unit_id) = 1
            )
            UPDATE "BinCardInformation" c SET "BalanceQuantity" = r.balance
            FROM card_rows r JOIN single_unit u USING (material_id, warehouse_id)
            WHERE c."Id" = r."Id";
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(Sql);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Removes only the rows this backfill posted. Recomputed saved balances are not restored: they were wrong.
            migrationBuilder.Sql("""
                DELETE FROM "BinCardInformation" WHERE "Id" IN (SELECT "BinCardInformationId" FROM "_BinCardTransferBackfill");
                DROP TABLE IF EXISTS "_BinCardTransferBackfill";
                """);
        }
    }
}
