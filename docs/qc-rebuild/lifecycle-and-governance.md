# Lifecycle and Governance Rules

Ten decisions, each locked against a specific real-world failure mode rather than a
default guess. See [domain-model.md](./domain-model.md) for the entities these apply to.

## Edit-triggers-versioning

Applies to all three entities that carry the Draft→UnderReview→Approved→Effective→
Superseded lifecycle: `StandardTestProcedure`, `WorksheetTemplate`, `Specification`.

- **While `Status = Draft`** (this is version 1 and has never been Approved/
  Effective): editing is in-place, no confirmation, no new version spawned. This is
  ordinary drafting and mistake-correction, not a controlled-document change.
- **While `Status = UnderReview`**: editing is also in-place — but any edit resets
  `Status` back to `Draft`, since a reviewer must not be evaluating a moving target.
  This is the same principle as `qc.worksheet.returnForCorrection` in spirit: a
  substantive change means review starts over.
- **Once `Status = Effective` (or the record is `Superseded` — viewed as history)**:
  there is a real, previously-approved controlled version in force. Editing it
  requires an **explicit "Create New Version" confirmation** — never a silent new
  draft. Only after confirming does a new `Draft` (version N+1) get created; the
  `Effective` version is untouched and stays in force until the new draft completes
  its own full approval cycle. This is deliberate: someone looking at a live,
  approved document should never be able to start typing and unknowingly spawn a
  new version — the action of creating one must be its own decision.

## Version pinning

A `TestRequest` and each of its `WorksheetInstance`s stay permanently on the
`Specification`/`WorksheetTemplate` version active when created. No in-flight
upgrades, even for non-breaking template revisions — the ARD alone must fully
reconstruct what ran, with no cross-referencing of change logs needed. If Specification
v3 changes to v4 after an in-flight `TestRequest` was created under v3, that
`TestRequest` and its `WorksheetInstance`s stay on v3 forever, even after v4 is
approved.

## OOS / OOT — formal workflow

Any field value failing its `Specification.Characteristic` (against `ActionLimit`, or
the plain acceptance criteria for non-tiered tests) auto-creates an `OOSCase` and
blocks release.

```
WorksheetInstance field submitted
        |
   Resolve against Specification.Characteristics
        |
   Within limits? --Yes--> Result stands, flows to COA
        |
       No
        |
   OOSCase created (auto)
   Status: Open
   Blocks: TestRequest cannot reach Released; COA cannot be issued
        |
   Phase 1 Investigation (lab error check)
        |
   +----+----+
   |         |
Lab error   No lab error found
found       confirmed
   |         |
Retest       Escalate to
(new linked  Phase 2 /
WorksheetInstance,  QA Investigation
original result
stays visible,
never overwritten)
   |         |
   +----+----+
        |
   QA Disposition
   Confirmed OOS | Invalidated (original) | Retest result accepted
        |
   OOSCase: Closed
        |
   TestRequest unblocks -> Released -> COA reflects disposed result
```

A retest never overwrites the original `WorksheetInstance` — it's a new instance,
linked by `RetestOfInstanceId`, and the disposition record says which one the COA is
allowed to draw from. Both stay visible in the trace.

**Supersedes the live `OosInvestigation` entity** (migration
`20260627101100_AddOosAndSpecificationReference`, applied — not shelved). That
implementation is per-`AnalyticalTestRequest`/per-`MaterialBatch` with binary
disposition (`QaApproved` | `PermanentlyRejected`) and reopens the same ATR to
`Testing` for a retest rather than linking a new record. `OOSCase` moves to
per-`FieldKey` granularity and a three-way disposition — deliberately, because one
Specification can carry 9+ Characteristics and a single failing one shouldn't read as
"the whole ATR is OOS." Two mechanics from the live system carry forward as-is
because they were real gaps in the original design: opening an `OOSCase` quarantines
the linked batch (`BatchStatus.Quarantine`); closing it releases
(`Invalidated`/`RetestAccepted` → `Available`) or rejects (`ConfirmedOOS` → `Rejected`)
it. The live system's pre-approval readiness check (required analysis must be
complete before an investigation can close favorably) also carries forward.

