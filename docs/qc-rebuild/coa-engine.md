# Test Categories and COA Generation

## Test categories

| Category | Rule |
|---|---|
| Material / Product | `Specification` determines Chemical-only, Microbial-only, or both (via a `MicrobialRequirement`-style flag). Both → one combined COA (strict hold — see below). One → single COA the moment that track releases. |
| Routine — Water | `Specification` always includes both Chemical and Microbial worksheet links; scheduled testing covers both together. Same strict-hold combination rule. |
| Routine — Environmental | Microbial only, always. Own certificate shape — see below. |
| Scheduled vs. Unscheduled | `MonitoringProgram` auto-generates scheduled `TestRequest`s via a daily due-date scan. "New Unscheduled Test" creates one outside any program with a mandatory `Reason`. |

## COA layout

**Header** (every COA, same shape regardless of Chemical/Microbial/combined):

```
CERTIFICATE OF ANALYSIS

Certificate Code:     COA-2026-00842
Product/Material:     Paracetamol Tablets 500mg
Batch Number:         PT260901
Specification:        FP-PARA-500-001, Rev 4
Manufacturing Date:   ...      Expiry Date: ...
Sample Date:          ...      Test Completion Date: ...
```

**Rows** come from `Specification.Characteristics`, ordered by `DisplayOrder`, grouped
by `GroupName`:

```
CHEMICAL
  Description           White tablet              Complies
  Identification         Positive                  Positive
  Assay                   95.0-105.0%                98.7%

MICROBIAL
  TAMC                    NMT 1000 CFU/g            45 CFU/g
  TYMC                    NMT 100 CFU/g              12 CFU/g
  E. coli                 Absent                     Absent

Overall Result:  COMPLIES
```

Only `Characteristics` with `IncludeOnCoa = true` render — a worksheet can carry
internal working fields (e.g. `raw_area`, `dilution_factor`) that feed a
`CalculatedValue` but never appear on the certificate themselves, only their resolved
`Result` field does.

## Combination rule

**Simplified once `TestRequest`/`TestRequestSubject` were actually designed
(build-briefs/03-test-requests-and-instances.md)**: a `Specification`'s
`WorksheetLinks` (up to one Chemical + one Microbial) are resolved once, at
`TestRequest` creation, into `WorksheetInstance`s per `(Subject × WorksheetLink)` —
so a Specification requiring both tracks already produces both `WorksheetInstance`s
under the **same** `TestRequest`, not two separate `TestRequest`s. `Coa` therefore
binds to exactly **one** `TestRequest` (an earlier version of this doc said "1, or 2
`TestRequestId`s for a combined certificate" — that no longer matches the committed
structure and has been corrected).

The COA engine groups by **Subject** within that one `TestRequest`, and only
assembles rows for a Subject once **every `WorksheetInstance` belonging to that
Subject has reached `Reviewed`** — regardless of `AnalysisType`. One `Specification`
requiring both Chemical and Microbial → both must be `Reviewed` before that Subject's
rows render. `Specification` requiring only one → that Subject's rows render as soon
as its one `WorksheetInstance` is `Reviewed`.

**Strict hold, no interim COA**: for a Specification requiring both Chemical and
Microbial, if Chemical finishes first and Microbial is still incubating (microbial
incubation routinely takes 3–7 days longer than a chemical assay), no COA — not even
an interim Chemical-only one — is issued until every required `WorksheetInstance` for
every Subject in the `TestRequest` is `Reviewed`. Simpler mental model, matches the
original combination rule exactly, and avoids ever holding a document that implies
release before microbial clears.

## Revision

COA is issue-once, append-only. A correction (retest changes a released result, or a
data-entry fix post-issuance) creates COA v2, marked `Supersedes: COA-2026-00842`; the
old one stays retrievable and visibly marked `SUPERSEDED`, never deleted or edited in
place. Reason-for-revision captured under the full e-signature rule in
[lifecycle-and-governance.md](./lifecycle-and-governance.md#electronic-signatures).

## Environmental exception

Same engine, different output shell — no "Manufacturing Date/Expiry" (there's no
batch), titled "Environmental Monitoring Report" rather than "Certificate of
Analysis," since it's reporting on a room/point, not releasing a product for sale.
Underneath, identical Subject → Specification → Characteristics → rows mechanism.
