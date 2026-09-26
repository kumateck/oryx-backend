# Build brief 08 — Specifications from worksheet import

Follows brief 07 (worksheet DOCX import). The importer proposes Specification
characteristics, sampling-point limit tiers and sampling points, but today only the
sampling points are saved. Everything else goes to a JSON download (`carry-forward.ts`
in oryx-next). This phase keeps those proposals on the server and turns them into
**Draft** Specifications through a reviewed step. Nothing is ever created Approved or
Effective, so the normal M2 lifecycle (`SpecificationRepository`) stays the only path
to approval.

## Locked decisions (2026-09-26)

1. **Scope: import proposals to Specifications only.** No migration of the live
   `ProductSpecification`/`MaterialSpecification` tables, which stay untouched.
2. **One Environmental Monitoring Specification.** There is a single
   `RoutineEnvironmental` Specification. It has a microbial link to the shared
   `EM-AIRBORNE-VIABLES` template and one characteristic per limit tier
   (`SamplingPointGroup`). Areas live on `SamplingPoint.Area`. Applying the EM sheets
   from a later upload adds their tiers to the existing Draft EM Specification instead
   of creating a second one.
3. **A product ARD becomes a microbial-only Draft Specification.** It has
   `AppliesTo = Product`, `Stage = Finished` and one microbial link to the imported
   template, with its TAMC/TYMC/pathogen characteristics. Chemical tests are added by
   hand before approval.
4. **Water** becomes a `RoutineWater` Specification with one characteristic per tier:
   SP1–3, SP4–9, and SP10–15 plus the NSPs.
5. **Media** sheets produce no Specification, because their limits stay on the sheet
   (brief 07). A proposal set with no characteristics is never stored.

## Backend (oryx-backend, target `desmond-latest`)

### Entity `SpecificationProposalSet` (new table + migration)

| Column | Notes |
|---|---|
| `Family` | `ArdFamily` (Product / PurifiedWater / EnvironmentalMonitoring) |
| `SourceFileName` | string(500) |
| `WorksheetTemplateId` | Guid, FK. The saved template the characteristics bind to. Required, because the import screen posts only after the template is saved. |
| `ProductName`, `SpecificationCode` | From the running header, when printed |
| `ProposalJson` | jsonb: `specificationProposals`, `samplingPointGroupProposals` and `samplingPointCodes`, exactly as the import screen's carry-forward document has them |
| `Status` | `Pending` / `Applied` / `Dismissed` |
| `AppliedSpecificationId` | Guid?, set on apply |
| `DismissReason` | string, required on dismiss |
| audit | BaseEntity |

### Endpoints (`QcSpecificationProposalController`, `api/v{version}/qc/specification-proposals`)

| Verb | Route | Permission | Purpose |
|---|---|---|---|
| POST | `/` | `CanImportQcWorksheetTemplates` | The import screen posts one set per saved file. This is the save step of the import action itself, so it reuses the import key. |
| GET | `/?status=&family=` | `CanViewQcSpecificationProposals` | List |
| GET | `/{id}` | `CanViewQcSpecificationProposals` | Detail |
| POST | `/draft` | `CanApplyQcSpecificationProposals` | Body `{ proposalSetIds[] }`, all of one family. Returns a **Specification draft plan**, the same shape as `CreateSpecificationRequest` plus `groups[]` (name, description, point codes) and `warnings[]`. Read-only. |
| POST | `/apply` | `CanApplyQcSpecificationProposals` | Body `{ proposalSetIds[], plan }`, where `plan` is the reviewer-edited draft plan. One transaction (see Apply). Returns the Specification id. |
| POST | `/{id}/dismiss` | `CanDismissQcSpecificationProposals` | Body `{ reason }` (required) |

The three new keys go in `QcWorksheetPermissionKeys` and `QcWorksheetPermissionCatalog`,
with coexistence tests to match. Each action gets its own key.

### Draft plan rules

- **Product** (one set per plan): name = `ProductName`, code = `SpecificationCode`,
  `AppliesTo = Product`, `Stage = Finished`, microbial link to `WorksheetTemplateId`,
  and one characteristic per proposal. The characteristic takes `TestName`, `Analyte`,
  `AcceptanceCriteria`, `ActionLimit`, `AlertLimit`, `SourceFieldKey`,
  `GroupName = "MICROBIAL"` and `IncludeOnCoa = true`.
