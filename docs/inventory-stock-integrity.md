# Inventory definition and stock integrity source repair — 2026-10-22

This change supports the authorized Oryx Live general-store migration. **Source only, not deployed.** No live stock, ledger, approval or financial data was changed. Existing HTTP request/response shapes remain the same. The stock endpoint still publishes an incorrect bare-array response declaration; Live validates its actual pagination envelope rather than editing generated code.

## Definition behavior

Item maintenance is extracted into a small partial repository file. It validates name/code, defined store/classification enums, non-negative integer levels, current unit/category references and same-store names. Newly created/renamed codes must be globally unique including historical items because the existing ledger identifies items only by code. Safe metadata edits may retain a legacy code; ambiguous historical ledgers are refused rather than merged or relabeled.

Current or historical vendor items, purchase/stock requisitions, sourcing/market/quotation/memo, GRN/shipment, stock entry, damaged/recoverable reports, consumed-job items, ledger postings or nonzero stock protect item deletion and code/store/unit/classification changes. Unused definitions can be soft-deleted with actor/time, and stock levels/name/description/status can still be maintained. Checks and writes share a serializable transaction on relational providers. Serialization/uniqueness/concurrency conflicts return a controlled review error, never an automatic replay. Audit hooks remain in ApplicationDbContext and the existing HTTP middleware.

## Authority and receiving

Item reads/CRUD and receiving list/decisions consult current user, active role, role claim and permission-type records. No role name or administrator bypass is introduced, and no stale permission cache grants these actions. Anonymous item and ledger reads are removed. Item identity can be read by an authorized stock/receiving reader; transaction history requires item/available-stock access. Receiving decision permissions are exact to the actual item store and require a known distinct creator. The GET stock envelope includes memo, creator, item unit and category references used by the desktop.

Pending receipts alone can be approved/rejected. Approval verifies active stock, unique historical item code, reconciled available/ledger balance, positive whole-number quantity and int overflow. It retains the original received quantity and saves entry state, available stock and one credit in one SaveChanges transaction. Rejection never changes stock and cannot replace a terminal decision. Existing approved receipt quantities that were previously overwritten are not silently repaired.

Damage/missing creation verifies positive in-stock quantities and defined damage type, unique/reconciled ledger identity, then saves the report, stock decrement and ledger debit together. It records the audit actor. The separate legacy damage edit/delete and recoverable-return implementations are not part of this repair and still require reconciliation fixes before migration.

Existing Item availability/code/store/unit/classification columns and StockEntry status are optimistic concurrency tokens. These affect UPDATE comparison predicates without adding columns. Controlled conflicts prevent stale stock or terminal decisions from being overwritten. Relational SaveChanges provides rollback for combined postings; EF InMemory does not prove PostgreSQL locking or rollback. Definition transactions and concurrent legacy writers require integration acceptance on the actual database.

## Verification

51 isolated tests pass: item definitions/identities/history; per-store permissions, current revocation, disabled users/deleted roles and no Super-account bypass; actual receiving-controller distinct/known creator and exact read/write scope; receipt original quantities, terminal replay refusal, reconciliation and overflow; damage/missing stock/report/ledger consistency and actors; concurrent stale receipt and item updates. No deployed PostgreSQL acceptance has been performed.

Remaining work includes resolving historical code collisions through an auditable migration, adding immutable item identity to the ledger contract, reviewing all other inventory writers and granular controllers, damage correction/return posting, PostgreSQL transaction/locking acceptance, and deploying the tested source before desktop live acceptance. No compensating adjustment or data mutation was generated as a shortcut.

Documentation updated: this note, backend services/workflows, Live inventory-and-stock/progress/scope/remaining-gaps/services/workflows.
