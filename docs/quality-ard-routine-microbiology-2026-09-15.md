# Chemical, Microbial, Routine QC, and Water Coverage

Date: 2026-09-15. Status: implemented; migrations generated and not applied.

## Domain contract

`AnalysisType` is numeric: Chemical `0`, Microbial `1`. Commercial Material and
Product ARDs carry the type and their own worksheet. A verified
`MicrobialRequirement` decides whether raw/packaging material or a Finished product
requires the Microbial track. Chemical remains required. Material sampling and ATR
records snapshot the exact required ARD IDs so later configuration changes do not
rewrite work in progress.

`CommercialCoaItem` explicitly controls which worksheet fields appear on a COA.
Microbial ARDs require at least one controlled reportable item. Existing Chemical
ARDs with no explicit selection are backfilled from their worksheet fields for
compatibility. An immutable `CommercialCertificate` uses approved responses from the
same sample/ATR and is Combined only when both analysis types apply.

Routine QC uses `RoutineDefinition`, `RoutineExecution`, `RoutineSample`, and
`RoutineTrack`. Definitions are Monthly or Quarterly. Executions are Scheduled or
Event-triggered emergency records. Emergency work has no cadence and requires its
trigger and reason. Water samples require a sampling point and create Chemical and
Microbial tracks. Environmental observations require a room/area and create a
Microbial track. Each track freezes its ARD, form revision, and reportable rows.

Approved water certificates can govern a `WaterQualityPeriod` for one sampling point.
A period has `ValidFrom` and `ValidUntil`; coverage before certificate issue requires
a recorded QA reason. `WaterUseRecord` links actual use to either a production
batch/activity or an R&D trial batch. Holding a period also holds every linked use.
None of these operations changes the unlimited `RMP020` / `WATER-UNLIMITED` stock
batch or its Available status.

A Routine execution may link an R&D trial batch. Trial-batch completion is rejected
while any linked Water or Environmental execution is not Approved. Direct Chemical
or Microbial analysis of an R&D product sample remains a follow-up because an approved
R&D ARD/method source and exact sampling identity have not been agreed.

## API surface

### Routine QC

- `GET|POST /api/v1/qc/routines/ards`
- `GET|POST /api/v1/qc/routines/definitions`
- `GET|POST /api/v1/qc/routines`
- `GET /api/v1/qc/routines/{id}`
- `POST /api/v1/qc/routines/{id}/samples`
- `GET /api/v1/qc/routines/tracks/{id}`
- `POST /api/v1/qc/routines/tracks/{id}/submit-approval`
- `POST /api/v1/qc/routines/samples/{id}/certificate`
- `GET /api/v1/qc/routines/certificates/{id}`

The existing Form assignment/draft/finalization endpoints accept exact
`RoutineTrackId` binding. Material responses and assignees accept exact
`MaterialSamplingId` binding.

### Commercial microbial analysis and certificates

- `GET|POST /api/v1/qc/microbial-requirements`
- verification model types `RoutineArd = 5` and `MicrobialRequirement = 6`
- existing material/product ARD creation accepts `AnalysisType` and `CoaItems`
- `POST /api/v1/qc/commercial-certificates/materials/{materialSamplingId}`
- `POST /api/v1/qc/commercial-certificates/material-batches/{materialBatchId}`
- `POST /api/v1/qc/commercial-certificates/products/{analyticalTestRequestId}`
- `GET /api/v1/qc/commercial-certificates/{id}`

Certificate generation verifies aggregate readiness and stores rows immutably. It does
not grant release by itself. Existing status-changing COA submission routes were not
replaced.

### Water quality coverage

Quality Control navigation and generated role permissions include Routine Tests,
Routine Setup, Microbial Requirements, Water Quality, and Quality Certificates.

- `GET /api/v1/qc/water-quality/periods`
- `POST /api/v1/qc/water-quality/periods`
- `POST /api/v1/qc/water-quality/uses`
- `POST /api/v1/qc/water-quality/periods/{id}/hold`

## Workflow gates and audit

Targeted readiness checks cover material approval, ATR release/retest, production
stage completion, OOS disposition, and final response approval. A required Chemical
or Microbial track that is absent, pending, rejected, or bound to an ambiguous ARD
blocks the transition. Routine completion waits for every required response in the
execution. Routine creation, sampling, assignment/submission, approval, certificate
issue, water-period activation, and water holds append actor/time/detail evidence.

## Persistence and validation

Generated migrations:

1. `20260915154318_AddRoutineQcAnalysis`
2. `20260915180946_FinalizeRoutineWorksheet`
3. `20260915191439_AddCommercialMicrobialQualityAnalysis`
4. `20260915194108_AddWaterQualityCoverage`

The commercial migration backfills Routine certificate execution IDs and controlled
Chemical COA rows before making the new relationships authoritative. No migration was
applied to a user database during this task.

Focused tests validate scheduled and emergency routines, separate/combined routine
certificates, exact commercial sample responses, reportable COA selection, water
coverage and holds, and the R&D completion gate. The focused run passes 12 tests.

Role permission replacement also now removes dependent permission types and existing
claims before inserting the replacement set. This prevents EF claim-key conflicts
from returning HTTP 500 when a role's permissions are updated or cleared.

## Follow-up boundary

Scheduled definitions currently support explicit execution creation for a calendar
period; a hosted due-occurrence generator and overdue notifications are not included.
Direct R&D product-sample Chemical/Microbial ARDs and automatic water-use capture from
production consumption require their own approved domain mapping. Existing legacy COA
submission methods remain in place for their current callers; new callers should use
the explicit immutable certificate endpoints.

Existing material samples, ATRs, responses, and assignments are not heuristically
rewritten to select historical ARDs or sample identities. Legacy null-bound Chemical
work remains readable when its configuration is unambiguous. Ambiguous records fail
closed, and a historical record needs a separately reviewed, auditable reconciliation
before the new immutable combined-certificate endpoint can issue from it.
