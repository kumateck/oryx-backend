# Specification microbial template

Date: 2026-11-27. A material or product specification has two template slots:

| Slot | Field | Holds |
| --- | --- | --- |
| Chemical | `FormId` (required) | The chemical tests and their references |
| Microbial | `MicrobialFormId` (optional) | The microbial tests (TAMC, TYMC, specified organisms) and their references |

Before this change a specification had one template, so microbial tests had to share the chemical form, and a microbial ARD could use any form.

## Rules

- The microbial template must be a different form from the chemical one, and it must have at least one section that applies to microbial analysis (`AnalysisType` Microbial or null). `SpecificationTemplateSlots.Validate` checks this on create and update for both material and product specifications.
- Test references are stored on each form's sections (`FormSection.Value`, stamped with the specification id), as before. The two templates are different forms, so a microbial reference never overwrites a chemical one. `SpecificationTestReferences` needs no change.
- Adding materials or products to a specification number copies the microbial template too.
- When a specification names a microbial template, creating or updating a **microbial** ARD for that material (or product stage) must use it (`MaterialArd.MicrobialForm` / `ProductArd.MicrobialForm`). A specification without one does not constrain microbial ARDs, so existing data keeps working.
- The microbial template cannot be changed or cleared while a microbial ARD of a material or product on that specification number uses it (`*.MicrobialFormInUse`). Change the ARD first.
- Update is a full replace: a client that omits `microbialFormId` clears it (unless an ARD uses it, see above). The desktop always sends it.

## Not changed

- Sampling and ATR still pick the verified microbial ARD and gate on `MicrobialRequirement`. The combined COA still waits for micro.
- Routine (water and environmental monitoring) is unchanged.
- No new endpoints or permission keys. Create/update/read use the existing specification keys.

## Migration

`AddSpecificationMicrobialTemplate` adds nullable `MicrobialFormId` (FK to `Forms`) to `MaterialSpecifications` and `ProductSpecifications`. No data change.

# Microbial question types and typed limits (step 2)

Date: 2026-11-27. Owner decisions: limits live on the **specification**, and the worksheet uses **new question types**.

## Question types

| Type | Value | Answer stored |
| --- | --- | --- |
| `Organism` | 15 | An organism code from `MicrobialOrganisms` (for example `escherichia-coli`) |
| `Dilution` | 16 | JSON `{sample, sampleUnit, diluent, factor}`; `factor` > 0 is required |
| `Incubation` | 17 | JSON `{temperature, hours, startedAt, endedAt}`; complete only once `endedAt` is set and not before `startedAt` |
| `ColonyCount` | 18 | JSON `{plates: [n, ...], tooNumerousToCount}`; counts are whole numbers ≥ 0 |
| `CfuCalculation` | 19 | Written by the server: JSON `{average, factor, result, tooNumerousToCount, unit}` |
| `GrowthObservation` | 20 | `Absent` or `Present` |

- A `CfuCalculation` question has exactly one option, its unit (`cfu/g`, `cfu/mL` or `cfu/plate`). The other microbial types have no options. A question with answers cannot change to or from a microbial type.
- A test (section) with a CFU calculation needs exactly one colony count and one dilution. ARD creation checks this (`Form.MicrobialSection`).
- Saving a colony count, dilution or CFU answer makes the server recalculate the section's CFU result as average plate count × dilution factor. Any value the browser sends for a CFU field is ignored. Too-numerous plates give no number.
- Analyst answers are checked by type when they are saved (`Response.MicrobialAnswer`).

## Typed limits on the specification

`SpecificationMicrobialLimits`: one row per limit per specification row. Each row has the result field of the microbial template, a kind, value/unit or organism/quantity/quantityUnit, display order, and the reason, actor and time of the save.

| Kind | Field type | Printed |
| --- | --- | --- |
| `NotMoreThan` | `CfuCalculation`, in the field's own unit | `NMT 1000 cfu/g` |
| `Absent` | `GrowthObservation` | `Absence of Escherichia coli in 1 g` |

- `GET/PUT api/v1/material-specifications/{id}/microbial-limits` and `GET/PUT api/v1/product-specifications/{id}/microbial-limits`. A PUT replaces the whole set. It needs a reason of at least 10 characters and a microbial template on the specification.
- Saving writes the same limits to every specification row sharing the number and clears their approval, so the specification is approved again.
- The edit key is read again inside the serializable transaction, both before and after the writes.

## Enforcement

- When QC checks a material or product certificate (`GenerateCertificateOfAnalysis*`), `MicrobialLimitRuntime.Apply` evaluates each limit against the response's saved result. The verdict replaces the compliance the browser sent, for every answer in that test. A missing or unreadable result blocks the check (`MicrobialLimit.Unresolved`). Too numerous to count never passes a count limit.
- The commercial certificate prints the generated limit text for a limited field instead of the COA item's typed text.
- The limits come from the specification of the response's material, or of the product and stage, whose microbial template is the response's form.

## Permissions

| Key | Module / submodule | Endpoints | Granted by `GrantSpecificationMicrobialLimitKeys` to roles holding |
| --- | --- | --- | --- |
| `CanViewRawMaterialMicrobialLimits` | Quality Control / Material Specification | GET material (raw) | raw spec view, create, edit or verify |
| `CanEditRawMaterialMicrobialLimits` | Quality Control / Material Specification | PUT material (raw) | `CanEditRawMaterialSpecification` |
| `CanViewPackagingMaterialMicrobialLimits` | Quality Control / Material Specification | GET material (packaging) | packaging spec view, create, edit or verify |
| `CanEditPackagingMaterialMicrobialLimits` | Quality Control / Material Specification | PUT material (packaging) | `CanEditPackagingMaterialSpecification` |
| `CanViewProductMicrobialLimits` | Quality Control / Product Specification | GET product | product spec view, create, edit or verify |
| `CanEditProductMicrobialLimits` | Quality Control / Product Specification | PUT product | `CanEditProductSpecification` |

Material endpoints choose raw or packaging from the stored specification's material (`QcDocumentPermission`).

## Migrations

`AddSpecificationMicrobialLimits` (table, with a check that exactly one specification is set) and `GrantSpecificationMicrobialLimitKeys` (data only).
