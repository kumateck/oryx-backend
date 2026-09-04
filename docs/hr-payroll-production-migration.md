# HR payroll and performance production migration

Migrations: `20260903215221_AddHrPayrollAndPerformanceModules` and
`20260903233548_NormalizeHrPayrollRunTable`

The legacy payroll schema already owns the `PayrollRuns` table. Its columns
and relationships belong to payroll companies, payroll periods, and pay
groups, so it cannot safely back the newer HR payroll workflow. The new
`PayrollRun` entity is therefore mapped to the dedicated `HrPayrollRuns`
table. The migration must not rename, alter, drop, or recreate the existing
`PayrollRuns` table.

The normalization migration also supports databases that applied the original
HR migration before this collision was found. It identifies that table by its
`PeriodStart` column and the absence of the legacy `PayrollCompanyId` column,
then renames it and its constraints to the dedicated HR names. It is a no-op
when `HrPayrollRuns` already exists or when `PayrollRuns` is the legacy table.

## Required rollout

1. Verify a restorable backup and record the existing `PayrollRuns` table
   definition and row count.
2. Record whether `20260903215221_AddHrPayrollAndPerformanceModules` is already
   present in `__EFMigrationsHistory`; both states are supported.
3. Generate and review an idempotent migration script. It must create
   `HrPayrollRuns`; approval and payslip foreign keys must reference that
   table; and the compatibility block may rename `PayrollRuns` only when the
   HR signature (`PeriodStart` without `PayrollCompanyId`) is present.
4. Apply the script through the deployment migration step while
   `Database:ApplyMigrationsOnStartup=false` remains configured.
5. Verify both payroll tables and the migration history before deploying the
   application container.

## Read-only validation

```sql
SELECT "MigrationId"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" IN (
  '20260903215221_AddHrPayrollAndPerformanceModules',
  '20260903233548_NormalizeHrPayrollRunTable'
)
ORDER BY "MigrationId";

SELECT table_name
FROM information_schema.tables
WHERE table_schema = 'public'
  AND table_name IN ('PayrollRuns', 'HrPayrollRuns')
ORDER BY table_name;

SELECT table_name, count(*) AS column_count
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name IN ('PayrollRuns', 'HrPayrollRuns')
GROUP BY table_name
ORDER BY table_name;
```

The migration-history query must return two rows and `HrPayrollRuns` must
exist. If a legacy `PayrollRuns` table existed during preflight, the table
query must still return both names. Compare that legacy table's post-deploy
definition and row count with the preflight values; both must be unchanged.

## Rollback

If application deployment fails after normalization and the previous
application still maps HR payroll to `PayrollRuns`, revert only
`20260903233548_NormalizeHrPayrollRunTable` before deploying that application.
Its marker makes the `Down` operation rename only an HR-shaped table that this
migration previously normalized; it never targets the legacy payroll table.
Do not run the destructive `Down` operation for
`20260903215221_AddHrPayrollAndPerformanceModules` after HR data exists.
Correct forward instead.
