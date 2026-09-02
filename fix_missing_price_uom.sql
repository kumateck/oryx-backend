-- Price UoM Repair Script
-- Database: PostgreSQL
--
-- Sources the price UoM from the awarded supplier quotation - the system of record -
-- and pushes it down the chain that lost it:
--
--     SupplierQuotationItems  ->  PurchaseOrderItems  ->  ShipmentInvoiceItems
--
-- Idempotent and re-runnable: every step only touches rows that are still missing a
-- value, so running it twice changes nothing the second time.
--
-- Run detect_missing_price_uom.sql before and after to compare.
--
-- IMPORTANT: run this only AFTER the migration that adds
-- ShipmentInvoiceItems."Price" / "PriceUoM" (AddPriceAndPriceUoMToShipmentInvoiceItem),
-- and BEFORE enabling the Procurement:EnforcePriceUoM guard.
--
-- SupplierQuotationItemStatus: 0 = NotProcessed, 1 = Processed, 2 = NotUsed

BEGIN;

-- =====================================================================
-- 1. PurchaseOrderItems.PriceUoM  <-  the AWARDED supplier's quotation
-- =====================================================================
-- The supplier predicate is not optional. When a quotation is awarded, every LOSING
-- supplier's quotation line for that material is also stamped with the winner's
-- PurchaseOrderId (the reassign-supplier picker looks them up by it). Joining on the
-- purchase order alone would copy a competitor's price UoM onto the winner's line.
UPDATE "PurchaseOrderItems" poi
SET "PriceUoM" = src."PriceUoM"
FROM (
    SELECT DISTINCT ON (sqi."PurchaseOrderId", sqi."MaterialId", sqi."UoMId")
        sqi."PurchaseOrderId",
        sqi."MaterialId",
        sqi."UoMId",
        sqi."PriceUoM"
    FROM "SupplierQuotationItems" sqi
    JOIN "SupplierQuotations" sq
        ON sq."Id" = sqi."SupplierQuotationId"
    JOIN "PurchaseOrders" po
        ON po."Id"         = sqi."PurchaseOrderId"
       AND po."SupplierId" = sq."SupplierId"          -- awarded supplier only
    WHERE sqi."DeletedAt" IS NULL
      AND sqi."Status" = 1                            -- Processed = the awarded line
      AND sqi."PriceUoM" IS NOT NULL
      AND btrim(sqi."PriceUoM") <> ''
    ORDER BY sqi."PurchaseOrderId", sqi."MaterialId", sqi."UoMId", sqi."CreatedAt" DESC
) AS src
WHERE poi."PurchaseOrderId" = src."PurchaseOrderId"
  AND poi."MaterialId"      = src."MaterialId"
  AND poi."UoMId"           = src."UoMId"
  AND poi."DeletedAt" IS NULL
  AND (poi."PriceUoM" IS NULL OR btrim(poi."PriceUoM") = '');

-- =====================================================================
-- 2. ShipmentInvoiceItems.Price / .PriceUoM  <-  PurchaseOrderItems
-- =====================================================================
-- These columns are new. Invoice prices used to be resolved live from the purchase
-- order at read time, which meant a later PO revision retroactively rewrote invoices
-- that had already been issued. They are now frozen on the row; this backfills the
-- rows that pre-date the change.
--
-- Only unambiguous matches are repaired. Where a purchase order holds more than one
-- line for the same material, the correct one cannot be determined from the data, so
-- the row is left alone and reported in step 3 instead of being guessed at.
UPDATE "ShipmentInvoiceItems" sii
SET "Price"    = CASE WHEN sii."Price" = 0 THEN src."Price" ELSE sii."Price" END,
    "PriceUoM" = COALESCE(NULLIF(btrim(sii."PriceUoM"), ''), src."PriceUoM")
FROM (
    SELECT
        poi."PurchaseOrderId",
        poi."MaterialId",
        min(poi."Price")    AS "Price",
        min(poi."PriceUoM") AS "PriceUoM"
    FROM "PurchaseOrderItems" poi
    WHERE poi."DeletedAt" IS NULL
    GROUP BY poi."PurchaseOrderId", poi."MaterialId"
    HAVING count(*) = 1                               -- unambiguous lines only
) AS src
WHERE sii."PurchaseOrderId" = src."PurchaseOrderId"
  AND sii."MaterialId"      = src."MaterialId"
  AND sii."DeletedAt" IS NULL
  AND (
        sii."Price" = 0
     OR sii."PriceUoM" IS NULL
     OR btrim(sii."PriceUoM") = ''
  );

COMMIT;

-- =====================================================================
-- 3. What could not be repaired
-- =====================================================================
-- Anything listed here needs a human decision - do not guess a unit onto it.

-- 3a. Purchase order lines with a price and still no unit, and why.
SELECT
    po."Code"        AS purchase_order,
    poi."MaterialId",
    poi."Price",
    CASE
        WHEN NOT EXISTS (
            SELECT 1
            FROM "SupplierQuotationItems" sqi
            JOIN "SupplierQuotations" sq ON sq."Id" = sqi."SupplierQuotationId"
            WHERE sqi."PurchaseOrderId" = poi."PurchaseOrderId"
              AND sqi."MaterialId"      = poi."MaterialId"
              AND sq."SupplierId"       = po."SupplierId"
        ) THEN 'no quotation from the awarded supplier'
        ELSE 'quotation exists but has no PriceUoM'
    END AS reason
FROM "PurchaseOrderItems" poi
JOIN "PurchaseOrders" po ON po."Id" = poi."PurchaseOrderId"
WHERE poi."DeletedAt" IS NULL
  AND poi."Price" > 0
  AND (poi."PriceUoM" IS NULL OR btrim(poi."PriceUoM") = '')
ORDER BY po."Code";

-- 3b. Invoice lines left unrepaired because the purchase order is ambiguous.
SELECT
    si."Code" AS shipment_invoice,
    sii."PurchaseOrderId",
    sii."MaterialId",
    sii."Price",
    sii."PriceUoM",
    'purchase order has multiple lines for this material' AS reason
FROM "ShipmentInvoiceItems" sii
JOIN "ShipmentInvoices" si ON si."Id" = sii."ShipmentInvoiceId"
WHERE sii."DeletedAt" IS NULL
  AND (sii."Price" = 0 OR sii."PriceUoM" IS NULL OR btrim(sii."PriceUoM") = '')
ORDER BY si."Code";
