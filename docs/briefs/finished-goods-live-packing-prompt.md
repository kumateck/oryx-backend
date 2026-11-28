# Agent prompt: live packing style, base-unit quantities, style history

Copy everything below the line into a new agent session started in the ERP folder that holds
`oryx-backend` and `oryx-live-desktop`.

---

You are implementing the finished goods packing rule agreed on 2026-11-28. Read these before
writing anything:

1. `oryx-backend/docs/finished-goods-live-packing.md`: the rule, target data model, invariants,
   permissions, migration and tests. It is the specification; follow it exactly.
2. `oryx-backend/docs/agent-guide.md` and `oryx-live-desktop/AGENTS.md`: branch, permission,
   migration and test rules. Every new key needs its constant, a single registration, `[Authorize]`,
   a grant migration, catalogue regeneration and deny/allow tests.
3. `oryx-backend/docs/finished-goods-workflow.md`: the workflow this change sits in.

## The problem you are fixing

A Blopen Gel order showed 200 shippers, but its packing list showed 10 shippers for the same 3,600 packs.
"Packs per shipper" is read from three places:
- the current style (`FinishedGoodsUnits` in `APP/Utils/PackConversion.cs`);
- the FGTN copy (`PacksPerShipperSnapshot`, preferred by the desktop's
  `apps/web/src/modules/fg-dispatch/schemas/dispatch-schema.ts`);
- the order line's stored `Shippers`/`Loose`.

Packing style saves (`ProductRepository.CreateProductPacking`) overwrite in place with no history.

## The rule, short form

- Base units (tube, bottle, tablet) are the only quantity of record.
- Until dispatch, every record (FGTN, stock, order, packing list, proforma, invoice, loading, costing,
  prints) shows packs and shippers from the packing style **as it is now**. An update changes them all
  at once.
- Every style save writes an append-only revision: who, when, reason, and the full style.
- Dispatch pins the revision, units per pack, packs per shipper and base units on each line. Dispatched
  and delivered records read only the pin.
- If a save changes **units per pack**, flag every undispatched line and document on that style as
  *re-pricing required*. Invoice issue, commercial clearance and dispatch are refused until finance
  clears the flag. A regroup that keeps units per pack the same needs no flag.

## Work order

Work in stages. Each stage is one backend PR (branch from `demo`, PR into `desmond-latest`), plus its
desktop PR if it has UI (branch from `development`, PR into `development`). Do not stack PRs.

**Stage A: style history**
- `ProductPackingRevision` entity, configuration and migration, with baseline revision 1 for every
  existing style.
- `CreateProductPacking` writes a revision in the same transaction for every created or changed style,
  and requires a reason on edits.
- A history endpoint with the view keys described in the spec.
- Desktop: a reason field in the packing style editor, and a history panel on the product page.

**Stage B: one resolver**
- `FinishedGoodsUnits.Layout(...)`: the dispatched pin, else the current style, else the legacy FGTN copy.
- Route every DTO, query, print (`FinishedGoodsWorkflow.Print.cs`), recall and correction path
  through it. FGTN snapshot columns are history only.
- Desktop: one resolver in `apps/web/src/shared/packing`. Remove the snapshot-first chains in
  `fg-dispatch/schemas/dispatch-schema.ts`, `fg-dispatch/components/use-priced-lines.ts`,
  `production-orders/schemas/order-schema.ts` and `shared/packing/fg-quantity.ts`.
- Packing list batch matching (`production-orders/components/packing-list-dialog.tsx`) compares by
  packing style id, never by a per-shipper number.

**Stage C: base units on orders**
- Add `OrderedBaseUnits` to order lines and backfill it (packs × current units per pack). Log lines
  that cannot convert; never guess.
- Packs, shippers and loose become derived values. Keep the old columns readable for one release, then remove them.
- Creating, editing, fulfilling and allocating all work in base units. Packs are entered by users and
  converted at the boundary through `PackConversion`.

**Stage D: dispatch pin and re-pricing**
- Pin columns on dispatched lines, written in the dispatch transaction. Backfill legacy dispatched lines
  from their recorded `UnitsPerPack` and the FGTN copy, marked `legacy`.
- When a style save changes units per pack: set `RepricingRequiredAt` and `RepricingRevisionId` on
  affected undispatched order lines, packing list lines and proforma lines.
- Gates: invoice issue, commercial clearance and dispatch check the flag.
- A finance action, under its own key, re-confirms the price and clears the flag.
- Desktop: a "Re-pricing required" badge, the blocked actions with an explanation, and a warning in the
  style editor listing the open orders an edit will affect.

## Rules you must keep

- Never change base-unit balances because of a style edit. Never round entered quantities.
- Never edit an applied migration. Keep `dotnet ef migrations has-pending-model-changes` clean.
- Never `git add -A` in the backend main checkout; work in a worktree.
- Re-check permissions immediately before every write (`CurrentPermissionSet.Load` and
  `usePermissionAction(...).check()`).
- Desktop: strict TypeScript, no `any`/`unknown`, quantities through `shared/units/quantity.ts`, page
  files at most 200 lines.

## Done means

- The tests in the spec's "Tests" section pass. That includes the Blopen example: 200 shippers × 360 =
  72,000 tubes on the order, FGTN, packing list, proforma, invoice and print.
- `docs/diagnostics/diagnose_packing_consistency.sql` returns no rows in sections 1–3 on the testing
  database after the migration. Attach its before and after output to the PR.
- Each PR lists its keys, the endpoints that use them, the roles granted by migration, and the matching
  desktop PR.
- `docs/finished-goods-live-packing.md` has its status changed to implemented, with the PR links.
