# DigitalOcean backend deployment and migration

DigitalOcean and the customer's physical server use separate release paths.

| Target | Release source | Migration application |
| --- | --- | --- |
| DigitalOcean demo | `demo`, `demo-docker-deploy.yml` | Generated SQL applied by SSH to `postgres_db`, database `entrancedb` |
| DigitalOcean dev | `dev`, `docker-deploy.yml` | Generated SQL applied by SSH to `postgres_db`, database `oryxdb` |
| DigitalOcean test | `test`, `test-docker-deploy.yml` | Generated SQL applied by SSH to `postgres_db`, database `oryxtestdb` |
| Customer physical server | `main`, installed `deploy.sh` tooling | Local maintenance/backup/migration/activation sequence using the installed API's database configuration |

The existing DigitalOcean `migrate` job generates an idempotent SQL artifact; it
does not connect to the server database. The demo job first tests the currency
migration and model consistency against its own disposable PostgreSQL service,
including deferred report-history capture. A green job means those checks and
generation/upload succeeded, not that the live database has been migrated.
The dependent `deploy` job downloads that artifact and applies it with
`psql -v ON_ERROR_STOP=1` before replacing the API container. A migration failure
stops deployment before container replacement. Do not replace this sequence with
the physical-server deployment tooling or add another migration executor.

## List-price currency failure on 2026-10-31

Run `41191663597` built the API and generated SQL successfully, then failed in
`deploy` → `Apply database migration` at `IX_ProductPrices_CurrencyId`:

```text
cannot CREATE INDEX "ProductPrices" because it has pending trigger events
```

The reporting-history capture trigger is `DEFERRABLE INITIALLY DEFERRED`.
`AddListPriceCurrency` updated prices before creating the index and foreign key,
leaving deferred capture events outstanding during that DDL. Create all three
schema objects before the backfill instead. Keep the same migration ID, normal
transaction, foreign key, reporting trigger and audit history. Previously applied
copies skip the migration; pending copies use the corrected ordering. No manual
history insertion, trigger disabling or database reset is needed.

Promote the fix from `desmond-latest` to `demo` through the release process. The
resulting new push must generate a fresh migration artifact from the fixed
commit. Re-running the old workflow run still checks out its old commit and
regenerates/reuses the old SQL, so it cannot deliver this fix. The corrected
idempotent artifact checks each target database's own `__EFMigrationsHistory`.
The physical server independently receives the same migration correction when
its selected `main` release contains it; its deployment commands stay separate.

See [customer-host deployment](customer-backend-deployment.md) for the physical
server and [Finance costing](finance-costing.md) for backfill/rollback behavior.
