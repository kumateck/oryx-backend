# Phase 3 — Rollout and Validation

Date: 2026-09-17. Status: designed now, execution gated on Phase 2 (implementation
architecture) being built. Nothing in this document runs until Phase 2 exists.

This is the plan for getting the Phase 2 architecture from built to live without
breaking in-flight QC work or losing audit continuity — in a GxP context, rollout of
a system that produces COAs, OOS dispositions, and e-signed records is itself a
validated activity, not just a deploy.

## Coexistence, not cutover

**Revised 2026-09-17**: the earlier version of this section assumed a hard
per-category cutover that froze the old system to read-only. That's no longer the
plan — the existing Material/Product/Packaging QC system turned out to be mature and
fully live (real controllers, real permission keys, real pages in production for
Specifications, STPs, ARDs, and `OosInvestigation`), not shelved prototype work like
the routine/water implementation. Replacing that outright, on a forced schedule,
carries more risk than the domain spec alone justifies.

Instead: **the new module launches purely additively, alongside the old one,
touching nothing that exists today.** New entities, new endpoints (a distinct route
namespace, not a replacement of existing controllers), new frontend routes (a
distinct area, not edits to `qc/material-specification`, `qc/product-specification`,
`qc/routines`, etc.). The old system keeps running exactly as it does now —
`OosInvestigation`, `MaterialSpecification`/`ProductSpecification`, STPs, ARDs, all
of it stays fully live and fully writable. Nothing freezes to read-only. See
[phase-2-implementation-architecture.md](./phase-2-implementation-architecture.md)
for how the new module is scoped to avoid any collision.

**Consolidation is a separate, later decision — not scheduled here.** Only once the
new module has run in production long enough to be trusted (see exit criteria below)
does the question of retiring, migrating, or permanently coexisting with the old
system even get asked. This document does not presuppose an answer.

## STP content migration

