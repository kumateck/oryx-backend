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
