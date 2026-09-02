# Product ATR response migration

Migration `20260902215903_AddUniqueProductResponseStep` prevents more than one
product form response for the same BMR and production step. It must be applied
separately to every database used by the application. Applying it to development
or demo does not change staging, production, tenant, or customer databases.

## Controlled rollout per database

1. Back up the target database and record its environment/tenant identifier.
2. Confirm the database has migration
   `20260902141424_AddStageScopedResponseApprovalRounds`.
3. Run the duplicate preflight below. A returned row must be reconciled with an
   audit record; do not delete or merge regulated responses automatically.
4. Point the target's `connectionString` at that database and run
   `./update-db.sh`, or execute a reviewed idempotent EF migration script.
5. Verify the migration-history row and unique index.
6. Repeat the complete process for every remaining database.

```sql
SELECT "BatchManufacturingRecordId", "ProductionActivityStepId", COUNT(*)
FROM "Responses"
WHERE "BatchManufacturingRecordId" IS NOT NULL
  AND "ProductionActivityStepId" IS NOT NULL
GROUP BY "BatchManufacturingRecordId", "ProductionActivityStepId"
HAVING COUNT(*) > 1;
```

```sql
SELECT "MigrationId"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20260902215903_AddUniqueProductResponseStep';

SELECT indexname
FROM pg_indexes
WHERE tablename = 'Responses'
  AND indexname =
    'IX_Responses_BatchManufacturingRecordId_ProductionActivityStep~';
```

Production should keep `Database:ApplyMigrationsOnStartup=false` and use the
controlled migration job. If startup migration is explicitly enabled in another
environment, a migration error now stops startup instead of leaving the API
running without the required constraint.

## Release order

Apply the migration to every database, deploy/restart the backend, deploy the
frontend, and smoke-test one replacement ATR stage before starting production.
The smoke test must show one exact BMR/step response, one COA approval round,
ATR `Released`, and the linked production step `Completed` after final approval.
