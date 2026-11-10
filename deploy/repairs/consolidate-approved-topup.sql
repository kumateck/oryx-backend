-- psql variables are quoted SQL literals, never interpolated shell commands.
-- Preview: apply=false, expected_token=''. Apply requires the exact reviewed token.
-- Execute with psql -X -v ON_ERROR_STOP=1. This file never grants permissions.
BEGIN ISOLATION LEVEL SERIALIZABLE;
SET LOCAL search_path = public, pg_catalog;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '45s';
SELECT set_config('oryx_topup.source', :'source_batch_id', true),
 set_config('oryx_topup.target', :'target_batch_id', true),
 set_config('oryx_topup.database', :'expected_database', true),
 set_config('oryx_topup.actor', :'actor_email', true),
 set_config('oryx_topup.reason', :'reason', true),
 set_config('oryx_topup.apply', :'apply', true),
 set_config('oryx_topup.expected', :'expected_token', true);

DO $locks$
DECLARE t record;
BEGIN
 IF current_database() <> current_setting('oryx_topup.database') THEN RAISE EXCEPTION 'Wrong database'; END IF;
 IF current_setting('oryx_topup.source') = current_setting('oryx_topup.target') THEN RAISE EXCEPTION 'Choose two different lots'; END IF;
 IF current_setting('oryx_topup.apply')::boolean THEN
  -- Existing writers do not share a repair advisory lock. Lock all lot references,
  -- including workflow tables, so no concurrent reservation or other reference can appear.
  FOR t IN SELECT DISTINCT n.nspname, c.relname FROM pg_constraint fk
   JOIN pg_class c ON c.oid=fk.conrelid JOIN pg_namespace n ON n.oid=c.relnamespace
   WHERE fk.confrelid='public."MaterialBatches"'::regclass AND fk.contype='f'
   UNION SELECT 'public','MaterialBatches' UNION SELECT 'public','StockMigrations'
   UNION SELECT 'public','Materials' UNION SELECT 'public','WarehouseLocationShelves'
   UNION SELECT 'public','WarehouseLocationRacks' UNION SELECT 'public','WarehouseLocations'
   UNION SELECT 'public','Warehouses' UNION SELECT 'public','users' UNION SELECT 'public','roles'
   UNION SELECT 'public','userroles' UNION SELECT 'public','roleclaims' UNION SELECT 'public','PermissionTypes'
   ORDER BY 1,2
  LOOP EXECUTE format('LOCK TABLE %I.%I IN SHARE ROW EXCLUSIVE MODE',t.nspname,t.relname); END LOOP;
 END IF;
END $locks$;

CREATE TEMP TABLE topup_lots ON COMMIT DROP AS
 SELECT b.*, m."Kind" AS material_kind FROM public."MaterialBatches" b
 JOIN public."Materials" m ON m."Id"=b."MaterialId" AND m."DeletedAt" IS NULL
 WHERE b."Id" IN (current_setting('oryx_topup.source')::uuid,current_setting('oryx_topup.target')::uuid);
CREATE TEMP TABLE topup_operator ON COMMIT DROP AS
 SELECT u."Id" AS actor_id,u."DepartmentId" AS department_id FROM public.users u
 WHERE lower(u."Email")=lower(current_setting('oryx_topup.actor')) AND u."DeletedAt" IS NULL AND NOT u."IsDisabled";
DO $target$ BEGIN
 IF (SELECT count(*) FROM topup_lots)<>2 OR (SELECT count(*) FROM topup_operator)<>1 THEN
  RAISE EXCEPTION 'Require two existing lots and one active operator'; END IF;
END $target$;
CREATE TEMP TABLE topup_shelves ON COMMIT DROP AS
 SELECT s.*, loc."WarehouseId" AS warehouse_id,w."DepartmentId" AS department_id,w."Type" AS store_type
 FROM public."ShelfMaterialBatches" s JOIN public."WarehouseLocationShelves" sh ON sh."Id"=s."WarehouseLocationShelfId"
 JOIN public."WarehouseLocationRacks" r ON r."Id"=sh."WarehouseLocationRackId"
 JOIN public."WarehouseLocations" loc ON loc."Id"=r."WarehouseLocationId"
 JOIN public."Warehouses" w ON w."Id"=loc."WarehouseId"
 WHERE s."MaterialBatchId" IN (SELECT "Id" FROM topup_lots) AND s."DeletedAt" IS NULL;
