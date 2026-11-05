-- Batch repair plan, derived from the reviewed migration-reservation maintenance checks.
CREATE TEMP TABLE repair_target ON COMMIT DROP AS
SELECT m."Id" AS migration_id, m."DepartmentId" AS department_id, m."ApprovedAt" AS approved_at,
  m."FileName" AS file_name, u."Id" AS actor_id
FROM public."StockMigrations" m CROSS JOIN public.users u
WHERE m."Id" = current_setting('oryx_reservation_repair.migration')::uuid
  AND m."Status" = 1 AND m."Mode" = 0 AND m."DeletedAt" IS NULL AND m."ApprovedAt" IS NOT NULL
  AND u."Id" = current_setting('oryx_reservation_repair.actor')::uuid
  AND u."DepartmentId" = m."DepartmentId" AND NOT u."IsDisabled" AND u."DeletedAt" IS NULL;
DO $target$ BEGIN
  IF (SELECT count(*) FROM repair_target) <> 1 THEN
    RAISE EXCEPTION 'Require one approved replacement migration and one active operator in its department';
  END IF;
END $target$;

CREATE TEMP TABLE affected_requisitions ON COMMIT DROP AS
SELECT DISTINCT r."Id" AS id
FROM public."Requisitions" r
JOIN public."RequisitionItems" i ON i."RequisitionId" = r."Id" AND i."DeletedAt" IS NULL
JOIN public."MaterialBatchReservedQuantities" q ON q."ProductionScheduleProductId" = r."ProductionScheduleProductId" AND q."DeletedAt" IS NULL
JOIN public."MaterialBatches" b ON b."Id" = q."MaterialBatchId" AND b."MaterialId" = i."MaterialId"
JOIN public."StockMigrationWriteOffs" off ON off."MaterialBatchId" = b."Id"
JOIN public."StockMigrations" old_replacement ON old_replacement."Id" = off."StockMigrationId"
CROSS JOIN repair_target t
WHERE r."ProductionScheduleProductId" = current_setting('oryx_reservation_repair.batch')::uuid
  AND r."DepartmentId" = t.department_id AND r."DeletedAt" IS NULL
  AND r."RequisitionType" = 0 AND r."Status" IN (0, 1, 2) AND b."Status" = 7
  AND old_replacement."Status" = 1 AND old_replacement."Mode" = 0
  AND old_replacement."DepartmentId" = t.department_id AND old_replacement."ApprovedAt" <= t.approved_at;

CREATE TEMP TABLE repair_problems (problem text NOT NULL) ON COMMIT DROP;
-- A missing or non-migration lot cannot be reconstructed from guessed material data.
INSERT INTO repair_problems
SELECT 'A reservation has missing, deleted or untraceable batch evidence; review it separately'
WHERE EXISTS (SELECT 1 FROM public."MaterialBatchReservedQuantities" q
  LEFT JOIN public."MaterialBatches" b ON b."Id" = q."MaterialBatchId"
  WHERE q."ProductionScheduleProductId" = current_setting('oryx_reservation_repair.batch')::uuid
    AND q."DeletedAt" IS NULL AND (b."Id" IS NULL OR b."DeletedAt" IS NOT NULL
      OR (b."Status" = 7 AND NOT EXISTS (SELECT 1 FROM public."StockMigrationWriteOffs" off
        JOIN public."StockMigrations" m ON m."Id" = off."StockMigrationId" CROSS JOIN repair_target t
        WHERE off."MaterialBatchId" = b."Id" AND m."DepartmentId" = t.department_id
          AND m."Status" = 1 AND m."Mode" = 0 AND m."ApprovedAt" <= t.approved_at))));
