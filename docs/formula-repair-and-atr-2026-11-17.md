# Formula repair, ATR and requisition changes — 16–17 November 2026

Server-side record of the changes shipped on 16–17 November 2026. The desktop's `docs/` folder has the matching user-facing pages:
- `formula-repair-recalculation-2026-11-17.md`
- `coa-compliance-limits.md`
- `atr-request-form.md`
- `stock-requisition-issuing-2026-11-17.md`

## Formula repair

| PR | Change |
| --- | --- |
| #1845 | `FormAnswerCompleteness` needs no readings table when a formula has no sample or standard columns |
| #1848 | Recalculation reads every input from the saved answer, replaces earlier results, and names only inputs with no value |
| #1852 | Manual readings are sent as their `evaluated` number, as `FormulaResponseInputResolver.ReadManual` does. `EngineRefusal` names the reading, value and reason (missing, invalid, divide by zero) instead of one generic 409 |
| #1855 | `FormAnswerCompleteness.Unwrap` reads a Simple formula stored as `{"type":0,"expression":"<settings JSON>"}`, the question editor's format. Before, `:2`, `:false` and `:0` were read as variables, so such answers could never be complete |
| #1858 | `SubmitFormResponseFinal` records, in append-only `LegacyFormulaRepairFinalizations`, the repair evaluation each repaired field's result came from; `LegacyFormulaConfigurationReader.Attach` then reports `FormulaResultFinalized`. Migration `20261117040009_AddLegacyFormulaRepairFinalizations` creates the table with the same immutable triggers as the other repair tables |

### Finalization

- Worksheets submitted before #1858 have no finalization record. Their repaired results keep the "not finalized" note.
- No backfill is included: existing responses carry no reliable "submitted" marker to backfill from.
- A retried submission records nothing new.

## Analytical test request (#1860)

- **`GetAnalyticalTestRequest`**
  - Includes `ProductionScheduleProduct.ProductPacking` (pack style).
  - Loads `State` separately. `StateId` is required, so an `Include` would be an inner join, and a request with a missing state row would return 404.
- **`GetAnalyticalTestRequestByActivityStep`** returns the same full request. Before, it returned the bare entity.
- **New key `CanPrintProductAnalyticalTestRequest`** (Quality Control › AnalyticalTestRequestProducts).
  - Migration `20261117050000_GrantPrintProductAnalyticalTestRequest` grants it to every role holding `CanCheckProductTest` or `CanViewPendingApprovals`.
  - No endpoint uses it: the desktop checks it on the print buttons.

## Requisitions (#1861)

`GET /api/v1/requisition/department` takes `issued`:
- `true`: the requisition's own status is Completed;
- `false`: neither Completed nor Rejected.

It is applied before the item-status filter, which is unchanged.

## Proposed, not built

`whole-container-issue.md`: issuing whole sealed containers and returning the leftover.

## Approved formula repair units — 21 November 2026

### Failure and correction

Recalculating an ATR field repaired from an approved formula revision could fail with
`INVALID_CONFIGURATION: Target unit does not match result unit`, leaving final submission
blocked with `FormulaRepair.RecalculationRequired`. The repair evaluator sent `null` for
every placement source and result target unit, even when the approved compiled definition
declared weight inputs or a percentage result.

`QuestionFormulaRepairService.Evaluation` now copies each variable's unit into its input
binding and each result's unit into its result target. Missing and explicitly null units
remain null for older repairs. The configuration hash and saved evaluation evidence are
generated from this corrected configuration. Formula expressions, scaling, precision and
saved analyst readings are unchanged.

Existing server checks still require `CanExecuteFormulaResponse`, the analyst's field
assignment and an open ATR/response. Grants, readings and repair state are checked again
before saving an evaluation. Final submission still requires a successful authoritative
recalculation; this fix does not bypass that guard.

### Validation and deployment

- Regression cases cover unitless definitions, `g` inputs with `%` results, and `mg`
  inputs with `mg/mL` results. The unit-bearing cases reproduced the null-unit defect
  before the implementation change.
- The isolated real-engine regression uses saved readings of 2 g and 4 g with the
  approved expression `:a/:b*100`. It verifies the authoritative `50.00` result with
  unit `%`, preserved readings and a cleared pending-recalculation flag.
- The existing formula repair suite covers denied access, assignment, stale state,
  recalculation invalidation and finalization. Formula client and permission catalog
  checks run alongside it.
- Validation completed with 52 tests passing and two PostgreSQL-only tests skipped
  because `ORYX_FORMULA_REPAIR_TEST_CONNECTION` was not configured. The test run
  compiled the backend successfully and exercised the real formula engine on an
  isolated loopback service using disposable test data.

Deploy the updated backend through the normal pipeline. No schema migration or formula
service release is needed for this change. On an affected open ATR, reload the test and
save/recalculate its readings before submitting again. Do not repair the field again
solely to clear this unit mismatch, because applying a repair clears its previous readings.
