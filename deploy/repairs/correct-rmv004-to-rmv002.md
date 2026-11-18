# Correct the RMV004 migration lot to RMV002

Prepared for the client-server run on 18 November 2026. Preparation did not change
demo or client data. Copy `correct-rmv004-to-rmv002.sql` to the client server;
no API deployment or EF migration is needed.

The approved opening-balance migration imported batch **YE-390921**, AR
**NQC/RM/22/016**, as **RMV004**. Its **41.65 kg** belongs to **RMV002**
(Vitamin E Acetate 50% Dry powder). This script corrects that one batch and its
working migration-line material selection. The two material masters retain their
codes. The original spreadsheet fields remain unchanged as source evidence.

The existing RMV002 lot stays separate. With the investigated stock unchanged,
RMV002 will hold **21.44 + 41.65 = 63.09 kg** in two lots. The script recalculates
the affected material/warehouse bin-card running balances; receipt/issue quantities
and receipt IDs remain unchanged. It preserves batch IDs, shelf allocations,
quantities, supplier batch, AR/GRN, expiry/retest dates and approval status.

The existing RMV002 lot **MHO718 / NQC/RM/23/173 (21.44 kg)** has the original
spreadsheet name **Vitamin A Acetate**, which differs from the current RMV002
material master. Record that separate discrepancy for client inventory review.
This repair preserves that lot and corrects only YE-390921.

## Run on the client server

Use `psql` with the client's existing database connection settings. The commands
below use standard `PGHOST`, `PGPORT`, `PGUSER` and `PGPASSFILE`/`.pgpass` settings;
they contain no database password. Set `ACTOR_EMAIL` to the operator actually
performing the correction. That active account must belong to the migration's
tablet department and currently hold `CanViewOpeningBalances`,
`CanCorrectOpeningBalances` and `CanReassignRawMaterialStock`. The script checks
these grants before each business write and grants nothing itself.

```sh
ACTOR_EMAIL='YOUR_OPERATOR_EMAIL'
REASON='Correct YE-390921 / NQC/RM/22/016 imported as RMV004; stock belongs to RMV002'

# Preview is the default. It creates temporary objects only.
psql -X -d entrancedb -v ON_ERROR_STOP=1 \
  -v expected_database=entrancedb \
  -v actor_email="$ACTOR_EMAIL" -v reason="$REASON" \
  -f correct-rmv004-to-rmv002.sql
```

Review the resulting JSON: the connected server/database, batch, migration row,
shelf, before/after bin-card plan and `problems`. It must describe the lot above
and have an empty `problems` array. Copy `systemIdentifier` and `previewToken`
from **this client-server preview** into the apply command. Tokens from demo or
another server cannot be used. Keep the actor and reason identical.

```sh
CLIENT_SYSTEM_IDENTIFIER='COPY_systemIdentifier_FROM_CLIENT_PREVIEW'
CLIENT_PREVIEW_TOKEN='COPY_previewToken_FROM_CLIENT_PREVIEW'

psql -X -d entrancedb -v ON_ERROR_STOP=1 \
  -v expected_database=entrancedb \
  -v actor_email="$ACTOR_EMAIL" -v reason="$REASON" \
  -v apply=true \
  -v expected_system_identifier="$CLIENT_SYSTEM_IDENTIFIER" \
  -v expected_token="$CLIENT_PREVIEW_TOKEN" \
  -f correct-rmv004-to-rmv002.sql
```

Successful output has `mode: "applied"`. Run the preview again to confirm
`mode: "already-applied"`, then refresh the stock and bin-card screens.
Repeated apply is also a no-op when the recorded correction and material mapping
agree. Before/after evidence, actor, database user, reason and reviewed token are
retained in the append-only `_MigrationMaterialCorrections` table.

The exact migration/line/batch/material identifiers are intentionally bound to
the investigated import. A different import, a changed preview, inconsistent
ledger, missing mg department link, later source-lot usage/reservation/QC, or
unknown batch/shelf reference blocks the correction. Resolve that reported
difference rather than changing the script's checks. Apply briefly locks the
affected inventory/reference/permission tables and rolls back on a lock timeout
or any failed check. It does not disable triggers or rewrite immutable history.

## Verification

The tests create and drop one uniquely named database on a local PostgreSQL
server. They use synthetic rows with the repair's required identifiers; they do
not import customer operational records or credentials.

```sh
PGHOST=127.0.0.1 python3 tests/repairs/test_rmv004_correction.py
```

Coverage includes preview with no persistent changes, apply and stock conservation,
bin-card accumulation, preservation of spreadsheet evidence, immutable audit,
repeat execution, stale/wrong tokens, wrong database/cluster, missing current
permissions, department scope, unit mismatch, consumed quantity, inconsistent
ledger, new foreign-key references, shelf-only references and legacy references
without foreign keys. No endpoint, desktop behavior, permission key or role grant
is changed.
