# Warehouse batch locations

Warehouse picking responses now carry the selected shelf's metadata so clients
can show `rack (shelf)` below each batch number. `BatchToSupply`,
`AlternativeBatchDto` and `SwapShelfMaterialBatchDto` expose
`WarehouseLocationShelf` using the existing
material shelf DTO. The same shelf is retained when a batch spans several
allocations; quantities and issue requests are unchanged.

Reserved-material reports expose `RackName` and `ShelfName` from the reservation's
source shelf. White Placebo stock choices include `Rack`; ingredient details
include `WarehouseLocationShelf` using the recorded source shelf-batch ID, read
in one batch after the preparation's department/permission checks. Missing
locations remain absent; no alternative shelf is inferred from the batch ID.

These are additive read-contract changes. No permissions, stock mutations,
approvals, reservations, audit events or schema migrations change. The companion
desktop displays locations in stock/issue lists, expiry reports and picking
printouts. Both applications need these changes for complete picking metadata.

Verification: 21 focused backend tests passed, including multi-shelf allocation,
reserved shelf/rack hydration, ingredient-source locations, unchanged shelf
balances and existing expiry/White Placebo workflows. No live ERP records or
deployment were changed.
