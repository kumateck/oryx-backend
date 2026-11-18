-- Run with psql -X -v ON_ERROR_STOP=1. Defaults to preview; see the companion .md.
-- This is a one-lot correction, not a code swap or a stock re-import.
\set ON_ERROR_STOP on
\if :{?apply}
\else
  \set apply false
\endif
\if :{?expected_token}
\else
  \set expected_token ''
\endif
\if :{?expected_system_identifier}
\else
  \set expected_system_identifier ''
\endif
BEGIN;
\if :apply
  SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
\else
  SET TRANSACTION ISOLATION LEVEL REPEATABLE READ;
\endif
SET LOCAL search_path = pg_catalog, public;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '60s';
SELECT set_config('rmv_fix.apply', :'apply', true),
       set_config('rmv_fix.database', :'expected_database', true),
       set_config('rmv_fix.cluster', :'expected_system_identifier', true),
       set_config('rmv_fix.actor', :'actor_email', true),
       set_config('rmv_fix.reason', :'reason', true),
       set_config('rmv_fix.token', :'expected_token', true);

DO $identity$
BEGIN
 IF current_database() <> current_setting('rmv_fix.database') THEN
  RAISE EXCEPTION 'Wrong database: connected to %', current_database();
 END IF;
 IF current_setting('rmv_fix.apply')::boolean AND
    current_setting('rmv_fix.cluster') IS DISTINCT FROM
    (SELECT system_identifier::text FROM pg_control_system()) THEN
  RAISE EXCEPTION 'Wrong or missing expected_system_identifier; use the reviewed client preview';
 END IF;
 IF length(btrim(current_setting('rmv_fix.reason'))) NOT BETWEEN 10 AND 1000 THEN
  RAISE EXCEPTION 'Supply a reason of 10-1000 characters';
 END IF;
END $identity$;

DO $parent_locks$ BEGIN
 IF current_setting('rmv_fix.apply')::boolean THEN
  PERFORM pg_advisory_xact_lock(20261118,4002);
  LOCK TABLE public."MaterialBatches",public."ShelfMaterialBatches" IN SHARE ROW EXCLUSIVE MODE;
 END IF;
END $parent_locks$;

-- Include declared FKs and legacy UUID references without an FK. Shelf references
-- also matter: a reservation can refer to a shelf allocation rather than the lot.
CREATE TEMP TABLE rmv_reference_columns ON COMMIT DROP AS
 SELECT DISTINCT n.nspname AS schema_name, c.relname AS table_name,
        a.attname AS column_name,
        CASE WHEN fk.confrelid='public."MaterialBatches"'::regclass THEN 'batch' ELSE 'shelf' END AS kind,
        cardinality(fk.conkey) AS key_columns
 FROM pg_constraint fk JOIN pg_class c ON c.oid=fk.conrelid
 JOIN pg_namespace n ON n.oid=c.relnamespace
 JOIN pg_attribute a ON a.attrelid=c.oid AND a.attnum=fk.conkey[1]
 WHERE fk.contype='f' AND fk.confrelid IN
       ('public."MaterialBatches"'::regclass,'public."ShelfMaterialBatches"'::regclass)
 UNION
 SELECT c.table_schema,c.table_name,c.column_name,
        CASE WHEN c.column_name='ShelfMaterialBatchId' THEN 'shelf' ELSE 'batch' END,1
 FROM information_schema.columns c
 WHERE c.table_schema='public' AND c.udt_name='uuid' AND c.column_name IN
       ('MaterialBatchId','ImportedBatchId','QuarantinedMaterialBatchId','OutputBatchId','BatchId','ShelfMaterialBatchId');

DO $locks$ DECLARE r record;
BEGIN
 IF NOT current_setting('rmv_fix.apply')::boolean THEN RETURN; END IF;
 -- Ordinary API writers do not take the advisory lock. These table locks prevent
 -- new usage/receipts/grants during the checked correction. A busy server fails fast.
 FOR r IN
  SELECT schema_name,table_name FROM rmv_reference_columns
  UNION SELECT 'public',unnest(ARRAY['Materials','StockMigrations','StockMigrationLines',
    'StockMigrationBalances','MaterialDepartments','UnitOfMeasures','BinCardInformation',
    'WarehouseLocationShelves','WarehouseLocationRacks','WarehouseLocations','Warehouses',
    'users','roles','userroles','roleclaims','PermissionTypes']) ORDER BY 1,2
 LOOP
  EXECUTE format('LOCK TABLE %I.%I IN SHARE ROW EXCLUSIVE MODE',r.schema_name,r.table_name);
 END LOOP;
