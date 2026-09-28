# Full Procedures template context enforcement (2026-09-28)

The Area catalog is the source of allowed template kinds and purpose/subject
pairings. Question, Section, Form, Activity, and Workflow creation previously
checked that the chosen purpose and subject independently belonged to the
Area. The shared `TemplateAreaCatalogProvider.AllowsTemplateContext` check now
also requires that the subject belongs to the chosen purpose and that the
purpose permits the requested template kind.

The check runs before draft creation, persistence, or audit logging. It does
not change existing template revisions. The existing authorization, review,
publication, and audit behavior still applies after context validation.

Verification: the focused Full Procedures backend tests passed, including a
new cross-purpose and unsupported-kind regression test. A live database and
staging deployment were not available for end-to-end validation.

Documentation updated for this task: this note, `docs/services.md`,
`docs/workflows.md`, and root `README.md`.
