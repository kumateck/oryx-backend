# Specification Structure

See [domain-model.md](./domain-model.md) for the `Specification`/`Characteristic`
shape this refines.

## Test × Analyte

A `Characteristic` supports an optional `Analyte` dimension for multi-active products.
Confirmed against a real dual-active-ingredient tablet worksheet (Artemether/
Lumefantrine): Dissolution and Assay each need separate rows — one per active
ingredient (`Artemether: NLT 70% (Q) in 60 minutes` / `Lumefantrine: NLT 65% (Q) in 45
minutes`), not one row per test name. The corresponding `WorksheetField`s are
per-analyte too (e.g. `assay_artemether`, `assay_lumefantrine`).

## Two-tier limits

`Characteristic` supports an optional `AlertLimit` alongside the `ActionLimit` —
confirmed against a real Environmental Monitoring certificate showing e.g. general
rooms at Alert 80 / Action 100 CFU/4Hrs vs. a Dispensing Booth at Alert 3 / Action 5.
Water shows the same grouped-tier pattern (e.g. SP1–3 at one limit, SP4–9 at another,
SP10–15 and NSP1–15 at a third) — this isn't Environmental-only, it's a general
"limit varies by sampling-point group" need. See
[lifecycle-and-governance.md](./lifecycle-and-governance.md#alert-vs-action-limits)
for what each tier triggers.

## Point-group tiers

An optional `SamplingPointGroup` on `Characteristic` lets limits vary by room/point
classification without a row per physical room — the group lives on
`MonitoringProgram`/`SamplingPoint`, and the `Specification` keys its limits off the
group rather than the individual point. Adding a new room to an existing
classification doesn't require editing the `Specification`.

## Rendering is invisible to the analyst

Real worksheets already print a "No. | Test | Result/Observation | Specification"
summary table on their first page, ending in an overall Remarks: Complies/Does not
comply and an Analysed By/Checked By signature block — confirmed on a real
combination-product worksheet covering 9 tests (Description, Identification, Average
Weight, Uniformity of Weight, Disintegration, Friability, Hardness, Dissolution,
Assay).

That's exactly what the execution UI needs to reproduce: resolve
`Specification.Characteristics` against `WorksheetInstance.FieldValues` live and
render them side by side, the same way the analyst already reads it on paper. This
confirms the `Specification`/`WorksheetTemplate` separation described in
[domain-model.md](./domain-model.md) costs the analyst nothing — it's a storage-layer
concern, not a UI concern. See [test-room-ux.md](./test-room-ux.md).
