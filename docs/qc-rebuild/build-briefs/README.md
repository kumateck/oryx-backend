# Build Briefs

Date: 2026-09-17. Status: **complete** — two spike briefs and all six milestone
briefs written. Every entity in [domain-model.md](../domain-model.md) has a
corresponding build brief. **Both spikes have been run and resolved** — the
formula engine (build new, not `FormulaWorksheetPreprocessor`) and the STP DOCX
parser (go, 8/8 real files parsed cleanly, three real corrections found). Nothing
has been implemented yet — that's the only thing left.

## Why this folder exists

**All implementation of this module is done by coding agents (Claude Code, Codex),
not by a human developer using judgment to fill gaps.** Everything else in
`docs/qc-rebuild/` (domain model, lifecycle rules, field catalog, Phase 2
architecture) is *product* documentation — it explains *why* the system is shaped
the way it is, and a human reader is expected to connect it to actual code. A coding
agent given only that documentation and told "implement Milestone 1" would have to
make dozens of small unstated decisions — exact property types, exact DTO shapes,
exact file paths, exact test scenarios — and different agents, or the same agent on
different days, would make them differently.

A build brief closes that gap. Each one is **self-contained**: an agent should be
able to open a single brief, with no other context, and implement exactly what it
describes without needing to ask a clarifying question or invent a convention the
rest of the codebase doesn't already use. Every brief:

- States its **objective** and **prerequisites** (what must already exist).
- Gives **exact entity definitions** — C# property lists with types and
  nullability, not prose descriptions of "an entity with roughly these fields."
- Gives **exact endpoint contracts** — route, HTTP verb, request/response DTO
  shapes, permission attribute.
- Gives **exact frontend routes and component boundaries**.
- Gives **acceptance criteria** written as concrete scenarios an agent can verify
  against, each tied back to the specific locked rule in
  [lifecycle-and-governance.md](../lifecycle-and-governance.md) it proves.
- States explicit **do-not-touch boundaries** — which existing files/tables/routes
  must not be modified, restating the coexistence rule from
  [README.md](../README.md) at the point where an agent could plausibly get it
  wrong (e.g. "add a migration" is exactly the kind of instruction an agent could
  misapply to an existing `DbContext` if not told which one).

## Numbering

