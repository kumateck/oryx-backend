-- Why does a purchase order line have no PriceUoM? (read-only)
-- Database: PostgreSQL. Change the code on the first line of each query.
--
-- The read-time fallback (PriceUoMExtensions) and fix_missing_price_uom.sql only take a
-- unit from a quotation line that matches ALL of: same PurchaseOrderId, same MaterialId,
-- same UoMId, Status = 1 (Processed), and a quotation from the order's own supplier.
-- The columns below show which of those fails.
--
-- SupplierQuotationItemStatus: 0 = NotProcessed, 1 = Processed, 2 = NotUsed

-- 1. The order's lines as stored.
SELECT po."Code", po."Status" AS po_status, po."SupplierId", poi."Id" AS line_id,
       poi."MaterialId", poi."UoMId", poi."Quantity", poi."Price", poi."PriceUoM", poi."DeletedAt"
FROM "PurchaseOrders" po
JOIN "PurchaseOrderItems" poi ON poi."PurchaseOrderId" = po."Id"
WHERE po."Code" = 'EPRC-69';

-- 2. Every quotation line for the same material, with the match checks spelled out.
SELECT sq."SupplierId" = po."SupplierId"        AS same_supplier,
       sqi."PurchaseOrderId" = po."Id"          AS same_order,
       sqi."UoMId" = poi."UoMId"                AS same_uom,
       sqi."Status"                              AS status,
       sqi."DeletedAt" IS NULL                   AS not_deleted,
       sqi."PriceUoM", sqi."QuotedPrice", sqi."UoMId" AS quote_uom_id,
       sqi."PurchaseOrderId" AS quote_po_id, sqi."CreatedAt", sq."Id" AS quotation_id
FROM "PurchaseOrders" po
JOIN "PurchaseOrderItems" poi ON poi."PurchaseOrderId" = po."Id" AND poi."DeletedAt" IS NULL
JOIN "SupplierQuotationItems" sqi ON sqi."MaterialId" = poi."MaterialId"
JOIN "SupplierQuotations" sq ON sq."Id" = sqi."SupplierQuotationId"
WHERE po."Code" = 'EPRC-69'
ORDER BY same_supplier DESC, same_order DESC, sqi."CreatedAt" DESC;

-- 3. The revision history of the order's lines (what each revision changed the unit/price from and to).
SELECT r.*
FROM "RevisedPurchaseOrders" r
JOIN "PurchaseOrders" po ON po."Id" = r."PurchaseOrderId"
WHERE po."Code" = 'EPRC-69'
ORDER BY r."RevisionNumber";