INSERT INTO repair_problems
SELECT r."Code" || ': requisition has issue evidence, an approval/quantity problem, or closed production'
FROM public."Requisitions" r JOIN affected_requisitions a ON a.id = r."Id"
LEFT JOIN public."ProductionScheduleProducts" p ON p."Id" = r."ProductionScheduleProductId"
LEFT JOIN public."ProductionSchedules" s ON s."Id" = p."ProductionScheduleId"
WHERE NOT r."Approved" OR p."Id" IS NULL OR p."Cancelled"
  OR s."Id" IS NULL OR s."DeletedAt" IS NOT NULL OR s."Status" IN (2, 4)
  OR (SELECT count(*) FROM public."ProductionActivities" run WHERE run."ProductionScheduleProductId" = p."Id"
      AND run."DeletedAt" IS NULL AND run."Status" NOT IN (2, 4) AND run."CompletedAt" IS NULL) <> 1
  OR EXISTS (SELECT 1 FROM public."StockRequisitionIssues" issue WHERE issue."RequisitionId" = r."Id")
  OR EXISTS (SELECT 1 FROM public."BinCardInformation" card WHERE card."RequisitionId" = r."Id" AND card."QuantityIssued" > 0)
  OR EXISTS (SELECT 1 FROM public."RequisitionItems" i WHERE i."RequisitionId" = r."Id"
      AND (i."Status" = 3 OR i."QuantityReceived" <> 0 OR i."DeletedAt" IS NOT NULL OR i."Quantity" <= 0))
  OR EXISTS (SELECT 1 FROM public."MaterialReturnNotes" n WHERE n."ProductionScheduleProductId" = p."Id")
  OR EXISTS (SELECT 1 FROM public."ProductionExtraPackings" n WHERE n."ProductionScheduleProductId" = p."Id")
  OR EXISTS (SELECT 1 FROM public."FinalPackings" n WHERE n."ProductionScheduleProductId" = p."Id");

CREATE TEMP TABLE repair_items ON COMMIT DROP AS
SELECT r."Id" AS requisition_id, r."Code" AS code, i."Id" AS item_id, i."MaterialId" AS material_id,
  i."Quantity" AS approved_quantity, i."UoMId" AS unit_id, r."ProductionScheduleProductId" AS production_id,
  m."Kind" AS kind, m."Code" AS material_code
FROM affected_requisitions a JOIN public."Requisitions" r ON r."Id" = a.id
JOIN public."RequisitionItems" i ON i."RequisitionId" = r."Id" AND i."DeletedAt" IS NULL
JOIN public."Materials" m ON m."Id" = i."MaterialId";
INSERT INTO repair_problems
SELECT i.code || '/' || i.material_code || ': reservations do not match one approved line and unit'
FROM repair_items i
WHERE (SELECT count(*) FROM repair_items j WHERE j.requisition_id = i.requisition_id AND j.material_id = i.material_id) <> 1
  OR i.unit_id IS NULL OR i.kind NOT IN (0, 1)
  OR i.approved_quantity <> coalesce((SELECT sum(q."Quantity") FROM public."MaterialBatchReservedQuantities" q
    JOIN public."MaterialBatches" b ON b."Id" = q."MaterialBatchId"
    WHERE q."ProductionScheduleProductId" = i.production_id AND b."MaterialId" = i.material_id AND q."DeletedAt" IS NULL), 0)
  OR EXISTS (SELECT 1 FROM public."MaterialBatchReservedQuantities" q JOIN public."MaterialBatches" b ON b."Id" = q."MaterialBatchId"
    WHERE q."ProductionScheduleProductId" = i.production_id AND b."MaterialId" = i.material_id AND q."DeletedAt" IS NULL
      AND (q."Quantity" <= 0 OR q."UoMId" <> i.unit_id OR b."UoMId" <> i.unit_id OR b."DeletedAt" IS NOT NULL));
INSERT INTO repair_problems
SELECT DISTINCT i.code || ': the material consumption step has started; reconcile its production records separately'
FROM repair_items i JOIN public."ProductionActivities" a ON a."ProductionScheduleProductId" = i.production_id AND a."DeletedAt" IS NULL
JOIN public."ProductionActivitySteps" step ON step."ProductionActivityId" = a."Id" AND step."DeletedAt" IS NULL
JOIN public."Operations" op ON op."Id" = step."OperationId"
WHERE (i.kind = 0 AND op."Name" LIKE 'Product Preparation%' OR i.kind = 1 AND op."Name" LIKE 'Final Packing%')
  AND (step."StartedAt" IS NOT NULL OR step."CompletedAt" IS NOT NULL OR step."Status" <> 0);

CREATE TEMP TABLE stale_reservations ON COMMIT DROP AS
SELECT i.*, q."Id" AS reservation_id, q."MaterialBatchId" AS old_batch_id,
  q."WarehouseLocationShelfId" AS old_shelf_id, q."WarehouseId" AS production_warehouse_id, q."Quantity" AS quantity,
  to_jsonb(q) AS before_row
