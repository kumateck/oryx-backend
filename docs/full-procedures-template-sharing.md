# Full Procedures template sharing

Date: 2026-09-16. Status: backend definition-sharing governance implemented;
target-owned adoption is implemented separately; Settings UI is not implemented.

## Boundary

Templates remain private to their owning area by default. A sharing grant records
two-area approval for one configured target area to adopt one exact Published
Question, Section, Form, Activity, or Workflow revision. The grant pins the stable
definition ID, immutable revision ID, SHA-256 content hash, purpose, and subject
type. It never grants access to completed responses, evidence, signatures,
employees, samples, batches, or files.

## Lifecycle and authorization

1. The source area Publisher may offer a revision, or the target area Author may
   request it. Both areas must be active and support the pinned purpose/subject.
2. A different actor assigned to the other area decides the pending request.
   Source-initiated requests require a target Author decision; target-initiated
   requests require a source Publisher decision.
3. Approval re-resolves the exact source revision and fails closed if it is no
   longer Published, its hash changed, or the target context is no longer valid.
4. An active grant can be revoked only by a Publisher in either participating
   area, using the current optimistic version and a recorded reason.
5. A filtered unique index permits only one Pending or Active grant for the same
   source, target, kind, definition, and revision.

Every transition appends an actor-, correlation-, version-, and reason-bound
JSON snapshot with its own SHA-256 hash. Audit rows cannot be deleted through
the lifecycle API.

## API

- `GET /api/v1/template-sharing/area/{areaId}` lists grants visible to an actor
  assigned to that area.
- `GET /api/v1/template-sharing/usage/{kind}/{definitionId}/{revisionId}` returns
  direct definition-reference and active-grant counts only.
- `POST /api/v1/template-sharing/requests` creates a Pending grant.
- `POST /api/v1/template-sharing/{grantId}/approve|reject|revoke` performs a
  version-checked, reasoned transition.

All endpoints require the relevant global template capability in addition to
the service's area-role check. The usage response is deliberately aggregate and
contains no response or subject data.

## Persistence and verification

Migration `20260916174624_AddFullProcedureTemplateSharing` adds only
`TemplateSharingGrants` and `TemplateSharingGrantAudits`, with area/user foreign
keys, database checks, optimistic versioning, and the open-grant uniqueness
index. It has been inspected and EF reports no pending model changes. It has not
been applied to a local or production database in this slice.

Six focused sharing tests pass. After the separate adoption slice, the complete
Full Procedures run reports 91 passed, 10 explicitly database-gated skips, and
zero failures.

## Relationship to adoption

An Active grant is governed eligibility, not a direct cross-area reference and
not a copy. The separate adoption command consumes a current Active grant to
create a target-owned Draft, retain source lineage and translate local
dependency/role bindings explicitly. The target Draft must obtain its own review
and publication approvals. Existing consumers never move revisions
automatically. See
[`full-procedures-template-adoption.md`](full-procedures-template-adoption.md).
