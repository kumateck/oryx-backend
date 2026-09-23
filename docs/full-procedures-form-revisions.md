# Full Procedures Form template revisions

Date: 2026-09-16. Status: implemented backend definition layer; not a Procedure runtime.

`/api/v1/template-forms` owns stable, area-scoped Form identities and immutable
revisions. A revision orders exact Published Section revision pairs, marks
sections required or optional, declares evidence/signature requirements, and
supports conditional display sourced from an exact Question revision in an
earlier pinned Section. Cross-context, unpublished, mismatched, forward and
missing-question dependencies fail closed.

The lifecycle is Draft → In Review → reviewed → Published → Retired. Draft edits
and transitions require the expected SHA-256 content hash and a reason. Author,
reviewer and publisher permissions are separate; the regulated policy enforces
three different actors. Publication rechecks Section dependencies and retires
the previous Published revision transactionally. Commands append immutable
actor/correlation-bound JSON audit snapshots.

Migration `20260916123348_AddFullProcedureFormRevisions` adds Form, revision,
section-placement, cross-section conditional-rule and audit tables. Composite
foreign keys preserve stable-ID/revision-ID integrity; filtered indexes allow
one open and one Published revision. It was reviewed and applied only to the
configured local development database.

`RequiresEvidence` and `RequiresSignature` are controlled declaration flags in
this slice. Typed evidence classes and exact signature-policy revision bindings
must be added before execution uses them.

Legacy Forms, Questions and Routes are unchanged. Published Forms are reusable
definitions; they grant no response access and execute no Procedure.

Verification: the focused Form suite passes 6/6 tests; the complete Full
Procedures suite passes 54 tests with 10 intentionally gated skips and no failures.