END $locks$;

CREATE TEMP TABLE rmv_context ON COMMIT DROP AS
 SELECT 'rmv004-rmv002-ye390921-v1'::text AS repair_key,
        'ce058b66-8dab-4d4d-86e6-f7655d481d6e'::uuid AS batch_id,
        '23eaa748-f373-412d-83c0-62ab264ddf84'::uuid AS line_id,
        'c13fe90f-3b43-4d49-826d-8fdccf081e4b'::uuid AS migration_id,
        '01994fb8-2b57-7b23-a5b9-5b5c68aa9abc'::uuid AS department_id,
        '01996128-3354-7ddc-9ab8-95055660323d'::uuid AS source_id,
        '01996128-3354-7ce1-a813-e1f8a377ef0e'::uuid AS target_id;
CREATE TEMP TABLE rmv_operator ON COMMIT DROP AS
 SELECT u."Id" AS actor_id FROM public.users u
 WHERE lower(u."Email")=lower(current_setting('rmv_fix.actor'));

CREATE FUNCTION pg_temp.rmv_assert_actor() RETURNS void LANGUAGE plpgsql AS $actor$
DECLARE actor uuid; dept uuid;
BEGIN
 IF (SELECT count(*) FROM rmv_operator)<>1 THEN RAISE EXCEPTION 'Require one named operator account'; END IF;
 SELECT actor_id INTO actor FROM rmv_operator;
 SELECT department_id INTO dept FROM rmv_context;
 IF NOT EXISTS (SELECT 1 FROM public.users u WHERE u."Id"=actor AND u."DeletedAt" IS NULL
                AND NOT u."IsDisabled" AND u."DepartmentId"=dept) THEN
  RAISE EXCEPTION 'Operator must be active in the migration department';
 END IF;
 IF EXISTS (
  SELECT 1 FROM unnest(ARRAY['CanViewOpeningBalances','CanCorrectOpeningBalances',
                            'CanReassignRawMaterialStock']) required(key)
  WHERE NOT EXISTS (
   SELECT 1 FROM public.userroles ur JOIN public.roles r ON r."Id"=ur."RoleId" AND r."DeletedAt" IS NULL
   JOIN public.roleclaims rc ON rc."RoleId"=r."Id" AND rc."ClaimType"='permission'
   JOIN public."PermissionTypes" p ON p."RoleClaimId"=rc."Id" AND p."Key"=rc."ClaimValue" AND p."Type"='Access'
   WHERE ur."UserId"=actor AND rc."ClaimValue"=required.key)) THEN
  RAISE EXCEPTION 'Operator requires current opening-balance view/correct and raw-stock reassign grants';
 END IF;
END $actor$;
SELECT pg_temp.rmv_assert_actor();

CREATE TEMP TABLE rmv_prior(report jsonb) ON COMMIT DROP;
DO $prior$ BEGIN
 IF to_regclass('public."_MigrationMaterialCorrections"') IS NOT NULL THEN
  EXECUTE 'INSERT INTO rmv_prior SELECT to_jsonb(a) FROM public."_MigrationMaterialCorrections" a
           WHERE a."RepairKey"=$1' USING (SELECT repair_key FROM rmv_context);
 END IF;
END $prior$;
CREATE TEMP TABLE rmv_batch ON COMMIT DROP AS
 SELECT b.* FROM public."MaterialBatches" b JOIN rmv_context x ON x.batch_id=b."Id";
CREATE TEMP TABLE rmv_line ON COMMIT DROP AS
 SELECT l.* FROM public."StockMigrationLines" l JOIN rmv_context x ON x.line_id=l."Id";
