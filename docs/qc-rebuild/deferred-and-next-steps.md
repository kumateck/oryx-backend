# Deferred Items and Next Steps

## Closed out in this pass

- **Implementation architecture (Phase 2)** — see [phase-2-implementation-architecture.md](./phase-2-implementation-architecture.md). Designed as purely additive, coexisting with the existing live Material/Product/Packaging system rather than replacing it.
- **Roles → permission-key matrix** — see [roles-permission-matrix.md](./roles-permission-matrix.md).
- **Shared Drive review** — the folder is fully reviewed. Beyond what's in
  [field-catalog.md](./field-catalog.md), the pass over `ARD and SPECS` → `Raw
  Material Spec - STPs` and `QC Filled ARD` surfaced `StandardTestProcedure` as a
  distinct entity (see [domain-model.md](./domain-model.md#standardtestprocedure-unifies-existing-per-materialper-product-stp-entities))
  and confirmed "ARD" in the lab's own vocabulary refers to the Analytical Raw Data
  worksheet record itself, matching the `TestRequest`/`WorksheetInstance` split
  already modeled. `COMPLEX FORMULARS` (multi-active worksheets) and the remaining
  per-material STP folders were sampled but not exhaustively read — they follow the
  same STP/worksheet patterns already confirmed and are unlikely to change the model
  further; worth a scan during implementation if a specific material's method looks
  unusual.
- **BMR/BPR dosage-form survey** — checked real BMR/BPR document pairs across
  every dosage form actually manufactured (Beta Capsules, Beta Tablet, Non-Beta
  Tablet, Ointment/Cream, Liquid Syrup, Liquid Suspension, Dry Powder for
  Suspension, Sachet) to confirm how `Specification.Stage` maps to real
  production gating — see [domain-model.md](./domain-model.md#specification).
  Every product gates on exactly two stages (Finished plus whichever of
  Intermediate/Bulk matches its manufacturing method), never one, never three.

## Explicitly out of scope for this pass

- **Actual code.** Phase 2 is an architecture design, not a diff — no entities,
  migrations, controllers, or frontend routes have been written yet.
- **The formula/calculation engine spike.** [phase-2-implementation-architecture.md](./phase-2-implementation-architecture.md#open-implementation-decision-formulacalculation-engine)
  names the approach (reuse `FormulaWorksheetPreprocessor`'s evaluation core) but the
  spike to confirm its API surface actually generalizes hasn't been done.
- **Phase 4 (Operate & Improve)** — deliberately not designed; depends on live data
  Phase 3 hasn't produced yet.

## Carried forward from `quality-ard-routine-microbiology-2026-09-15.md`

These were named in the superseded implementation's own follow-up boundary and
haven't been re-litigated in this pass — they still apply to the new model:

- **R&D trial-batch gate**: a Routine execution may link an R&D trial batch; trial
  completion should stay blocked while any linked Water or Environmental execution is
  not Approved.
- **Hosted due-occurrence generator and overdue notifications** for scheduled
  `MonitoringProgram`s are not yet designed beyond the daily due-date scan described
  in [coa-engine.md](./coa-engine.md) — that scan creates the `TestRequest`, but
  proactive overdue alerting is unaddressed.
- **Automatic water-use capture from production consumption** — i.e. `WaterUseRecord`
  creation triggered directly by a production batch/activity drawing water, rather
  than recorded after the fact — requires its own approved domain mapping into
  production workflows and was explicitly deferred by the prior implementation too.
- **Direct R&D product-sample Chemical/Microbial ARDs** — an approved R&D ARD/method
  source and exact sampling identity for R&D samples hasn't been agreed.

## Next step

Domain spec (Phase 1), architecture (Phase 2), and rollout plan (Phase 3) are all
designed — there's still nothing to run or test, since no code has been written.
Phase 2 architecture design is the reference for actually starting to build:
`StandardTestProcedure` + `WorksheetTemplate` first, per the suggested build sequence
in [phase-2-implementation-architecture.md](./phase-2-implementation-architecture.md#suggested-build-sequence-within-phase-2).