Unlike the rest of the new module, STPs have a concrete, already-known migration
source: the existing library of `.docx` STP documents (the numbered raw-material
library alone runs past 20 entries, plus a set per dosage-form folder). The DOCX
import tool (see
[phase-2-implementation-architecture.md](./phase-2-implementation-architecture.md#stp-docx-import))
is the intended path — batch upload, parsed into Draft `StandardTestProcedure`/
`StpStep` rows, every one still going through the full review/approval lifecycle
before it's `Effective`. This is content migration only: it populates the new
module's own tables from source documents, and is a different thing from the
coexistence/consolidation question above — it doesn't touch
`MaterialStandardTestProcedure`/`ProductStandardTestProcedure` at all, and doesn't
imply or require any decision about the old system's future.

## Phased proving sequence

Bringing the new module on for real work by test category, not big-bang, simplest
first — each phase must clear its own exit criteria (below) before the next one
opens. Because the old system keeps running throughout, "opening" a phase means new
work in that category *may* use the new module, not that it *must* — QA/QC staff can
keep using the old path for that category until confidence is established.

1. **Environmental Monitoring** — Microbial-only, single-track, simplest COA shape
   (no combination-hold logic to prove). Lowest blast radius if something's wrong,
   and it has no equivalent live system today to run alongside (Environmental was
   part of the shelved routine implementation) — so this is the cleanest first proof.
2. **Water** — dual-track (Chemical + Microbial), exercises the strict-hold
   combination rule and the `WaterQualityPeriod`/`WaterUseRecord` validity mechanics
   for the first time. Same as above — no live equivalent to coexist against, only
   the new module's own track record to build.
3. **Material / Product** — highest volume, most specifications to have authored by
   this point, and the category where the new `OOSCase` model runs directly
   alongside the live `OosInvestigation` it's meant to eventually replace (see
   [domain-model.md](./domain-model.md#oosCase-supersedes-the-live-oosinvestigation-entity)
   — "supersedes" there describes the *design* relationship; the live entity itself
   is untouched and keeps operating until a consolidation decision is made).

**Exit criteria per phase**: a defined number of consecutive real rounds processed
end-to-end — Configuration → Execution → Review → Release → COA — with zero
validation-severity deviations, before the next phase's category is opened to the
new module. Clearing exit criteria proves the new module for that category; it does
not by itself retire anything on the old side.

## IQ / OQ / PQ protocol

Full formal validation package, gated on Phase 2 existing:

- **URS traceability matrix.** Every locked rule in
  [lifecycle-and-governance.md](./lifecycle-and-governance.md) and
  [coa-engine.md](./coa-engine.md) becomes one or more numbered requirements, each
  mapped to the OQ script(s) that prove it. This matrix is the single artifact an
  auditor or inspector gets handed.
- **IQ (Installation Qualification)**: verifies the deployed environment matches
  spec — correct entities/migrations present, permission keys seeded exactly as in
  [permissions.md](./permissions.md), roles mapped exactly as in
  [roles-permission-matrix.md](./roles-permission-matrix.md).
- **OQ (Operational Qualification)**: one scripted test per locked rule. Examples,
  not an exhaustive list:
  - Create a `TestRequest` under Specification v3; approve Specification v4;
    confirm the `TestRequest` and its `WorksheetInstance`s stay on v3.
  - Submit a field value failing `ActionLimit`; confirm an `OOSCase` auto-opens and
    the linked batch quarantines.
  - Submit a field value between `AlertLimit` and `ActionLimit`; confirm it flags for
    trend review without opening an `OOSCase`.
  - Release only one track of a dual-Specification `TestRequest`; confirm no COA is
    issued (strict-hold combination rule).
  - Disposition an `OOSCase` as `RetestAccepted`; confirm the original
    `WorksheetInstance` is untouched and the retest is a separate linked record.
  - Attempt to start a worksheet with an expired-calibration Instrument field;
    confirm the hard block.
  - Approve a Water period retroactively; confirm the captured reason is mandatory
    and `WaterUseRecord`s in the backdated window are covered.
- **PQ (Performance Qualification)**: one full real-world representative round per
  test category, run by actual QC/QA staff in the target environment under
  production-like conditions — a real Environmental Monitoring round, a real Water
  round, a real Product Assay — proving the end-to-end flow works in practice, not
  only in isolated OQ scripts.
- **Validation summary report**, signed off, before any category's cutover is
  finalized.

## Training

Because the `WorksheetTemplate`/`Specification` separation is invisible at the UI
layer (see [specification-model.md](./specification-model.md#rendering-is-invisible-to-the-analyst)
and [test-room-ux.md](./test-room-ux.md)), end users don't need to be taught the
underlying data model at all. Training narrows to two real, previously-flagged
behavior changes plus role-specific paths:

- **OOS retest is now a separate linked `WorksheetInstance`**, not the same record
  reopened — QA staff used to the live `OosInvestigation` reopen-in-place pattern
  need this called out explicitly, not discovered mid-investigation.
- **OOS is now per-characteristic, not per-ATR/batch** — an `OOSCase` names exactly
  which test failed; a batch can have one characteristic under investigation while
  others already passed review.
- **Role-specific paths**, mapped directly from
  [roles-permission-matrix.md](./roles-permission-matrix.md): QC Officer on Test
  Room execution; QA Executive on review + investigation initiation; QC Manager on
  worksheet/specification authoring and program configuration; QA
  Manager/Deputy/Head on disposition and COA issuance.

## Rollback

Trivial under coexistence, since the old system never stopped running: if a category
surfaces a validation-severity defect in the new module, simply **stop directing new
work to it for that category** — QA/QC staff keep using the old path they were
already on, nothing to revert. Already-open new-module `TestRequest`s from that
category continue on the new path to completion; they aren't force-migrated back,
since reverting a partially-executed `WorksheetInstance` to the old model isn't a
defined operation.

## Open items

- Exact "N consecutive clean rounds" threshold per phase's exit criteria isn't set
  yet — depends on each category's real throughput once Phase 2 exists.
- The consolidation decision itself (retire the old system entirely, keep both
  permanently, or something in between) is explicitly not made here — it's a
  question for after the new module has a production track record, not before.
- The old system's data is unaffected by any of this — it keeps operating under
  whatever regulatory retention policy already governs it today, unchanged.
  those tables — not redefined here.
