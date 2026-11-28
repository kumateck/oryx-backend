# Finished goods: live packing style, base-unit quantities, style history

Status: agreed rule (2026-11-28), not yet implemented. The implementation brief is
`docs/briefs/finished-goods-live-packing-prompt.md`. Read-only checks:
`docs/diagnostics/diagnose_packing_consistency.sql`.

## Why

A Blopen Gel order showed **200 shippers**, but its packing list showed **10 shippers**, both
for "3,600 packs". Each screen took "packs per shipper" from a different place:

| Reader | Source today |
| --- | --- |
| Backend `FinishedGoodsUnits.PacksPerShipper` (`APP/Utils/PackConversion.cs`) | The current packing style, then the FGTN copy |
| Desktop packing list (`fg-dispatch/schemas/dispatch-schema.ts`) | The FGTN copy (`PacksPerShipperSnapshot`), then the style |
| Production order line (`ProductionOrderProducts`) | Its own stored `TotalOrderQuantity` (packs), `Shippers` and `Loose` |

When the copies disagree, one consignment shows two different shipper counts. A packing style
save also overwrites the row in place, so nothing records what the style used to say.

## The rule

1. **Stock is held in the smallest unit** (the base packing unit: tube, bottle, tablet). Base units
   are the only quantity of record for stock, reservations, allocations and dispatches.
2. **Packs, shippers and loose are always derived from the packing style as it is now.** This
   holds on every record that has not been dispatched: FGTN, stock and bin card views, order line,
   packing list, proforma, invoice, loading and costing. When the style is updated, every one of
   those shows the new layout immediately. No screen reads a stored copy instead.
3. **Every packing style change is history.** Each create or update writes an append-only revision
   with who, when, the reason, and the full style before and after. Revisions are never edited or
   deleted.
4. **Dispatch pins the layout.** When a consignment is dispatched, each line records the style
   revision in force, with its units per pack, packs per shipper and the base units shipped. A
   dispatched or delivered consignment always shows its pinned layout, so later style edits never
   rewrite what the customer received.
5. **Price is per pack, so a pack-size change needs re-pricing.** A style change that only regroups
   shippers (units per pack unchanged) needs nothing more. A change that alters **units per pack**
   changes what a priced pack is. It still takes effect everywhere, but every undispatched order line,
   packing list, proforma and invoice on that style is marked *re-pricing required*. Invoice issue,
   commercial clearance and dispatch are refused until finance re-confirms the price per pack or
   reissues the document. Nothing is re-priced silently.

The existing operating rule "Preserve the original conversion" in `finished-goods-workflow.md` is
replaced by this document.

## Worked example

Style "18x20x30g": Shipper = 18 outer cartons, outer carton = 20 tubes, PackPerShipper = 360.
`PackConversion.UnitsPerPack` finds 18 × 20 = 360 at the tube level, so **the pack is the tube**,
with 1 tube per pack and 360 tubes per shipper.

- The customer orders 200 shippers. The order line stores 72,000 tubes and shows 72,000 packs,
  200 shippers and 0 loose.
- The FGTN for a batch of 75,000 tubes shows 208 shippers + 120 packs from the same style.
- The packing list takes 72,000 tubes and shows 200 shippers of 360. Every screen agrees, because
  every screen reads the same style.

The style is then edited to PackPerShipper = 18, which makes the outer carton the pack (20 tubes per
pack). Units per pack changed from 1 to 20, so:

- the order and packing list now show 3,600 packs = 200 shippers, still 72,000 tubes;
- the open order line, packing list and any issued proforma or invoice are marked *re-pricing required*,
  because $2.00 per tube is not $2.00 per carton;
- the revision history shows the change, who made it and why;
- a consignment dispatched before the edit still shows 200 shippers × 360 tubes.

## Data model (target)