FROM repair_items i JOIN public."MaterialBatchReservedQuantities" q
  ON q."ProductionScheduleProductId" = i.production_id AND q."DeletedAt" IS NULL
JOIN public."MaterialBatches" b ON b."Id" = q."MaterialBatchId" AND b."MaterialId" = i.material_id
WHERE b."Status" = 7 AND EXISTS (SELECT 1 FROM public."StockMigrationWriteOffs" off
  JOIN public."StockMigrations" sm ON sm."Id" = off."StockMigrationId" CROSS JOIN repair_target t
  WHERE off."MaterialBatchId" = b."Id" AND sm."DepartmentId" = t.department_id
    AND sm."Status" = 1 AND sm."Mode" = 0 AND sm."ApprovedAt" <= t.approved_at);
INSERT INTO repair_problems
SELECT 'A reservation belongs to multiple outstanding requisition lines'
WHERE EXISTS (SELECT 1 FROM stale_reservations GROUP BY reservation_id HAVING count(*) <> 1);
INSERT INTO repair_problems
SELECT s.code || ': production warehouse does not belong to the selected department'
FROM stale_reservations s CROSS JOIN repair_target t
WHERE NOT EXISTS (SELECT 1 FROM public."Warehouses" w WHERE w."Id" = s.production_warehouse_id
  AND w."Type" = 3 AND w."DepartmentId" = t.department_id AND w."DeletedAt" IS NULL);

-- Only a unique, released lot from the explicitly selected replacement can be
-- chosen automatically. Multiple lots/shelves need a separately reviewed allocation.
CREATE TEMP TABLE replacement_candidates ON COMMIT DROP AS
SELECT DISTINCT s.reservation_id, b."Id" AS new_batch_id, sb."Id" AS shelf_stock_id,
  sb."WarehouseLocationShelfId" AS new_shelf_id, sb."Quantity" AS shelf_quantity,
  b."TotalQuantity" AS total_quantity, b."ConsumedQuantity" AS consumed_quantity,
  b."ExpiryDate" AS expiry_date, b."RetestDate" AS retest_date, b."Status" AS batch_status,
  w."Id" AS source_warehouse_id
FROM stale_reservations s CROSS JOIN repair_target t
JOIN public."StockMigrationLines" line ON line."StockMigrationId" = t.migration_id AND line."DeletedAt" IS NULL
JOIN public."MaterialBatches" b ON b."Id" = line."ImportedBatchId" AND b."MaterialId" = s.material_id
JOIN public."ShelfMaterialBatches" sb ON sb."MaterialBatchId" = b."Id"
JOIN public."WarehouseLocationShelves" sh ON sh."Id" = sb."WarehouseLocationShelfId"
JOIN public."WarehouseLocationRacks" rk ON rk."Id" = sh."WarehouseLocationRackId"
JOIN public."WarehouseLocations" loc ON loc."Id" = rk."WarehouseLocationId"
JOIN public."Warehouses" w ON w."Id" = loc."WarehouseId"
JOIN public."Materials" m ON m."Id" = b."MaterialId"
WHERE b."Status" = 3 AND b."DeletedAt" IS NULL AND m."DeletedAt" IS NULL
  AND b."UoMId" = s.unit_id AND sb."UoMId" = s.unit_id AND sb."Quantity" > 0
  AND sb."DeletedAt" IS NULL AND sh."DeletedAt" IS NULL AND rk."DeletedAt" IS NULL
  AND loc."DeletedAt" IS NULL AND w."DeletedAt" IS NULL AND w."DepartmentId" = t.department_id
  AND w."Type" = CASE WHEN s.kind = 0 THEN 0 ELSE 1 END
  AND (CASE WHEN NOT coalesce(b."ExpiryDate" > now(), false)
      THEN coalesce(b."RetestDate", b."ExpiryDate") ELSE b."ExpiryDate" END IS NULL
    OR CASE WHEN NOT coalesce(b."ExpiryDate" > now(), false)
      THEN coalesce(b."RetestDate", b."ExpiryDate") ELSE b."ExpiryDate" END = '-infinity'::timestamptz
    OR CASE WHEN NOT coalesce(b."ExpiryDate" > now(), false)
      THEN coalesce(b."RetestDate", b."ExpiryDate") ELSE b."ExpiryDate" END > now());
