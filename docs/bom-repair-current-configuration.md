# Repair against the corrected BOM

BOM repair uses the product's saved configuration to rebuild unissued batch
requirements and reservations. If old separate main/substitute BOM rows have
been corrected into one main/substitute pair, holding both old materials no
longer blocks repair. The planner treats them as one current requirement and
releases their obsolete holds when applying the new allocation.

An audited store choice remains explicit. Without one, an ambiguous old pair
starts with the main material. If usable stock cannot cover it, the planner tries
allowed substitutes in BOM order. Each substitute uses its own corrected quantity
and department unit. This batch's old holds count toward usable stock; holds for
other batches do not. An intentional existing single substitute is retained.
Shortages on an explicit choice block repair instead of silently switching it.

The preview includes material codes. Before/after quantities and reservations
show which old line is retired and which current option will be reserved.
Preview evidence includes option availability and explicit choice history so
changed evidence requires a new preview.

Apply retains the existing atomic transaction, permission reloads, stock locks,
request/audit records and all-or-nothing schedule repair. Rewritten requisitions
require fresh approval. Issued/consumed stock and return/final-packing history
remain protected; repairing a BOM never issues stock.

Permissions are unchanged: `CanRepairBom` / `CanRepairPackaging`, with the
existing raw/packaging stock-choice permissions for explicit store choices.
The new read-only issue-queue endpoints use
`CanViewRawMaterialRequisitions` and `CanViewPackagingMaterialRequisitions`
under Warehouse / Issue Stock Requisitions. No new key or grant migration is
required. The redundant packaging view entry under Extra Packaging Requisitions
is removed from the generated catalog; its key and existing role claims remain.

Certificate approval rows now carry subject codes, material names and product
stage. Identity is enriched only for IDs already selected by the caller's
approval queue; it remains readable across production departments and archived
master records. ATR rows carry an independent COA status from their current
chemical/microbial responses, using the latest rework for the matching batch
and activity step. Required tracks must all be generated/approved.

Validation: API build; 37 focused tests including real PostgreSQL reservation
replacement, substitute quantity/own holds, explicit-main shortage, permissions,
FIFO filtering before pagination, cross-department COA identity and COA rework /
required-track status. PostgreSQL tests ran against the dedicated local
`oryx_material_repair_test` database, not application data. No schema migration
or deployment is part of this source change.