| Entity | Change |
| --- | --- |
| `ProductPackingRevision` (new, append-only) | `ProductPackingId`, `RevisionNumber`, `Name`, `Description`, `PackPerShipper`, `BasePackingUomId`, `BasePackingQuantity`, unit rows (JSON: UoM id, name, symbol, quantity, order), computed `UnitsPerPack` (null when it cannot convert), `ChangedById`, `ChangedAt`, `Reason`. Unique (`ProductPackingId`, `RevisionNumber`). |
| `ProductPacking` | Gains `CurrentRevisionNumber`. Edits require a reason. |
| `ProductionOrderProducts` | Gains `OrderedBaseUnits` (decimal) as the quantity of record. `TotalOrderQuantity`, `Shippers` and `Loose` stop being stored truths: they are derived for display and the API. Drop them in a later migration once nothing reads them. |
| `ProductionOrderProductQuantity`, `AllocateProductQuantity` | `YieldQuantity` (base units) is the truth. `Quantity` (packs) and `UnitsPerPack` are derived while undispatched. |
| Dispatched line pin (on `AllocateProductQuantity`) | `DispatchedPackingRevisionId`, `DispatchedUnitsPerPack`, `DispatchedPacksPerShipper`, `DispatchedBaseUnits`, set in the dispatch transaction. |
| `FinishedGoodsTransferNote` | `PackingStyleSnapshot`, `PacksPerShipperSnapshot` and `QuantityPerPack` become "as transferred" history only. They are never a display or calculation source while the note has a linked style. They remain the fallback only for legacy notes with no `ProductPackingId`. |
| Re-pricing flag | `RepricingRequiredAt` and `RepricingRevisionId` on undispatched order lines, packing list lines and proforma lines, cleared by a finance action that re-confirms the price. |

## Single resolver

Backend: one function, `FinishedGoodsUnits.Layout(record)`, returns `{ revision, unitsPerPack,
packsPerShipper, styleName }`:

- **dispatched line:** its pin;
- **otherwise:** the linked style's current values through `PackConversion.UnitsPerPack`;
- **legacy record with no linked style:** the FGTN copy, labelled as such.

Every query, DTO, print and report goes through this function.

Desktop: one shared resolver with the same rule (`shared/packing`). The `?? snapshot` chains in
`fg-dispatch` and `production-orders` are removed.

## Invariants

- The sum of base units reserved, dispatched and on hand never changes because of a style edit.
- A style edit never changes a dispatched or delivered line.
- A style that cannot convert (its rows never reach PackPerShipper) cannot be saved while any undispatched
  stock or order uses it.
- Every style save writes exactly one revision in the same transaction, including a save that only
  changes the name.
- A units-per-pack change flags every affected undispatched line in the same transaction. Dispatch,
  invoice issue and commercial clearance check the flag.
- Base units on an order line are whole multiples of the current units per pack. If an edit makes them
  not whole, the line shows packs + leftover units and is flagged for re-pricing, never rounded.

## Permissions

Follow `docs/agent-guide.md`. Expected keys (confirm none exists first):

- **Editing a packing style:** keeps its existing keys, `CanEditProductionProductPackingStyles`,
  `CanEditQaProductPackingStyles` and `CanEditApprovedProductPackingStyle`, chosen as today. The reason
  becomes mandatory.
- **Style history:** one view key per product-page variant, following the existing Production/QA split
  (proposed `CanViewProductionProductPackingStyleHistory` and `CanViewQaProductPackingStyleHistory`).
  Granted to roles that hold the matching packing-style edit key today.
- **`CanConfirmFinishedGoodsRepricing`** (proposed): clear the re-pricing flag. Granted to roles holding
  `CanConfirmFinishedGoodsCosting`.

## Migration

1. Create revision 1 for every existing style from its current values, marked "baseline".
2. Backfill `OrderedBaseUnits` = `TotalOrderQuantity` × current units per pack. Log every line whose
   style cannot convert, with its order code, and leave it for manual correction. Never guess.
3. Pin already-dispatched lines from their recorded `UnitsPerPack` and the FGTN copy, which is the
   best evidence of what left, and mark the pin `legacy`.
4. Run `docs/diagnostics/diagnose_packing_consistency.sql` before and after, and attach the output to
   the PR.

## Tests

- A style regroup (18 → 360 per shipper, same units per pack) changes shipper counts on the FGTN, order,
  packing list, proforma and print at once. Base units are unchanged.
- A units-per-pack change flags the undispatched lines, and dispatch, invoice issue and clearance are refused
  until the flag is cleared.
- A dispatched consignment keeps its pinned layout after a later edit.
- Every save writes one revision, with the reason, the actor and before/after values. A revision cannot be edited or deleted.
- The Blopen example runs end to end: 200 shippers × 360 = 72,000 tubes on every screen.
- Permission deny and allow tests for each new key.
