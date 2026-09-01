-- Comprehensive Inconsistency Detection Script (Warehouse-Aware)
-- Database: PostgreSQL
-- Run this before fix_inconsistencies.sql. "Unlimited" materials (e.g. WATER) are excluded
-- throughout since they use a sentinel quantity and never reconcile against real stock.

-- === Duplicate event check (run first) ===
-- Flags pairs of events on the same batch/type/warehouse/quantity logged within 60 seconds
-- of each other -- the signature of a double-submitted request (see ERTT-4523367). These are
-- NOT auto-fixed by fix_inconsistencies.sql; a human must pick which row to delete.
SELECT
    mb."BatchNumber",
    a."Type",
    a."Quantity",
    a."ConsumptionWarehouseId",
    a."CreatedAt" as first_event_at,
    b."CreatedAt" as second_event_at,
    a."Id" as first_event_id,
    b."Id" as second_event_id,
    'CRITICAL: Possible duplicate event' as InconsistencyType
FROM "MaterialBatchEvents" a
JOIN "MaterialBatchEvents" b ON a."BatchId" = b."BatchId"
    AND a."Type" = b."Type"
    AND a."Quantity" = b."Quantity"
    AND a."Id" < b."Id"
    AND COALESCE(a."ConsumptionWarehouseId"::text, '') = COALESCE(b."ConsumptionWarehouseId"::text, '')
    AND ABS(EXTRACT(EPOCH FROM (b."CreatedAt" - a."CreatedAt"))) < 60
JOIN "MaterialBatches" mb ON mb."Id" = a."BatchId"
JOIN "Materials" m ON m."Id" = mb."MaterialId"
WHERE m."IsUnlimited" = false
ORDER BY a."CreatedAt";

WITH ShelfByWarehouse AS (
    SELECT 
        smb."MaterialBatchId",
        w."Id" as WarehouseId,
        w."Name" as WarehouseName,
        SUM(smb."Quantity") as TotalOnShelves
    FROM "ShelfMaterialBatches" smb
    JOIN "WarehouseLocationShelves" wls ON smb."WarehouseLocationShelfId" = wls."Id"
    JOIN "WarehouseLocationRacks" wlr ON wls."WarehouseLocationRackId" = wlr."Id"
    JOIN "WarehouseLocations" wl ON wlr."WarehouseLocationId" = wl."Id"
    JOIN "Warehouses" w ON wl."WarehouseId" = w."Id"
    GROUP BY smb."MaterialBatchId", w."Id", w."Name"
),
EventsByWarehouse AS (
    SELECT 
        "BatchId",
        "ConsumptionWarehouseId" as WarehouseId,
        SUM("Quantity") as TotalConsumed
    FROM "MaterialBatchEvents"
    WHERE "Type" = 3 -- Consumed
    GROUP BY "BatchId", "ConsumptionWarehouseId"
),
GlobalAggregates AS (
    SELECT
        mb."Id" as BatchId,
        mb."BatchNumber",
        mb."Status", -- 3 is Available, 7 is Consumed
        mb."TotalQuantity",
        mb."ConsumedQuantity",
        mb."QuantityAssigned"
    FROM "MaterialBatches" mb
    JOIN "Materials" m ON m."Id" = mb."MaterialId"
    WHERE m."IsUnlimited" = false
),
WarehouseLevels AS (
    SELECT 
        COALESCE(s."MaterialBatchId", e."BatchId") as BatchId,
        COALESCE(s.WarehouseId, e.WarehouseId) as WarehouseId,
        s.WarehouseName,
        COALESCE(s.TotalOnShelves, 0) as ActualShelfQuantity,
        COALESCE(e.TotalConsumed, 0) as ActualConsumedQuantity
    FROM ShelfByWarehouse s
    FULL OUTER JOIN EventsByWarehouse e ON s."MaterialBatchId" = e."BatchId" AND s.WarehouseId = e.WarehouseId
)
SELECT 
    ga."BatchNumber",
    wl.WarehouseName,
    ga."Status" as CurrentStatus,
    ga."TotalQuantity" as GlobalTotal,
    ga."ConsumedQuantity" as GlobalConsumed,
    ga."QuantityAssigned" as GlobalAssigned,
    wl.ActualShelfQuantity,
    wl.ActualConsumedQuantity,
    -- Calculation for Unassigned (Inventory not on any shelf)
    (ga."TotalQuantity" - ga."ConsumedQuantity" - ga."QuantityAssigned") as Unassigned_Lobby_Quantity,
    
    -- Diagnostic Flags
    CASE 
        WHEN ABS(ga."QuantityAssigned" - (SELECT SUM("Quantity") FROM "ShelfMaterialBatches" WHERE "MaterialBatchId" = wl.BatchId)) > 0.001 THEN 'MISMATCH: Assigned vs Shelves'
        WHEN ABS(ga."ConsumedQuantity" - (SELECT SUM("Quantity") FROM "MaterialBatchEvents" WHERE "BatchId" = wl.BatchId AND "Type" = 3)) > 0.001 THEN 'MISMATCH: Consumed vs Events'
        WHEN (ga."TotalQuantity" - ga."ConsumedQuantity" - ga."QuantityAssigned") < -0.001 THEN 'CRITICAL: Over-assigned (Negative Unassigned)'
        WHEN ga."Status" = 3 AND (ga."TotalQuantity" - ga."ConsumedQuantity") <= 0 THEN 'STATUS ERROR: Available but Empty'
        ELSE 'Warehouse OK (Check Global Totals)'
    END as InconsistencyType

FROM WarehouseLevels wl
JOIN GlobalAggregates ga ON wl.BatchId = ga.BatchId
WHERE 
    ABS(ga."QuantityAssigned" - (SELECT COALESCE(SUM("Quantity"), 0) FROM "ShelfMaterialBatches" WHERE "MaterialBatchId" = wl.BatchId)) > 0.001
    OR ABS(ga."ConsumedQuantity" - (SELECT COALESCE(SUM("Quantity"), 0) FROM "MaterialBatchEvents" WHERE "BatchId" = wl.BatchId AND "Type" = 3)) > 0.001
    OR (ga."TotalQuantity" - ga."ConsumedQuantity" - ga."QuantityAssigned") < -0.001
    OR (ga."Status" = 3 AND (ga."TotalQuantity" - ga."ConsumedQuantity") <= 0);