CREATE TEMP TABLE rmv_shelves ON COMMIT DROP AS
 SELECT s.*, w."Id" AS warehouse_id,w."DepartmentId" AS department_id,w."Type" AS store_type,
        (sh."DeletedAt" IS NULL AND r."DeletedAt" IS NULL AND loc."DeletedAt" IS NULL
         AND w."DeletedAt" IS NULL) AS location_active
 FROM public."ShelfMaterialBatches" s JOIN rmv_context x ON x.batch_id=s."MaterialBatchId"
 JOIN public."WarehouseLocationShelves" sh ON sh."Id"=s."WarehouseLocationShelfId"
 JOIN public."WarehouseLocationRacks" r ON r."Id"=sh."WarehouseLocationRackId"
 JOIN public."WarehouseLocations" loc ON loc."Id"=r."WarehouseLocationId"
 JOIN public."Warehouses" w ON w."Id"=loc."WarehouseId";
CREATE TEMP TABLE rmv_references(schema_name text,table_name text,column_name text,kind text,
                               row_count bigint,rows jsonb) ON COMMIT DROP;
DO $refs$ DECLARE r record; ids uuid[];
BEGIN
 FOR r IN SELECT * FROM rmv_reference_columns ORDER BY 1,2,3,4 LOOP
  IF r.kind='batch' THEN ids:=ARRAY[(SELECT batch_id FROM rmv_context)];
  ELSE SELECT array_agg("Id") INTO ids FROM rmv_shelves; END IF;
  EXECUTE format('INSERT INTO rmv_references SELECT %L,%L,%L,%L,count(*),
          coalesce(jsonb_agg(to_jsonb(t) ORDER BY to_jsonb(t)::text),''[]''::jsonb)
          FROM %I.%I t WHERE %I=ANY($1)',r.schema_name,r.table_name,r.column_name,r.kind,
          r.schema_name,r.table_name,r.column_name) USING ids;
 END LOOP;
END $refs$;

CREATE TEMP TABLE rmv_problems(problem text) ON COMMIT DROP;
INSERT INTO rmv_problems SELECT 'Unsupported composite batch/shelf foreign key'
 WHERE EXISTS (SELECT 1 FROM rmv_reference_columns WHERE key_columns<>1);
INSERT INTO rmv_problems SELECT 'Known batch or migration line is missing/duplicated'
 WHERE (SELECT count(*) FROM rmv_batch)<>1 OR (SELECT count(*) FROM rmv_line)<>1;
INSERT INTO rmv_problems SELECT 'Material identities must be the two active raw-material masters'
 WHERE (SELECT count(*) FROM public."Materials" m CROSS JOIN rmv_context x
        WHERE m."DeletedAt" IS NULL AND m."Kind"=0 AND
        ((m."Id"=x.source_id AND m."Code"='RMV004') OR (m."Id"=x.target_id AND m."Code"='RMV002')))<>2
 OR EXISTS (SELECT 1 FROM public."Materials" m CROSS JOIN rmv_context x
            WHERE m."DeletedAt" IS NULL AND m."Code" IN ('RMV004','RMV002') AND m."Id" NOT IN(x.source_id,x.target_id));
INSERT INTO rmv_problems SELECT 'Batch differs from unused, available YE-390921 / 41.65 kg'
 FROM rmv_batch b CROSS JOIN rmv_context x
 WHERE b."DeletedAt" IS NOT NULL OR b."MaterialId"<>x.source_id OR b."BatchNumber" IS DISTINCT FROM 'YE-390921'
 OR b."Status"<>3 OR b."TotalQuantity"<>41650000 OR b."QuantityAssigned"<>41650000
 OR b."ConsumedQuantity"<>0 OR b."SampledQuantity"<>0 OR b."QuantityDistributed"<>0
 OR b."GrnId" IS NOT NULL OR b."StockTransferId" IS NOT NULL OR b."ChecklistId" IS NOT NULL
 OR b."WarehouseArrivalLocationId" IS NOT NULL OR b."IssuedAt" IS NOT NULL;
