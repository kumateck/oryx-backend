# Milestone 5: Coa

Status: not started. Depends on Milestone 2
([02-specifications.md](./02-specifications.md)) and Milestone 3
([03-test-requests-and-instances.md](./03-test-requests-and-instances.md)).
Milestone 4 ([04-oos-cases.md](./04-oos-cases.md)) is a soft dependency: a
`TestRequest` with an open `OosCase` cannot reach the state this milestone requires
to issue, so build order doesn't strictly require Milestone 4 to exist first, but
testing this milestone properly does.

## Objective

Deliver `Coa` end to end: generation once every required `WorksheetInstance` for
every `TestRequestSubject` in a `TestRequest` is `Reviewed`, the certificate viewer,
issuance, and append-only revision. Covers both certificate shapes — "Certificate of
Analysis" (Material/Product/Water, has batch/expiry dates) and "Environmental
Monitoring Report" (Environmental, no batch concept) — from one engine.

## Do-not-touch boundary

New code only, under `DOMAIN.Entities.QcWorksheets`, `api/v{version}/qc/worksheets/...`,
`qc/worksheets/...`. Do not modify `CommercialCertificate`, `CommercialCoaItem`,
`RoutineCertificate`, or any existing certificate-generation code. See
[README.md](../README.md#the-existing-materialproductpackaging-system-is-mature-not-shelved--and-the-new-module-coexists-with-it).

## Correction to domain-model.md and coa-engine.md

Both previously described `Coa` as binding to "1, or 2 `TestRequestId`s" for a
combined certificate. That predates Milestone 3's actual design: a
`Specification`'s Chemical + Microbial `WorksheetLinks` already resolve into
`WorksheetInstance`s under the **same** `TestRequest`, per `(Subject ×
WorksheetLink)`. A combined certificate never spans two `TestRequest`s — `Coa`
binds to exactly one. Both docs are already corrected; this brief is built against
the corrected version.

## Entities

```
Coa : BaseEntity
  TestRequestId: Guid (required)
  CertificateCode: string (required, max 100, unique)
  CertificateShape: enum { CertificateOfAnalysis, EnvironmentalMonitoringReport }
      // derived from TestRequest.Type at generation time (RoutineEnvironmental ->
      // EnvironmentalMonitoringReport, everything else -> CertificateOfAnalysis)
      // but stored, not recomputed, so a later Type taxonomy change can't silently
      // reshape an already-issued certificate
  SupersedesId: Guid? (FK to Coa, self-referencing)
  Status: enum { Draft, Issued, Superseded }
  IssuedAt: DateTime?
  IssuedById: Guid?
  Rows: List<CoaRow>

CoaRow
  CoaId: Guid (required)
  TestRequestSubjectId: Guid (required)   // which Subject this row is for --
      // Material/Product normally has one Subject so one implicit row-group;
      // Water/EM rounds have many, one row-group per point/room, matching the
      // real EM/Water COAs reviewed (one row per room)
  SpecificationCharacteristicId: Guid (required)
  DisplayLabel: string (required, max 255)   // snapshotted from Characteristic.TestName
      // at generation time -- never joins live to Specification, since the
      // Specification could be superseded after this Coa is issued and the
      // certificate must keep reading exactly as issued
  GroupName: string? (max 200)               // snapshotted from Characteristic.GroupName
  DisplayOrder: int
  AcceptanceCriteria: string (required)      // snapshotted, not joined live
  ResultValue: string (required)             // snapshotted from the resolved
      // WorksheetInstance.FieldValue at generation time
  Complies: bool (required)                  // from the LimitEvaluator result
      // (Milestone 4) at generation time
```

**Every `CoaRow` field that comes from `Specification`/`WorksheetInstance` is
snapshotted at generation time, not live-joined.** This is deliberate: `Coa` is a
regulatory document — once issued, its content must never change because someone
later edits the Specification or a WorksheetInstance's stored representation
changes. Revision (below) is the only path to a changed certificate, and it's
explicit and versioned.

## Generation trigger

Not a user action — computed automatically. On every `WorksheetInstance` transition
to `Reviewed` (Milestone 3's `POST /{id}/review`), after the transition commits,
check: are all `WorksheetInstance`s under this instance's `TestRequestSubjectId`
now `Reviewed`? If yes, and the `TestRequest`'s other Subjects (if any) are also
fully reviewed, and there is no `Open`/`InvestigationInProgress`/`RetestRequested`/
`PendingQaDisposition` `OosCase` anywhere in the `TestRequest`, and every Subject's
required `WorksheetInstance`s per the linked `Specification.WorksheetLinks` are
present and `Reviewed` (the strict-hold combination check) — generate a `Coa` at
`Status = Draft`. This is a check run after every relevant review action, not a
polling job.

## Backend permission keys

Already defined — no new keys:

```
CanIssueQcCertificate, CanReviseQcCertificate, CanViewQcCertificate
```

## Backend endpoints

`CoaController` at `api/v{version}/qc/worksheets/certificates`:

| Verb | Route | Permission | Notes |
|---|---|---|---|
| GET | `/` | `CanViewQcCertificate` | filter by Status, CertificateShape, date range |
| GET | `/{id}` | `CanViewQcCertificate` | full detail: header block (below) + `Rows[]` grouped by `GroupName`/`TestRequestSubjectId`, ordered by `DisplayOrder` |
| POST | `/{id}/issue` | `CanIssueQcCertificate` | `Draft -> Issued`; sets `IssuedAt`/`IssuedById`; no re-auth wrapper here (issuance isn't itself an Approval-chain action — the `WorksheetInstance` reviews that gated generation already went through the full re-auth'd Approval flow in Milestone 3) |
| POST | `/{id}/revise` | `CanReviseQcCertificate` | body: `Reason` (required); creates a new `Coa` (`Status = Draft`, `SupersedesId = {id}`) recomputing `Rows[]` fresh from current data (a retest completed, a data-entry correction was made); on that new `Coa`'s own `/issue`, the original transitions `Issued -> Superseded` |

No create/edit endpoints — `Coa` is never authored directly, only generated and
revised.

## Header block rendering

Computed at generation time and snapshotted onto `Coa` itself (not re-derived on
every view — same "never changes after issuance" principle as `CoaRow`):

```
CertificateOfAnalysis shape:
  Certificate Code, Product/Material Name, Batch Number (TestRequestSubject.SubjectRef),
  Specification Code + Version, Manufacturing Date, Expiry Date (Material/Product
  Subject fields), Sample Date (CollectedAt), Test Completion Date (latest
  WorksheetInstance Reviewed timestamp among this Subject's instances)

EnvironmentalMonitoringReport shape:
  Report Code, Area/Room, Specification Code + Version, Sample Date, Test
  Completion Date -- no Manufacturing/Expiry Date fields at all, not blank ones
```

## Frontend

Add to `src/lib/permission-keys/qc-worksheets.ts`:

```ts
certificates: { issue, revise, view },
```

Route:

```
qc/worksheets/certificates            list — CertificateCode/Product-or-Area/
                                       Status/IssuedAt columns
qc/worksheets/certificates/[id]       viewer: rendered header + Rows grouped by
                                       GroupName (CertificateOfAnalysis) or by
                                       Subject (EnvironmentalMonitoringReport, one
                                       section per room/point, matching the real EM
                                       COA's per-room layout); Issue button when
                                       Draft; Revise button (with mandatory Reason)
                                       when Issued; a superseded certificate shows a
                                       persistent "SUPERSEDED" banner and a link
                                       forward to whatever superseded it
```

## Acceptance criteria

1. **Auto-generation on full review, not before**: Create a `TestRequest` with a
   Specification requiring both tracks, one Subject. Review the Chemical
   `WorksheetInstance`. Verify no `Coa` exists yet. Review the Microbial one.
   Verify a `Coa` is now generated at `Status = Draft`.
2. **Strict hold blocks generation while an OosCase is open**: Same setup, but the
   Chemical result fails `ActionLimit` (auto-creating an `OosCase` per Milestone 4).
   Review the Microbial instance. Verify no `Coa` is generated while the `OosCase`
   remains open, even though every `WorksheetInstance` is `Reviewed`. Close the
   `OosCase` (any disposition). Verify `Coa` generation now proceeds.
3. **Multi-subject round produces one Coa, many row-groups**: Create an
   Environmental `TestRequest` with 5 Subjects (rooms). Review all their
   `WorksheetInstance`s. Verify exactly one `Coa` is generated, with `Rows[]`
   spanning all 5 `TestRequestSubjectId`s, not 5 separate certificates.
4. **Snapshotting, not live-join**: Generate a `Coa`. Afterward, create a new
   version of the underlying `Specification` and change a `Characteristic`'s
   `AcceptanceCriteria` text. Verify the already-generated `Coa`'s `CoaRow.AcceptanceCriteria`
   is unchanged — it still reads exactly as it did at generation time.
5. **Revision supersedes, never overwrites**: Issue a `Coa`. Revise it. Verify the
   original is now `Superseded` (only after the *new* one is issued, not
   immediately on `/revise`), remains fully retrievable, and the new one's
   `SupersedesId` points back correctly.
6. **Environmental shape has no batch dates**: Generate a `Coa` for a
   `RoutineEnvironmental` `TestRequest`. Verify the header has no
   Manufacturing/Expiry Date fields present at all (not empty strings — the fields
   don't exist in that shape's rendering).
7. **Coexistence**: Existing `qc/commercial-certificates` and `qc/routines/certificates`
   pages still load and function unchanged.
