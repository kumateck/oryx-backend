-- Comprehensive Inconsistency Fix Script
-- Database: PostgreSQL
-- Author: Senior Engineer

BEGIN;

-- NOTE: "Unlimited" materials (Materials.IsUnlimited = true, e.g. WATER) use a sentinel
-- quantity and are intentionally excluded from every step below -- they are not real
-- stock and will never reconcile against shelves/events.

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
AND (SELECT "IsUnlimited" FROM "Materials" WHERE "Id" = mb."MaterialId") = false
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
AND (SELECT "IsUnlimited" FROM "Materials" WHERE "Id" = mb."MaterialId") = false
AND ABS(mb."QuantityAssigned" - sub.TotalOnShelves) > 0.0001;

-- 3. Reset aggregates to 0 for batches that have no ledger records but non-zero cached values
UPDATE "MaterialBatches" mb
SET "ConsumedQuantity" = 0
WHERE NOT EXISTS (SELECT 1 FROM "MaterialBatchEvents" mbe WHERE mbe."BatchId" = mb."Id" AND mbe."Type" = 3)
AND (SELECT "IsUnlimited" FROM "Materials" WHERE "Id" = mb."MaterialId") = false
AND mb."ConsumedQuantity" != 0;

UPDATE "MaterialBatches" mb
SET "QuantityAssigned" = 0
WHERE NOT EXISTS (SELECT 1 FROM "ShelfMaterialBatches" smb WHERE smb."MaterialBatchId" = mb."Id")
AND (SELECT "IsUnlimited" FROM "Materials" WHERE "Id" = mb."MaterialId") = false
AND mb."QuantityAssigned" != 0;

-- 4. Status Correction: If a batch is fully consumed (Remaining <= 0), update status to Consumed (7)
-- Only auto-fix if it was previously Available (3) or similar active states.
UPDATE "MaterialBatches"
SET "Status" = 7 -- Consumed
WHERE "Status" = 3 -- Available
AND (SELECT "IsUnlimited" FROM "Materials" WHERE "Id" = "MaterialId") = false
AND ("TotalQuantity" - "ConsumedQuantity") <= 0;

-- NOTE: This script deliberately does NOT touch batches flagged as
-- 'CRITICAL: Possible duplicate event' by detect_inconsistencies.sql. Those need a human
-- to pick which duplicate ledger row to remove -- see the "Duplicate event" section of that
-- script and resolve each one individually (as was done manually for batch ERTT-4523367).

-- 5. Audit: Provide summary of changes
SELECT 'Fix logic applied. Please re-run detect_inconsistencies.sql to verify final state.' as Status;

COMMIT;
