-- Comprehensive Inconsistency Fix Script
-- Database: PostgreSQL
-- Author: Senior Engineer

BEGIN;

-- 1. Sync GLOBAL ConsumedQuantity with the SUM of individual consumption events
UPDATE "MaterialBatches" mb
SET "ConsumedQuantity" = sub.TotalConsumed
FROM (
    SELECT "BatchId", SUM("Quantity") as TotalConsumed
    FROM "MaterialBatchEvents"
    WHERE "Type" = 3 -- Consumed
    GROUP BY "BatchId"
) AS sub
WHERE mb."Id" = sub."BatchId"
AND ABS(mb."ConsumedQuantity" - sub.TotalConsumed) > 0.0001;

-- 2. Sync GLOBAL QuantityAssigned with the SUM of physical shelf records
UPDATE "MaterialBatches" mb
SET "QuantityAssigned" = sub.TotalOnShelves
FROM (
    SELECT "MaterialBatchId", SUM("Quantity") as TotalOnShelves
    FROM "ShelfMaterialBatches"
    GROUP BY "MaterialBatchId"
) AS sub
WHERE mb."Id" = sub."MaterialBatchId"
AND ABS(mb."QuantityAssigned" - sub.TotalOnShelves) > 0.0001;

-- 3. Reset aggregates to 0 for batches that have no ledger records but non-zero cached values
UPDATE "MaterialBatches" mb
SET "ConsumedQuantity" = 0
WHERE NOT EXISTS (SELECT 1 FROM "MaterialBatchEvents" mbe WHERE mbe."BatchId" = mb."Id" AND mbe."Type" = 3)
AND mb."ConsumedQuantity" != 0;

UPDATE "MaterialBatches" mb
SET "QuantityAssigned" = 0
WHERE NOT EXISTS (SELECT 1 FROM "ShelfMaterialBatches" smb WHERE smb."MaterialBatchId" = mb."Id")
AND mb."QuantityAssigned" != 0;

-- 4. Status Correction: If a batch is fully consumed (Remaining <= 0), update status to Consumed (7)
-- Only auto-fix if it was previously Available (3) or similar active states.
UPDATE "MaterialBatches"
SET "Status" = 7 -- Consumed
WHERE "Status" = 3 -- Available
AND ("TotalQuantity" - "ConsumedQuantity") <= 0;

-- 5. Audit: Provide summary of changes
SELECT 'Fix logic applied. Please re-run detect_inconsistencies.sql to verify final state.' as Status;

COMMIT;
