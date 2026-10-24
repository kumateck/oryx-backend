# Supplier purchase-unit symbol synchronization

## Contract and storage

Authenticated `GET /api/v1/material/{materialId}/purchase-units` returns
`UnitOfMeasureDto[]` from active material links across all departments. Zero stock
does not hide a linkage. Active symbols are trimmed/deduplicated without regard to
case. This exposes reference units, not departmental quantities. The existing
`/uom` lookup remains department-specific for inventory consumers.

Supplier association create/update accepts `purchaseUnit` (symbol, max 20).
Responses expose `purchaseUnit` and `pendingPurchaseUnit`; `uoMId` remains the
compatible quantity reference. Legacy requests specifying only `uoMId` derive a
symbol. New symbol choices must be linked in at least one department or belong to
a linked mass/volume family. Existing unchanged legacy symbols can be retained.
Manufacturers for one supplier/material must agree on the symbol. Soft-deleted
associations remain in audit storage and are excluded from active reads.

## Workflow and audit

`SupplierPurchaseUnitPolicy` validates/normalizes supplier writes;
`SupplierPurchaseUnitSync` coordinates agreement/default changes;
`SupplierPurchaseUnitReadModel` projects currently effective pricing onto all
supplier association reads. `SupplierQuotationPriceRecorder` owns quotation
validation, audit metadata and the transaction containing price/unit updates.

Without current approved pricing, a valid supplier/quotation unit updates the
default. With current pricing in another compatible unit, a supplier edit submits
an equivalent-price replacement (20/kg becomes 0.02/g); quotation entry submits the
actual quoted amount. Proposing either requires `CanManageSupplierContracts` and
uses existing agreement approvals, maker/checker, action logs and activation.
No new approval bypass is introduced: the established no-configured-stages policy
continues to apply. Pending changes leave the effective default untouched and
expose the proposed symbol separately. Rejecting a proposal preserves prior terms.
Direct agreement changes update the default when activated; future starts are
respected through effective-date reads. Updates use actor/timestamp metadata and
existing HTTP activity logging. Unchanged supplier edits remain possible while a
unit revision is pending. Conflicting pending revisions or multiple current
agreements are refused for reconciliation.

Supplier and quotation writes use serializable relational transactions. Quotes
validate the complete payload before changing prices; foreign and processed/closed
lines are refused. Recorded quotes/orders remain historical snapshots and are not
relabelled after a default change. Inventory and production quantity units are
unchanged. Cross-dimension conversions (kg/L) require separate material density
support and are refused where costing cannot reconcile the units.

## Controlled rollout

1. Back up and inspect unresolved supplier quantity-unit links, blank unit symbols,
   duplicate active associations and overlapping effective pricing agreements.
2. Review/apply `20261024143000_AddSupplierPurchaseUnitSymbol` through the existing
   deployment process. It adds only nullable varchar(20) `PurchaseUnit` and backfills
   normalized symbols from existing `UoMId` references, retaining those references.
3. Deploy this backend, verify the global lookup and symbol create/edit readback,
   and export its OpenAPI for the companion desktop release.
4. Release the desktop only after the new endpoint/DTOs are available. Check
   supplier update → pending agreement → approval/rejection → supplier reload.
5. Reconcile effective agreement/default differences before using them for new
   purchasing. Existing historical transaction snapshots are not rewritten.

No migration was applied and no live data was mutated during this implementation.
The Down migration removes the symbol column; coordinate application rollback and
retain a backup of new symbols and agreement revisions before using it.

## Verification

Focused tests cover linked-department union/deletion/zero stock, symbol families,
supplier symbol-only create/edit reload, compatible quantity-reference retention,
price-preserving and quoted-price proposals, approval/rejection, permission denial,
conflicting pending revisions, unrelated supplier edits, future effective dates
and processed quotation protection. They use isolated EF InMemory databases;
PostgreSQL concurrency and deployment smoke checks remain release checks.
EF model-diff and SQL generation checks inspect the migration without connecting
to or applying changes to a database. SQL adds/backfills only the new column.

Documentation updated: this note, README, services and workflows.

Final focused run: 60 backend tests passed; companion frontend 48 tests passed.
EF reports no model changes beyond the migration, and generated SQL contains only
the new column/backfill/history entry inside a transaction. No database migration
was applied. Production PostgreSQL transaction/concurrency checks remain a rollout
step, as the regression suite uses isolated InMemory databases.
