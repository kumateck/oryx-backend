# Oryx QC Rebuild

Date: 2026-09-17. Status: Phase 1 (domain spec) closed out. Phase 2 (implementation
architecture) designed, with all six milestone build briefs + two spikes written in
[build-briefs/](./build-briefs/) — nothing implemented yet. Phase 3
(rollout/validation) designed, execution gated on Phase 2 actually being built.
Phase 4 (operate & improve) deliberately not planned yet — depends on data Phase 3
hasn't produced.

## What this is

A ground-up rebuild of Oryx's QC modules, covering Worksheet Templates, Specifications,
Test Requests/ARDs, Test Execution, Quality Review (including OOS/OOT), and COA
generation for both Material/Product testing and Routine testing (Water, Environmental
Monitoring).

- **Phase 1 — Domain spec** (closed out): domain model, lifecycle rules, field
  catalog, governance decisions, roles→permission matrix. This is the bulk of this
  folder.
- **Phase 2 — Implementation architecture** (designed, not built): see
  [phase-2-implementation-architecture.md](./phase-2-implementation-architecture.md).
  Purely additive — new entities, endpoints, and frontend routes under a distinct
  namespace, coexisting with the existing system rather than replacing it.
  **All implementation is done by coding agents (Claude Code, Codex), not human
  developers filling gaps with judgment** — so the architecture above is turned into
  self-contained, unambiguous task specs in [build-briefs/](./build-briefs/) before
  any of it gets built. Start there, not here, when actually implementing.
- **Phase 3 — Rollout and validation** (designed, not executed): see
  [rollout-and-validation.md](./rollout-and-validation.md). Cutover strategy, phased
  go-live sequence, full IQ/OQ/PQ protocol, training. Execution is gated on Phase 2
  existing.

## The existing Material/Product/Packaging system is mature, not shelved — and the new module coexists with it

**Revised 2026-09-17.** Phase 2 planning surfaced something Phase 1 undersold: the
"regular" Material/Product/Packaging QC path — `MaterialSpecification`,
`ProductSpecification`, `MaterialStandardTestProcedure`/`ProductStandardTestProcedure`,
`MaterialAnalyticalRawData`/`ProductAnalyticalRawData`, `OosInvestigation` — is a
mature, full-stack, **live** system: real controllers
(`MaterialSpecificationController`, `ProductSpecificationController`,
`OosInvestigationController`, etc.), a granular permission-key catalog already
matching the "one key per action" convention almost exactly
(`src/lib/permission-keys/quality-control.ts` and `quality-assurance.ts` — assign,
reassign, request-retest, complete-retest, verify, all per Material/Product/Packaging
already), and real production pages under `qc/material-specification`,
`qc/product-specification`, `qc/material-stp`, `qc/analytical-raw-data`, etc. This is
not comparable to the shelved routine/water implementation below.

