-- Finished goods units check. READ ONLY: it changes no data. A temporary helper view is created for this
-- session only (it disappears when the session ends); every query then runs in a read-only transaction.
--
-- Rule: finished goods stock is stored as yield, the exact base units produced (tablets, bottles, tubes).
-- Customer orders, allocation lines and invoices are counted in packs, because prices are per pack.
-- Allocation is the one place the two meet; the backend converts with the packing style's units per pack.
--
-- Older desktop versions saved some transfer notes and final packings in packs. This script lists what
-- needs converting before the yield-based release goes live, and can be re-run afterwards: once the data
-- is fixed, sections 2, 3 and 6 should return no rows.
--
-- Units per pack (the same rule as the backend's PackConversion and the desktop's packingLayout):
--   * rows naming the base unit itself ("Tube = 25" on a Tube style) give its fill, not a count: ignored;
--   * rows in a mass or volume unit (mg, g, ml…) describe contents, not a count: ignored;
--   * the shipper row comes first (unit rows carry no real order: every "Order" is 0), others by id;
--   * the pack is the first level whose running product from the shipper equals PackPerShipper,
--     and units per pack is the product of the counted rows below it (1 when the pack is the base unit);
--   * a style with no counted rows counts one base unit per pack;
--   * a style whose rows never reach PackPerShipper cannot be converted (units_per_pack is NULL).
--
-- Run: psql "$DATABASE_URL" -f detect_fg_yield_units.sql

CREATE OR REPLACE TEMP VIEW fg_units_per_pack AS
WITH counted AS (
    SELECT l."ProductPackingId", l."Id", l."Quantity",
           CASE WHEN u."Name" ILIKE 'shipper%' THEN 0 ELSE 1 END AS shipper_first
    FROM "ProductPackingList" l
    JOIN "ProductPackings" p ON p."Id" = l."ProductPackingId"
    JOIN "UnitOfMeasures" u ON u."Id" = l."UomId"
    WHERE l."UomId" IS DISTINCT FROM p."BasePackingUomId"
      AND LOWER(TRIM(COALESCE(u."Symbol", ''))) NOT IN ('mg', 'g', 'kg', 'mcg', 'µg', 'ug', 'ml', 'l', 'cl', 'dl')
),
rows AS (
    SELECT c.*,
           ROW_NUMBER() OVER (PARTITION BY c."ProductPackingId" ORDER BY c.shipper_first, c."Id") AS level,
           BOOL_AND(c."Quantity" > 0) OVER (PARTITION BY c."ProductPackingId") AS all_positive
    FROM counted c
),
running AS (
    SELECT r.*,
           ROUND(EXP(SUM(LN(r."Quantity")) OVER (PARTITION BY r."ProductPackingId" ORDER BY r.level
                 ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW))::numeric, 6) AS down_to_here,
           ROUND(EXP(SUM(LN(r."Quantity")) OVER (PARTITION BY r."ProductPackingId"))::numeric, 6) AS whole_shipper
    FROM rows r
    WHERE r.all_positive
),
pack_level AS (
    SELECT DISTINCT ON (r."ProductPackingId") r."ProductPackingId", r.level, r.down_to_here, r.whole_shipper
    FROM running r
    JOIN "ProductPackings" p ON p."Id" = r."ProductPackingId"
    WHERE p."PackPerShipper" > 0 AND r.down_to_here = p."PackPerShipper"
    ORDER BY r."ProductPackingId", r.level
)
SELECT p."Id" AS packing_id,
       p."Name" AS packing,
       p."PackPerShipper" AS packs_per_shipper,
       CASE
           WHEN NOT EXISTS (SELECT 1 FROM counted c WHERE c."ProductPackingId" = p."Id") THEN 1::numeric
           ELSE ROUND(pl.whole_shipper / NULLIF(pl.down_to_here, 0), 6)
       END AS units_per_pack
FROM "ProductPackings" p
LEFT JOIN pack_level pl ON pl."ProductPackingId" = p."Id";

BEGIN TRANSACTION READ ONLY;

-- 1. Summary.
SELECT 'transfer notes' AS what, COUNT(*) AS how_many FROM "FinishedGoodsTransferNotes" WHERE "DeletedAt" IS NULL
UNION ALL SELECT 'final packings', COUNT(*) FROM "FinalPackings" WHERE "DeletedAt" IS NULL
UNION ALL SELECT 'packing styles that cannot convert (used by stock)', COUNT(DISTINCT u.packing_id)
    FROM fg_units_per_pack u JOIN "FinishedGoodsTransferNotes" n ON n."ProductPackingId" = u.packing_id AND n."DeletedAt" IS NULL
    WHERE u.units_per_pack IS NULL
