# Brief 08 — Specification proposals: HTTP contract (backend)

Base route: `api/v1/qc/specification-proposals` (controller `QcSpecificationProposalController`).
JSON is camelCase. **Enums are serialized as numbers** (the API has no string-enum converter).
All endpoints require auth; `Sub` is the user id.

## Enums used

| Enum | Values |
|---|---|
| `ArdFamily` (`family`) | `ProductMicro = 1`, `EnvironmentalMonitoring = 3`, `PurifiedWater = 4` (others — Unknown 0, CultureMedia 2, CompletedCertificate 5 — are rejected on create) |
| `SpecificationProposalStatus` (`status`) | `Pending = 0`, `Applied = 1`, `Dismissed = 2` |
| `SpecificationAppliesTo` | `Product = 2`, `RoutineWater = 3`, `RoutineEnvironmental = 4` (RawMaterial 0, PackagingMaterial 1) |
| `SpecificationStage` | `Intermediate = 0`, `Bulk = 1`, `Finished = 2` |
| `QcRetestPolicy` | `SameSample = 1`, `FreshResample = 2` — no default, no 0 |
| `SpecificationAnalysisType` | `Chemical = 0`, `Microbial = 1` |
| `QcDocumentStatus` | `Draft 0`, `UnderReview 1`, `Approved 2`, `Effective 3`, `Superseded 4` |
| `ImportConfidence` | `High 0`, `Medium 1`, `Low 2` |

## Permission keys

| Key | Endpoints |
|---|---|
| `CanImportQcWorksheetTemplates` (existing) | `POST /` |
| `CanViewQcSpecificationProposals` | `GET /`, `GET /{id}` |
| `CanApplyQcSpecificationProposals` | `POST /draft`, `POST /apply` |
| `CanDismissQcSpecificationProposals` | `POST /{id}/dismiss` |

Catalog submodule for the three new keys: `"QC Specification Proposals"` (module `"Quality Control"`).

## Errors

Standard problem details; the frontend should read `errors[0].code`:

```json
{ "status": 400, "title": "Bad Request", "type": "...", "errors": [{ "code": "QcSpecificationProposal.TierConflict", "description": "..." }] }
```

Validation → 400, NotFound → 404, Conflict → 409. Model-binding failures (`[Required]` fields) return
the default ASP.NET 400 validation response.

## 1. `POST /` — store one file's proposal set

Called by the import screen after the file's template is saved, only when it has specification proposals.

Request (`CreateSpecificationProposalSetRequest`):

```json
{
  "family": 3,
  "sourceFileName": "EM Monitoring Worksheet (Tablet).docx",
  "worksheetTemplateId": "guid",
  "productName": "string | null",
  "specificationCode": "string | null",
  "specificationProposals": [ SpecificationCharacteristicProposal ],
  "samplingPointGroupProposals": [ SamplingPointGroupProposal ],
  "samplingPointCodes": ["SF-01", "..."]
}
```

`specificationProposals` / `samplingPointGroupProposals` are exactly the objects the import endpoint
already returns on `WorksheetImportProposal` (post them through unchanged):

- `SpecificationCharacteristicProposal`: `testName, analyte, acceptanceCriteria, printedCriteria, alertLimit, alertLimitSource, actionLimit, sourceFieldKey, sourceWorksheetTemplateId, groupName, stage, productName, specificationCode, confidence, location`
- `SamplingPointGroupProposal`: `name, acceptanceCriteria, printedPoints, pointCodes[], location`

For products, send `productName`/`specificationCode` from the first specification proposal's
`productName`/`specificationCode`.

Response 200: `SpecificationProposalSetDetailDto` (below).

Errors: `QcSpecificationProposal.FamilyNotSupported` (400), `QcSpecificationProposal.NoCharacteristics`
(400, empty `specificationProposals`), `QcWorksheetTemplate.NotFound` (404).

## 2. `GET /?status=&family=` — list

Both query params optional (numbers). Unpaginated, newest first.
Response 200: `SpecificationProposalSetSummaryDto[]`:

```json
{
  "id": "guid",
  "family": 3,
  "sourceFileName": "string",
  "worksheetTemplateId": "guid",
  "worksheetTemplate": { "id": "guid", "code": "EM-AIRBORNE-VIABLES", "name": "string", "version": 1, "category": 1, "status": 3 },
  "productName": "string | null",
  "specificationCode": "string | null",
  "status": 0,
  "characteristicCount": 3,
  "tierCount": 3,
  "appliedSpecificationId": "guid | null",
  "dismissReason": "string | null",
  "createdAt": "ISO date",
  "createdBy": { UserDto } 
}
```

`tierCount` = number of `samplingPointGroupProposals` (0 for products); `characteristicCount` =
number of `specificationProposals`.

## 3. `GET /{id}` — detail

Response 200: `SpecificationProposalSetDetailDto` = summary fields plus
`specificationProposals[]`, `samplingPointGroupProposals[]`, `samplingPointCodes[]`.
Errors: `QcSpecificationProposal.NotFound` (404).

## 4. `POST /draft` — build the draft plan (read-only)

Request: `{ "proposalSetIds": ["guid", ...] }` — all Pending, one family, one template; a product plan takes exactly one set.

Response 200: `SpecificationDraftPlan`:

