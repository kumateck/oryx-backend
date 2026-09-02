-- Price UoM Detection Script
-- Database: PostgreSQL
-- Run this BEFORE fix_missing_price_uom.sql, and again after, to compare.
--
-- Context: a price is meaningless without the unit it is quoted in. PriceUoM is
-- authoritative on "SupplierQuotationItems" and must travel with every price
-- downstream. These queries measure where it did not.
--
-- SupplierQuotationItemStatus: 0 = NotProcessed, 1 = Processed, 2 = NotUsed

-- === 1. Headline counts: prices stored without a unit ===
-- This is the number that must reach zero and stay there.
SELECT
    'PurchaseOrderItems' AS table_name,
    count(*) FILTER (WHERE "PriceUoM" IS NULL OR btrim("PriceUoM") = '') AS missing_price_uom,
    count(*) AS total_rows
FROM "PurchaseOrderItems"
WHERE "DeletedAt" IS NULL AND "Price" > 0

UNION ALL

SELECT
    'SupplierQuotationItems',
    count(*) FILTER (WHERE "PriceUoM" IS NULL OR btrim("PriceUoM") = ''),
    count(*)
FROM "SupplierQuotationItems"
WHERE "DeletedAt" IS NULL AND "QuotedPrice" > 0

UNION ALL

-- Price and PriceUoM were added to this table by
-- AddPriceAndPriceUoMToShipmentInvoiceItem; before that the price was resolved live
-- from the purchase order at read time.
SELECT
    'ShipmentInvoiceItems',
    count(*) FILTER (WHERE "PriceUoM" IS NULL OR btrim("PriceUoM") = ''),
    count(*)
FROM "ShipmentInvoiceItems"
WHERE "DeletedAt" IS NULL AND "Price" > 0;

-- === 2. Cross-supplier contamination ===
-- After an award, the "process for everyone else" loop stamps the winner's
-- PurchaseOrderId onto every LOSING supplier's quotation item for that material.
-- Any row here means a read-side backfill keyed only on (PurchaseOrderId,
-- MaterialId, UoMId) can return a competitor's PriceUoM next to the winner's price.
-- Expect > 0 rows on an unfixed database.
SELECT
    poi."PurchaseOrderId",
    poi."MaterialId",
    poi."UoMId",
    count(DISTINCT sq."SupplierId")                         AS competing_suppliers,
    count(DISTINCT coalesce(sqi."PriceUoM", '<null>'))      AS distinct_price_uoms,
    string_agg(DISTINCT coalesce(sqi."PriceUoM", '<null>'), ', ') AS price_uoms_seen
FROM "PurchaseOrderItems" poi
JOIN "PurchaseOrders" po
    ON po."Id" = poi."PurchaseOrderId"
JOIN "SupplierQuotationItems" sqi
    ON sqi."PurchaseOrderId" = poi."PurchaseOrderId"
   AND sqi."MaterialId"      = poi."MaterialId"
   AND sqi."UoMId"           = poi."UoMId"
   AND sqi."DeletedAt" IS NULL
JOIN "SupplierQuotations" sq
    ON sq."Id" = sqi."SupplierQuotationId"
WHERE poi."DeletedAt" IS NULL
GROUP BY poi."PurchaseOrderId", poi."MaterialId", poi."UoMId"
HAVING count(DISTINCT sq."SupplierId") > 1
ORDER BY competing_suppliers DESC;

-- === 3. Ambiguous (PurchaseOrder, Material) pairs ===
-- OryxMapper resolves ShipmentInvoiceItem price via
-- PurchaseOrder.Items.First(i => i.MaterialId == src.MaterialId).
-- Any row here means that .First() picks arbitrarily between two real lines.
SELECT
    "PurchaseOrderId",
    "MaterialId",
    count(*)                                              AS line_count,
    count(DISTINCT "UoMId")                               AS distinct_uoms,
    string_agg(DISTINCT coalesce("PriceUoM", '<null>'), ', ') AS price_uoms_seen
FROM "PurchaseOrderItems"
WHERE "DeletedAt" IS NULL
GROUP BY "PurchaseOrderId", "MaterialId"
HAVING count(*) > 1
ORDER BY line_count DESC;

-- === 4. How much of the gap is repairable from quotations? ===
-- Splits the missing PurchaseOrderItems rows into those the repair script can
-- source from the awarded supplier's quotation, and those it cannot.
SELECT
    CASE
        WHEN sqi."Id" IS NULL                              THEN 'no matching awarded quotation - UNRESOLVABLE'
        WHEN sqi."PriceUoM" IS NULL
          OR btrim(sqi."PriceUoM") = ''                    THEN 'quotation itself has no PriceUoM - UNRESOLVABLE'
        ELSE                                                    'repairable from quotation'
    END AS repair_outlook,
    count(*) AS rows_affected
FROM "PurchaseOrderItems" poi
JOIN "PurchaseOrders" po
    ON po."Id" = poi."PurchaseOrderId"
LEFT JOIN "SupplierQuotationItems" sqi
    ON sqi."PurchaseOrderId" = poi."PurchaseOrderId"
   AND sqi."MaterialId"      = poi."MaterialId"
   AND sqi."UoMId"           = poi."UoMId"
   AND sqi."Status"          = 1                    -- Processed: the awarded line
   AND sqi."DeletedAt" IS NULL
   AND EXISTS (
       SELECT 1
       FROM "SupplierQuotations" sq
       WHERE sq."Id"         = sqi."SupplierQuotationId"
         AND sq."SupplierId" = po."SupplierId"      -- the winning supplier only
   )
WHERE poi."DeletedAt" IS NULL
  AND poi."Price" > 0
  AND (poi."PriceUoM" IS NULL OR btrim(poi."PriceUoM") = '')
GROUP BY 1
ORDER BY rows_affected DESC;
