# Material-distribution page projection — 2026-10-24

## Problem and implementation

The distribution GET previously hydrated shipment/invoice graphs, repeated a
requisition query per material and a department lookup per request, and invoked
general material mapping that could query stock and specifications. The page
uses none of the inventory, user, or storage hierarchy data behind those loads.

`MaterialDistributionPageReader` now executes at most three queries:

1. The shipment's invoice reference, honoring shipment filters.
2. Undistributed invoice source scalars: material labels, unit, manufacturer,
   purchase-order label, frozen price/currency, quantities and creation time.
3. One batch of eligible requisition scalars and department labels for all
   materials. An EXISTS linkage avoids duplicate requests from duplicate source
   links, without loading full purchase orders or source requisitions.

Missing shipments stop after the first query; empty invoice lines stop after the
second. No queries execute inside material/request loops. Reads use no tracking
and suppress incidental AutoIncludes. SQL tags identify all three commands.
No AutoMapper stock/specification resolver runs on this public read path.

## API contract and clients

The URL and authorization stay unchanged:

`GET /api/v1/procurement/shipment-document/{shipmentDocumentId}/material-distribution`

OpenAPI now declares `MaterialDistributionPageDto`. It retains the `sections`,
`material`, `totalQuantity`, `uoM`, `items`, `department`, `requisitionItem`,
quantity and `distributions[].shipmentInvoiceItem` paths used by desktop and
Next detail pages. Source IDs, expected/received quantities, manufacturer/order
labels, frozen price, price unit and currency are retained. The lightweight
purchase-order label also exposes that source currency for Next's current
nested currency lookup.

Unused general DTO fields (stock totals, specifications, creator records,
warehouse trees, supplier/manufacturer collections, batches and full purchase
orders) are omitted rather than serialized as misleading defaults. Clients
needing those fields must use their respective entity endpoints. Generated
frontend clients are not edited manually; existing page accesses remain valid.
Shipment/supplier headers continue using the separate shipment-detail GET.

## Workflow, validation, and audit

The public read invokes the existing `ProcessMaterialDistributions` calculation
with projected values, retaining proportional allocation, decimal arithmetic,
rounding and FIFO attribution. Fulfilled and unlinked requests remain excluded;
existing query filters remain active. The legacy linkage rule allowing any PO
on the shipment to link a requesting requisition is preserved, not silently
reinterpreted as part of this performance fix.

Confirmation still uses its private overload and existing approval, receiving
warehouse, inventory mutation and audit behavior. Its filter policy is not
merged with the public GET. The earlier narrowed department lookup remains for
that internal path. The page read never calls SaveChanges and produces no stock
or workflow events. Existing error codes for missing shipment and no eligible
requisitions are retained; an absent invoice reference returns not found.

## Verification and delivery

Regression coverage checks the arrived-shipment quantities, multiple materials,
completed/unlinked requests, deleted source filtering, duplicate source links,
FIFO across multiple invoice lines, both clients' required fields, omitted
unrelated response fields, and absence of tracking or inventory mutations.
PostgreSQL-provider SQL compilation checks the batched queries exclude user,
storage, stock, specification, and full purchase-order collection joins.
Neighboring shipment invoice and clearance/payment regressions are included.
These are local tests and SQL compilation, not a live database timing benchmark.

Deploy the backend to apply the fix. No migration or frontend deployment is
required for the existing pages. No live distribution, restart, or deployment
was performed during implementation.

Documentation updated: this note, `docs/services.md`, `docs/workflows.md`, and the
cross-app query review in `oryx-next/docs/material-distribution-query-review-2026-10-24.md`.
