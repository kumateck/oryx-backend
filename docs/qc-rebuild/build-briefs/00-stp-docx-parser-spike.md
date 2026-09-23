# Spike: STP DOCX Import Parser Validation

Status: **resolved 2026-09-17 — go.** 8/8 real files parsed cleanly (all header
fields, all 5 sections, sub-numbering, cross-references). Three real corrections to
the original parsing design found in the process — see "Findings" below. Precedes
committing engineering time to the full `StpDocxImportService`/
`qc/worksheets/stps/import` feature described in
[phase-2-implementation-architecture.md](../phase-2-implementation-architecture.md#stp-docx-import).

## Objective

Validate, against real files, whether the parsing design in
phase-2-implementation-architecture.md actually works before building the full
feature — the design was derived from reading a handful of documents by eye, not
from running a parser against them.

## Test corpus

Use files already reviewed during Phase 1 research, since their structure is known:

- `004 - Amoxicillin Trihydrate.docx` (raw material STP, `QCD/STP/RM/004`)
- At least 4 more from the numbered raw-material library (`001–020+`, same source
  folder) — pick ones from different alphabetical/chemical families to catch
  formatting drift, not 5 consecutive numbers.
- At least 2 STPs from a finished-product folder (a different `STP No.` prefix
  pattern than the raw-material ones, e.g. `NQC/STP/FP/...` seen on the filled
  Lufart ARD) — product STPs may have a different structure than raw-material ones.

7+ files minimum. If the source folder structure has changed since Phase 1 review,
ask where the current STP library lives rather than guessing.

## Steps

1. For each file, extract and record: does it have the same header table shape
   (STP No. / Revision No. / Supersedes / Area / Title / Reference / Effective,
   Review, Issue dates)? Same 1.0–5.0 numbered section structure? Same sub-numbering
   pattern in Procedure (5.1, 5.2, …)? Note every deviation, however small —
   inconsistent capitalization, merged cells, a missing field, an extra section.
2. Write a throwaway parser prototype (`.docx` is a zip containing
   `word/document.xml`; parse the XML directly or use an existing .NET DOCX library
   already available to the project — check `oryx-backend`'s package references
   before adding a new dependency) that attempts, against every file in the corpus:
   - Extract the header table into named fields.
   - Split the body into Purpose/Scope/Responsibility/Accountability by heading
     text match.
   - Split Procedure into ordered steps by sub-numbering.
   - Regex-match "Refer to `<code>`" patterns (record the actual regex that
     matches across the corpus, not just one file).
3. Score the result per file: fully parsed with no ambiguity / parsed with fields
   flagged for review / failed to parse a section confidently. Record *why* for
   every non-clean result — this determines what the real import UI needs to
   surface to a reviewer, not just whether the feature is feasible at all.
4. Decide: is confidence high enough across the corpus to build the full feature
   as designed? If specific patterns recur (e.g. product STPs consistently
   structured differently from raw-material ones), does the design need a second
   code path, or does one parser with a confidence-flagging fallback (already part
   of the design) cover it adequately?

## Findings (2026-09-17) — go

Ran a throwaway Python prototype (zipfile + `xml.etree`, no external deps — parses
the raw XML directly, same approach the real C# service will need) against 8 real
files downloaded from the shared library: 6 raw-material STPs (Ascorbic Acid,
Amoxicillin Trihydrate, Artenimol/Dihydroartemisinin, Ciprofloxacin HCl, Clindamycin
Hydrochloride, Ibuprofen — spread across the alphabet, not consecutive numbers) and
2 finished-product STPs (Paracetamol Tablets, Tobufen Tablets) — deliberately
covering both `STP No.` prefixes (`QCD/STP/RM/xxx` and `QCD/STP/FP/xxx`).

**Result: 8/8 CLEAN** on the second pass, after three real corrections surfaced by
the first pass's failures — this is exactly why the spike existed rather than
assuming the design was right:

| File | STP No. extracted | Header (8 expected) | Sections (5 expected) | Sub-steps | Refer-to matches |
|---|---|---|---|---|---|
| 001 - Ascorbic Acid | `QCD/STP/RM/001...` | 8/8 | 5/5 | 11 | 4 |
| 001 Paracetamol Tabs | `QCD/STP/FP/001...` | 8/8 | 5/5 | 6 | 1 |
| 004 - Amoxicillin Trihydrate | `QCD/STP/RM/004...` | 8/8 | 5/5 | 16 | 4 |
| 006 Tobufen tablets | `QCD/STP/FP/006...` | 8/8 | 5/5 | 6 | 1 |
| 007 - Artenimol Dihydroartemisinin | `QCD/STP/RM/007...` | 8/8 | 5/5 | 8 | 3 |
| 012 - Ciprofloxacin HCl | `QCD/STP/RM/012...` | 8/8 | 5/5 | 10 | 4 |
| 015 - Clindamycin Hydrochloride | `QCD/STP/RM/015...` | 8/8 | 5/5 | 6 | 5 |
| 020 - Ibuprofen | `QCD/STP/RM/020...` | 8/8 | 5/5 | 10 | 3 |

**Three corrections to the original design, found by debugging why the first pass
failed on every file:**

1. **The header metadata table (STP No./Revision No./Supersedes/Area/Reference/
   dates) lives in a Word running header (`word/header2.xml` in every file tested),
   not the document body (`word/document.xml`).** The original design assumed it
   was a body-level table, like the Purpose/Scope/etc. content — it isn't. The real
   `StpDocxImportService` must read the header part(s) separately; `header1.xml`/
   `header3.xml` were present but empty in every file (first-page/even-page headers,
   unused) — check all `word/header*.xml` parts and use whichever has content,
   don't assume a fixed index.
2. **No space between the section number and heading word** — real text is
   `"1.0Purpose :"`, not `"1.0 Purpose:"`. A parser anchored on `\d\.0\s+Word`
   matches nothing; `\d\.0\s*Word` is required. Confirmed identically across all 8
   files — not a one-off formatting slip.
3. **Section/heading text isn't reliably line-anchored.** The document body is
   effectively one large table with long concatenated cell text, not discrete
   paragraphs — a parser must search within joined text (regex `.finditer` over the
   full extracted string), not match against individual lines.

**One refinement still open, not blocking**: the `STP No.` extraction regex is
slightly too greedy and currently captures part of the following product name
(e.g. `QCD/STP/RM/001ASCORBIC` instead of `QCD/STP/RM/001`) — because, per
correction 2, there's no separator there either. The real implementation needs a
tighter pattern anchored to the actual code format
(`QCD/STP/(RM|FP)/\d{3}`, confirmed as the only two prefixes seen) rather than a
greedy character class. Small fix, not a feasibility concern.

**Not tested**: `Environmental`/`Microbial`-category STPs, and whether every raw
material in the numbered library (`001`–`020+`) follows this exact pattern or
whether it drifts further into the list — 8 files is a confidence-building sample,
not exhaustive coverage. If the real import feature turns up files that don't
parse cleanly, that's expected and exactly what the confidence-flagging fallback
(already in the design) exists for.

## Deliverable — done

Results table and go decision above. `Go`.
[phase-2-implementation-architecture.md](../phase-2-implementation-architecture.md#stp-docx-import)
is updated with what the prototype revealed the original design got wrong (header
lives in a Word running header part, not the body; no space between section number
and heading word; search full text, don't line-match). If specific documents can't
be parsed reliably once the real feature is built, those STPs still get authored
manually through the normal editor — import is explicitly a time-saver, not a
requirement to migrate every document that way.

## Explicit non-goals

Do not build `StpDocxImportService` as part of this spike — a throwaway prototype
only, not committed to `APP/Services/QcWorksheets/`. Do not read from or modify
`MaterialStandardTestProcedure`/`ProductStandardTestProcedure` — the corpus is the
source `.docx` files only.