INSERT INTO repair_problems
SELECT s.code || '/' || s.material_code || ': expected one eligible replacement lot and shelf; found ' || count(c.reservation_id)
FROM stale_reservations s LEFT JOIN replacement_candidates c ON c.reservation_id = s.reservation_id
GROUP BY s.reservation_id, s.code, s.material_code HAVING count(c.reservation_id) <> 1;

CREATE TEMP TABLE repair_bindings ON COMMIT DROP AS
SELECT s.*, c.new_batch_id, c.shelf_stock_id, c.new_shelf_id, c.shelf_quantity,
  c.total_quantity, c.consumed_quantity, c.expiry_date, c.retest_date, c.batch_status, c.source_warehouse_id
FROM stale_reservations s JOIN replacement_candidates c USING (reservation_id);
CREATE TEMP TABLE waiting_queue ON COMMIT DROP AS
SELECT q."Id" AS id, b."MaterialId" AS material_id, q."UoMId" AS unit_id, q."Quantity" AS quantity,
  to_jsonb(q) AS reservation, to_jsonb(r) AS requisition
FROM public."MaterialBatchReservedQuantities" q
JOIN public."MaterialBatches" b ON b."Id" = q."MaterialBatchId"
JOIN public."Requisitions" r ON r."ProductionScheduleProductId" = q."ProductionScheduleProductId"
CROSS JOIN repair_target t
WHERE q."DeletedAt" IS NULL AND b."Status" = 7 AND r."DeletedAt" IS NULL
  AND r."DepartmentId" = t.department_id AND r."RequisitionType" = 0 AND r."Status" IN (0,1,2)
  AND EXISTS (SELECT 1 FROM public."RequisitionItems" i WHERE i."RequisitionId" = r."Id"
    AND i."MaterialId" = b."MaterialId" AND i."DeletedAt" IS NULL)
  AND EXISTS (SELECT 1 FROM public."StockMigrationWriteOffs" off
    JOIN public."StockMigrations" m ON m."Id" = off."StockMigrationId"
    WHERE off."MaterialBatchId" = b."Id" AND m."DepartmentId" = t.department_id
      AND m."Status" = 1 AND m."Mode" = 0 AND m."ApprovedAt" <= t.approved_at);

CREATE TEMP TABLE capacity ON COMMIT DROP AS
SELECT c.new_batch_id, c.shelf_stock_id, min(c.shelf_quantity) AS shelf_quantity,
  min(c.total_quantity) AS total_quantity, min(c.consumed_quantity) AS consumed_quantity,
  (SELECT coalesce(sum(q.quantity),0) FROM (SELECT DISTINCT id, quantity FROM waiting_queue
    WHERE material_id = c.material_id AND unit_id = c.unit_id) q) AS required_quantity,
  coalesce((SELECT sum(q."Quantity") FROM public."MaterialBatchReservedQuantities" q
    WHERE q."MaterialBatchId" = c.new_batch_id AND q."DeletedAt" IS NULL
      AND NOT EXISTS (SELECT 1 FROM public."Requisitions" r JOIN public."RequisitionItems" i ON i."RequisitionId" = r."Id"
        JOIN public."MaterialBatches" b ON b."Id" = q."MaterialBatchId"
        WHERE r."ProductionScheduleProductId" = q."ProductionScheduleProductId" AND i."MaterialId" = b."MaterialId"
          AND r."RequisitionType" = 0 AND r."Status" = 3 AND r."DeletedAt" IS NULL)), 0) AS pending_reserved,
  coalesce((SELECT sum(q."Quantity") FROM public."MaterialBatchReservedQuantities" q
    WHERE q."MaterialBatchId" = c.new_batch_id AND q."DeletedAt" IS NULL), 0) AS all_reserved
FROM repair_bindings c GROUP BY c.new_batch_id, c.shelf_stock_id, c.material_id, c.unit_id;
INSERT INTO repair_problems
SELECT 'Replacement lot ' || new_batch_id || ': insufficient uncommitted stock for all outstanding requisitions'
FROM capacity WHERE required_quantity > least(shelf_quantity - pending_reserved, total_quantity - consumed_quantity - all_reserved)
  OR consumed_quantity < 0 OR all_reserved < 0 OR pending_reserved < 0;