- **Water / EM** (one or more sets per plan): one characteristic per tier, with
  `SamplingPointGroupId` resolved by group name.
  - A group that doesn't exist yet appears in `groups[]` and is created on apply.
  - Identical tiers from different sets (same group name, same limits) merge into one.
  - Same group name with **different** limits → `warnings[]` entry `TierConflict`. Apply
    refuses until the plan has one row per group.
- **EM with an existing EM Specification:**
  - A **Draft or UnderReview** Specification exists: the plan targets it
    (`targetSpecificationId`), and apply appends only the tiers it doesn't already have.
  - Only an **Effective** one exists: warning `EffectiveEmSpecificationExists`, and the
    plan is for a new Specification. The reviewer must instead create a new version of
    the Effective one through the normal flow and then apply into that Draft.
- `RetestPolicy` has **no default** (M2 rule). The plan leaves it null and apply rejects
  a null value; the reviewer picks it.
- A characteristic whose `SourceFieldKey` isn't on the pinned template version →
  warning `FieldNotOnTemplate`, and the row is left out of the plan.
- A template that isn't Effective → warning `TemplateNotEffective`. This is only a
  warning, because M2 already lets a Draft Specification link any template version.

### Apply (one transaction)

1. Re-check that every set is still `Pending` (concurrency).
2. Upsert the `SamplingPointGroup`s by name.
3. For each group's point codes, set `SamplingPoint.SamplingPointGroupId` on the live
   point with that code.
   - A code with no point → error `SamplingPointNotFound`. The import screen normally
     creates the points; if it didn't, the reviewer creates them first.
   - A point already in a **different** group → error `SamplingPointGroupChange`. Moving
     a room between tiers is a limit change and must be done deliberately in
     sampling-point master data.
4. Create the Specification, or update the target Draft, **through
   `SpecificationRepository`'s existing create/update methods**, so all M2 validation
   runs unchanged.
5. Mark the sets `Applied` with `AppliedSpecificationId`.

### Tests

- Draft plan per family, built from the real-corpus proposals when `QC_ARD_CORPUS_DIR`
  is set, and from synthetic proposals otherwise.
- EM: 8 area sets make one Specification.
- Water: 3 tiers.
- Tier conflict.
- Appending to an existing Draft EM Specification.
- Effective EM Specification warning.
- Point group change error.
- Null RetestPolicy rejected.
- Double apply rejected.
- A permission test per endpoint.

## Frontend (oryx-next, target `final-for-live`)

1. **Import screen.** After each file's template save, which the save-all dialog
   already does, POST its proposal set, but only when it has specification proposals.
   Keep the JSON download. A failure shows a toast and doesn't undo the template save.
2. **New page `qc/worksheets/specifications/proposals`** (view key).
   - Pending sets are grouped by family. Each row shows the file, product, number of
     tiers or characteristics, and the date.
   - Actions: **Create Specification** (apply key) and **Dismiss** (dismiss key, which
     asks for a reason).
   - For EM and water, several sets can be selected together.
   - The sidebar entry sits under Specifications and is gated by
     `CanViewQcSpecificationProposals`, in `permission/navigation.tsx`.
3. **Review screen** (FormWizard + zod).
   - It loads `/draft` and shows warnings at the top.
   - Editable header: code, name, stage, and **RetestPolicy (required, no preselect)**.
   - A characteristics grid with inline edits for test name, criteria, alert and action
     limits, and group.
   - A groups list showing the point codes.
   - Apply calls `/apply`, then redirects to the Specification detail page.
   - The Specification then continues through the existing submit-for-review flow.
4. RTK Query endpoints in `src/lib/redux/api/qc-worksheets`, plus the permission keys
   in the QC permission constants.
5. Tests: plan-to-form mapping, the RetestPolicy-required validation, and grouping
   pending sets by family.

## Out of scope

- Migrating legacy Specifications.
- Chemical characteristics for products.
- Auto-approving anything.
- Media limits.