```json
{
  "targetSpecificationId": "guid | null",
  "family": 3,
  "code": "string | null",
  "name": "string | null",
  "appliesTo": 4,
  "stage": null,
  "retestPolicy": null,
  "worksheetLinks": [ { "worksheetTemplateId": "guid", "analysisType": 1 } ],
  "characteristics": [
    {
      "testName": "Airborne viables",
      "analyte": null,
      "acceptanceCriteria": "NMT 100 cfu/4Hrs",
      "alertLimit": null,
      "actionLimit": "NMT 100 cfu/4Hrs",
      "samplingPointGroupId": "guid | null",
      "samplingPointGroupName": "Rooms",
      "sourceWorksheetTemplateId": "guid",
      "sourceFieldKey": "airborne_viables",
      "includeOnCoa": true,
      "displayOrder": 1,
      "groupName": "MICROBIAL"
    }
  ],
  "groups": [
    { "name": "Rooms", "description": "NMT 100 cfu/4Hrs", "samplingPointGroupId": "guid | null", "isNew": true, "pointCodes": ["SF-01"] }
  ],
  "warnings": [ { "code": "TierConflict", "message": "..." } ]
}
```

Plan contents by family:

- **ProductMicro**: `code` = set's `specificationCode`, `name` = `productName` (either may be null → reviewer fills),
  `appliesTo 2`, `stage 2`, one Microbial link, one row per proposal, `groupName "MICROBIAL"`, no tiers, `groups []`.
- **PurifiedWater**: `name "Purified Water"`, `code null`, `appliesTo 3`, `stage null`; one row per tier
  (`samplingPointGroupName` set) plus ungrouped pathogen rows (`samplingPointGroupName null`).
- **EnvironmentalMonitoring**: `name "Environmental Monitoring"`, `code null`, `appliesTo 4`, `stage null`, one row per tier.
  If a Draft/UnderReview EM Specification exists: `targetSpecificationId`, `code`, `name`, `worksheetLinks`
  come from it, and `characteristics` holds only tiers it doesn't already have (apply appends them).
- `retestPolicy` is **always null**; the reviewer must choose (required, no preselect).
- `samplingPointGroupId` on a row is filled when a live group of that name exists; `samplingPointGroupName`
  is what apply resolves. `groups[]` lists every tier (existing ones with `isNew: false` + id) with the point
  codes apply will assign.

Warning codes (`warnings[].code`):

| Code | Meaning |
|---|---|
| `TierConflict` | Same tier name with different limits across sets. All candidate rows are in `characteristics`; apply refuses until one row per `samplingPointGroupName` remains. (Rows that differ only because one has a suggested alert limit are merged, keeping the alert.) |
| `EffectiveEmSpecificationExists` | Only an Effective EM spec exists; plan is for a new one. Reviewer should instead create a new version of it and apply into that Draft. |
| `FieldNotOnTemplate` | A proposal's `sourceFieldKey` isn't on the pinned template version; that row was left out. |
| `TemplateNotEffective` | The linked template isn't Effective. Warning only. |

Errors: `QcSpecificationProposal.SetsRequired` (400), `.NotFound` (404), `.NotPending` (409),
`.MixedFamilies` (400), `.ProductSingleSet` (400), `.TemplateMismatch` (400), `QcWorksheetTemplate.NotFound` (404).

## 5. `POST /apply` — apply the reviewed plan (one transaction)

Request: `{ "proposalSetIds": ["guid", ...], "plan": SpecificationDraftPlan }` — `plan` is the reviewer-edited
draft plan, same shape as returned by `/draft` (`warnings` and `groups[].isNew` are ignored).

Response 200: `{ "specificationId": "guid" }` — a **Draft** Specification (new or the appended target).
Redirect to the Specification detail page.

Order: re-check sets Pending → retest-policy / tier / group checks → validate every point code → upsert
groups by name → assign points → create (or update target) through `SpecificationRepository` → mark sets Applied.

Errors (proposal-specific):

| Code | HTTP | When |
|---|---|---|
| `QcSpecificationProposal.PlanRequired` | 400 | `plan` missing |
| `QcSpecificationProposal.SetsRequired` / `.NotFound` / `.MixedFamilies` / `.ProductSingleSet` / `.TemplateMismatch` | 400/404 | as for `/draft` |
| `QcSpecificationProposal.NotPending` | 409 | a set is already Applied/Dismissed (double apply, concurrency) |
| `QcSpecification.RetestPolicyRequired` | 400 | `plan.retestPolicy` null (checked before anything is written) |
| `QcSpecificationProposal.TierConflict` | 400 | more than one row with the same `samplingPointGroupName` |
| `QcSpecificationProposal.GroupNotInPlan` | 400 | a row names a tier that is neither in `groups[]` nor an existing group |
| `QcSpecificationProposal.SamplingPointNotFound` | 400 | a `groups[].pointCodes` code has no live sampling point (create it first) |
| `QcSpecificationProposal.SamplingPointGroupChange` | 400 | a point is already in a different group (move it in master data) |

Plus any M2 Specification error passed through unchanged from create/update, e.g.
`QcSpecification.AppliesToRequired`, `.StageRequired`, `.StageNotApplicable`, `.CharacteristicFieldKeyNotFound`,
`.CharacteristicTemplateNotLinked`, `.LinkedTemplateNotFound`, `.EnvironmentalIsMicrobialOnly`,
`QcSpecification.NotFound` (404, target gone), `QcWorksheet.NotEditable` (target no longer Draft/UnderReview).
Missing `code`/`name` or row `testName`/`acceptanceCriteria`/`sourceFieldKey` fail model validation (400).

## 6. `POST /{id}/dismiss`

Request: `{ "reason": "string" }` (required, non-blank).
Response 200: `SpecificationProposalSetDetailDto` with `status 2` and `dismissReason`.
Errors: `QcSpecificationProposal.NotFound` (404), `.DismissReasonRequired` (400), `.NotPending` (409).
