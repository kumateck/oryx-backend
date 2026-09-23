# Full Procedures Activity template revisions

Date: 2026-09-16. Status: implemented backend definition layer; not executable.

`/api/v1/template-activities` owns stable, area-scoped Activity identities and
immutable revisions. Revisions bind exact Published Forms, ordered typed action
occurrences, area-controlled resource capabilities, typed inputs/outputs, and
explicit completion rules.

Action roles are stored separately as performer, independent checker and
approver assignments. The server rejects unknown area roles, role overlap,
missing required role sets, ApproveRelease without approval, and action types
unsupported by configured capabilities. Required Forms, evidence actions,
approvals and domain transactions need matching completion rules.

The lifecycle is Draft → In Review → reviewed → Published → Retired. Expected
SHA-256 hashes and reasons fence writes. Author, reviewer and publisher authority
remain separate. Publication rechecks Forms, roles and capabilities, then
transactionally supersedes the prior Published revision. Commands append
actor/correlation-bound JSON audit snapshots.

Migration `20260916130752_AddFullProcedureActivityRevisions` contains only the
exact-Form alternate key plus Activity tables, constraints and indexes. It is
generated and reviewed. The concurrent `FormSections.AnalysisType` change now
has its own `20260916132115_AddAnalysisTypeToFormSection` migration instead of
being bundled here. EF reports no pending model changes; the local database was
unavailable for an apply check.

The four action types describe controlled intent: Perform, CaptureEvidence,
ApproveRelease and PostTransaction. They do not execute a domain handler. A
governed action-contract catalog, exact interpreter version, release bundle and
idempotent effect adapter remain required before execution.

Legacy Routes and Forms are unchanged. Activity tests pass 6/6; the complete
Full Procedures suite passes 72 with 10 intentionally gated skips after the
subsequent Workflow layer and its hardening tests.
