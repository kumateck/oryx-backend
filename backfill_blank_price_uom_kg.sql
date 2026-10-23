-- Backfill blank price units: 'kg' for mass lines, the line's own unit for counted lines (2026-10-23)
-- Database: PostgreSQL
--
-- Quotation responses entered before 2026-08-06 could be saved with no price unit
-- (empty string or NULL). Those blanks flowed into purchase order and shipment invoice
-- lines, where a price with no unit is costed against the quantity in mg
-- (EPRC-69: 650 kg x 1.2 shown as 780,000,000).
--
-- Decision (user, 2026-10-23): treat every blank price unit as 'kg'.
--
-- Limits, so the backfill cannot create a new wrong number:
--   * Only lines whose own unit is a mass unit (mg, g, kg) get 'kg'.
--   * A line in a counted unit (pcs, pack, ...) gets its own unit instead: a count is stored as
--     entered, so price x quantity was already its true value. No amount changes (step 2b).
--   * Lines in volume or length units (ml, L, m) are listed in step 1b and left alone: a blank
--     there could mean per ml or per L, and only a person can tell.
--   * Lines with no price yet (losing or unused quotes) are included, so no blank unit is left
--     behind for detect_missing_price_uom.sql to report (its ", kg" rows: one blank, one kg).
--   * Lines priced above 0 but under 0.01 are left alone and listed in step 1c. A price that small is
--     almost certainly per g or per mg (e.g. 0.0003), and per kg it would be a thousand or
--     a million times too cheap.
--   * Every changed row is recorded in "_PriceUoMKgBackfill", so the change can be undone
--     (step 4).
--
-- Run step 1 first and read it. Then run step 2 (the transaction). Step 3 checks the result.

-- =====================================================================
-- 1. Preview (read-only)
-- =====================================================================

-- 1a. What will be set to 'kg', per table.
SELECT 'SupplierQuotationItems' AS table_name, count(*) AS rows_to_update
FROM "SupplierQuotationItems" x JOIN "UnitOfMeasures" u ON u."Id" = x."UoMId"
WHERE x."DeletedAt" IS NULL AND btrim(coalesce(x."PriceUoM", '')) = ''
  AND (x."QuotedPrice" IS NULL OR x."QuotedPrice" = 0 OR x."QuotedPrice" >= 0.01) AND lower(btrim(u."Symbol")) IN ('mg', 'g', 'kg')
UNION ALL
SELECT 'PurchaseOrderItems', count(*)
FROM "PurchaseOrderItems" x JOIN "UnitOfMeasures" u ON u."Id" = x."UoMId"
WHERE x."DeletedAt" IS NULL AND btrim(coalesce(x."PriceUoM", '')) = ''
  AND (x."Price" = 0 OR x."Price" >= 0.01) AND lower(btrim(u."Symbol")) IN ('mg', 'g', 'kg')
UNION ALL
SELECT 'ShipmentInvoiceItems', count(*)
FROM "ShipmentInvoiceItems" x JOIN "UnitOfMeasures" u ON u."Id" = x."UoMId"
WHERE x."DeletedAt" IS NULL AND btrim(coalesce(x."PriceUoM", '')) = ''
  AND (x."Price" = 0 OR x."Price" >= 0.01) AND lower(btrim(u."Symbol")) IN ('mg', 'g', 'kg');

-- 1a (detail). Purchase order lines that will change.
SELECT po."Code", m."Name" AS material, x."Quantity", u."Symbol" AS quantity_unit, x."Price"
FROM "PurchaseOrderItems" x
JOIN "PurchaseOrders" po ON po."Id" = x."PurchaseOrderId"
JOIN "Materials" m ON m."Id" = x."MaterialId"
JOIN "UnitOfMeasures" u ON u."Id" = x."UoMId"
WHERE x."DeletedAt" IS NULL AND btrim(coalesce(x."PriceUoM", '')) = ''
  AND (x."Price" = 0 OR x."Price" >= 0.01) AND lower(btrim(u."Symbol")) IN ('mg', 'g', 'kg')