INSERT INTO rmv_problems SELECT 'Approved opening-balance row/AR/quantity/material association differs'
 FROM rmv_line l CROSS JOIN rmv_context x
 WHERE l."StockMigrationId"<>x.migration_id OR l."ImportedBatchId" IS DISTINCT FROM x.batch_id
 OR l."RowNumber"<>263 OR l."SheetMaterialCode" IS DISTINCT FROM 'RMV004'
 OR l."MaterialCode" IS DISTINCT FROM 'RMV004' OR l."ResolvedMaterialId" IS DISTINCT FROM x.source_id
 OR (l."MaterialId" IS NOT NULL AND l."MaterialId"<>x.source_id)
 OR l."BatchNumber" IS DISTINCT FROM 'YE-390921' OR l."ArNumber" IS DISTINCT FROM 'NQC/RM/22/016'
 OR l."Quantity" IS DISTINCT FROM 41650000::numeric OR l."Excluded" OR l."DeletedAt" IS NOT NULL
 OR l."UoMId" IS DISTINCT FROM (SELECT "UoMId" FROM rmv_batch)
 OR NOT EXISTS (SELECT 1 FROM public."StockMigrations" m WHERE m."Id"=x.migration_id
                AND m."Status"=1 AND m."Mode"=0 AND m."ApprovedAt" IS NOT NULL
                AND m."DeletedAt" IS NULL AND m."DepartmentId"=x.department_id);
INSERT INTO rmv_problems SELECT 'Target must have one active mg department link matching the lot unit'
 WHERE (SELECT count(*) FROM public."MaterialDepartments" md CROSS JOIN rmv_context x
        JOIN rmv_batch b ON true JOIN public."UnitOfMeasures" u ON u."Id"=b."UoMId"
        WHERE md."MaterialId"=x.target_id AND md."DepartmentId"=x.department_id AND md."DeletedAt" IS NULL
        AND md."UoMId"=b."UoMId" AND u."DeletedAt" IS NULL AND lower(u."Symbol")='mg')<>1
 OR (SELECT count(*) FROM public."MaterialDepartments" md CROSS JOIN rmv_context x
     WHERE md."MaterialId"=x.target_id AND md."DepartmentId"=x.department_id AND md."DeletedAt" IS NULL)<>1;
INSERT INTO rmv_problems SELECT 'Lot must be wholly present on its original active department shelf'
 WHERE (SELECT count(*) FROM rmv_shelves)<>1
 OR EXISTS (SELECT 1 FROM rmv_shelves s CROSS JOIN rmv_context x JOIN rmv_batch b ON true
            WHERE s."DeletedAt" IS NOT NULL OR s."Quantity"<>b."TotalQuantity" OR NOT s.location_active
            OR s.department_id IS DISTINCT FROM x.department_id OR s.store_type<>0
            OR coalesce(s."UoMId",b."UoMId")<>b."UoMId");
INSERT INTO rmv_problems
 SELECT 'Lot has QC, reservation, production, repair or other usage: '||schema_name||'.'||table_name||'.'||column_name
 FROM rmv_references WHERE row_count>0 AND
 (kind='shelf' OR schema_name<>'public' OR table_name NOT IN
  ('StockMigrationLines','ShelfMaterialBatches','BinCardInformation','MassMaterialBatchMovements'));
INSERT INTO rmv_problems SELECT 'Lot must have exactly one approved import row, receipt and initial warehouse movement'
 WHERE EXISTS (SELECT 1 FROM rmv_references WHERE table_name IN
               ('StockMigrationLines','ShelfMaterialBatches','BinCardInformation','MassMaterialBatchMovements') AND row_count<>1)
 OR (SELECT count(*) FROM public."MassMaterialBatchMovements" m CROSS JOIN rmv_context x
     WHERE m."BatchId"=x.batch_id AND m."DeletedAt" IS NULL AND m."FromWarehouseId" IS NULL
     AND m."ToWarehouseId"=(SELECT warehouse_id FROM rmv_shelves ORDER BY "Id" LIMIT 1) AND m."Quantity"=41650000 AND m."MovementType"=0)<>1
 OR (SELECT count(*) FROM public."BinCardInformation" c CROSS JOIN rmv_context x
     WHERE c."MaterialBatchId"=x.batch_id AND c."DeletedAt" IS NULL AND c."QuantityReceived"=41650000
     AND c."QuantityIssued"=0 AND c."WarehouseId"=(SELECT warehouse_id FROM rmv_shelves ORDER BY "Id" LIMIT 1)
     AND c."ArNumber"='NQC/RM/22/016')<>1;
