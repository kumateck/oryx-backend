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