ORDER BY po."Code";

-- 1b. Blank lines NOT changed: measured in volume or length (per ml or per L cannot be told apart).
SELECT 'SupplierQuotationItems' AS table_name, x."Id", u."Symbol", x."QuotedPrice" AS price
FROM "SupplierQuotationItems" x JOIN "UnitOfMeasures" u ON u."Id" = x."UoMId"
WHERE x."DeletedAt" IS NULL AND btrim(coalesce(x."PriceUoM", '')) = '' AND x."QuotedPrice" > 0
  AND lower(btrim(coalesce(u."Symbol", ''))) IN ('', 'ml', 'l', 'm', 'km', 'm²', 'm³')
UNION ALL
SELECT 'PurchaseOrderItems', x."Id", u."Symbol", x."Price"
FROM "PurchaseOrderItems" x JOIN "UnitOfMeasures" u ON u."Id" = x."UoMId"
WHERE x."DeletedAt" IS NULL AND btrim(coalesce(x."PriceUoM", '')) = '' AND x."Price" > 0
  AND lower(btrim(coalesce(u."Symbol", ''))) IN ('', 'ml', 'l', 'm', 'km', 'm²', 'm³')
UNION ALL
SELECT 'ShipmentInvoiceItems', x."Id", u."Symbol", x."Price"
FROM "ShipmentInvoiceItems" x JOIN "UnitOfMeasures" u ON u."Id" = x."UoMId"
WHERE x."DeletedAt" IS NULL AND btrim(coalesce(x."PriceUoM", '')) = '' AND x."Price" > 0
  AND lower(btrim(coalesce(u."Symbol", ''))) IN ('', 'ml', 'l', 'm', 'km', 'm²', 'm³');

-- 1c. Blank lines NOT changed: price under 0.01, so probably per g or per mg.
SELECT 'SupplierQuotationItems' AS table_name, x."Id", u."Symbol", x."QuotedPrice" AS price
FROM "SupplierQuotationItems" x JOIN "UnitOfMeasures" u ON u."Id" = x."UoMId"
WHERE x."DeletedAt" IS NULL AND btrim(coalesce(x."PriceUoM", '')) = ''
  AND x."QuotedPrice" > 0 AND x."QuotedPrice" < 0.01
UNION ALL
SELECT 'PurchaseOrderItems', x."Id", u."Symbol", x."Price"
FROM "PurchaseOrderItems" x JOIN "UnitOfMeasures" u ON u."Id" = x."UoMId"
WHERE x."DeletedAt" IS NULL AND btrim(coalesce(x."PriceUoM", '')) = ''
  AND x."Price" > 0 AND x."Price" < 0.01
UNION ALL
SELECT 'ShipmentInvoiceItems', x."Id", u."Symbol", x."Price"
FROM "ShipmentInvoiceItems" x JOIN "UnitOfMeasures" u ON u."Id" = x."UoMId"
WHERE x."DeletedAt" IS NULL AND btrim(coalesce(x."PriceUoM", '')) = ''
  AND x."Price" > 0 AND x."Price" < 0.01;

