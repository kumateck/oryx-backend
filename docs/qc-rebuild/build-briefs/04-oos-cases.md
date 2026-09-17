# Milestone 4: OosCase

Status: not started. Depends on Milestone 3
([03-test-requests-and-instances.md](./03-test-requests-and-instances.md)).

## Objective

Deliver the formal OOS/OOT workflow end to end: automatic detection on submission,
Phase 1 investigation, retest authorization (creating a linked `WorksheetInstance`
per Milestone 3's `RetestOfInstanceId`), QA disposition, and the real batch
quarantine/release side effects — reconciled against the live `OosInvestigation`
entity exactly as locked in
[lifecycle-and-governance.md](../lifecycle-and-governance.md#oos--oot--formal-workflow).
This milestone does not include `Coa` generation (Milestone 5) — a `TestRequest`
reaching `Released` after a closed `OosCase` is as far as this goes.

## Do-not-touch boundary, with one explicit, deliberate exception

New code only, under `DOMAIN.Entities.QcWorksheets`, `api/v{version}/qc/worksheets/...`,
`qc/worksheets/...`. Do not modify `OosInvestigation`, its controller, or its
repository — it stays live and untouched per the coexistence decision.

**The one exception**: `OosCase` disposition **does** write to `MaterialBatch.Status`
(Material) and `BatchManufacturingRecord.Status` (Product) — the same two fields
`OosInvestigation` already writes today. This is deliberate, not a coexistence
violation: batch status is shared, system-of-record state used across Warehouse/
Production/the rest of the ERP, not data owned by the old QC screens. A QC-only
shadow quarantine flag that production doesn't see would defeat the entire point of
quarantining a batch. Write only to these two existing `Status` fields (and
`MaterialBatch.DateRejected`, mirroring what `OosInvestigation.ReviewByQa` already
sets) — never modify either entity's schema, never touch any other
Material/Product-specification table.

**Verify before implementing, don't assume**: `BatchManufacturingRecord.Status` is
`BatchManufacturingStatus`, a different enum from `MaterialBatch.BatchStatus` — read
its actual values before writing the disposition logic; do not assume it has a
1:1 `Quarantine`/`Available`/`Rejected` mapping just because `MaterialBatch` does.

Same read-and-extend, not modify, principle applies to disposition's use of the
existing `Approval`/`IApprovalRepository` engine (Milestone 1's `QcApproval`
pattern) — call it, subclass its base types, never alter it.

## Corrections to prior milestones

Two real gaps, caught while designing this milestone, fixed at the source:

1. **`Specification` was missing a `RetestPolicy` field.**
   [lifecycle-and-governance.md](../lifecycle-and-governance.md#retest-sampling-policy)
   already locked "same-sample vs. fresh resample, configurable per Specification,"
   but [02-specifications.md](./02-specifications.md) never added the property. Add
   to `Specification`:
   ```
   RetestPolicy: enum { SameSample, FreshResample } (required, no default — every
       Specification must declare this explicitly, since the correct answer
       genuinely varies by test type and guessing wrong has real consequences)
   ```
   Update `domain-model.md` and `02-specifications.md`'s entity block and acceptance
   criteria (add: "Specification cannot be saved without RetestPolicy set") to
   match before or alongside implementing this milestone.

2. **`TestRequestSubject` had no link to the real batch entity** — only a
   `SubjectRef` string. Quarantine needs a real FK to act on. Add to
   `TestRequestSubject` (Milestone 3):
   ```
   MaterialBatchId: Guid?     // set when TestRequest.Type = Material
   BatchManufacturingRecordId: Guid?   // set when TestRequest.Type = Product
   ```
   Both null for `RoutineWater`/`RoutineEnvironmental` — those have no batch
   concept. Update `03-test-requests-and-instances.md`'s entity block and the
   `TestRequest` creation endpoint (resolving the right FK from whatever
   batch-picker the frontend uses) to match.

## Entities

```
OosCase : BaseEntity, IRequireApproval
  Approved: bool   // looked up via the shared QcApproval table (Milestone 1),
                    // EntityType = "OosCase" — set on disposition, not before
  WorksheetInstanceId: Guid (required)
  FieldKey: string (required)          // which Characteristic-bound Result field
                                        // failed — TestRequestSubjectId is derived
                                        // via WorksheetInstanceId, never stored
                                        // redundantly
  Status: enum { Open, InvestigationInProgress, RetestRequested,
      PendingQaDisposition, Closed }
  OpenedAt: DateTime (required)        // system-set at auto-creation, no "OpenedBy"
                                        // — this case wasn't opened by a person

  InvestigationDetails: string?
  RootCauseAnalysis: string?
  CorrectiveActions: string?
  PreventiveActions: string?
  InvestigatedById: Guid?
  InvestigatedAt: DateTime?

  RetestAuthorizedById: Guid?
  RetestAuthorizedAt: DateTime?
  RetestWorksheetInstanceId: Guid?     // the new instance created for the retest —
                                        // this is the FK Milestone 3's
                                        // WorksheetInstance.RetestOfInstanceId
                                        // points back from

  DispositionOutcome: enum? { ConfirmedOOS, Invalidated, RetestAccepted }
  DispositionById: Guid?
  DispositionAt: DateTime?
  DispositionComments: string?

  QuarantinedMaterialBatchId: Guid?           // set at Open, cleared at Close
  QuarantinedBatchManufacturingRecordId: Guid?
```

## Automatic detection (extends Milestone 3's submit endpoint)

`POST /{id}/submit` on `WorksheetInstanceController` (Milestone 3), after accepting
the submission, evaluates every `Result`-typed field against its bound
`Specification.Characteristic` (joined via `TestRequest.SpecificationId` ->
`Characteristic.SourceWorksheetTemplateId`/`SourceFieldKey` matching this
`WorksheetTemplateId`/`FieldKey`, and `SamplingPointGroupId` matching the
`TestRequestSubject`'s, when set):

```
LimitEvaluator.Evaluate(characteristic, submittedValue) -> Compliant | Alert | ActionOos
```

Grammar the evaluator must support, derived from the actual `ActionLimit`/
`AlertLimit`/`AcceptanceCriteria` text seen across every real document reviewed —
do not invent additional syntax beyond this:

- **`NMT <number> <unit>`** (Not More Than) — submitted numeric value must be
  `<= number`.
- **`NLT <number> <unit>`** (Not Less Than) — submitted numeric value must be
  `>= number`.
- **`<number>-<number> <unit>`** or **`<number> to <number> <unit>`** (range) —
  submitted numeric value must fall within, inclusive.
- **Exact qualitative match**, case-insensitive — anything that isn't one of the
  three numeric patterns above is treated as a literal string to match against the
  submitted value (e.g. `"Absence of E. coli in 1g"` — the submitted `Result` value
  must equal `"Absent"`/`"Absence"` after normalization; define the exact
  normalization rule, e.g. strip trailing context like "in 1g", lowercase, trim).
- **Unparseable/ambiguous limit text**: fail safe — flag the field for manual
  review rather than silently treating it as compliant. Log which
  `SpecificationCharacteristic` had unparseable text; this is a data-quality signal
  for whoever authored that Specification, not a silent pass.

Evaluate `AlertLimit` first if present: a breach of `AlertLimit` alone (not yet
`ActionLimit`) flags for trend review — no `OosCase`, no block. A breach of
`ActionLimit` (or of `AcceptanceCriteria` when no separate `ActionLimit` exists)
auto-creates an `OosCase` and blocks the `TestRequest` from reaching `Released`.

## Migration

`AddQcOosCases` — creates `OosCase`. Also includes the two corrections above:
adding `RetestPolicy` to `Specification` and `MaterialBatchId`/
`BatchManufacturingRecordId` to `TestRequestSubject` (these are alterations to
tables created in Milestones 2 and 3's migrations — if those migrations haven't
been applied to any real database yet, amend them directly instead of adding a
separate alter-table migration; check before choosing).

## Backend permission keys

Already defined — no new keys:

```
CanInvestigateQcOosCase, CanAuthorizeQcOosRetest, CanDispositionQcOosCase
```

## Backend endpoints

`OosCaseController` at `api/v{version}/qc/worksheets/oos-cases`:

| Verb | Route | Permission | Notes |
|---|---|---|---|
| GET | `/` | `CanInvestigateQcOosCase` | filter by Status |
| GET | `/{id}` | `CanInvestigateQcOosCase` | full detail incl. linked WorksheetInstance/TestRequestSubject/Specification context |
| POST | `/{id}/start-investigation` | `CanInvestigateQcOosCase` | `Open -> InvestigationInProgress`; also sets `QuarantinedMaterialBatchId`/`QuarantinedBatchManufacturingRecordId` and writes the real batch `Status` — this is where quarantine actually happens, not at auto-creation, since the analyst/reviewer needs a moment to confirm before locking a batch out of use system-wide |
| PUT | `/{id}/investigation` | `CanInvestigateQcOosCase` | body: `InvestigationDetails`, `RootCauseAnalysis`, `CorrectiveActions`, `PreventiveActions` — editable while `InvestigationInProgress` |
| POST | `/{id}/authorize-retest` | `CanAuthorizeQcOosRetest` | creates the new linked `WorksheetInstance` (same `WorksheetTemplateId`+pinned `Version`; new `TestRequestSubjectId` if `Specification.RetestPolicy = FreshResample` — creating that new Subject in the same `TestRequest` first — or the same `TestRequestSubjectId` if `SameSample`); sets `RetestWorksheetInstanceId` and the new instance's `RetestOfInstanceId`; `-> RetestRequested`. When that retest `WorksheetInstance` later reaches `Reviewed`, the system automatically transitions this case `-> PendingQaDisposition` and calls `IApprovalRepository.CreateApproval` for `modelType = "QcOosCase"` |
| POST | `/{id}/escalate` | `CanAuthorizeQcOosRetest` | alternative to retest — no new instance; `-> PendingQaDisposition` directly, calling `CreateApproval` immediately (same `modelType`) since there's no retest completion to wait for |
| POST | `/{id}/disposition` | `CanDispositionQcOosCase` | body: `Outcome` (`ConfirmedOOS`/`Invalidated`/`RetestAccepted`), re-auth credential, `DispositionComments`; the QC re-auth wrapper (Milestone 1) calling `ApproveItem`/`RejectItem` for `modelType = "QcOosCase"`, recording a `QcApproval` row (`EntityType = "OosCase"`) with `ReauthConfirmedAt`; **readiness check first**: reject if any other `WorksheetInstance` under the same `TestRequestSubjectId` is not `Reviewed`/`Locked` — same rule the live `OosInvestigation.ReviewByQa` already enforces; on success: `ConfirmedOOS` -> real batch `Status = Rejected` (+`DateRejected` for Material); `Invalidated`/`RetestAccepted` -> real batch `Status = Available`; either way `Status -> Closed`, `QuarantinedMaterialBatchId`/`QuarantinedBatchManufacturingRecordId` cleared |

## Frontend

Add to `src/lib/permission-keys/qc-worksheets.ts`:

```ts
oosCases: { investigate, authorizeRetest, disposition },
```

Routes:

```
qc/worksheets/oos-cases           list — Status/FieldKey/opened-date columns,
                                   filter by Status; QA Executive/QC Officer see
                                   Open/InvestigationInProgress cases they can act
                                   on, QA Manager/Deputy/Head see
                                   PendingQaDisposition cases awaiting them
qc/worksheets/oos-cases/[id]      detail: submitted value vs. Specification limit
                                   that triggered it, investigation fields (editable
                                   while InvestigationInProgress), retest-vs-escalate
                                   action buttons, and — once PendingQaDisposition —
                                   the disposition form (Outcome radio, Comments,
                                   re-auth). If a retest was authorized, shows a link
                                   to the retest WorksheetInstance alongside the
                                   original (never replacing it in the view)
```

Also update `qc/worksheets/review` (Milestone 3's "Awaiting My Review" queue): a
`WorksheetInstance` whose submission triggered an `OosCase` renders with a red flag
and a link into the case, per
[test-room-ux.md](../test-room-ux.md#reviewer-queue) — this was already specified
in Milestone 3's UX but the actual OOS flag/link only has something to point at
once this milestone exists.

## Acceptance criteria

1. **Auto-detection on ActionLimit breach**: Submit a `WorksheetInstance` with a
   `Result` field value that fails its Characteristic's `ActionLimit`. Verify an
   `OosCase` is auto-created with `Status = Open`, and the parent `TestRequest`
   cannot reach `Released`.
2. **Alert breach doesn't create a case**: Submit a value between `AlertLimit` and
   `ActionLimit`. Verify no `OosCase` is created, and the `TestRequest` is not
   blocked — only flagged for trend review.
3. **Quarantine on investigation start, not on auto-creation**: Verify the linked
   `MaterialBatch`/`BatchManufacturingRecord` status is unchanged immediately after
   auto-creation, and only transitions to a quarantine-equivalent status after
   `POST /{id}/start-investigation`.
4. **Retest policy drives Subject creation**: For a Specification with
   `RetestPolicy = FreshResample`, authorize a retest. Verify a new
   `TestRequestSubject` is created under the same `TestRequest`, with its own
   `CollectedAt`. For `RetestPolicy = SameSample`, verify the retest
   `WorksheetInstance` is created under the *same* `TestRequestSubjectId` as the
   original.
5. **Original result is never touched**: After authorizing and completing a
   retest, verify the original `WorksheetInstance`'s `FieldValues` are byte-for-byte
   unchanged, and `RetestOfInstanceId` on the new instance correctly points back.
6. **Readiness check blocks premature disposition**: With another
   `WorksheetInstance` under the same `TestRequestSubject` still `InProgress`,
   attempt `/disposition`. Verify rejection. Complete that other instance to
   `Reviewed`. Verify disposition now succeeds.
7. **Disposition outcome drives real batch status**: Dispose as `ConfirmedOOS`.
   Verify the real `MaterialBatch.Status` is `Rejected` and `DateRejected` is set.
   Repeat with `RetestAccepted` on a fresh case. Verify `Status` becomes `Available`.
8. **E-signature on disposition, via the centralized table**: Verify a `QcApproval`
   row exists for every disposition (`EntityType = "OosCase"`), with re-authentication
   required — not just the caller's ambient session — and that it's queryable from
   the same `qc/worksheets/approvals` screen used for STP/WorksheetTemplate
   approvals, not a separate OOS-only mechanism.
9. **Coexistence**: `OosInvestigation`'s existing screens and endpoints (the live
   `qa/oos-investigations` route) are completely unaffected — verify an unrelated
   `OosInvestigation` record's workflow still functions identically.