UNION ALL SELECT 'allocation lines (packing lists)', COUNT(*) FROM "AllocateProductQuantity"
UNION ALL SELECT 'allocation lines (orders)', COUNT(*) FROM "ProductionOrderProductQuantity";

-- 1b. Every packing style used by stock, with the units per pack this rule gives. Review it once: a wrong
--     unit row on a product (e.g. "Inner Carton = 25" meaning 25 g) gives a wrong count everywhere.
SELECT pr."Name" AS product, u.packing, u.packs_per_shipper, u.units_per_pack,
       (SELECT STRING_AGG(m."Name" || ' = ' || l."Quantity"::float, ' > ' ORDER BY l."Id")
          FROM "ProductPackingList" l JOIN "UnitOfMeasures" m ON m."Id" = l."UomId" WHERE l."ProductPackingId" = u.packing_id) AS unit_rows,
       bu."Name" AS base_unit, COUNT(n."Id") AS transfer_notes
FROM fg_units_per_pack u
JOIN "ProductPackings" p ON p."Id" = u.packing_id
JOIN "Products" pr ON pr."Id" = p."ProductId"
LEFT JOIN "UnitOfMeasures" bu ON bu."Id" = p."BasePackingUomId"
JOIN "FinishedGoodsTransferNotes" n ON n."ProductPackingId" = u.packing_id AND n."DeletedAt" IS NULL
GROUP BY pr."Name", u.packing_id, u.packing, u.packs_per_shipper, u.units_per_pack, bu."Name"
ORDER BY pr."Name", u.packing;

-- 2. Packing styles used by stock that cannot convert. Fix these on the product before releasing:
--    approval and allocation refuse them.
SELECT u.packing_id, pr."Name" AS product, u.packing, u.packs_per_shipper,
       (SELECT STRING_AGG(l."Quantity"::text, ' x ' ORDER BY l."Order") FROM "ProductPackingList" l WHERE l."ProductPackingId" = u.packing_id) AS unit_rows,
       COUNT(n."Id") AS transfer_notes
FROM fg_units_per_pack u
JOIN "ProductPackings" p ON p."Id" = u.packing_id
JOIN "Products" pr ON pr."Id" = p."ProductId"
JOIN "FinishedGoodsTransferNotes" n ON n."ProductPackingId" = u.packing_id AND n."DeletedAt" IS NULL
WHERE u.units_per_pack IS NULL
GROUP BY u.packing_id, pr."Name", u.packing, u.packs_per_shipper
ORDER BY transfer_notes DESC;

-- 3. Final packings recorded in packs. "Available to transfer" (TotalQuantityPacked) should be yield:
--    whole packs x units per pack. TotalNumberOfBottles is what was left after samples, in base units.
SELECT f."Id" AS final_packing_id, b."BatchNumber", pr."Name" AS product, u.packing, u.units_per_pack,
       f."TotalQuantityPacked" AS stored, f."TotalNumberOfBottles" AS after_samples_units,
       f."TotalQuantityPacked" * u.units_per_pack AS stored_as_yield_if_packs,
       f."CreatedAt"
FROM "FinalPackings" f
JOIN fg_units_per_pack u ON u.packing_id = f."ProductPackingId"
JOIN "ProductionScheduleProducts" psp ON psp."Id" = f."ProductionScheduleProductId"
JOIN "Products" pr ON pr."Id" = psp."ProductId"
LEFT JOIN "BatchManufacturingRecords" b ON b."ProductionScheduleProductId" = f."ProductionScheduleProductId"
WHERE f."DeletedAt" IS NULL
  AND u.units_per_pack > 1
  AND f."TotalNumberOfBottles" > 0
  -- Packs: stored x units per pack lands within one pack of what was left after samples.
  AND f."TotalQuantityPacked" * u.units_per_pack <= f."TotalNumberOfBottles"
  AND f."TotalQuantityPacked" * u.units_per_pack > f."TotalNumberOfBottles" - u.units_per_pack
ORDER BY f."CreatedAt";

