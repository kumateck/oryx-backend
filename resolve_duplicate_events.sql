-- Resolve Duplicate Ledger Events (double-submit cleanup)
-- Database: PostgreSQL
--
-- Root cause: UpdateStatusOfProductionActivityStep (APP/Repository/ProductionScheduleRepository.cs)
-- had no guard against being re-submitted while a step was already InProgress. A double-click /
-- retried request re-ran material consumption for every batch in that step's stock requisition,
-- producing an exact-duplicate MaterialBatchEvent (and, for "Moved" events, a duplicate
-- MassMaterialBatchMovement row too) a few seconds after the original. That bug is now patched,
-- but existing duplicate rows from before the fix still inflate ConsumedQuantity/QuantityAssigned.
--
-- This script:
--   1. Snapshots both ledger tables to persistent backup tables (kept after commit, for audit/rollback).
--   2. Deletes duplicate rows: for any (batch, type, quantity, warehouse) group where every event in
--      the group falls within a 120-second window, keeps only the earliest row and deletes the rest.
--      The 120s window matches the "CRITICAL: Possible duplicate event" check in
--      detect_inconsistencies.sql, so run that script first and confirm the groups it lists are what
--      you expect before running this.
--   3. Reports how many rows were removed from each table.
--
-- Run fix_inconsistencies.sql AFTER this script to resync MaterialBatches.ConsumedQuantity /
-- QuantityAssigned / Status from the now-deduplicated ledger.
--
-- "Unlimited" materials (Materials.IsUnlimited = true) are excluded throughout.

BEGIN;

CREATE TABLE IF NOT EXISTS "_backup_MaterialBatchEvents_predup" (LIKE "MaterialBatchEvents" INCLUDING ALL);
CREATE TABLE IF NOT EXISTS "_backup_MassMaterialBatchMovements_predup" (LIKE "MassMaterialBatchMovements" INCLUDING ALL);
INSERT INTO "_backup_MaterialBatchEvents_predup" SELECT * FROM "MaterialBatchEvents";
INSERT INTO "_backup_MassMaterialBatchMovements_predup" SELECT * FROM "MassMaterialBatchMovements";

-- 1. Deduplicate MaterialBatchEvents (covers both "Moved" and "Consumed" duplicates)
WITH dup_groups AS (
    SELECT mbe."BatchId", mbe."Type", mbe."Quantity", mbe."ConsumptionWarehouseId"
    FROM "MaterialBatchEvents" mbe
    JOIN "MaterialBatches" mb ON mb."Id" = mbe."BatchId"
    JOIN "Materials" m ON m."Id" = mb."MaterialId"
    WHERE m."IsUnlimited" = false
    GROUP BY mbe."BatchId", mbe."Type", mbe."Quantity", mbe."ConsumptionWarehouseId"
    HAVING count(*) > 1
       AND EXTRACT(EPOCH FROM (max(mbe."CreatedAt") - min(mbe."CreatedAt"))) < 120
),
ranked AS (
    SELECT mbe."Id",
           ROW_NUMBER() OVER (
               PARTITION BY mbe."BatchId", mbe."Type", mbe."Quantity", mbe."ConsumptionWarehouseId"
               ORDER BY mbe."CreatedAt", mbe."Id"
           ) as rn
    FROM "MaterialBatchEvents" mbe
    JOIN dup_groups g
        ON g."BatchId" = mbe."BatchId"
        AND g."Type" = mbe."Type"
        AND g."Quantity" = mbe."Quantity"
        AND g."ConsumptionWarehouseId" IS NOT DISTINCT FROM mbe."ConsumptionWarehouseId"
)
DELETE FROM "MaterialBatchEvents" WHERE "Id" IN (SELECT "Id" FROM ranked WHERE rn > 1);

-- 2. Deduplicate MassMaterialBatchMovements (mirrors the "Moved" events above)
WITH dup_groups AS (
    SELECT mbm."BatchId", mbm."FromWarehouseId", mbm."ToWarehouseId", mbm."Quantity"
    FROM "MassMaterialBatchMovements" mbm
    JOIN "MaterialBatches" mb ON mb."Id" = mbm."BatchId"
    JOIN "Materials" m ON m."Id" = mb."MaterialId"
    WHERE m."IsUnlimited" = false
    GROUP BY mbm."BatchId", mbm."FromWarehouseId", mbm."ToWarehouseId", mbm."Quantity"
    HAVING count(*) > 1
       AND EXTRACT(EPOCH FROM (max(mbm."MovedAt") - min(mbm."MovedAt"))) < 120
),
ranked AS (
    SELECT mbm."Id",
           ROW_NUMBER() OVER (
               PARTITION BY mbm."BatchId", mbm."FromWarehouseId", mbm."ToWarehouseId", mbm."Quantity"
               ORDER BY mbm."MovedAt", mbm."Id"
           ) as rn
    FROM "MassMaterialBatchMovements" mbm
    JOIN dup_groups g
        ON g."BatchId" = mbm."BatchId"
        AND g."FromWarehouseId" IS NOT DISTINCT FROM mbm."FromWarehouseId"
        AND g."ToWarehouseId" IS NOT DISTINCT FROM mbm."ToWarehouseId"
        AND g."Quantity" = mbm."Quantity"
)
DELETE FROM "MassMaterialBatchMovements" WHERE "Id" IN (SELECT "Id" FROM ranked WHERE rn > 1);

-- 3. Report what changed
SELECT
    (SELECT count(*) FROM "_backup_MaterialBatchEvents_predup") - (SELECT count(*) FROM "MaterialBatchEvents") AS events_deleted,
    (SELECT count(*) FROM "_backup_MassMaterialBatchMovements_predup") - (SELECT count(*) FROM "MassMaterialBatchMovements") AS movements_deleted;

SELECT 'Duplicates removed. Now run fix_inconsistencies.sql to resync MaterialBatches aggregates.' as Status;

COMMIT;