CREATE TEMP TABLE topup_references(table_name text,column_name text,source_count bigint,target_count bigint,fingerprint text) ON COMMIT DROP;
DO $references$
DECLARE fk record; src uuid:=current_setting('oryx_topup.source')::uuid; dst uuid:=current_setting('oryx_topup.target')::uuid;
BEGIN
 FOR fk IN SELECT n.nspname,c.relname,a.attname,cardinality(k.conkey) AS columns FROM pg_constraint k
  JOIN pg_class c ON c.oid=k.conrelid JOIN pg_namespace n ON n.oid=c.relnamespace
  JOIN pg_attribute a ON a.attrelid=c.oid AND a.attnum=k.conkey[1]
  WHERE k.contype='f' AND k.confrelid='public."MaterialBatches"'::regclass ORDER BY n.nspname,c.relname,a.attname
 LOOP
  IF fk.columns<>1 THEN RAISE EXCEPTION 'Unsupported compound lot reference'; END IF;
  EXECUTE format('INSERT INTO topup_references SELECT %L,%L,count(*) FILTER (WHERE %I=$1),count(*) FILTER (WHERE %I=$2),
   encode(pg_catalog.sha256(convert_to(coalesce(jsonb_agg(to_jsonb(x) ORDER BY to_jsonb(x)::text)::text,''[]''),''UTF8'')),''hex'')
   FROM %I.%I x WHERE %I IN ($1,$2)',fk.nspname||'.'||fk.relname,fk.attname,fk.attname,fk.attname,fk.nspname,fk.relname,fk.attname) USING src,dst;
 END LOOP;
END $references$;
CREATE TEMP TABLE topup_problems(problem text) ON COMMIT DROP;
INSERT INTO topup_problems
 SELECT 'Lots must be available, active and have a nonempty supplier batch number' FROM topup_lots
 WHERE "DeletedAt" IS NOT NULL OR "Status"<>3 OR trim(coalesce("BatchNumber",''))='';
INSERT INTO topup_problems
 SELECT 'Material, batch number or UOM differs' FROM topup_lots s,topup_lots t
 WHERE s."Id"=current_setting('oryx_topup.source')::uuid AND t."Id"=current_setting('oryx_topup.target')::uuid
 AND (s."MaterialId"<>t."MaterialId" OR s."UoMId"<>t."UoMId" OR upper(trim(s."BatchNumber"))<>upper(trim(t."BatchNumber")));
INSERT INTO topup_problems
 SELECT 'Manufacturing, expiry or retest months differ' FROM topup_lots s,topup_lots t
 WHERE s."Id"=current_setting('oryx_topup.source')::uuid AND t."Id"=current_setting('oryx_topup.target')::uuid
 AND (date_trunc('month',s."ManufacturingDate" AT TIME ZONE 'UTC') IS DISTINCT FROM date_trunc('month',t."ManufacturingDate" AT TIME ZONE 'UTC')
 OR date_trunc('month',s."ExpiryDate" AT TIME ZONE 'UTC') IS DISTINCT FROM date_trunc('month',t."ExpiryDate" AT TIME ZONE 'UTC')
 OR date_trunc('month',s."RetestDate" AT TIME ZONE 'UTC') IS DISTINCT FROM date_trunc('month',t."RetestDate" AT TIME ZONE 'UTC'));
INSERT INTO topup_problems
 SELECT 'Source must be an unused top-up fully present on shelves' FROM topup_lots s
 WHERE s."Id"=current_setting('oryx_topup.source')::uuid AND (s."TotalQuantity"<=0 OR s."ConsumedQuantity"<>0 OR s."SampledQuantity"<>0
 OR s."QuantityAssigned"<>s."TotalQuantity" OR s."TotalQuantity"<>coalesce((SELECT sum("Quantity") FROM topup_shelves WHERE "MaterialBatchId"=s."Id"),0));
INSERT INTO topup_problems
 SELECT 'Source contains workflow or correction references: '||table_name FROM topup_references WHERE source_count>0
 AND table_name NOT IN ('public.StockMigrationLines','public.StockMigrationBalances','public.ShelfMaterialBatches',
 'public.MaterialBatchEvents','public.MassMaterialBatchMovements','public.BinCardInformation');
INSERT INTO topup_problems
 SELECT 'Target QC/correction references require manual review: '||table_name FROM topup_references WHERE target_count>0
 AND table_name IN ('public.MaterialSamplings','public.MaterialRetestRequests','public.MigrationRetestDateChanges');
INSERT INTO topup_problems
 SELECT 'Source requires approved top-up evidence only, in the operator department'
 WHERE NOT EXISTS (SELECT 1 FROM public."StockMigrationLines" l JOIN public."StockMigrations" m ON m."Id"=l."StockMigrationId"
  CROSS JOIN topup_operator o WHERE l."ImportedBatchId"=current_setting('oryx_topup.source')::uuid AND m."Mode"=1
  AND m."Status"=1 AND m."DeletedAt" IS NULL AND m."DepartmentId"=o.department_id)
 OR EXISTS (SELECT 1 FROM public."StockMigrationLines" l JOIN public."StockMigrations" m ON m."Id"=l."StockMigrationId"
  CROSS JOIN topup_operator o WHERE l."ImportedBatchId"=current_setting('oryx_topup.source')::uuid
  AND (m."Mode"<>1 OR m."Status"<>1 OR m."DeletedAt" IS NOT NULL OR m."DepartmentId" IS DISTINCT FROM o.department_id));
INSERT INTO topup_problems
 SELECT 'AR references differ or target has no approved migration evidence'
 WHERE (SELECT string_agg(ar,',' ORDER BY ar) FROM (SELECT DISTINCT upper(trim(l."ArNumber")) AS ar FROM public."StockMigrationLines" l
  JOIN public."StockMigrations" m ON m."Id"=l."StockMigrationId" WHERE l."ImportedBatchId"=current_setting('oryx_topup.source')::uuid
  AND m."Status"=1 AND nullif(trim(l."ArNumber"),'') IS NOT NULL) x) IS DISTINCT FROM
 (SELECT string_agg(ar,',' ORDER BY ar) FROM (SELECT DISTINCT upper(trim(l."ArNumber")) AS ar FROM public."StockMigrationLines" l
  JOIN public."StockMigrations" m ON m."Id"=l."StockMigrationId" WHERE l."ImportedBatchId"=current_setting('oryx_topup.target')::uuid
  AND m."Status"=1 AND nullif(trim(l."ArNumber"),'') IS NOT NULL) x)
 OR NOT EXISTS (SELECT 1 FROM public."StockMigrationLines" l JOIN public."StockMigrations" m ON m."Id"=l."StockMigrationId"
  CROSS JOIN topup_operator o WHERE l."ImportedBatchId"=current_setting('oryx_topup.target')::uuid AND m."Status"=1
  AND m."DeletedAt" IS NULL AND m."DepartmentId"=o.department_id);
INSERT INTO topup_problems
 SELECT 'Shelf stock must use the batch UOM and the operator department store' FROM topup_shelves sh
 JOIN topup_lots b ON b."Id"=sh."MaterialBatchId" CROSS JOIN topup_operator o
 WHERE sh."Quantity"<0 OR coalesce(sh."UoMId",b."UoMId")<>b."UoMId" OR sh.department_id IS DISTINCT FROM o.department_id
 OR sh.store_type<>CASE WHEN b.material_kind=0 THEN 0 ELSE 1 END;
INSERT INTO topup_problems
 SELECT 'Operator lacks current '||CASE WHEN b.material_kind=0 THEN 'CanConsolidateRawMaterialTopUps' ELSE 'CanConsolidatePackagingMaterialTopUps' END
 FROM topup_lots b CROSS JOIN topup_operator o WHERE b."Id"=current_setting('oryx_topup.source')::uuid
 AND NOT EXISTS (SELECT 1 FROM public.userroles ur JOIN public.roles r ON r."Id"=ur."RoleId" AND r."DeletedAt" IS NULL
  JOIN public.roleclaims rc ON rc."RoleId"=r."Id" AND rc."ClaimType"='permission'
  JOIN public."PermissionTypes" p ON p."RoleClaimId"=rc."Id" AND p."Key"=rc."ClaimValue" AND p."Type"='Access'
  WHERE ur."UserId"=o.actor_id AND rc."ClaimValue"=CASE WHEN b.material_kind=0 THEN 'CanConsolidateRawMaterialTopUps' ELSE 'CanConsolidatePackagingMaterialTopUps' END);

CREATE TEMP TABLE topup_report ON COMMIT DROP AS
 SELECT jsonb_build_object('database',current_database(),'sourceBatchId',current_setting('oryx_topup.source'),
 'targetBatchId',current_setting('oryx_topup.target'),'actorId',o.actor_id,'departmentId',o.department_id,
 'reason',current_setting('oryx_topup.reason'),'quantity',(SELECT "TotalQuantity" FROM topup_lots WHERE "Id"=current_setting('oryx_topup.source')::uuid),
 'beforeLots',(SELECT jsonb_agg(to_jsonb(b) ORDER BY b."Id") FROM topup_lots b),
 'beforeShelves',coalesce((SELECT jsonb_agg(to_jsonb(s) ORDER BY s."Id") FROM topup_shelves s),'[]'),
 'references',(SELECT jsonb_agg(to_jsonb(r) ORDER BY r.table_name,r.column_name) FROM topup_references r),
 'problems',coalesce((SELECT jsonb_agg(DISTINCT problem ORDER BY problem) FROM topup_problems),'[]')) AS report FROM topup_operator o;
ALTER TABLE topup_report ADD COLUMN token text;
UPDATE topup_report SET token=encode(pg_catalog.sha256(convert_to(report::text,'UTF8')),'hex');

DO $apply$
DECLARE src uuid:=current_setting('oryx_topup.source')::uuid; dst uuid:=current_setting('oryx_topup.target')::uuid;
 actor uuid; repair uuid:=gen_random_uuid(); qty numeric; token text; snapshot jsonb; row record; balance numeric; target_shelf uuid;
BEGIN
 IF NOT current_setting('oryx_topup.apply')::boolean THEN RETURN; END IF;
 SELECT r.report,r.token INTO snapshot,token FROM topup_report r;
 IF token<>current_setting('oryx_topup.expected') THEN RAISE EXCEPTION 'Preview changed; review a fresh preview'; END IF;
 IF EXISTS (SELECT 1 FROM topup_problems) THEN RAISE EXCEPTION 'Repair blocked; resolve every preview problem'; END IF;
 IF length(trim(current_setting('oryx_topup.reason'))) NOT BETWEEN 10 AND 1000 THEN RAISE EXCEPTION 'Reason must be 10-1000 characters'; END IF;
 SELECT actor_id INTO actor FROM topup_operator;
 SELECT "TotalQuantity" INTO qty FROM topup_lots WHERE "Id"=src;
 FOR row IN SELECT * FROM topup_shelves WHERE "MaterialBatchId"=src AND "Quantity">0 ORDER BY "Id" LOOP
  SELECT "Id" INTO target_shelf FROM public."ShelfMaterialBatches" WHERE "MaterialBatchId"=dst
   AND "WarehouseLocationShelfId"=row."WarehouseLocationShelfId" AND "DeletedAt" IS NULL;
  IF target_shelf IS NULL THEN
   INSERT INTO public."ShelfMaterialBatches" ("Id","MaterialBatchId","WarehouseLocationShelfId","UoMId","Quantity","Note","CreatedAt","CreatedById")
   VALUES (gen_random_uuid(),dst,row."WarehouseLocationShelfId",row."UoMId",row."Quantity",'Top-up consolidation '||repair,transaction_timestamp(),actor);
  ELSE
   UPDATE public."ShelfMaterialBatches" SET "Quantity"="Quantity"+row."Quantity","UpdatedAt"=transaction_timestamp(),"LastUpdatedById"=actor WHERE "Id"=target_shelf;
  END IF;
  UPDATE public."ShelfMaterialBatches" SET "Quantity"=0,"UpdatedAt"=transaction_timestamp(),"LastUpdatedById"=actor WHERE "Id"=row."Id";
  SELECT c."BalanceQuantity" INTO balance FROM public."BinCardInformation" c JOIN public."MaterialBatches" b ON b."Id"=c."MaterialBatchId"
   WHERE b."MaterialId"=(SELECT "MaterialId" FROM topup_lots WHERE "Id"=src) AND c."WarehouseId"=row.warehouse_id
   AND c."DeletedAt" IS NULL ORDER BY c."CreatedAt" DESC,c."Id" DESC LIMIT 1;
  IF balance IS NULL OR balance<row."Quantity" THEN RAISE EXCEPTION 'Bin-card balance requires manual review'; END IF;
  INSERT INTO public."BinCardInformation" ("Id","MaterialBatchId","WarehouseId","UoMId","QuantityReceived","QuantityIssued","BalanceQuantity","Description","CreatedAt","CreatedById")
   VALUES (gen_random_uuid(),src,row.warehouse_id,row."UoMId",0,row."Quantity",balance-row."Quantity",'Top-up consolidation '||repair||': transfer to '||dst,transaction_timestamp(),actor),
   (gen_random_uuid(),dst,row.warehouse_id,row."UoMId",row."Quantity",0,balance,'Top-up consolidation '||repair||': transfer from '||src,transaction_timestamp()+interval '1 microsecond',actor);
 END LOOP;
 UPDATE public."MaterialBatches" SET "TotalQuantity"="TotalQuantity"+qty,
  "QuantityAssigned"="QuantityAssigned"+(SELECT "QuantityAssigned" FROM topup_lots WHERE "Id"=src),
  "UpdatedAt"=transaction_timestamp(),"LastUpdatedById"=actor WHERE "Id"=dst;
 UPDATE public."MaterialBatches" SET "TotalQuantity"=0,"QuantityAssigned"=0,"Status"=7,
  "UpdatedAt"=transaction_timestamp(),"LastUpdatedById"=actor WHERE "Id"=src;
 INSERT INTO public."MaterialBatchEvents" ("Id","BatchId","Quantity","UserId","Type","CreatedAt","CreatedById")
  VALUES (gen_random_uuid(),src,qty,actor,2,transaction_timestamp(),actor),(gen_random_uuid(),dst,qty,actor,2,transaction_timestamp(),actor);
 IF (SELECT sum("TotalQuantity") FROM public."MaterialBatches" WHERE "Id" IN(src,dst))<>(SELECT sum("TotalQuantity") FROM topup_lots)
 OR (SELECT sum("Quantity") FROM public."ShelfMaterialBatches" WHERE "MaterialBatchId" IN(src,dst) AND "DeletedAt" IS NULL)
 <>(SELECT sum("Quantity") FROM topup_shelves) THEN RAISE EXCEPTION 'Stock conservation failed'; END IF;
 CREATE TABLE IF NOT EXISTS public."_MigrationTopUpConsolidations" (
  "Id" uuid PRIMARY KEY,"RepairedAt" timestamptz NOT NULL,"DatabaseUser" text NOT NULL,"ActorId" uuid NOT NULL,
  "SourceBatchId" uuid NOT NULL UNIQUE,"TargetBatchId" uuid NOT NULL,"Reason" text NOT NULL,"PreviewToken" text NOT NULL UNIQUE,
  "Before" jsonb NOT NULL,"After" jsonb NOT NULL);
 INSERT INTO public."_MigrationTopUpConsolidations" VALUES (repair,transaction_timestamp(),current_user,actor,src,dst,
  trim(current_setting('oryx_topup.reason')),token,snapshot,jsonb_build_object(
   'lots',(SELECT jsonb_agg(to_jsonb(b) ORDER BY b."Id") FROM public."MaterialBatches" b WHERE b."Id" IN(src,dst)),
   'shelves',(SELECT jsonb_agg(to_jsonb(s) ORDER BY s."Id") FROM public."ShelfMaterialBatches" s WHERE s."MaterialBatchId" IN(src,dst))));
END $apply$;
-- Preview creates temporary tables only. Audit protection is installed only on apply.
DO $protect$ BEGIN
 IF current_setting('oryx_topup.apply')::boolean THEN
  EXECUTE 'CREATE OR REPLACE FUNCTION public.oryx_protect_topup_consolidations() RETURNS trigger LANGUAGE plpgsql AS $f$ BEGIN RAISE EXCEPTION ''Top-up consolidation evidence is immutable''; END $f$';
  EXECUTE 'DROP TRIGGER IF EXISTS protect_topup_consolidations ON public."_MigrationTopUpConsolidations"';
  EXECUTE 'CREATE TRIGGER protect_topup_consolidations BEFORE UPDATE OR DELETE OR TRUNCATE ON public."_MigrationTopUpConsolidations" FOR EACH STATEMENT EXECUTE FUNCTION public.oryx_protect_topup_consolidations()';
 END IF;
END $protect$;
SELECT report || jsonb_build_object('previewToken',token,'mode',CASE WHEN current_setting('oryx_topup.apply')::boolean THEN 'applied' ELSE 'preview' END) AS report FROM topup_report;
COMMIT;
