-- Finished goods yield repair
-- Database: PostgreSQL
--
-- Brings stored finished goods quantities to the rule the yield-based release enforces:
--   * transfer notes and final packing hold YIELD, the exact base units produced;
--   * allocation lines (packing lists, order fulfilment) hold PACKS, plus the yield they took
--     from stock (YieldQuantity) and the units per pack used (UnitsPerPack);
--   * a transfer note's AllocatedQuantity is the yield its allocation lines took.
--
-- Older desktop versions saved some transfer notes and final packings in packs and stamped
-- the batch's whole yield into QuantityPerPack. This converts those rows and fills the new
-- columns. Units per pack follows the same rule as the backend's PackConversion.
--
-- Order of release:
--   1. deploy the backend migration AddYieldToFinishedGoodsAllocations;
--   2. run detect_fg_yield_units.sql and review it; fix any packing style it lists as unable
--      to convert (this script refuses to run while one is used by stock);
--   3. run this script;
--   4. run detect_fg_yield_units.sql again: sections 2, 3 and 6 should be empty;
--   5. release the backend and desktop together.
--
-- Idempotent and re-runnable: each step only touches rows not yet converted, so a second
-- run changes nothing. Runs in one transaction and aborts, changing nothing, if stock would
-- not balance afterwards.

BEGIN;

-- Units per pack, worked out once for the whole run.
CREATE TEMP TABLE fg_units_per_pack ON COMMIT DROP AS
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
    SELECT DISTINCT ON (r."ProductPackingId") r."ProductPackingId", r.down_to_here, r.whole_shipper
    FROM running r
    JOIN "ProductPackings" p ON p."Id" = r."ProductPackingId"
    WHERE p."PackPerShipper" > 0 AND r.down_to_here = p."PackPerShipper"
    ORDER BY r."ProductPackingId", r.level
)
SELECT p."Id" AS packing_id,
       CASE
           WHEN NOT EXISTS (SELECT 1 FROM counted c WHERE c."ProductPackingId" = p."Id") THEN 1::numeric
           ELSE ROUND(pl.whole_shipper / NULLIF(pl.down_to_here, 0), 6)
       END AS units_per_pack
FROM "ProductPackings" p
LEFT JOIN pack_level pl ON pl."ProductPackingId" = p."Id";

-- 0. Refuse to run while stock uses a packing style that cannot convert.
DO $$
DECLARE broken int;
BEGIN
    SELECT COUNT(*) INTO broken
    FROM "FinishedGoodsTransferNotes" n
    LEFT JOIN fg_units_per_pack u ON u.packing_id = n."ProductPackingId"
    WHERE n."DeletedAt" IS NULL AND u.units_per_pack IS NULL;
    IF broken > 0 THEN
        RAISE EXCEPTION '% transfer note(s) use a packing style that cannot convert (or have none). Fix them first: see section 2 of detect_fg_yield_units.sql.', broken;
    END IF;
END $$;

-- 1. Transfer notes saved in packs: the older desktop stamped the batch's whole yield into
--    QuantityPerPack, or the note came from a final packing recorded in packs. Convert every
--    quantity on the note to yield. (AllocatedQuantity is rebuilt in step 5.)
WITH packs_notes AS (
    SELECT DISTINCT n."Id", u.units_per_pack
    FROM "FinishedGoodsTransferNotes" n
    JOIN fg_units_per_pack u ON u.packing_id = n."ProductPackingId"
    JOIN "BatchManufacturingRecords" b ON b."Id" = n."BatchManufacturingRecordId"
    LEFT JOIN "FinalPackings" f ON f."ProductionScheduleProductId" = b."ProductionScheduleProductId" AND f."DeletedAt" IS NULL
    WHERE n."DeletedAt" IS NULL
      AND u.units_per_pack > 1
      AND n."QuantityPerPack" <> u.units_per_pack
      AND (
            (n."QuantityPerPack" > 0 AND n."QuantityPerPack" = f."YieldTotalQuantityPacked")
         OR (f."TotalNumberOfBottles" > 0
             AND f."TotalQuantityPacked" * u.units_per_pack <= f."TotalNumberOfBottles"
             AND f."TotalQuantityPacked" * u.units_per_pack > f."TotalNumberOfBottles" - u.units_per_pack
             AND n."TotalQuantity" <= f."TotalQuantityPacked")
      )
)
UPDATE "FinishedGoodsTransferNotes" n
SET "TotalQuantity"    = n."TotalQuantity" * p.units_per_pack,
    "QuantityReceived" = n."QuantityReceived" * p.units_per_pack,
    "QuantityPerPack"  = p.units_per_pack
