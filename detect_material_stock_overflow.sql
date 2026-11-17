-- READ-ONLY. GET production-schedule/material-stock OverflowException (2026-11-17).
-- System.Decimal holds 28 significant digits (29 when the value is below 7.9e28); Npgsql throws OverflowException beyond that.
-- Queries 1-2 list sums over 28 digits (at risk); query 3 lists stored values over 29 digits (definitely unreadable).
-- length(replace(ltrim(trim_scale(abs(x))::text,'0.'),'.','')) = significant digits of x.
-- Run on the customer database (PostgreSQL 13+ for trim_scale). Changes nothing.

BEGIN TRANSACTION READ ONLY;

-- 1. The failing schedule product: which per-material SQL sums are too long (the endpoint's five sums).
WITH psp AS (SELECT '01a1d081-db1d-7d40-8699-84b16916dd9d'::uuid AS id)
SELECT 'reserved (this batch)' AS sum_kind, mb."MaterialId", sum(r."Quantity")::text AS total, length(replace(ltrim(trim_scale(abs(sum(r."Quantity")))::text, '0.'), '.', '')) AS digits
FROM "MaterialBatchReservedQuantities" r JOIN "MaterialBatches" mb ON mb."Id" = r."MaterialBatchId", psp
WHERE r."ProductionScheduleProductId" = psp.id GROUP BY mb."MaterialId"
UNION ALL
SELECT 'extra packing', p."MaterialId", sum(p."Quantity")::text, length(replace(ltrim(trim_scale(abs(sum(p."Quantity")))::text, '0.'), '.', ''))
FROM "ProductionExtraPackings" p, psp WHERE p."ProductionScheduleProductId" = psp.id GROUP BY p."MaterialId"
ORDER BY digits DESC;

-- 2. Shelf stock per material and warehouse whose SQL sum is too long (affects every stock screen that sums in SQL).
SELECT mb."MaterialId", wl."WarehouseId", count(*) AS shelf_rows,
       sum(s."Quantity")::text AS total, length(replace(ltrim(trim_scale(abs(sum(s."Quantity")))::text, '0.'), '.', '')) AS digits
FROM "ShelfMaterialBatches" s
JOIN "MaterialBatches" mb ON mb."Id" = s."MaterialBatchId"
JOIN "WarehouseLocationShelves" sh ON sh."Id" = s."WarehouseLocationShelfId"
JOIN "WarehouseLocationRacks" rk ON rk."Id" = sh."WarehouseLocationRackId"
JOIN "WarehouseLocations" wl ON wl."Id" = rk."WarehouseLocationId"
WHERE s."DeletedAt" IS NULL
GROUP BY mb."MaterialId", wl."WarehouseId"
HAVING length(replace(ltrim(trim_scale(abs(sum(s."Quantity")))::text, '0.'), '.', '')) > 28
ORDER BY digits DESC;

-- 3. Individual stored values that are themselves unreadable (only SQL-side writes can produce these, for example
--    POST material/resolve-inconsistencies or fix_inconsistencies.sql storing a SUM). Expected: no rows.
SELECT 'ShelfMaterialBatches' AS table_name, "Id", "Quantity"::text FROM "ShelfMaterialBatches" WHERE length(replace(ltrim(trim_scale(abs("Quantity"))::text, '0.'), '.', '')) > 29
UNION ALL SELECT 'MaterialBatchReservedQuantities', "Id", "Quantity"::text FROM "MaterialBatchReservedQuantities" WHERE length(replace(ltrim(trim_scale(abs("Quantity"))::text, '0.'), '.', '')) > 29
UNION ALL SELECT 'MaterialBatches.TotalQuantity', "Id", "TotalQuantity"::text FROM "MaterialBatches" WHERE length(replace(ltrim(trim_scale(abs("TotalQuantity"))::text, '0.'), '.', '')) > 29
UNION ALL SELECT 'MaterialBatches.ConsumedQuantity', "Id", "ConsumedQuantity"::text FROM "MaterialBatches" WHERE length(replace(ltrim(trim_scale(abs("ConsumedQuantity"))::text, '0.'), '.', '')) > 29
UNION ALL SELECT 'MaterialBatches.QuantityAssigned', "Id", "QuantityAssigned"::text FROM "MaterialBatches" WHERE length(replace(ltrim(trim_scale(abs("QuantityAssigned"))::text, '0.'), '.', '')) > 29;

ROLLBACK;