INSERT INTO rmv_problems SELECT 'Opening migration unexpectedly contains material balance evidence; review manually'
 WHERE EXISTS (SELECT 1 FROM public."StockMigrationBalances" s CROSS JOIN rmv_context x
               WHERE s."StockMigrationId"=x.migration_id AND s."MaterialId" IN(x.source_id,x.target_id));
INSERT INTO rmv_problems SELECT 'Correction annotation would exceed the existing field limit'
 FROM rmv_line WHERE length(coalesce("Corrections",''))+length(current_setting('rmv_fix.reason'))+110>4000;

CREATE TEMP TABLE rmv_cards ON COMMIT DROP AS
 SELECT c.*,b."MaterialId" AS old_material_id,
        CASE WHEN b."Id"=x.batch_id THEN x.target_id ELSE b."MaterialId" END AS new_material_id
 FROM public."BinCardInformation" c JOIN public."MaterialBatches" b ON b."Id"=c."MaterialBatchId"
 CROSS JOIN rmv_context x
 WHERE b."MaterialId" IN(x.source_id,x.target_id) AND c."DeletedAt" IS NULL
 AND c."WarehouseId" IN(SELECT warehouse_id FROM rmv_shelves);
CREATE TEMP TABLE rmv_card_plan ON COMMIT DROP AS
 SELECT "Id",sum("QuantityReceived"-"QuantityIssued") OVER
        (PARTITION BY new_material_id,"WarehouseId","UoMId" ORDER BY "CreatedAt","Id" ROWS UNBOUNDED PRECEDING) AS new_balance
 FROM rmv_cards;
INSERT INTO rmv_problems SELECT 'Existing bin-card chronology or unit is inconsistent; review the ledger first'
 WHERE EXISTS (SELECT 1 FROM (
  SELECT c.*,sum("QuantityReceived"-"QuantityIssued") OVER
   (PARTITION BY old_material_id,"WarehouseId","UoMId" ORDER BY "CreatedAt","Id" ROWS UNBOUNDED PRECEDING) AS old_expected
  FROM rmv_cards c) q WHERE q."BalanceQuantity" IS DISTINCT FROM q.old_expected
   OR q."UoMId" IS DISTINCT FROM (SELECT "UoMId" FROM rmv_batch)
   OR q."QuantityReceived"<0 OR q."QuantityIssued"<0)
 OR EXISTS (SELECT 1 FROM rmv_card_plan WHERE new_balance<0);

-- An already-recorded repair is a no-op, including after legitimate later stock use.
DO $already$ BEGIN
 IF EXISTS(SELECT 1 FROM rmv_prior) THEN
  IF (SELECT count(*) FROM rmv_prior)<>1
   OR NOT EXISTS(SELECT 1 FROM rmv_batch b CROSS JOIN rmv_context x WHERE b."MaterialId"=x.target_id)
   OR NOT EXISTS(SELECT 1 FROM rmv_line l CROSS JOIN rmv_context x WHERE l."MaterialCode"='RMV002'
                 AND l."MaterialId"=x.target_id AND l."ResolvedMaterialId"=x.target_id AND l."ImportedBatchId"=x.batch_id)
  THEN RAISE EXCEPTION 'Recorded correction and current lot/line disagree; investigate manually'; END IF;
  DELETE FROM rmv_problems;
 END IF;
END $already$;

