# Production stock issue and bin card reconciliation

## New issues

An approved stock requisition is issued once. The issue operation rejects a completed
requisition or one containing a completed item. It also requires one line per material,
a positive line quantity, and a reservation total exactly equal to each line quantity.
Every reserved batch must still have enough stock on the source warehouse shelf.
The issue transaction uses serializable isolation so two concurrent attempts cannot
both commit the same issue.

An issue moves the reserved batch quantities from the raw or packaging warehouse
to the production warehouse. It records source shelf deductions, material movements,
material batch events, and source warehouse bin card issues in one transaction.
Starting Product Preparation later consumes issued raw material reservations; starting
Final Packing consumes packaging material reservations. These are different events.

New source bin card issue rows carry `ProductId`, `RequisitionId`,
`RequisitionCode`, and the available `ProductBatchNumber`. The requisition code is
the outbound reference. `WayBill` stays empty when no outbound waybill exists;
an inbound waybill is not copied onto an issue. AR uses the latest material sampling,
or a single unambiguous AR from this material batch's receipt rows. Supplier and
manufacturer use the material batch checklist when present. Missing source data
remains empty rather than being guessed.

Migration `AddStockIssueBinCardReferences` adds three nullable columns to
`BinCardInformation`. Apply it before deploying code that writes the new fields.
The API adds nullable `requisitionId`, `requisitionCode`, and
`productBatchNumber` to `BinCardInformationDto`.
The response also includes `warehouseName`, scoped to the requested warehouse
department and ordered newest first. Clients show the recorded row balance;
they must not recalculate a balance from a paginated, multiwarehouse response.

## Existing rows

Existing bin cards are retained unchanged. A historical row with `N/A` or no
product cannot safely be linked to a requisition by quantity and time alone.
Before any correction, export the requisition, its approvals and items, reserved
batch rows (including deleted rows), material movements, batch events, both
warehouse bin cards, and production activity logs. Compare batch IDs, quantities,
warehouses, actors, and timestamps. Record the evidence and the reviewer who
approved each correction. Use a compensating stock movement with an audit reason
if an actual duplicate issue is proven; do not delete bin card history.

This read-only query identifies possible repeated issues for review. It does not
establish that a pair is a duplicate:

```sql
SELECT a."Id" AS first_id, b."Id" AS second_id,
       a."MaterialBatchId", a."WarehouseId", a."QuantityIssued",
       a."CreatedAt" AS first_at, b."CreatedAt" AS second_at
FROM "BinCardInformation" a
JOIN "BinCardInformation" b
  ON a."MaterialBatchId" = b."MaterialBatchId"
 AND a."WarehouseId" = b."WarehouseId"
 AND a."QuantityIssued" = b."QuantityIssued"
 AND a."CreatedAt" < b."CreatedAt"
 AND b."CreatedAt" <= a."CreatedAt" + interval '10 minutes'
WHERE a."QuantityIssued" > 0
ORDER BY a."CreatedAt", b."CreatedAt";
```

## Documentation updated

This note records the validation, event, API, and historical review rules.
`docs/services.md` records the endpoint contract. `docs/workflows.md` records
the issue and production consumption sequence.
