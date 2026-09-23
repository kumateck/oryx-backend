# Milestone 2: Specification

Status: not started. Depends on Milestone 1
([01-stp-and-worksheet-templates.md](./01-stp-and-worksheet-templates.md)) —
`WorksheetTemplate` and `WorksheetField` must exist, since `Specification`
characteristics reference them by `FieldKey`.

## Objective

Deliver `Specification` end to end: backend entity, migration, endpoints, and the
frontend builder — a structured table/list editor, not a drag-and-drop canvas (see
below for why those are different UI shapes for a reason). Includes a new
`SamplingPointGroup` master-data entity, introduced here because Specification
needs it before `MonitoringProgram` (Milestone 6) exists.

This milestone does **not** include `TestRequest`, `WorksheetInstance`, or COA
generation — those are later milestones that read `Specification` but aren't built
here.

## Do-not-touch boundary

Same as Milestone 1: new code lives under `DOMAIN.Entities.QcWorksheets`,
`api/v{version}/qc/worksheets/...`, `qc/worksheets/...`. Do not modify
`MaterialSpecification`, `ProductSpecification`, or anything else in the existing
live system. See
[README.md](../README.md#the-existing-materialproductpackaging-system-is-mature-not-shelved--and-the-new-module-coexists-with-it).

## Why the builder is a table, not a canvas

Milestone 1's `WorksheetTemplate` builder is drag-and-drop because an author is
*composing* new typed fields from a palette. A `Specification` doesn't compose
anything new — it maps existing `WorksheetField`s (already defined, already have a
`FieldKey`) to acceptance criteria. That's an add/edit/remove-row table, each row
picking from dropdowns sourced from already-approved `WorksheetTemplate`s, not a
canvas. Building this as a canvas would be over-engineering a data-entry form into a
composition tool it doesn't need to be.

## SamplingPointGroup — new master-data entity

```
SamplingPointGroup : BaseEntity
  Name: string (required, max 200, unique)   // e.g. "General Rooms", "Dispensing Booth"
  Description: string?
```

Selected via dropdown wherever `Characteristic.SamplingPointGroup` is set — never
free text — specifically because a typo here silently applies the wrong Alert/
Action tier, which is the one thing this field exists to prevent. Milestone 6's
`MonitoringProgram` will add a FK to this same table; nothing here depends on
Milestone 6.

## Entities

```
Specification : BaseEntity
  Code: string (required, max 100)
  Name: string (required, max 500)
  AppliesTo: enum { RawMaterial, PackagingMaterial, Product, RoutineWater,
      RoutineEnvironmental }   // split Material into RawMaterial/PackagingMaterial
      // to match the live system's existing separation (quality-control.ts keys
      // rawMaterialSpecifications/packagingMaterialSpecifications separately) —
      // caught late, see build-briefs/04-oos-cases.md's corrections pattern
  Stage: enum? { Intermediate, Bulk, Finished }   // required only when
      // AppliesTo = Product, null otherwise — carries forward
      // AnalyticalTestRequest.Stage / ProductSpecification.TestStage from the live
      // system, missed in the original design until flagged
  Version: int (required, default 1)
  SupersedesId: Guid? (FK to Specification, self-referencing)
  EffectiveDate: DateTime?
  Status: enum { Draft, UnderReview, Approved, Effective, Superseded }
  RetestPolicy: enum { SameSample, FreshResample } (required, no default — added
      retroactively, see build-briefs/04-oos-cases.md; the OOS retest flow reads
      this to decide whether a retest reuses the same TestRequestSubject or creates
      a new one)
  WorksheetLinks: List<SpecificationWorksheetLink>
  Characteristics: List<SpecificationCharacteristic>

SpecificationWorksheetLink : BaseEntity
  SpecificationId: Guid (required)
  WorksheetTemplateId: Guid (required)
  AnalysisType: enum { Chemical, Microbial }
  // Application-layer rule, not a DB constraint: at most one Chemical and one
  // Microbial link per Specification. AppliesTo = RoutineEnvironmental only ever
  // has a Microbial link (enforce at validation, not schema).

SpecificationCharacteristic : BaseEntity
  SpecificationId: Guid (required)
  TestName: string (required, max 200)
  Analyte: string? (max 200)
  AcceptanceCriteria: string (required, max 2000)   // free text, e.g. "95.0-105.0%",
                                                     // "Absence of E. coli in 1g"
  AlertLimit: string? (max 500)                      // structured only where it
                                                       // needs to be evaluated
                                                       // programmatically (see below)
  ActionLimit: string? (max 500)
  SamplingPointGroupId: Guid? (FK to SamplingPointGroup)
  SourceWorksheetTemplateId: Guid (required, must be one of this Specification's
      own WorksheetLinks — validated at the application layer, not just referential)
  SourceFieldKey: string (required, must exist on SourceWorksheetTemplateId's
      current version)
  IncludeOnCoa: bool (required, default true)
  DisplayOrder: int (required)
  GroupName: string? (max 200)    // COA section heading, e.g. "CHEMICAL"/"MICROBIAL"
```

**On `AlertLimit`/`ActionLimit` being strings, not numbers**: real acceptance
criteria aren't always a clean numeric threshold — "NMT 100 CFU/4Hrs" and "Absence
of E. coli" are both valid `ActionLimit` values, and the OOS engine (Milestone 4)
needs to evaluate a submitted `Result` field's value against this text. That
evaluation logic — parsing "NMT 100" into a comparable threshold vs. matching
"Absence"/"Presence" — is Milestone 4's concern, not this one's; this milestone only
stores the text. Do not attempt to build limit-parsing logic here.

**A `SourceWorksheetTemplateId`/`SourceFieldKey` pair may appear on more than one
`SpecificationCharacteristic`** within the same Specification — this is how one EM
test (e.g. Airborne Viables) gets two rows with different `SamplingPointGroupId`
and different Alert/Action tiers, both sourcing the same `WorksheetField`.

## Migration

`AddQcSpecifications` — creates `SamplingPointGroup`, `Specification`,
`SpecificationWorksheetLink`, `SpecificationCharacteristic`, in that order.

## Backend permission keys

Already defined in [permissions.md](../permissions.md) and
[phase-2-implementation-architecture.md](../phase-2-implementation-architecture.md) —
no new keys needed for `Specification` itself:

```
CanViewQcSpecifications, CanCreateQcSpecification, CanEditQcSpecification,
CanApproveQcSpecification, CanSupersedeQcSpecification
```

Add one new key for the master-data entity, following the same naming convention:

```
CanManageSamplingPointGroups   // single key, not view/create/edit/approve/supersede
                                // split — this is plain reference-data maintenance,
                                // not a controlled document with its own lifecycle;
                                // granted to QC Manager, matching monitoringProgram's
                                // .create/.edit tier in roles-permission-matrix.md
```

## Backend endpoints

`SpecificationController` at `api/v{version}/qc/worksheets/specifications` — same
verb/lifecycle shape as Milestone 1's `StandardTestProcedureController` (`GET /`,
`GET /{id}`, `POST /`, `PUT /{id}`, `POST /{id}/create-new-version`,
`POST /{id}/submit-for-review`, `POST /{id}/approve`, `POST /{id}/make-effective`,
`POST /{id}/supersede`), no import endpoint (nothing to migrate — specifications
weren't a `.docx` library). Request/response DTOs carry `WorksheetLinks[]` and
`Characteristics[]` instead of `Steps[]`/`Sections[]`.

Two additional endpoints specific to authoring:

| Verb | Route | Permission | Purpose |
|---|---|---|---|
| GET | `/{id}/available-fields` | `CanEditQcSpecification` | Given the Specification's current `WorksheetLinks`, returns every `WorksheetField` (Label, FieldKey, Type, Unit) from those templates' current *Effective* versions — powers the SourceFieldKey dropdown; only `Result`-typed and `CalculatedValue`-typed fields are realistic candidates, but return all fields and let the frontend filter, since a future field type might also warrant COA inclusion |

`SamplingPointGroupController` at `api/v{version}/qc/worksheets/sampling-point-groups`:
plain CRUD (`GET /`, `GET /{id}`, `POST /`, `PUT /{id}`, `DELETE /{id}`), all four
behind `CanManageSamplingPointGroups`. No lifecycle — this is reference data, not a
controlled document.

## Frontend

Add to `src/lib/permission-keys/qc-worksheets.ts`:

```ts
specifications: { view, create, edit, approve, supersede },
samplingPointGroups: { manage },
```

Routes:

```
qc/worksheets/specifications              list page (Code/Name/AppliesTo/Version/Status)
qc/worksheets/specifications/create       form: header metadata (Code, Name,
                                           AppliesTo, RetestPolicy — required, no
                                           pre-selected default),
                                           a WorksheetLinks section (pick up to one
                                           Chemical + one Microbial WorksheetTemplate,
                                           each a searchable dropdown of Effective
                                           templates), a Characteristics table (add/
                                           remove row; each row: TestName, Analyte,
                                           AcceptanceCriteria, AlertLimit, ActionLimit,
                                           SamplingPointGroup dropdown, a
                                           SourceWorksheetTemplate + SourceFieldKey
                                           cascading dropdown pair sourced from
                                           GET /{id}/available-fields, IncludeOnCoa
                                           checkbox, DisplayOrder, GroupName)
qc/worksheets/specifications/[id]         same form, pre-filled; read-only with a
                                           "Create New Version" button when
                                           Status = Effective (identical pattern to
                                           Milestone 1 — see
                                           lifecycle-and-governance.md#edit-triggers-versioning)
qc/worksheets/sampling-point-groups       simple list + inline create/edit/delete,
                                           no lifecycle, no versioning
```

## Acceptance criteria

1. **WorksheetLink constraint**: Attempt to add two Chemical `WorksheetLink`s to one
   Specification. Verify rejection.
2. **SourceFieldKey validated against the linked template**: Attempt to save a
   Characteristic whose `SourceWorksheetTemplateId` isn't one of the Specification's
   own `WorksheetLinks`. Verify rejection. Attempt a `SourceFieldKey` that doesn't
   exist on that template's current Effective version. Verify rejection.
3. **Same field, two tiers**: Create a Specification with two Characteristics
   sharing the same `SourceWorksheetTemplateId`/`SourceFieldKey` but different
   `SamplingPointGroupId` and different `ActionLimit` values. Verify both save
   without conflict.
4. **SamplingPointGroup is dropdown-only**: Verify the Specification form has no
   free-text path to set `SamplingPointGroupId` — only selection from existing
   `SamplingPointGroup` records.
5. **Stage required for Product, forbidden otherwise**: Attempt to save a
   Specification with `AppliesTo = Product` and `Stage` unset. Verify rejection.
   Attempt to save one with `AppliesTo = RawMaterial` and `Stage` set to any value.
   Verify rejection — Stage is Product-only, not merely optional elsewhere.
6. **Edit-triggers-versioning**: Same scenario as Milestone 1's acceptance
   criterion 1, run against `Specification` instead of `StandardTestProcedure` —
   confirms the shared lifecycle behavior wasn't accidentally implemented
   differently for this entity.
7. **RetestPolicy is mandatory**: Attempt to save a Specification with
   `RetestPolicy` unset. Verify rejection — there is no default to silently fall
   back to.
8. **Coexistence**: Existing `qc/material-specification`/`qc/product-specification`
   pages still load and function unchanged after this milestone is built.