The move away from reopen-in-place to a separately linked retest instance is a real
workflow change for QA staff used to the live pattern, not just an implementation
detail — flag it explicitly in training/rollout material.

## Alert vs. Action limits

An `AlertLimit` breach (real pattern confirmed against Environmental Monitoring and
Water COAs — e.g. general rooms at Alert 80 / Action 100 CFU/4Hrs vs. a Dispensing
Booth at Alert 3 / Action 5) flags for trend review without blocking the COA or
triggering the OOS workflow above. Only an `ActionLimit` / hard-acceptance-criteria
breach triggers formal OOS.

## Assignment enforcement

A `WorksheetInstance` has exactly one assignee at a time. Only that user may start
it, enter or edit `FieldValues`, or submit it — enforced server-side (the permission
layer rejects the action for anyone else), not merely hidden from other users in the
Test Room UI. This is what makes an entry attributable in the GxP sense: `EnteredBy`
on a `FieldValue` is only ever the person actually permitted to be doing the work at
that moment, never inferred or defaulted.

Reassignment is a separate, audited action from initial assignment — distinct
permission key, distinct record of who reassigned, from whom, to whom, and when (see
[permissions.md](./permissions.md)). It changes who may act next; it never rewrites
`EnteredBy` on values already entered under the previous assignee, and it never
resets the `WorksheetInstance`'s `Status`.

## Retest sampling policy

Same-sample retest vs. mandatory fresh resample is **configurable per Specification**
— it genuinely varies by test type in real labs (chemical assay retests are often
same-sample; microbial often needs a fresh sample).

## COA combination

For a `Specification` requiring both Chemical and Microbial: **strict hold**. No
interim single-track COA is issued — not even a Chemical-only one marked
"interim" — until *both* required `TestRequest`s reach `Released`. Environmental
Specifications only ever require Microbial, so they're never gated on a second track.
See [coa-engine.md](./coa-engine.md) for the full generation rules.

## Water validity periods

Dynamic `ValidUntil` (tied to that sampling point's next scheduled
`MonitoringProgram` test — not a fixed duration, so a delayed next round needs to
actively flag the old period as no-longer-current rather than silently keep covering
production). Retroactive `ValidFrom` from the sample date once a passing result is
approved (covering the incubation-wait gap), with the reason captured for audit —
carrying forward the intent of the existing `RetrospectiveReason` field. On
failure/hold, every `WaterUseRecord` in that window auto-flags for Quality Impact
Assessment — nothing depends on someone remembering to check.

## Instrument / Reagent / Reference Standard gating

A worksheet field of type Instrument/Reagent/ReferenceStandard checks
calibration/expiry status at use-time; an expired one **hard-blocks** starting or
submitting that test, not just a warning. Prevents invalid data at the source rather
than catching it at review.

## Electronic signatures

Full meaning-of-signature: re-authentication (password/PIN) + a captured meaning
("Reviewed by," "Approved by," "Disposition authorized by") + timestamp on every
submit/review/approve/disposition action, plus a mandatory reason-for-change on any
amendment. Matches 21 CFR Part 11 expectations.

**Implementation reuses the codebase's existing generic `Approval`/
`ResponsibleApprovalStage`/`IRequireApproval` engine** (already used by the Form
module's `Response`/`ResponseApproval`) for the workflow mechanics — stage routing,
multi-approver chains, the unified pending-approvals inbox — rather than a bespoke
QC-only audit table. The one gap in the existing engine is re-authentication: its
generic `ApproveItem`/`RejectItem` endpoints only require a valid session. QC wraps
it with its own re-auth layer to meet the bar above, recording the result in one
shared `QcApproval` table (deliberately centralized across every QC approval point,
not one table per entity) — see
[build-briefs/01-stp-and-worksheet-templates.md](./build-briefs/01-stp-and-worksheet-templates.md)
for the exact mechanism. This was a real correction made mid-build: an earlier draft
invented a parallel `QcESignature` audit table before the existing engine was found.

## Permission granularity

One permission key per state-machine transition, following the project's existing
convention (every action/view gets its own dedicated permission key, never shared).
See [permissions.md](./permissions.md) for the full key list.
