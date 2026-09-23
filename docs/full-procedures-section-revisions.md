# Full Procedures Section revisions

Date: 2026-09-16. Status: implemented backend definition layer; not a Procedure runtime.

`/api/v1/template-sections` owns stable, area-scoped Section identities and
immutable revisions. A revision orders exact Published Question revision pairs
and may declare local conditional rules that depend only on earlier questions
in that revision. It never substitutes a newer Question revision for a pin.

The lifecycle is Draft → In Review → reviewed → Published → Retired. Draft edits
and transitions require the expected SHA-256 content hash and a reason. Author,
reviewer and publisher permissions are separate; the regulated policy enforces
three different actors. Publishing a replacement retires the prior Published
revision transactionally. Commands append actor/correlation-bound JSON audits.

Migration `20260916074547_AddFullProcedureSectionRevisions` adds Section,
revision, question-placement, conditional-rule and audit tables with exact
composite foreign keys and filtered lifecycle uniqueness. It was reviewed and
applied only to the configured local development database.

Legacy Questions, Forms and Routes are unchanged. Published Sections are
reusable definitions; they grant no response access and execute no Procedure.

Verification: the focused Section service suite passes 9/9 tests.