Given that, **the new module does not replace, migrate, or freeze any of this.** It
launches as a purely additive system — new entities, a distinct API route namespace,
distinct frontend routes — running alongside the existing one with zero modification
to it. The old system keeps operating exactly as it does today, fully live and fully
writable, for as long as it takes to build confidence in the new one. See
[phase-2-implementation-architecture.md](./phase-2-implementation-architecture.md)
for how the new module's namespacing avoids any collision, and
[rollout-and-validation.md](./rollout-and-validation.md#coexistence-not-cutover) for
why the earlier cutover-and-freeze plan was replaced with this. Consolidating,
retiring, or permanently keeping both systems is an explicitly deferred decision —
not made until the new module has a real production track record.

## Relationship to `OosInvestigation` (live, applied)

**`OOSCase` (see [domain-model.md](./domain-model.md)) is the design successor to
it — but per the coexistence decision above, `OosInvestigation` itself is untouched
and keeps operating.** Its migration (`20260627101100_AddOosAndSpecificationReference`)
is applied and live, unlike the shelved routine/water implementation below. It's
per-ATR/per-batch with binary disposition and reopens the same record for a retest.
`OOSCase` moves to per-`FieldKey` granularity with three-way disposition, but carries
forward its batch quarantine/release mechanics and its pre-approval readiness check,
which were real gaps in the original design. See
[lifecycle-and-governance.md](./lifecycle-and-governance.md#oos--oot--formal-workflow)
for the full reconciliation and the retest-mechanism change this means for QA staff
whenever the new module eventually reaches parity and adoption.

## Relationship to `quality-ard-routine-microbiology-2026-09-15.md`

**This supersedes it.** That document (2026-09-15, status "implemented; migrations
generated and not applied") describes a real but never-applied implementation of
Routine QC (`RoutineArd`/`RoutineDefinition`/`RoutineExecution`/`RoutineTrack`),
Commercial microbial analysis (`CommercialCoaItem`/`CommercialCertificate`), and Water
Quality Coverage (`WaterQualityPeriod`/`WaterUseRecord`). Because its migrations were
never applied to a database, nothing live is broken by superseding it.

Several of its mechanics were independently re-derived and confirmed correct in this
spec — the Combined-COA-only-if-both-tracks rule, and water periods' retroactive
validity with a recorded QA reason and holds cascading to linked uses — so they carry
forward as validated design, just against the new independent entity model rather than
the Form-coupled one that implementation used. Its explicitly named gaps (no
independent worksheet-template entity, no cross-worksheet referencing, no formal OOS
workflow, no Alert/Action tiers, no Media Qualification worksheet category, no full
meaning-of-signature, no hosted due-occurrence generator) are what this rebuild adds.

Its R&D trial-batch gate (trial completion blocked while any linked Water/Environmental
execution isn't Approved) and its explicitly flagged follow-up boundary
(automatic water-use capture from production consumption is unmapped) are carried
forward as open items for this rebuild too — see [deferred-and-next-steps.md](./deferred-and-next-steps.md).

## How this design was built

1. Review of the existing `oryx-backend` QC-adjacent entities — three parallel,
   duplicated ARD → COA-item → Certificate implementations (Material, Product,
   Routine), and `MaterialSpecification`/`ProductSpecification` owning their worksheet
   content directly rather than referencing reusable templates.
2. A structured design discussion working through domain model, lifecycle,
   governance rules (version pinning, OOS/OOT, retest policy, permissions,
   e-signatures, instrument/reagent gating, COA combination, water validity).
3. Review of ~20 real analytical worksheets, COAs, and media-qualification records
   from the lab's own paper process — Environmental Monitoring, Purified Water,
   culture-media growth-promotion worksheets, and finished-product chemical assay
   worksheets — which grounded the field catalog and surfaced mechanics (Alert/Action
   tiers, field Mode, multi-analyte specs, per-worksheet cross-references) that
   weren't otherwise obvious.

## Contents

- [domain-model.md](./domain-model.md) — core entities and relationships
- [lifecycle-and-governance.md](./lifecycle-and-governance.md) — the locked lifecycle/governance rules, OOS case flow
- [field-catalog.md](./field-catalog.md) — worksheet field types, Mode, what the real worksheets confirmed
- [specification-model.md](./specification-model.md) — Test × Analyte, Alert/Action tiers, point-group tiers
- [coa-engine.md](./coa-engine.md) — test categories → COA generation rules
- [test-room-ux.md](./test-room-ux.md) — analyst-facing execution UX
- [permissions.md](./permissions.md) — permission-key list
- [roles-permission-matrix.md](./roles-permission-matrix.md) — which real job roles (QC Officer, QA Executive, QC Manager, QA Manager, Deputy QA Manager, QA Head) get which permission keys
- [phase-2-implementation-architecture.md](./phase-2-implementation-architecture.md) — Phase 2: backend entities/migrations/controllers, frontend routes, namespacing that avoids the existing system
- [rollout-and-validation.md](./rollout-and-validation.md) — Phase 3: coexistence strategy, phased proving sequence, IQ/OQ/PQ protocol, training
- [build-briefs/](./build-briefs/) — self-contained, agent-ready task specs (exact entity code shapes, endpoint contracts, acceptance criteria) that turn Phase 2's architecture into what a coding agent actually builds from
- [deferred-and-next-steps.md](./deferred-and-next-steps.md) — explicitly out of scope for this pass, and what comes next

A rendered version of this spec is also published as a shareable doc:
https://claude.ai/artifact/1teWkaxLhgoShp6XDqxob3
