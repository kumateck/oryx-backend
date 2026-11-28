-- Finished goods packing consistency check. READ-ONLY: a single SELECT, safe in Adminer.
-- Spec: docs/finished-goods-live-packing.md
--
-- Units per pack is worked out exactly as APP/Utils/PackConversion.UnitsPerPack does it:
--   * ignore rows in the style's base unit and rows in mass/volume units (mg, g, ml, ...);
--   * shipper row first, then the stored order;
--   * the pack is the first level whose running product equals PackPerShipper;
--   * units per pack = product of the rows below that level (1 when there are no counted rows);
--   * NULL when the rows never reach PackPerShipper (the style cannot convert).
--
-- Sections (column "section"):
--   1 FGTN copy differs from its current style  -> screens disagree today (the 200 vs 10 shippers case)
--   2 Open order line shippers/loose stored differently from what the current style gives
--   3 Undispatched packing list line drawn from a batch of a different style, or whose batch copy
--     gives a different packs-per-shipper than the order line's style
--   4 Undispatched packing list line whose recorded units per pack differs from the current style
--     -> would be "re-pricing required" under the new rule
--   5 Style that cannot convert at all
-- After the fix, sections 1-3 must return nothing.

WITH counted AS (
    SELECT l."ProductPackingId" AS pid, l."Quantity" AS q, COALESCE(u."Name", '?') AS unit,
           ROW_NUMBER() OVER (PARTITION BY l."ProductPackingId"
               ORDER BY CASE WHEN lower(trim(COALESCE(u."Name", ''))) LIKE 'shipper%' THEN 0 ELSE 1 END, l."Order") AS lvl
    FROM "ProductPackingList" l
    JOIN "ProductPackings" p ON p."Id" = l."ProductPackingId"
    LEFT JOIN "UnitOfMeasures" u ON u."Id" = l."UomId"
    WHERE l."UomId" IS DISTINCT FROM p."BasePackingUomId"
      AND lower(trim(COALESCE(u."Symbol", ''))) NOT IN ('mg', 'g', 'kg', 'mcg', 'µg', 'ug', 'ml', 'l', 'cl', 'dl')
),
running AS (
    SELECT pid, lvl, q, unit,
           round(exp(sum(ln(q)) OVER (PARTITION BY pid ORDER BY lvl))::numeric, 6) AS down_to
    FROM counted WHERE q > 0
),
style AS (
    SELECT p."Id", p."ProductId", p."Name" AS style_name, p."PackPerShipper",
           (SELECT string_agg(c.unit || ' ' || trim(to_char(c.q, 'FM999999990.######')), ' > ' ORDER BY c.lvl)
              FROM counted c WHERE c.pid = p."Id") AS layout,
           CASE
             WHEN NOT EXISTS (SELECT 1 FROM counted c WHERE c.pid = p."Id") THEN 1::numeric
             WHEN p."PackPerShipper" <= 0 OR EXISTS (SELECT 1 FROM counted c WHERE c.pid = p."Id" AND c.q <= 0) THEN NULL
             WHEN EXISTS (SELECT 1 FROM running r WHERE r.pid = p."Id" AND r.down_to = p."PackPerShipper")
               THEN (SELECT trim_scale(round(r.down_to / p."PackPerShipper", 6)) FROM running r WHERE r.pid = p."Id" ORDER BY r.lvl DESC LIMIT 1)
             ELSE NULL
           END AS units_per_pack
    FROM "ProductPackings" p
    WHERE p."DeletedAt" IS NULL
),
open_lines AS (  -- undispatched, uncancelled packing list lines with their order line style
    SELECT a."Id" AS packing_list_id, o."Code" AS order_code, ap."ProductId", ap."ProductPackingId" AS line_style_id,
           q."FinishedGoodsTransferNoteId" AS note_id, q."Quantity" AS packs, q."UnitsPerPack", q."YieldQuantity"
    FROM "AllocateProductionOrders" a
    JOIN "ProductionOrders" o ON o."Id" = a."ProductionOrderId"
    JOIN "AllocateProductionOrderProduct" ap ON ap."AllocateProductionOrderId" = a."Id"
    JOIN "AllocateProductQuantity" q ON q."AllocateProductionOrderProductAllocateProductionOrderId" = ap."AllocateProductionOrderId"
                                    AND q."AllocateProductionOrderProductId" = ap."Id"
    WHERE a."DispatchedAt" IS NULL AND a."CancelledAt" IS NULL AND a."DeletedAt" IS NULL
)

