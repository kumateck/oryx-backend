# Spike: Formula/Calculation Engine for CalculatedValue Fields

Status: **resolved 2026-09-17**. Decision: build a new, small local evaluator — do
not reuse `FormulaWorksheetPreprocessor`. See "Findings" below for the evidence and
[field-catalog.md](../field-catalog.md) for the resulting formula syntax spec.

## Objective

Answer two questions with evidence, not opinion, and produce one artifact: a written
decision plus a working formula syntax spec that Milestone 1 and later milestones
can build against.

1. **Can `APP/Services/Formulas/FormulaWorksheetPreprocessor` be reused** for
   `CalculatedValue` field resolution, or does it need to be built new?
2. **What is the exact formula syntax** a `WorksheetTemplate` author writes when
   configuring a `CalculatedValue` field?

## Why this needs a spike, not a design decision

[field-catalog.md](../field-catalog.md) confirmed against real worksheets that
`CalculatedValue` fields need to do two different things:
- Reference **plain scalar fields** by `FieldKey`, worksheet-scoped (e.g. `Result
  (cfu/g) = Average Count × Dilution Factor`, referencing two other fields).
- Reference **table-column aggregates** (Sum, Average, Min, Max, %RSD/StdDev) over
  a `Table` field's rows (e.g. the Dissolution `% Dissolution = (Avg Abs Spl / Avg
  Abs Std) × (Std conc / Spl conc) × 100`, where `Avg Abs Spl` is an average across
  a 6-row table column, combined with plain scalar fields from elsewhere in the
  worksheet).

`FormulaWorksheetPreprocessor` already resolves row/column cell references and
evaluates expressions against them — for the *manufacturing Formulas* module, a
different domain. Whether its resolver generalizes to `WorksheetInstance.FieldValues`
(keyed by `FieldKey`, not by manufacturing-formula row/column coordinates) is a real
open question that reading the code hasn't answered — hence the spike.

## Steps

1. Read `APP/Services/Formulas/FormulaWorksheetPreprocessor.cs` and its tests
   (`tests/APP.Tests/Repository/FormulaWorksheetInputResolverTests.cs`) in full.
   Identify: what input shape does it expect (row/column coordinates, or something
   more general)? What expression syntax does it evaluate? What does its output
   shape look like? Is aggregate-function support (Sum/Avg/Min/Max/%RSD) present,
   or would it need to be added?
2. Write a throwaway prototype (not committed to the main branch — a scratch
   script or a temporary test file) that feeds it a small synthetic
   `WorksheetInstance`-shaped input (a handful of `FieldKey`/value pairs plus a
   3-row table) and attempts to evaluate: (a) a plain two-field formula, (b) a
   formula referencing a table-column average. Record what works unmodified, what
   needs adaptation, and what's fundamentally incompatible.
3. Based on step 2, decide: **reuse the evaluation core** (with adapter code
   translating `WorksheetInstance.FieldValues` into whatever shape it expects) or
   **build a new, small expression evaluator** scoped only to what
   `CalculatedValue` needs (arithmetic over named values, plus Sum/Avg/Min/Max/
   %RSD/StdDev over a named table column — no need for anything the manufacturing
   Formulas module supports that QC worksheets don't use).
4. Write the formula syntax spec: the exact string/expression format an author
   types into a `CalculatedValue` field's configuration in the
   `qc/worksheets/templates` builder. Cover both cases from field-catalog.md
   verbatim — a plain-field formula and a table-aggregate formula — with concrete
   examples using real `FieldKey`s from the CFU and Dissolution cases already
   documented.
5. Update [field-catalog.md](../field-catalog.md) and
   [phase-2-implementation-architecture.md](../phase-2-implementation-architecture.md#open-implementation-decision-formulacalculation-engine)
   to replace the "open implementation decision" framing with the actual decision
   and the syntax spec, once steps 1–4 are done.

## Findings (2026-09-17)

Step 1 (full read of `FormulaWorksheetPreprocessor.cs`) surfaced disqualifying facts
before step 2's prototype was needed — reuse was ruled out by what the code
actually is, not by a synthetic test failing:

- **`internal static class`** — not accessible outside `APP.Services.Formulas`
  without changing its accessibility modifier, which the coexistence do-not-touch
  rule forbids.
- **It is not a local evaluation core.** `ResolveAsync` reads specific
  `(row, column)` cells out of a `tableData` JSON grid into named inputs, then
  calls `IFormulaCalculationClient.EvaluateAsync(...)` — shipping the actual
  expression evaluation to an **external formula microservice** (`schemaVersion:
  "oryx-formula-service-evaluate-request-v1"`) with its own versioned DSL
  (`formulaLanguageVersion`), hash-locked definitions (`definitionHash`/
  `configurationHash`, verified round-trip against the response), and a
  `numericPolicyVersion` — machinery built for manufacturing batch-weight rounding
  and audit requirements, not QC arithmetic.
- Reusing it would mean every `CalculatedValue` field in the Test Room makes a
  network call to that remote service. Wrong tradeoff for something that should
  resolve instantly as an analyst types.
- Checked, not assumed: no existing lightweight local expression-evaluation library
  (NCalc, DynamicExpresso, or similar) is already a dependency anywhere in
  `oryx-backend`'s `.csproj` files.

Given this, steps 2–4 collapsed into directly authoring the syntax spec (step 4)
without needing the comparative prototype (step 2) — there was nothing left to
compare against once reuse was ruled out on structural grounds. The syntax spec is
in [field-catalog.md](../field-catalog.md), derived directly from the real CFU and
Dissolution formulas already documented: `{field_key}` for scalar references,
`AVG({table_key.column_key})`/`SUM(...)`/`MIN(...)`/`MAX(...)`/`RSD(...)` for
table-column aggregates, standard arithmetic operators and precedence.

## Deliverable — done

A decision (build new, per the Findings above) plus a formula syntax spec concrete
enough that Milestone 1's `WorksheetField.CalculatedValue` configuration UI and the
backend evaluator have an unambiguous contract to build against. Both docs
referenced in step 5 (`field-catalog.md`,
`phase-2-implementation-architecture.md`) are updated. What remains — actually
writing the evaluator (a small arithmetic parser plus the five aggregate
functions) — is implementation work for whoever builds Milestone 1's
`CalculatedValue` handling, not part of this spike.

## Explicit non-goals

Do not build the production evaluator as part of this spike. Do not modify
`FormulaWorksheetPreprocessor` or anything in the manufacturing Formulas module —
if reuse is the decision, the QC module calls into it or wraps it; it does not
change to accommodate QC's needs, since it has its own existing callers.