-- 1d. Counted-unit lines that will get their own unit (amounts unchanged).
SELECT 'SupplierQuotationItems' AS table_name, u."Symbol", count(*) AS rows_to_update
FROM "SupplierQuotationItems" x JOIN "UnitOfMeasures" u ON u."Id" = x."UoMId"
WHERE x."DeletedAt" IS NULL AND btrim(coalesce(x."PriceUoM", '')) = '' AND lower(btrim(coalesce(u."Symbol", ''))) NOT IN ('', 'mg', 'g', 'kg', 'ml', 'l', 'm', 'km', 'm²', 'm³')
GROUP BY u."Symbol"
UNION ALL
SELECT 'PurchaseOrderItems', u."Symbol", count(*)
FROM "PurchaseOrderItems" x JOIN "UnitOfMeasures" u ON u."Id" = x."UoMId"
WHERE x."DeletedAt" IS NULL AND btrim(coalesce(x."PriceUoM", '')) = '' AND lower(btrim(coalesce(u."Symbol", ''))) NOT IN ('', 'mg', 'g', 'kg', 'ml', 'l', 'm', 'km', 'm²', 'm³')
GROUP BY u."Symbol"
UNION ALL
SELECT 'ShipmentInvoiceItems', u."Symbol", count(*)
FROM "ShipmentInvoiceItems" x JOIN "UnitOfMeasures" u ON u."Id" = x."UoMId"
WHERE x."DeletedAt" IS NULL AND btrim(coalesce(x."PriceUoM", '')) = '' AND lower(btrim(coalesce(u."Symbol", ''))) NOT IN ('', 'mg', 'g', 'kg', 'ml', 'l', 'm', 'km', 'm²', 'm³')
GROUP BY u."Symbol";

-- =====================================================================
-- 2. Backfill (one transaction)
-- =====================================================================
BEGIN;

CREATE TABLE IF NOT EXISTS "_PriceUoMKgBackfill" (
    table_name text        NOT NULL,
    row_id     uuid        NOT NULL,
    old_value  text,
    changed_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (table_name, row_id)
);

WITH changed AS (
    UPDATE "SupplierQuotationItems" x
    SET "PriceUoM" = 'kg'
    FROM "UnitOfMeasures" u, "SupplierQuotationItems" old
    WHERE u."Id" = x."UoMId" AND old."Id" = x."Id"
      AND x."DeletedAt" IS NULL AND btrim(coalesce(x."PriceUoM", '')) = ''
      AND (x."QuotedPrice" IS NULL OR x."QuotedPrice" = 0 OR x."QuotedPrice" >= 0.01) AND lower(btrim(u."Symbol")) IN ('mg', 'g', 'kg')
    RETURNING x."Id", old."PriceUoM"
)
INSERT INTO "_PriceUoMKgBackfill" (table_name, row_id, old_value)
SELECT 'SupplierQuotationItems', "Id", "PriceUoM" FROM changed
ON CONFLICT DO NOTHING;

WITH changed AS (
    UPDATE "PurchaseOrderItems" x
    SET "PriceUoM" = 'kg'
    FROM "UnitOfMeasures" u, "PurchaseOrderItems" old
    WHERE u."Id" = x."UoMId" AND old."Id" = x."Id"
      AND x."DeletedAt" IS NULL AND btrim(coalesce(x."PriceUoM", '')) = ''
      AND (x."Price" = 0 OR x."Price" >= 0.01) AND lower(btrim(u."Symbol")) IN ('mg', 'g', 'kg')
    RETURNING x."Id", old."PriceUoM"
)
INSERT INTO "_PriceUoMKgBackfill" (table_name, row_id, old_value)
SELECT 'PurchaseOrderItems', "Id", "PriceUoM" FROM changed
ON CONFLICT DO NOTHING;

WITH changed AS (
    UPDATE "ShipmentInvoiceItems" x
    SET "PriceUoM" = 'kg'
    FROM "UnitOfMeasures" u, "ShipmentInvoiceItems" old
    WHERE u."Id" = x."UoMId" AND old."Id" = x."Id"
      AND x."DeletedAt" IS NULL AND btrim(coalesce(x."PriceUoM", '')) = ''
      AND (x."Price" = 0 OR x."Price" >= 0.01) AND lower(btrim(u."Symbol")) IN ('mg', 'g', 'kg')
    RETURNING x."Id", old."PriceUoM"
)
INSERT INTO "_PriceUoMKgBackfill" (table_name, row_id, old_value)
SELECT 'ShipmentInvoiceItems', "Id", "PriceUoM" FROM changed
ON CONFLICT DO NOTHING;