`00-*` briefs are spikes — small, bounded investigations that must run before or
alongside the first milestone, because they resolve a real unknown rather than
implement a known design. `01-*` onward are milestone briefs, in the build order
from [phase-2-implementation-architecture.md](../phase-2-implementation-architecture.md#suggested-build-sequence-within-phase-2).

## Contents

- [00-formula-engine-spike.md](./00-formula-engine-spike.md) — **resolved**:
  `FormulaWorksheetPreprocessor` is not reusable (`internal`, and a thin adapter to
  an external formula microservice, not a local evaluation core) — build a new
  small local evaluator instead. Formula syntax defined in
  [field-catalog.md](../field-catalog.md).
- [00-stp-docx-parser-spike.md](./00-stp-docx-parser-spike.md) — **resolved, go**:
  8/8 real files (6 raw-material, 2 finished-product) parsed cleanly. Three real
  corrections found: the header metadata lives in a Word running header part
  (`word/headerN.xml`), not the document body; no space between section number and
  heading word; must search full joined text, not match line-by-line.
- [01-stp-and-worksheet-templates.md](./01-stp-and-worksheet-templates.md) —
  Milestone 1: `StandardTestProcedure`/`StpStep` and `WorksheetTemplate`/
  `WorksheetSection`/`WorksheetField`, plus a corrected architectural foundation:
  QC approvals reuse the codebase's existing generic `Approval`/`IRequireApproval`
  engine (not a bespoke audit table — an earlier draft invented one, `QcESignature`,
  before the real system was found) via one shared, centralized `QcApproval` table
  across every QC entity, wrapped with QC-specific re-authentication. Every later
  milestone reuses this pattern. No dependency on either spike above — can start
  immediately, in parallel with them.
- [02-specifications.md](./02-specifications.md) — Milestone 2: `Specification`/
  `SpecificationWorksheetLink`/`SpecificationCharacteristic`, plus a new
  `SamplingPointGroup` master-data entity introduced here (ahead of Milestone 6's
  `MonitoringProgram`, which will reference the same table) because Specification
  needs it first. Depends on Milestone 1.
- [03-test-requests-and-instances.md](./03-test-requests-and-instances.md) —
  Milestone 3: `TestRequest`/`TestRequestSubject`/`WorksheetInstance`/
  `WorksheetFieldValue` — the execution layer (Test Room, assignment enforcement,
  header rendering, ReferencedResult resolution, hard instrument/reagent gates).
  Introduces `TestRequestSubject` as an explicit child entity — a real structural
  fix, since `TestRequest` originally only had a scalar `SubjectRef` despite Phase 1
  prose already describing multi-subject rounds. Reuses the existing `QcEquipment`
  entity for instrument calibration data (read-only) rather than duplicating it.
  Depends on Milestones 1 and 2.
- [04-oos-cases.md](./04-oos-cases.md) — Milestone 4: `OosCase`, the formal OOS
  workflow — automatic ActionLimit/AlertLimit detection (with an explicit
  NMT/NLT/range/qualitative-match grammar), Phase 1 investigation, retest
  authorization, QA disposition. Writes to the real `MaterialBatch.Status`/
  `BatchManufacturingRecord.Status` fields as a deliberate, explicit exception to
  the do-not-touch rule — quarantine has to have real system-wide effect. Fixed two
  retroactive gaps in Milestones 2 and 3 while writing this one: `Specification`
  was missing `RetestPolicy`, and `TestRequestSubject` was missing real batch FKs.
  Depends on Milestone 3.
- [05-certificates.md](./05-certificates.md) — Milestone 5: `Coa`/`CoaRow` —
  automatic generation once every `WorksheetInstance` in a `TestRequest` is
  `Reviewed` and no `OosCase` is open, snapshotted (never live-joined) rows so an
  issued certificate can't silently change if the Specification is later edited,
  append-only revision. Corrected a stale "1, or 2 `TestRequestId`s" framing in
  `domain-model.md`/`coa-engine.md` to match Milestone 3's actual design — a
  combined certificate binds to exactly one `TestRequest`, since Chemical+Microbial
  `WorksheetInstance`s already live under the same round. Depends on Milestones 2
  and 3; Milestone 4 is a soft dependency (open `OosCase`s block generation).
- [06-monitoring-programs-and-water-quality.md](./06-monitoring-programs-and-water-quality.md) —
  Milestone 6: `SamplingPoint`/`MonitoringProgram`/`WaterQualityPeriod`/
  `WaterUseRecord`. Reconciled "one point per program" (configuration granularity)
  against the real one-round-many-points execution pattern — the daily due-date
  scan groups same-day-due programs sharing Type+Specification into one
  multi-subject `TestRequest`, not one per program. Introduces `SamplingPoint` as
  proper master data (was only ever a loose string) and retroactively links
  `TestRequestSubject` to it, same pattern as Milestone 4's batch-FK fix. Water
  periods start `PendingActivation` on Coa issuance and require an explicit,
  reasoned `/activate` action before production can rely on them. Depends on
  Milestones 2, 3, and 5.
- [07-worksheet-docx-import.md](./07-worksheet-docx-import.md) — the worksheet DOCX
  importer. It turns ARD Word files into Draft templates, plus SamplingPoint and
  Specification-characteristic proposals. It rests on a raw-XML analysis of all 49 files
  in the lab's microbiology corpus. The EM/water room lists become SamplingPoints, not
  template rows. Phase A adds the one missing model piece (Select options storage) and
  formalizes the existing fixed-column contract. Depends on M1–M6, all merged. The
  Specifications work follows it.

## Status

M1–M6 are complete and merged (backend into `desmond-latest`, frontend into
`final-for-live`). Brief 07 is the next build.