INSERT INTO repair_problems
SELECT DISTINCT b.material_code || ': source shelf stock does not reconcile with bin-card history'
FROM repair_bindings b
WHERE coalesce((SELECT sum(card."QuantityReceived" - card."QuantityIssued") FROM public."BinCardInformation" card
  JOIN public."MaterialBatches" lot ON lot."Id" = card."MaterialBatchId"
  WHERE card."WarehouseId" = b.source_warehouse_id AND lot."MaterialId" = b.material_id), 0)
  <> coalesce((SELECT sum(sb."Quantity") FROM public."ShelfMaterialBatches" sb
    JOIN public."MaterialBatches" lot ON lot."Id" = sb."MaterialBatchId"
    JOIN public."WarehouseLocationShelves" sh ON sh."Id" = sb."WarehouseLocationShelfId"
    JOIN public."WarehouseLocationRacks" rk ON rk."Id" = sh."WarehouseLocationRackId"
    JOIN public."WarehouseLocations" loc ON loc."Id" = rk."WarehouseLocationId"
    WHERE loc."WarehouseId" = b.source_warehouse_id AND lot."MaterialId" = b.material_id AND sb."DeletedAt" IS NULL), 0);

CREATE TEMP TABLE repair_report ON COMMIT DROP AS
SELECT jsonb_build_object('database', current_database(), 'migrationId', t.migration_id, 'migrationFile', t.file_name,
  'departmentId', t.department_id, 'actorId', t.actor_id, 'batchId', current_setting('oryx_reservation_repair.batch'),
  'requisitions', coalesce((SELECT jsonb_agg(jsonb_build_object('id', r."Id", 'code', r."Code", 'updatedAt', r."UpdatedAt",
    'approved', r."Approved", 'status', r."Status", 'productionId', r."ProductionScheduleProductId") ORDER BY r."Id")
    FROM affected_requisitions a JOIN public."Requisitions" r ON r."Id" = a.id), '[]'),
  'items', coalesce((SELECT jsonb_agg(to_jsonb(i) ORDER BY i.item_id) FROM repair_items i), '[]'),
  'bindings', coalesce((SELECT jsonb_agg(to_jsonb(b) ORDER BY b.reservation_id, b.new_batch_id, b.shelf_stock_id) FROM repair_bindings b), '[]'),
  'waitingQueue', coalesce((SELECT jsonb_agg(to_jsonb(q) ORDER BY q.id, q.requisition::text) FROM waiting_queue q), '[]'),
  'capacity', coalesce((SELECT jsonb_agg(to_jsonb(c) ORDER BY c.new_batch_id, c.shelf_stock_id) FROM capacity c), '[]'),
  'rows', coalesce((SELECT jsonb_agg(jsonb_build_object(
    'reservationId', s.reservation_id, 'requisitionCode', s.code, 'materialCode', s.material_code,
    'materialName', m."Name", 'unit', u."Symbol", 'quantity', s.quantity, 'oldBatchId', s.old_batch_id,
    'oldBatchNumber', coalesce(old."BatchNumber", ''), 'oldShelf', coalesce(sh."Name", ''),
    'candidates', coalesce((SELECT jsonb_agg(jsonb_build_object(
      'batchId', c.new_batch_id, 'batchNumber', coalesce(lot."BatchNumber", ''), 'shelfId', c.new_shelf_id,
      'shelf', coalesce(shelf."Name", ''), 'availableQuantity', least(c.shelf_quantity - cap.pending_reserved,
        c.total_quantity - c.consumed_quantity - cap.all_reserved)) ORDER BY c.new_batch_id, c.new_shelf_id)
      FROM replacement_candidates c JOIN public."MaterialBatches" lot ON lot."Id" = c.new_batch_id
      JOIN public."WarehouseLocationShelves" shelf ON shelf."Id" = c.new_shelf_id
      LEFT JOIN capacity cap ON cap.new_batch_id = c.new_batch_id AND cap.shelf_stock_id = c.shelf_stock_id
      WHERE c.reservation_id = s.reservation_id), '[]')) ORDER BY s.reservation_id)
    FROM stale_reservations s JOIN public."Materials" m ON m."Id" = s.material_id
    JOIN public."UnitOfMeasures" u ON u."Id" = s.unit_id
    JOIN public."MaterialBatches" old ON old."Id" = s.old_batch_id
    LEFT JOIN public."WarehouseLocationShelves" sh ON sh."Id" = s.old_shelf_id), '[]'),
  'problems', coalesce((SELECT jsonb_agg(problem ORDER BY problem) FROM repair_problems), '[]')) AS report
FROM repair_target t;
SELECT report::text FROM repair_report;
