# Full Procedures question revisions

Date: 2026-09-16. Status: backend storage and API implemented; migration generated,
reviewed and applied to the configured local development database. No production
deployment was performed. Legacy `Question` and `Form` contracts remain unchanged.

## Boundary

This slice implements the first immutable definition kind in the new
Questions & Templates module. A `TemplateQuestion` is the stable identity owned
by one active Template Area, purpose and subject type. Its
`TemplateQuestionRevision` rows carry the versioned meaning.

This is not a migration of legacy Settings Questions. It does not yet provide
section, form, activity or workflow revision storage, response capture, sharing,
usage-impact reporting, Procedure execution, or a Settings UI.

## API

All routes are under `/api/v1/template-questions` and require an authenticated
actor. Controller permission and service resource authorization are both
required.

| Method and route | Permission | Resource rule |
| --- | --- | --- |
| `GET /area/{areaId}` | `CanViewQuestionTemplates` | Actor must have any grant in that area, own it, or be an Administrator. |
| `GET /{questionId}` | `CanViewQuestionTemplates` | Same area-resource rule. |
| `POST /` | `CanManageQuestionRevision` | Author grant, owner role, or Administrator; area must be active. |
| `POST /{questionId}/revisions` | `CanManageQuestionRevision` | Author authority; no Draft/In Review revision may already exist. |
| `PUT /revisions/{revisionId}` | `CanManageQuestionRevision` | Draft only, author authority, matching expected content hash. |
| `POST /revisions/{revisionId}/submit-review` | `CanManageQuestionRevision` | Draft to In Review, matching hash and reason. |
| `POST /revisions/{revisionId}/record-review` | `CanReviewTemplateRevision` | In Review only; reviewer cannot be the author. |
| `POST /revisions/{revisionId}/return-draft` | `CanReviewTemplateRevision` | In Review to Draft for reasoned correction; clears prior review evidence. |
| `POST /revisions/{revisionId}/publish` | `CanPublishTemplateRevision` | Reviewed In Review revision only; publisher cannot be the author. |
| `POST /revisions/{revisionId}/retire` | `CanPublishTemplateRevision` | Published to Retired with expected hash and reason. |

For a `RegulatedThreePerson` area, the reviewer and publisher must also be
different actors. Other profiles still require author/publisher separation.
There is no generic status setter.

## Content and reference rules

- The server owns the numeric answer-type taxonomy and its allowed existing
  `InputTypes` presentation mapping. An arbitrary client mapping is rejected.
- Choice answers require unique non-empty options; non-choice answers cannot
  carry options.
- Minimum cannot exceed maximum. A referenced UOM must exist.
- Calculation questions require references; other types cannot carry them.
- Every calculation source must be a different question in the same area and
  must have a Published revision.
- A calculation stores both the stable source question ID and the exact source
  revision ID resolved at draft write. Publishing a newer source revision cannot
  silently change an already stored calculation definition.
- Purpose and subject bindings must exist on the owning area. An area update
  cannot remove a binding used by a question.

## Lifecycle, concurrency and audit

Draft content may be replaced only while the revision is Draft and only when
`expectedContentHash` matches. Every meaning-bearing snapshot receives a
canonical SHA-256 content hash. Submission, review and publication each require
that hash, so a reviewer or publisher cannot act on content different from the
content they loaded.

Publication retires the previously Published revision in the same relational
transaction. Filtered unique indexes enforce at most one Published revision and
at most one open Draft/In Review revision per stable question. A unique sequence
index prevents duplicate revision numbers.

Create, edit, return, retirement and lifecycle transitions append an audit row containing prior/new
status, action, reason, content hash, full JSON snapshot, actor, time and
correlation ID. Published and Retired content are immutable; a change requires a
new revision.

## Persistence

Migration `20260916072607_AddFullProcedureQuestionRevisions` adds only:

- `TemplateQuestions`
- `TemplateQuestionRevisions`
- `TemplateQuestionOptions`
- `TemplateQuestionCalculationReferences`
- `TemplateQuestionRevisionAudits`

The generated idempotent SQL was inspected from the preceding Template Area
migration. It contains the five additive tables, foreign keys, check constraints
and indexes described above; no unrelated drop or alteration is present. It was
applied to the configured local development database after review, not to a
production environment.

## Verification

`dotnet test tests/APP.Tests/APP.Tests.csproj --filter FullyQualifiedName~FullProcedures --no-restore`
reports **39 passed, 10 skipped, 0 failed**. The skipped cases are explicitly
gated disposable-PostgreSQL runtime resilience tests. Nine question-service
tests cover validation, resource permissions, three-person governance,
published immutability/replacement, stale hashes and exact calculation-revision
pinning. The area suite also proves an in-use purpose/subject binding cannot be
removed.

That count records the Question slice at completion. After the subsequent
Section and Form slices, the aggregate Full Procedures run is 54 passed,
10 intentionally gated skips and 0 failed.