CREATE TEMP TABLE rmv_report ON COMMIT DROP AS
 SELECT jsonb_build_object(
  'repairKey',x.repair_key,'database',current_database(),
  'systemIdentifier',(SELECT system_identifier::text FROM pg_control_system()),
  'serverAddress',inet_server_addr(),'serverPort',inet_server_port(),
  'actorId',(SELECT actor_id FROM rmv_operator),'reason',btrim(current_setting('rmv_fix.reason')),
  'fromCode','RMV004','toCode','RMV002','quantityKg',41.65,'alreadyApplied',EXISTS(SELECT 1 FROM rmv_prior),
  'batch',(SELECT to_jsonb(b) FROM rmv_batch b),'line',(SELECT to_jsonb(l) FROM rmv_line l),
  'migration',(SELECT to_jsonb(m) FROM public."StockMigrations" m WHERE m."Id"=x.migration_id),
  'masters',(SELECT jsonb_agg(to_jsonb(m) ORDER BY m."Id") FROM public."Materials" m WHERE m."Id" IN(x.source_id,x.target_id)),
  'targetLinks',(SELECT jsonb_agg(to_jsonb(md) ORDER BY md."Id") FROM public."MaterialDepartments" md
                 WHERE md."MaterialId"=x.target_id AND md."DepartmentId"=x.department_id),
  'shelves',(SELECT jsonb_agg(to_jsonb(s) ORDER BY s."Id") FROM rmv_shelves s),
  'references',(SELECT jsonb_agg(to_jsonb(r) ORDER BY schema_name,table_name,column_name,kind) FROM rmv_references r),
  'cardsBefore',(SELECT jsonb_agg(to_jsonb(c) ORDER BY c."CreatedAt",c."Id") FROM rmv_cards c),
  'ledgerBatches',(SELECT jsonb_agg(to_jsonb(b) ORDER BY b."Id") FROM public."MaterialBatches" b
                   WHERE b."Id" IN(SELECT "MaterialBatchId" FROM rmv_cards)),
  'cardPlan',(SELECT jsonb_agg(to_jsonb(p) ORDER BY p."Id") FROM rmv_card_plan p),
  'problems',coalesce((SELECT jsonb_agg(DISTINCT problem ORDER BY problem) FROM rmv_problems),'[]'::jsonb)) AS report
 FROM rmv_context x;
ALTER TABLE rmv_report ADD COLUMN token text;
UPDATE rmv_report SET token=encode(sha256(convert_to(report::text,'UTF8')),'hex');