-- 1 FGTN copy differs from its current style
SELECT 1 AS section, pr."Name" AS product, s.style_name AS style, n."TransferNoteNumber" AS record,
       'FGTN copy vs current style' AS check_name,
       'per shipper ' || COALESCE(n."PacksPerShipperSnapshot"::text, 'none') || ', units/pack ' || n."QuantityPerPack" AS stored,
       'per shipper ' || s."PackPerShipper" || ', units/pack ' || COALESCE(s.units_per_pack::text, 'cannot convert') || ' [' || COALESCE(s.layout, 'no rows') || ']' AS current
FROM "FinishedGoodsTransferNotes" n
JOIN style s ON s."Id" = n."ProductPackingId"
JOIN "Products" pr ON pr."Id" = s."ProductId"
WHERE n."DeletedAt" IS NULL
  AND n."DispatchedQuantity" < n."QuantityReceived" + n."AdjustmentQuantity" + n."ReleasedReturnQuantity"
  AND (n."PacksPerShipperSnapshot" IS DISTINCT FROM s."PackPerShipper"::numeric
       OR n."QuantityPerPack" IS DISTINCT FROM s.units_per_pack)

UNION ALL
-- 2 Open order line stored shippers/loose vs the current style
SELECT 2, pr."Name", s.style_name, o."Code",
       'Order line shippers/loose vs current style',
       op."TotalOrderQuantity" || ' packs = ' || op."Shippers" || ' shippers + ' || op."Loose" || ' loose',
       'per shipper ' || s."PackPerShipper" || ' -> ' || floor(op."TotalOrderQuantity"::numeric / s."PackPerShipper") || ' shippers + '
         || (op."TotalOrderQuantity" - floor(op."TotalOrderQuantity"::numeric / s."PackPerShipper") * s."PackPerShipper") || ' loose'
FROM "ProductionOrders" o
JOIN "ProductionOrderProducts" op ON op."ProductionOrderId" = o."Id"
JOIN style s ON s."Id" = op."ProductPackingId"
JOIN "Products" pr ON pr."Id" = op."ProductId"
WHERE o."DeletedAt" IS NULL AND o."DeliveredAt" IS NULL AND s."PackPerShipper" > 0
  AND (op."Shippers" <> floor(op."TotalOrderQuantity"::numeric / s."PackPerShipper")
       OR op."Loose" <> op."TotalOrderQuantity" - floor(op."TotalOrderQuantity"::numeric / s."PackPerShipper") * s."PackPerShipper")

UNION ALL
-- 3 Packing list line: batch style or batch copy disagrees with the order line's style
SELECT 3, pr."Name", ls.style_name, l.order_code || ' / ' || n."TransferNoteNumber",
       'Packing list batch vs order line style',
       'batch style ' || COALESCE(ns.style_name, n."PackingStyleSnapshot", 'none') || ', batch copy per shipper ' || COALESCE(n."PacksPerShipperSnapshot"::text, 'none')
         || ', ' || l.packs || ' packs',
       'order style per shipper ' || ls."PackPerShipper"
FROM open_lines l
JOIN "FinishedGoodsTransferNotes" n ON n."Id" = l.note_id
JOIN style ls ON ls."Id" = l.line_style_id
LEFT JOIN style ns ON ns."Id" = n."ProductPackingId"
JOIN "Products" pr ON pr."Id" = l."ProductId"
WHERE n."ProductPackingId" IS DISTINCT FROM l.line_style_id
   OR n."PacksPerShipperSnapshot" IS DISTINCT FROM ls."PackPerShipper"::numeric

UNION ALL
-- 4 Packing list line whose recorded units per pack differs from the current style (re-pricing under the new rule)
SELECT 4, pr."Name", ls.style_name, l.order_code || ' / ' || l.packing_list_id::text,
       'Recorded units per pack vs current style',
       l.packs || ' packs x ' || l."UnitsPerPack" || ' = ' || l."YieldQuantity" || ' base units',
       'units/pack now ' || COALESCE(ls.units_per_pack::text, 'cannot convert')
FROM open_lines l
JOIN style ls ON ls."Id" = l.line_style_id
JOIN "Products" pr ON pr."Id" = l."ProductId"
WHERE l."UnitsPerPack" IS DISTINCT FROM ls.units_per_pack

UNION ALL
-- 5 Styles that cannot convert
SELECT 5, pr."Name", s.style_name, s."Id"::text,
       'Style rows never reach packs per shipper',
       COALESCE(s.layout, 'no rows'),
       'per shipper ' || s."PackPerShipper"
FROM style s
JOIN "Products" pr ON pr."Id" = s."ProductId"
WHERE s.units_per_pack IS NULL

ORDER BY 1, 2, 4;
