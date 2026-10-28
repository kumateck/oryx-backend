namespace INFRASTRUCTURE.Reporting;

public static class ReportHistorySchema
{
    public static string InstallSql => Definitions + "\n" + Install(ReportHistoryCatalog.Tables);
    internal static string Install(IEnumerable<string> tables)
    {
        var names = string.Join(",", tables.Select(name => "'" + name.Replace("'", "''") + "'"));
        return $$"""
        DO $history$
        DECLARE relation text; keys text[]; baseline timestamptz; existing text[];
        BEGIN
          WITH RECURSIVE dependencies(oid) AS (
            SELECT c.oid FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
              WHERE n.nspname='public' AND c.relkind='r' AND c.relname=ANY(ARRAY[{{names}}]::text[])
            UNION
            SELECT fk.confrelid FROM pg_constraint fk JOIN dependencies d ON d.oid=fk.conrelid
              JOIN pg_class c ON c.oid=fk.confrelid JOIN pg_namespace n ON n.oid=c.relnamespace
              WHERE fk.contype='f' AND n.nspname='public' AND c.relkind='r'
                AND c.relname NOT IN ('roles','userroles','roleclaims','userclaims','usertokens','userlogins','PermissionTypes','PermissionModules')
          ) SELECT array_agg(c.relname::text ORDER BY c.relname) INTO existing FROM dependencies d JOIN pg_class c ON c.oid=d.oid;
          FOREACH relation IN ARRAY COALESCE(existing,ARRAY[]::text[]) LOOP
            EXECUTE format('LOCK TABLE public.%I IN SHARE ROW EXCLUSIVE MODE', relation);
          END LOOP;
          baseline := clock_timestamp();
          FOREACH relation IN ARRAY COALESCE(existing,ARRAY[]::text[]) LOOP
            SELECT array_agg(a.attname::text ORDER BY k.ordinality) INTO keys
              FROM pg_index i JOIN LATERAL unnest(i.indkey) WITH ORDINALITY k(attnum,ordinality) ON true
              JOIN pg_attribute a ON a.attrelid=i.indrelid AND a.attnum=k.attnum
              WHERE i.indrelid=format('public.%I',relation)::regclass AND i.indisprimary;
            IF keys IS NULL THEN RAISE EXCEPTION 'Historical report source % has no primary key',relation; END IF;
            IF NOT EXISTS(SELECT 1 FROM public."ReportHistoryCoverage" WHERE "TableName"=relation) THEN
              EXECUTE format('INSERT INTO public."ReportHistoryVersions" ("TableName","EntityKey","RecordedAt","TransactionId","IsDeleted","Payload") SELECT %L,public.oryx_report_key(to_jsonb(t),%L::text[]),%L::timestamptz,txid_current(),false,public.oryx_report_payload(%L,to_jsonb(t)) FROM public.%I t',relation,keys,baseline,relation,relation);
              INSERT INTO public."ReportHistoryCoverage" ("TableName","AvailableFrom") VALUES(relation,baseline);
            END IF;
            EXECUTE format('DROP TRIGGER IF EXISTS oryx_report_no_truncate ON public.%I',relation);
            EXECUTE format('CREATE TRIGGER oryx_report_no_truncate BEFORE TRUNCATE ON public.%I FOR EACH STATEMENT EXECUTE FUNCTION public.oryx_report_immutable()',relation);
            EXECUTE format('DROP TRIGGER IF EXISTS oryx_report_capture ON public.%I',relation);
            EXECUTE format('CREATE CONSTRAINT TRIGGER oryx_report_capture AFTER INSERT OR UPDATE OR DELETE ON public.%I DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION public.oryx_report_capture(%L)',relation,keys);
          END LOOP;
        END $history$;
        """;
    }

