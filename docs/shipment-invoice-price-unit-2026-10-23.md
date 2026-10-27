# Shipment invoice price-unit validation — 2026-10-23

## Incident

The reported `POST /api/v1/procurement/shipment-invoice` request failed in
`ValidatePriceUoM`: a new invoice line carried price `1.5` without its price unit.
The request's quantities (12,000,000,000; 900,000,000; 123,000,000) fit the decimal
quantity contract. The failure came from legacy purchase-order pricing.

## Contract and validation

The request/response shape is unchanged. Clients continue sending quantities in
the existing smallest-unit convention. Invoice prices and units come from the
purchase-order line, regardless of client-supplied pricing.

Creation uses the stored purchase-order `PriceUoM` first. For a legacy blank unit,
it reuses `BuildQuotationPriceUoMLookup` / `ResolvePriceUoM`, scoped to the
purchase order, material, UoM, awarded supplier and processed quotation. Competing
suppliers' quotations cannot supply the unit. No quantity-unit guess is allowed.

A positive price with no authoritative unit returns `Item.PriceUoM` as a normal
validation failure (HTTP 400 through the existing controller/result mapping),
instead of reaching the save guard and returning HTTP 500. Correct the price
unit through the existing purchase-order correction/revision workflow when no
awarded quotation can resolve it. The database guard stays enabled.

## Workflow and audit

Every line is validated before creating an invoice or changing any tracked order.
On success, invoice prices/units/currencies are frozen and received quantities
advance each purchase order to Linked or PartiallyLinked as before. The invoice
and order changes share the existing single `SaveChangesAsync` call. Existing
current-user timestamps, actor records and activity middleware remain in place.

Tracked orders are no longer graph-updated: only changed quantities/statuses are
saved, so unrelated legacy price fields are not marked as rewritten. Resolving a
unit for the new invoice does not rewrite legacy purchase-order pricing. Approval
rules, payments and inventory movements are unchanged.

## Verification and rollout

Regression tests cover the reported quantity values and repeated material across
different purchase orders, authoritative quotation fallback, competing supplier
exclusion, unresolved-unit validation without tracked/persisted mutations, stored
pricing precedence, partial linking and over-receipt validation.

The fix is application code; no migration or bulk data repair is required.
Deploy the compatible backend before retrying the failed invoice. This change
has not been deployed or used to create production records.

Documentation updated: this incident note, `docs/services.md`, and
`docs/workflows.md`; frontend documentation links the same contract behavior.
