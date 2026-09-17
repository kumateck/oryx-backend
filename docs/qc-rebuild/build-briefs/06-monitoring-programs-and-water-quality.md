# Milestone 6: MonitoringProgram + Water Quality

Status: not started. Depends on Milestone 2 ([02-specifications.md](./02-specifications.md),
for `SamplingPointGroup`), Milestone 3 ([03-test-requests-and-instances.md](./03-test-requests-and-instances.md)),
and Milestone 5 ([05-certificates.md](./05-certificates.md), Water period
activation is triggered by Coa issuance). Last milestone in the build sequence —
once this lands, every entity in [domain-model.md](../domain-model.md) exists.

## Objective

Deliver scheduled Routine testing end to end: `SamplingPoint` master data,
`MonitoringProgram` configuration, the daily due-date scan that auto-generates
`TestRequest`s (grouping same-day-due programs into one multi-subject round, not
one `TestRequest` per point), and Water validity periods
(`WaterQualityPeriod`/`WaterUseRecord`) with retroactive activation and hold
cascading.

## Do-not-touch boundary

New code only, under `DOMAIN.Entities.QcWorksheets`, `api/v{version}/qc/worksheets/...`,
`qc/worksheets/...`. Do not modify `RoutineDefinition`, `RoutineExecution`,
`RoutineSample`, `RoutineTrack`, or any other entity from the shelved routine
implementation — those migrations were never applied and hold no data, but the
*code* still exists and stays untouched, same as every other do-not-touch boundary
in this series. See
[README.md](../README.md#the-existing-materialproductpackaging-system-is-mature-not-shelved--and-the-new-module-coexists-with-it).

## Correction: MonitoringProgram is scheduling configuration, not an execution unit

"One point per program" (locked in Phase 1) is about *configuration* granularity —
each point's frequency, Specification, and pause state are independently
manageable. It does not mean one `TestRequest` per program. The real EM/Water
documents confirm one physical round routinely covers many points (60–90 rooms, 15+
water points) tested together. The daily scan (below) reconciles both: it groups
every `MonitoringProgram` due on the same day, sharing the same `Type` and
`SpecificationId`, into **one** `TestRequest` with one `TestRequestSubject` per
due program's `SamplingPoint` — matching Milestone 3's existing
`TestRequest`/`TestRequestSubject` shape exactly, no new structure needed.

## Correction to Milestone 3: TestRequestSubject needs a real SamplingPoint link

`TestRequestSubject.SubjectRef` (a free-text sampling-point code) has the same
typo/drift risk that `SamplingPointGroup` was introduced to prevent in Milestone 2.
Add:

```
TestRequestSubject.SamplingPointId: Guid?   // FK to SamplingPoint, RoutineWater/
    // RoutineEnvironmental only; auto-populated from the MonitoringProgram when
    // system-generated (below), optional when a Subject is added manually via
    // Milestone 3's POST /{id}/subjects (unscheduled routine testing can still
    // name a point that has no MonitoringProgram configured for it yet)
```

`SubjectRef` stays as the display string (mirrors `SamplingPoint.Code` when set),
since `TestRequestSubject` is also used for Material/Product Subjects that have no
`SamplingPoint` concept at all.

## Entities

```
SamplingPoint : BaseEntity
  Code: string (required, max 100, unique)
  Name: string (required, max 500)
  Area: string? (max 200)
  Type: enum { Water, Environmental }
  SamplingPointGroupId: Guid? (FK to SamplingPointGroup — drives which Alert/Action
      tier applies; a point can exist without a group only if its Specification's
      Characteristics don't use grouped limits)

MonitoringProgram : BaseEntity
  SamplingPointId: Guid (required)         // one point per program, locked
  SpecificationId: Guid (required)          // singular — matches TestRequest's own
      // design; the linked Specification's WorksheetLinks already determine
      // Chemical/Microbial/both, so MonitoringProgram doesn't track that separately
  Frequency: enum { Daily, Weekly, Monthly, Quarterly, Custom }
  CustomIntervalDays: int?    // required and only meaningful when Frequency = Custom
  LeadTimeDays: int (required, default 0)
  NextDueDate: DateTime (required)   // advanced at generation time, not completion
      // time — see the scan logic below
  Status: enum { Active, Paused }

WaterQualityPeriod : BaseEntity
  SamplingPointId: Guid (required)
  TestRequestSubjectId: Guid (required)     // the Subject whose Reviewed results
                                              // back this period
  ValidFrom: DateTime (required)             // = TestRequestSubject.CollectedAt,
                                              // always — see below
  ValidUntil: DateTime?                      // set only on activation
  RetrospectiveReason: string (required)     // always required, not conditional —
      // ValidFrom is always backdated relative to activation time given
      // incubation lag, so there is always something to justify
  Status: enum { PendingActivation, Active, Held, Expired }
  ActivatedById: Guid?
  ActivatedAt: DateTime?
  HeldById: Guid?
  HeldAt: DateTime?
  HoldReason: string?

WaterUseRecord : BaseEntity
  WaterQualityPeriodId: Guid (required)
  UsedAt: DateTime (required)
  BatchManufacturingRecordId: Guid?
  ProductionActivityStepId: Guid?
  RecordedById: Guid (required)
  Status: enum { Recorded, Held }
```

**`WaterUseRecord` creation is a manual entry endpoint in this milestone, not an
automatic production-triggered capture.** Automatic capture from production
consumption was explicitly deferred in
[deferred-and-next-steps.md](../deferred-and-next-steps.md) (carried forward from
the superseded routine implementation's own follow-up boundary) and stays deferred
here — this milestone doesn't attempt it.

## Daily due-date scan

A scheduled job (hosted background task — check the existing codebase for its
scheduled-job convention, e.g. Hangfire/Quartz/a hosted service, before adding a
new one):

1. Find every `MonitoringProgram` where `Status = Active` and
   `NextDueDate <= today + LeadTimeDays`.
2. Group them by `(Type derived from SamplingPoint.Type, SpecificationId)`.
3. For each group with no existing open `TestRequest` already covering today's
   occurrence: create one `TestRequest` (`ScheduleOrigin = Scheduled`,
   `SpecificationId` from the group), with one `TestRequestSubject` per program in
   the group (`SubjectRef`/`SamplingPointId` from each program's `SamplingPoint`,
   `SamplingPointGroupId` from that `SamplingPoint`).
4. Immediately advance each included program's `NextDueDate` by its `Frequency` —
   **at generation time, not completion time** — so the same or next day's scan run
   never creates a duplicate `TestRequest` for the occurrence just generated.

## Water period activation

On a `Coa` reaching `Issued` (Milestone 5) for a `RoutineWater` `TestRequest`: for
each `TestRequestSubject` in it, create a `WaterQualityPeriod` at
`Status = PendingActivation`, `ValidFrom = TestRequestSubject.CollectedAt`. This is
scaffolding only — it does not yet cover production use. Activation is a separate,
explicit action:

`POST api/v{version}/qc/worksheets/water-quality/periods/{id}/activate`
(`CanActivateQcWaterQualityPeriod`) — body: `RetrospectiveReason` (required). Sets
`ValidUntil` = the `SamplingPoint`'s `MonitoringProgram.NextDueDate` at the moment
of activation (dynamic, per
[lifecycle-and-governance.md](../lifecycle-and-governance.md#water-validity-periods)
— not a fixed duration), `Status = Active`, `ActivatedById`/`ActivatedAt`. This is
deliberately not automatic: someone confirms the retroactive coverage is justified
before production is told it can rely on this water for the backdated window.

`POST .../periods/{id}/hold` (`CanHoldQcWaterQualityPeriod`) — body: `HoldReason`
(required). `Status -> Held`. Cascades: every `WaterUseRecord` under this period
with `Status = Recorded` transitions to `Status = Held` — this is the flag; the
actual Quality Impact Assessment investigation process is out of scope for this
milestone (not modeled anywhere in this design — a QA process, not a QC data
question).

`POST .../uses` (`CanRecordQcWaterUse`) — body: `WaterQualityPeriodId`, `UsedAt`,
`BatchManufacturingRecordId`/`ProductionActivityStepId`. Rejected if the period is
not `Status = Active` — recording use against a `PendingActivation` or `Held`
period isn't meaningful.

## Migration

`AddQcWaterQualityCoverage` — creates `SamplingPoint`, `MonitoringProgram`,
`WaterQualityPeriod`, `WaterUseRecord`, plus the `TestRequestSubject.SamplingPointId`
alteration to Milestone 3's table.

## Backend permission keys

Already defined — no new keys:

```
CanViewMonitoringPrograms, CanCreateMonitoringProgram, CanEditMonitoringProgram,
CanPauseMonitoringProgram
CanViewQcWaterQualityPeriods, CanActivateQcWaterQualityPeriod,
CanHoldQcWaterQualityPeriod, CanRecordQcWaterUse
```

## Backend endpoints

`MonitoringProgramController` at `api/v{version}/qc/worksheets/monitoring-programs`:
plain CRUD (`GET /`, `GET /{id}`, `POST /`, `PUT /{id}`,
`POST /{id}/pause`, `POST /{id}/resume`), permission keys as above. No approval
lifecycle — `MonitoringProgram` is operational configuration, not a controlled
document.

`WaterQualityController` at `api/v{version}/qc/worksheets/water-quality`:

| Verb | Route | Permission | Notes |
|---|---|---|---|
| GET | `/periods` | `CanViewQcWaterQualityPeriods` | filter by Status, SamplingPointId |
| GET | `/periods/{id}` | `CanViewQcWaterQualityPeriods` | includes linked `WaterUseRecord[]` |
| POST | `/periods/{id}/activate` | `CanActivateQcWaterQualityPeriod` | see above |
| POST | `/periods/{id}/hold` | `CanHoldQcWaterQualityPeriod` | see above, cascades to `WaterUseRecord`s |
| POST | `/uses` | `CanRecordQcWaterUse` | manual entry, see above |

## Frontend

Add to `src/lib/permission-keys/qc-worksheets.ts`:

```ts
monitoringPrograms: { view, create, edit, pause },
waterQuality: { view, activatePeriod, holdPeriod, recordUse },
```

Routes:

```
qc/worksheets/monitoring-programs         calendar view (per test-room-ux.md):
                                           Overdue / Due Today / Due This Week /
                                           Completed groupings; create/edit form
                                           (SamplingPoint picker, Specification
                                           picker filtered to matching Type,
                                           Frequency, LeadTimeDays); Pause/Resume
qc/worksheets/water-quality                period list (Status/SamplingPoint/
                                           ValidFrom/ValidUntil columns), Activate
                                           (with RetrospectiveReason prompt) and
                                           Hold (with HoldReason prompt) actions;
                                           a linked use-record log per period, with
                                           Held records visually flagged
```

## Acceptance criteria

1. **Grouping, not one-TestRequest-per-program**: Configure 5 Active
   `MonitoringProgram`s, same `Type` and `SpecificationId`, all due today. Run the
   scan. Verify exactly one `TestRequest` is created, with 5 `TestRequestSubject`s —
   not 5 separate `TestRequest`s.
2. **NextDueDate advances at generation, not completion**: After the scan in (1),
   verify each program's `NextDueDate` has already advanced by its `Frequency`,
   before any `WorksheetInstance` under the generated `TestRequest` has been
   touched. Run the scan again the same day. Verify no duplicate `TestRequest` is
   created.
3. **SamplingPoint is dropdown-only on TestRequestSubject**: Verify the manual
   "add Subject" flow (Milestone 3) for `RoutineWater`/`RoutineEnvironmental`
   offers a `SamplingPoint` picker, not free text, when one is selected.
4. **Water period starts PendingActivation, not Active**: Issue a `Coa` for a
   `RoutineWater` `TestRequest`. Verify a `WaterQualityPeriod` is created at
   `Status = PendingActivation` with no `ValidUntil` set — production cannot yet
   rely on it.
5. **Activation requires a reason and sets a dynamic ValidUntil**: Attempt
   `/activate` with no `RetrospectiveReason`. Verify rejection. Retry with one.
   Verify `ValidUntil` equals the `SamplingPoint`'s `MonitoringProgram.NextDueDate`
   at that moment, not a fixed offset from `ValidFrom`.
6. **Hold cascades to use records**: Record two `WaterUseRecord`s against an
   `Active` period. Hold the period. Verify both records transition to
   `Status = Held`.
7. **Use recording blocked outside Active**: Attempt `POST /uses` against a
   `PendingActivation` period. Verify rejection. Attempt against a `Held` period.
   Verify rejection.
8. **Coexistence**: `qc/routines/water-quality` and every other existing `qc/routines/*`
   page still loads and functions unchanged.

## This closes the build-brief set

Every entity in [domain-model.md](../domain-model.md) now has a corresponding
build brief, and both spikes
([00-formula-engine-spike.md](./00-formula-engine-spike.md),
[00-stp-docx-parser-spike.md](./00-stp-docx-parser-spike.md)) are run and resolved.
Nothing stands between this and starting Milestone 1.