-- 4. Transfer notes, with both readings side by side so each can be judged. A note is "packs" when it
--    came from a final packing recorded in packs (section 3) or when the older desktop stamped the
--    batch's total yield into QuantityPerPack; "yield" when QuantityPerPack is the units in one pack.
WITH notes AS (
    SELECT n."Id", n."TransferNoteNumber", b."BatchNumber", pr."Name" AS product, u.packing, u.packs_per_shipper, u.units_per_pack,
           n."QuantityPerPack", n."TotalQuantity", n."QuantityReceived", n."AllocatedQuantity", n."IsApproved", n."CreatedAt",
           f."TotalQuantityPacked" AS fp_available, f."YieldTotalQuantityPacked" AS fp_yield, f."TotalNumberOfBottles" AS fp_after_samples
    FROM "FinishedGoodsTransferNotes" n
    JOIN "BatchManufacturingRecords" b ON b."Id" = n."BatchManufacturingRecordId"
    JOIN "ProductionScheduleProducts" psp ON psp."Id" = b."ProductionScheduleProductId"
    JOIN "Products" pr ON pr."Id" = psp."ProductId"
    LEFT JOIN fg_units_per_pack u ON u.packing_id = n."ProductPackingId"
    LEFT JOIN "FinalPackings" f ON f."ProductionScheduleProductId" = b."ProductionScheduleProductId" AND f."DeletedAt" IS NULL
    WHERE n."DeletedAt" IS NULL
)
SELECT *,
       CASE
           WHEN units_per_pack IS NULL THEN 'packing style cannot convert'
           WHEN units_per_pack = 1 THEN 'same either way'
           WHEN "QuantityPerPack" = units_per_pack THEN 'yield'
           WHEN "QuantityPerPack" > 0 AND "QuantityPerPack" = fp_yield THEN 'packs'
           WHEN fp_available IS NOT NULL AND fp_after_samples > 0
                AND fp_available * units_per_pack <= fp_after_samples
                AND fp_available * units_per_pack > fp_after_samples - units_per_pack
                AND "TotalQuantity" <= fp_available THEN 'packs'
           ELSE 'yield (check)'
       END AS stored_in,
       FLOOR("TotalQuantity" / NULLIF(units_per_pack, 0) / NULLIF(packs_per_shipper, 0)) AS shippers_if_yield,
       FLOOR("TotalQuantity" / NULLIF(packs_per_shipper, 0)) AS shippers_if_packs
FROM notes
ORDER BY stored_in, "CreatedAt";

-- 5. Allocation lines. Their Quantity is packs (checked against the order, which is in packs). Each one
--    took that many packs out of a transfer note; in yield that is Quantity x units per pack.
SELECT 'packing list' AS source, q."FinishedGoodsTransferNoteId", n."TransferNoteNumber", u.packing, u.units_per_pack,
       q."Quantity" AS packs, q."Quantity" * u.units_per_pack AS as_yield, a."Approved", a."CreatedAt"
FROM "AllocateProductQuantity" q
JOIN "AllocateProductionOrders" a ON a."Id" = q."AllocateProductionOrderProductAllocateProductionOrderId"
JOIN "FinishedGoodsTransferNotes" n ON n."Id" = q."FinishedGoodsTransferNoteId"
LEFT JOIN fg_units_per_pack u ON u.packing_id = n."ProductPackingId"
WHERE a."DeletedAt" IS NULL
ORDER BY a."CreatedAt";

-- 6. Stock that does not balance: a note's AllocatedQuantity should equal the yield of the approved
--    allocations taken from it, and never exceed what was transferred.
WITH taken AS (
    SELECT q."FinishedGoodsTransferNoteId" AS note_id, SUM(q."Quantity") AS packs
    FROM "ProductionOrderProductQuantity" q
    GROUP BY q."FinishedGoodsTransferNoteId"
)
SELECT n."Id", n."TransferNoteNumber", u.packing, u.units_per_pack, n."TotalQuantity", n."AllocatedQuantity",
       COALESCE(t.packs, 0) AS allocated_packs,
       COALESCE(t.packs, 0) * u.units_per_pack AS allocated_yield_expected,
       n."TotalQuantity" - COALESCE(t.packs, 0) * u.units_per_pack AS remaining_yield_expected
FROM "FinishedGoodsTransferNotes" n
LEFT JOIN taken t ON t.note_id = n."Id"
LEFT JOIN fg_units_per_pack u ON u.packing_id = n."ProductPackingId"
WHERE n."DeletedAt" IS NULL
  AND (n."AllocatedQuantity" <> COALESCE(t.packs, 0) * u.units_per_pack
       OR n."AllocatedQuantity" > n."TotalQuantity"
       OR u.units_per_pack IS NULL AND n."AllocatedQuantity" > 0)
ORDER BY n."CreatedAt";

ROLLBACK;