FROM packs_notes p
WHERE n."Id" = p."Id";

-- 2. Final packings saved in packs: "available to transfer" becomes the yield in whole packs.
UPDATE "FinalPackings" f
SET "TotalQuantityPacked" = f."TotalQuantityPacked" * u.units_per_pack
FROM fg_units_per_pack u
WHERE u.packing_id = f."ProductPackingId"
  AND f."DeletedAt" IS NULL
  AND u.units_per_pack > 1
  AND f."TotalNumberOfBottles" > 0
  AND f."TotalQuantityPacked" * u.units_per_pack <= f."TotalNumberOfBottles"
  AND f."TotalQuantityPacked" * u.units_per_pack > f."TotalNumberOfBottles" - u.units_per_pack;

-- 3. Every other note: save the units per pack it is counted in, so later edits to the
--    packing style cannot change what the stock means.
UPDATE "FinishedGoodsTransferNotes" n
SET "QuantityPerPack" = u.units_per_pack
FROM fg_units_per_pack u
WHERE u.packing_id = n."ProductPackingId"
  AND n."DeletedAt" IS NULL
  AND n."QuantityPerPack" IS DISTINCT FROM u.units_per_pack;

-- 4. Allocation lines: their Quantity is packs (it was checked against the order, in packs).
--    Record the yield each took and the units per pack used.
UPDATE "AllocateProductQuantity" q
SET "UnitsPerPack"  = n."QuantityPerPack",
    "YieldQuantity" = q."Quantity" * n."QuantityPerPack"
FROM "FinishedGoodsTransferNotes" n
WHERE n."Id" = q."FinishedGoodsTransferNoteId"
  AND q."UnitsPerPack" = 0
  AND n."QuantityPerPack" > 0;

UPDATE "ProductionOrderProductQuantity" q
SET "UnitsPerPack"  = n."QuantityPerPack",
    "YieldQuantity" = q."Quantity" * n."QuantityPerPack"
FROM "FinishedGoodsTransferNotes" n
WHERE n."Id" = q."FinishedGoodsTransferNoteId"
  AND q."UnitsPerPack" = 0
  AND n."QuantityPerPack" > 0;

-- 5. A note's AllocatedQuantity is the yield its allocation lines took.
UPDATE "FinishedGoodsTransferNotes" n
SET "AllocatedQuantity" = taken.yield
FROM (
    SELECT n2."Id", COALESCE(SUM(q."YieldQuantity"), 0) AS yield
    FROM "FinishedGoodsTransferNotes" n2
    LEFT JOIN "ProductionOrderProductQuantity" q ON q."FinishedGoodsTransferNoteId" = n2."Id"
    WHERE n2."DeletedAt" IS NULL
    GROUP BY n2."Id"
) taken
WHERE n."Id" = taken."Id"
  AND n."AllocatedQuantity" IS DISTINCT FROM taken.yield;

-- 6. Stock must balance: nothing allocated beyond what the store holds. Abort otherwise.
DO $$
DECLARE over_allocated int;
DECLARE unconverted int;
BEGIN
    SELECT COUNT(*) INTO over_allocated
    FROM "FinishedGoodsTransferNotes" n
    WHERE n."DeletedAt" IS NULL
      AND n."AllocatedQuantity" > CASE WHEN n."IsApproved" AND n."QuantityReceived" > 0 THEN n."QuantityReceived" ELSE n."TotalQuantity" END;
    IF over_allocated > 0 THEN
        RAISE EXCEPTION '% transfer note(s) would have more allocated than they hold. Nothing was changed; review section 6 of detect_fg_yield_units.sql.', over_allocated;
    END IF;

    SELECT (SELECT COUNT(*) FROM "AllocateProductQuantity" WHERE "UnitsPerPack" = 0)
         + (SELECT COUNT(*) FROM "ProductionOrderProductQuantity" WHERE "UnitsPerPack" = 0)
    INTO unconverted;
    IF unconverted > 0 THEN
        RAISE EXCEPTION '% allocation line(s) could not be converted. Nothing was changed.', unconverted;
    END IF;
END $$;

COMMIT;