DO $apply$
DECLARE x record; actor uuid; card_change record; snapshot jsonb; reviewed text;
BEGIN
 IF NOT current_setting('rmv_fix.apply')::boolean OR EXISTS(SELECT 1 FROM rmv_prior) THEN RETURN; END IF;
 SELECT report,token INTO snapshot,reviewed FROM rmv_report;
 IF reviewed IS DISTINCT FROM current_setting('rmv_fix.token') THEN
  RAISE EXCEPTION 'Preview token differs; review a fresh preview on this client server';
 END IF;
 IF EXISTS(SELECT 1 FROM rmv_problems) THEN RAISE EXCEPTION 'Correction blocked: %',snapshot->'problems'; END IF;
 SELECT * INTO x FROM rmv_context;
 SELECT actor_id INTO actor FROM rmv_operator;
 PERFORM pg_temp.rmv_assert_actor();
 UPDATE public."MaterialBatches" SET "MaterialId"=x.target_id,"UpdatedAt"=transaction_timestamp(),
  "LastUpdatedById"=actor WHERE "Id"=x.batch_id;
 PERFORM pg_temp.rmv_assert_actor();
 UPDATE public."StockMigrationLines" SET "MaterialCode"='RMV002',"MaterialId"=x.target_id,
  "ResolvedMaterialId"=x.target_id,"UpdatedAt"=transaction_timestamp(),"LastUpdatedById"=actor,
  "Corrections"=concat_ws(E'\n',nullif("Corrections",''),'Material identity correction ['||x.repair_key||
                 ']: RMV004 -> RMV002. '||btrim(current_setting('rmv_fix.reason')))
 WHERE "Id"=x.line_id;
 FOR card_change IN SELECT p.* FROM rmv_card_plan p JOIN rmv_cards b USING("Id")
          WHERE b."BalanceQuantity" IS DISTINCT FROM p.new_balance ORDER BY p."Id" LOOP
  PERFORM pg_temp.rmv_assert_actor();
  UPDATE public."BinCardInformation" SET "BalanceQuantity"=card_change.new_balance,"UpdatedAt"=transaction_timestamp(),
   "LastUpdatedById"=actor WHERE "Id"=card_change."Id";
 END LOOP;
 -- Assert the exact allowed field deltas, not just a total that could hide damage.
 IF (SELECT to_jsonb(b)-ARRAY['MaterialId','UpdatedAt','LastUpdatedById'] FROM public."MaterialBatches" b WHERE b."Id"=x.batch_id)
   IS DISTINCT FROM ((snapshot->'batch')-ARRAY['MaterialId','UpdatedAt','LastUpdatedById'])
 OR (SELECT to_jsonb(l)-ARRAY['MaterialCode','MaterialId','ResolvedMaterialId','Corrections','UpdatedAt','LastUpdatedById']
     FROM public."StockMigrationLines" l WHERE l."Id"=x.line_id)
   IS DISTINCT FROM ((snapshot->'line')-ARRAY['MaterialCode','MaterialId','ResolvedMaterialId','Corrections','UpdatedAt','LastUpdatedById'])
 THEN RAISE EXCEPTION 'Unexpected change to lot quantities/dates or original migration evidence'; END IF;
 IF EXISTS(SELECT 1 FROM public."ShelfMaterialBatches" s JOIN rmv_shelves before USING("Id")
           WHERE to_jsonb(s) IS DISTINCT FROM
            (to_jsonb(before)-ARRAY['warehouse_id','department_id','store_type','location_active']))
 OR EXISTS(SELECT 1 FROM public."BinCardInformation" a JOIN rmv_cards before USING("Id")
           JOIN rmv_card_plan p USING("Id")
           WHERE a."BalanceQuantity" IS DISTINCT FROM p.new_balance
           OR (to_jsonb(a)-ARRAY['BalanceQuantity','UpdatedAt','LastUpdatedById']) IS DISTINCT FROM
              (to_jsonb(before)-ARRAY['BalanceQuantity','UpdatedAt','LastUpdatedById','old_material_id','new_material_id']))
 THEN RAISE EXCEPTION 'Unexpected shelf or bin-card quantity/reference change'; END IF;
 PERFORM pg_temp.rmv_assert_actor();
 CREATE TABLE IF NOT EXISTS public."_MigrationMaterialCorrections"(
  "RepairKey" text PRIMARY KEY,"RepairedAt" timestamptz NOT NULL,"DatabaseUser" text NOT NULL,
  "ActorId" uuid NOT NULL,"BatchId" uuid NOT NULL UNIQUE,"Reason" text NOT NULL,
  "PreviewToken" text NOT NULL,"Before" jsonb NOT NULL,"After" jsonb NOT NULL);
 PERFORM pg_temp.rmv_assert_actor();
 INSERT INTO public."_MigrationMaterialCorrections"
 VALUES(x.repair_key,transaction_timestamp(),current_user,actor,x.batch_id,
  btrim(current_setting('rmv_fix.reason')),reviewed,snapshot,jsonb_build_object(
   'batch',(SELECT to_jsonb(b) FROM public."MaterialBatches" b WHERE b."Id"=x.batch_id),
   'line',(SELECT to_jsonb(l) FROM public."StockMigrationLines" l WHERE l."Id"=x.line_id),
   'cards',(SELECT jsonb_agg(to_jsonb(c) ORDER BY c."CreatedAt",c."Id")
            FROM public."BinCardInformation" c WHERE c."Id" IN(SELECT "Id" FROM rmv_cards))));
 PERFORM pg_temp.rmv_assert_actor();
 EXECUTE 'CREATE OR REPLACE FUNCTION public.oryx_protect_migration_material_corrections()
          RETURNS trigger LANGUAGE plpgsql AS $f$ BEGIN
          RAISE EXCEPTION ''Migration material correction evidence is immutable''; END $f$';
 PERFORM pg_temp.rmv_assert_actor();
 EXECUTE 'CREATE TRIGGER protect_migration_material_corrections
          BEFORE UPDATE OR DELETE OR TRUNCATE ON public."_MigrationMaterialCorrections"
          FOR EACH STATEMENT EXECUTE FUNCTION public.oryx_protect_migration_material_corrections()';
END $apply$;
SELECT report || jsonb_build_object('previewToken',token,'mode',
 CASE WHEN EXISTS(SELECT 1 FROM rmv_prior) THEN 'already-applied'
      WHEN current_setting('rmv_fix.apply')::boolean THEN 'applied' ELSE 'preview' END) AS report
FROM rmv_report;
COMMIT;
