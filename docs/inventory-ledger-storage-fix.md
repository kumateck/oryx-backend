# Inventory ledger transaction type storage

The receiving approval failure reported on 2026-10-28 was reproduced with the
same PostgreSQL exception: `Reading as 'System.Int32' is not supported for fields
having DataTypeName 'text'` in `InventoryLedger.Balance`. The initial migration
created `ItemTransactionLogs.TransactionType` as text. A later domain change made
the property an enum without retaining a string converter. Loading an existing
ledger row then failed before receipt approval could commit. Stock issue uses the
same balance check and is affected by this mapping too.

`ItemTransactionLogConfiguration` restores enum names in the existing text column.
This covers reads, transaction filters, and new receipt/issue postings. It does not
skip reconciliation, change balances, or retry a failed stock write automatically.
Waiting does not resolve this mapping failure; a later successful retry is not
explained by the supplied logs alone.

Migration `20261028194616_RestoreItemTransactionTypeText` aligns the model snapshot.
On original text installations its SQL leaves the column and rows untouched. For
databases created with integer storage, it converts the six known enum ordinals to
their names under an exclusive table lock in the migration transaction. Unknown
ordinals, null integer values, and unexpected storage types abort the migration
instead of assigning a guessed meaning. Rollback retains the original text storage
and ledger evidence; it does not attempt to cast names into integers.

Review and apply the migration through the existing controlled backend deployment
process. Do not run a blanket text-to-integer `ALTER TABLE` on the customer ledger.
No server/database changes were made while preparing this fix.

Read-only database preflight (run in the configured application database):

```sql
SELECT table_schema, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'ItemTransactionLogs' AND column_name = 'TransactionType';

SELECT "TransactionType", count(*)
FROM "ItemTransactionLogs"
GROUP BY "TransactionType" ORDER BY "TransactionType";
```

Local verification uses `InventoryLedgerPostgresTests` with a dedicated disposable
database named `oryx_inventory_ledger_test` on loopback, supplied through
`ORYX_INVENTORY_TEST_CONNECTION`. It reproduces legacy text storage, approves a
receipt once, issues stock, verifies resulting stock/ledger/requisition status and
actor attribution, filters and round-trips every transaction type, and checks
migration preservation for text and integer storage plus rejection of unknown
ordinals. Existing receiving, concurrency, requisition and procurement integrity
tests are also required.