-- 2b. Counted units: each line takes its own unit (40 pcs at 2.5 stays 100).
WITH changed AS (
    UPDATE "SupplierQuotationItems" x
    SET "PriceUoM" = btrim(u."Symbol")
    FROM "UnitOfMeasures" u, "SupplierQuotationItems" old
    WHERE u."Id" = x."UoMId" AND old."Id" = x."Id"
      AND x."DeletedAt" IS NULL AND btrim(coalesce(x."PriceUoM", '')) = ''
      AND lower(btrim(coalesce(u."Symbol", ''))) NOT IN ('', 'mg', 'g', 'kg', 'ml', 'l', 'm', 'km', 'm²', 'm³')
    RETURNING x."Id", old."PriceUoM"
)
INSERT INTO "_PriceUoMKgBackfill" (table_name, row_id, old_value)
SELECT 'SupplierQuotationItems', "Id", "PriceUoM" FROM changed
ON CONFLICT DO NOTHING;

WITH changed AS (
    UPDATE "PurchaseOrderItems" x
    SET "PriceUoM" = btrim(u."Symbol")
    FROM "UnitOfMeasures" u, "PurchaseOrderItems" old
    WHERE u."Id" = x."UoMId" AND old."Id" = x."Id"
      AND x."DeletedAt" IS NULL AND btrim(coalesce(x."PriceUoM", '')) = ''
      AND lower(btrim(coalesce(u."Symbol", ''))) NOT IN ('', 'mg', 'g', 'kg', 'ml', 'l', 'm', 'km', 'm²', 'm³')
    RETURNING x."Id", old."PriceUoM"
)
INSERT INTO "_PriceUoMKgBackfill" (table_name, row_id, old_value)
SELECT 'PurchaseOrderItems', "Id", "PriceUoM" FROM changed
ON CONFLICT DO NOTHING;

WITH changed AS (
    UPDATE "ShipmentInvoiceItems" x
    SET "PriceUoM" = btrim(u."Symbol")
    FROM "UnitOfMeasures" u, "ShipmentInvoiceItems" old
    WHERE u."Id" = x."UoMId" AND old."Id" = x."Id"
      AND x."DeletedAt" IS NULL AND btrim(coalesce(x."PriceUoM", '')) = ''
      AND lower(btrim(coalesce(u."Symbol", ''))) NOT IN ('', 'mg', 'g', 'kg', 'ml', 'l', 'm', 'km', 'm²', 'm³')
    RETURNING x."Id", old."PriceUoM"
)
INSERT INTO "_PriceUoMKgBackfill" (table_name, row_id, old_value)
SELECT 'ShipmentInvoiceItems', "Id", "PriceUoM" FROM changed
ON CONFLICT DO NOTHING;

-- Rows changed in this run, per table.
SELECT table_name, count(*) FROM "_PriceUoMKgBackfill"
WHERE changed_at = now() GROUP BY table_name;

COMMIT;

-- =====================================================================
-- 3. Check
-- =====================================================================
-- EPRC-69 should now read kg.
SELECT po."Code", x."Price", x."PriceUoM"
FROM "PurchaseOrderItems" x JOIN "PurchaseOrders" po ON po."Id" = x."PurchaseOrderId"
WHERE po."Code" = 'EPRC-69';

-- =====================================================================
-- 4. Undo (only if needed)
-- =====================================================================
-- BEGIN;
-- UPDATE "SupplierQuotationItems" x SET "PriceUoM" = b.old_value FROM "_PriceUoMKgBackfill" b
--   WHERE b.table_name = 'SupplierQuotationItems' AND b.row_id = x."Id";
-- UPDATE "PurchaseOrderItems" x SET "PriceUoM" = b.old_value FROM "_PriceUoMKgBackfill" b
--   WHERE b.table_name = 'PurchaseOrderItems' AND b.row_id = x."Id";
-- UPDATE "ShipmentInvoiceItems" x SET "PriceUoM" = b.old_value FROM "_PriceUoMKgBackfill" b
--   WHERE b.table_name = 'ShipmentInvoiceItems' AND b.row_id = x."Id";
-- COMMIT;