    public const string Definitions = """
        CREATE TABLE IF NOT EXISTS public."ReportHistoryCoverage" (
          "TableName" text PRIMARY KEY, "AvailableFrom" timestamptz NOT NULL);
        CREATE TABLE IF NOT EXISTS public."ReportHistoryVersions" (
          "Sequence" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
          "TableName" text NOT NULL, "EntityKey" text NOT NULL, "RecordedAt" timestamptz NOT NULL,
          "TransactionId" bigint NOT NULL, "IsDeleted" boolean NOT NULL, "Payload" jsonb NOT NULL);
        CREATE INDEX IF NOT EXISTS "IX_ReportHistory_EntityTime" ON public."ReportHistoryVersions"
          ("TableName","EntityKey","RecordedAt" DESC,"Sequence" DESC);
        CREATE INDEX IF NOT EXISTS "IX_ReportHistory_TransactionTime" ON public."ReportHistoryVersions" ("TransactionId","RecordedAt");
        CREATE OR REPLACE FUNCTION public.oryx_report_key(payload jsonb, keys text[]) RETURNS text
          LANGUAGE sql IMMUTABLE STRICT AS $$ SELECT jsonb_object_agg(k,payload->k)::text FROM unnest(keys) k $$;
        CREATE OR REPLACE FUNCTION public.oryx_report_payload(relation text, payload jsonb) RETURNS jsonb
          LANGUAGE sql IMMUTABLE STRICT AS $$ SELECT CASE WHEN relation='users' THEN payload - ARRAY[
            'PasswordHash','SecurityStamp','ConcurrencyStamp','AuthenticatorKey','RecoveryCodes','RefreshToken','AccessToken'
          ]::text[] ELSE payload END $$;
        CREATE OR REPLACE FUNCTION public.oryx_report_capture() RETURNS trigger LANGUAGE plpgsql
          SET search_path=pg_catalog,public AS $$
        DECLARE payload jsonb; key text; prior text;
        BEGIN
          payload := CASE WHEN TG_OP='DELETE' THEN to_jsonb(OLD) ELSE to_jsonb(NEW) END;
          key := public.oryx_report_key(payload,TG_ARGV[0]::text[]);
          IF TG_OP='UPDATE' THEN
            prior := public.oryx_report_key(to_jsonb(OLD),TG_ARGV[0]::text[]);
            IF prior<>key THEN
              INSERT INTO public."ReportHistoryVersions" ("TableName","EntityKey","RecordedAt","TransactionId","IsDeleted","Payload")
                VALUES(TG_TABLE_NAME,prior,clock_timestamp(),txid_current(),true,public.oryx_report_payload(TG_TABLE_NAME,to_jsonb(OLD)));
            END IF;
          END IF;
          INSERT INTO public."ReportHistoryVersions" ("TableName","EntityKey","RecordedAt","TransactionId","IsDeleted","Payload")
            VALUES(TG_TABLE_NAME,key,clock_timestamp(),txid_current(),TG_OP='DELETE',public.oryx_report_payload(TG_TABLE_NAME,payload));
          RETURN NULL;
        END $$;
        CREATE OR REPLACE FUNCTION public.oryx_report_immutable() RETURNS trigger LANGUAGE plpgsql AS $$
          BEGIN RAISE EXCEPTION 'Report history is append-only'; END $$;
        DROP TRIGGER IF EXISTS oryx_report_no_truncate ON public."ReportHistoryVersions";
        CREATE TRIGGER oryx_report_no_truncate BEFORE TRUNCATE ON public."ReportHistoryVersions"
          FOR EACH STATEMENT EXECUTE FUNCTION public.oryx_report_immutable();
        DROP TRIGGER IF EXISTS oryx_report_immutable ON public."ReportHistoryVersions";
        CREATE TRIGGER oryx_report_immutable BEFORE UPDATE OR DELETE ON public."ReportHistoryVersions"
          FOR EACH ROW EXECUTE FUNCTION public.oryx_report_immutable();
        """;

    public const string RemoveSql = """
        DO $history$ DECLARE relation record; BEGIN
          FOR relation IN SELECT c.relname FROM pg_trigger t JOIN pg_class c ON c.oid=t.tgrelid JOIN pg_namespace n ON n.oid=c.relnamespace
            WHERE t.tgname='oryx_report_capture' AND n.nspname='public'
          LOOP
            EXECUTE format('DROP TRIGGER oryx_report_capture ON public.%I',relation.relname);
            EXECUTE format('DROP TRIGGER IF EXISTS oryx_report_no_truncate ON public.%I',relation.relname);
          END LOOP;
        END $history$;
        DROP FUNCTION IF EXISTS public.oryx_report_capture();
        DROP TABLE IF EXISTS public."ReportHistoryVersions";
        DROP TABLE IF EXISTS public."ReportHistoryCoverage";
        DROP FUNCTION IF EXISTS public.oryx_report_immutable();
        DROP FUNCTION IF EXISTS public.oryx_report_key(jsonb,text[]);
        DROP FUNCTION IF EXISTS public.oryx_report_payload(text,jsonb);
        """;
}
