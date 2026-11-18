-- Read-only. Lists the live production holds on one material, so a store can see why
-- "reserved for production" is larger than the stock on its shelves.
-- Run with:  psql "$DB" -v code="'RML001'" -f diagnose-material-holds.sql
--
-- One row per unreleased hold, with the lot's current shelf balance and whether the
-- batch it is held for has already been issued its stock or was cancelled.
-- Every stock screen counts a hold as reserved only while it is live: not released,
-- its batch not cancelled, and its stock not yet issued (stock_issued_to_batch = false).
-- A live hold larger than its shelf counts only up to what the shelf holds.

SELECT
    m."Code"                                         AS material,
    lot."BatchNumber"                                AS lot,
    h."Quantity"                                     AS held,
    u."Symbol"                                       AS unit,
    (SELECT COALESCE(SUM(s."Quantity"), 0) FROM "ShelfMaterialBatches" s
      WHERE s."MaterialBatchId" = lot."Id" AND s."DeletedAt" IS NULL) AS lot_on_shelves,
    psp."BatchNumber"                                AS production_batch,
    psp."Cancelled"                                  AS batch_cancelled,
    h."CreatedAt"                                    AS held_since,
    EXISTS (SELECT 1 FROM "RequisitionItems" ri JOIN "Requisitions" r ON r."Id" = ri."RequisitionId"
             JOIN "StockRequisitionIssues" i ON i."RequisitionId" = r."Id"
            WHERE r."ProductionScheduleProductId" = h."ProductionScheduleProductId"
              AND ri."MaterialId" = m."Id")          AS stock_issued_to_batch,
    h."WarehouseLocationShelfId" IS NULL             AS no_shelf_recorded
FROM "MaterialBatchReservedQuantities" h
JOIN "MaterialBatches" lot ON lot."Id" = h."MaterialBatchId"
JOIN "Materials" m         ON m."Id" = lot."MaterialId"
LEFT JOIN "UnitOfMeasures" u ON u."Id" = h."UoMId"
LEFT JOIN "ProductionScheduleProducts" psp ON psp."Id" = h."ProductionScheduleProductId"
WHERE m."Code" = :code
  AND h."DeletedAt" IS NULL
  AND h."ReleasedAt" IS NULL
ORDER BY lot."BatchNumber", h."CreatedAt";
