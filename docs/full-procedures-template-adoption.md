# Full Procedures template adoption

Date: 2026-09-16. Status: backend target-owned Draft adoption implemented;
Settings UI and frontend client integration are not implemented.

## Boundary

Template adoption consumes one current Active sharing grant and creates a new
Draft owned by the grant's target area. The source remains unchanged. The new
definition retains immutable lineage to the exact source definition, revision,
content hash, grant version and adoption snapshot.

Adoption does not copy source lifecycle approvals, actor identities, completed
responses, evidence, signatures, files or existing consumers. The target Draft
must complete its own review and publication lifecycle before use.

## API and authorization

- `POST /api/v1/template-adoptions/from-grant/{grantId}` creates the target
  Draft and adoption lineage.
- `GET /api/v1/template-adoptions/{adoptionId}` reads lineage when the actor may
  view the target area.

The command requires `CanShareTemplateRevision`, an active current-version
grant, active source and target areas, and Author assignment in the target area.
The exact source revision is re-resolved and must still be Published with the
pinned content hash. A grant can be adopted only once.

## Dependency translation

The request supplies exact source-to-target dependency mappings. Missing,
extra, duplicate or context-incompatible mappings fail closed:

- Question: calculation-source Question revision mappings.
- Section: pinned Question revision mappings.
- Form: pinned Section mappings plus conditional source Question mappings; the
  mapped Question must belong to the mapped target Section.
- Activity: pinned Form mappings plus explicit performer/checker/approver role
  mappings. Target resources and capabilities are revalidated.
- Workflow: pinned Activity mappings; semantic nodes, edges and separate layout
  are copied through the target Workflow service.

The existing kind-specific create services perform target-area validation and
write their normal Draft/audit records. Adoption is not a validation bypass.

## Transaction and audit behavior

The production EF operation runs in a serializable transaction. Source/grant
validation, target Draft creation, grant version increment, the grant's
`Adopted` audit event and immutable adoption lineage commit together. A source
retirement or grant revocation cannot race a partially completed clone.

## Persistence and verification

Migration `20260916234752_AddFullProcedureTemplateAdoption` adds only the
`TemplateAdoptions` table, its checks, restricted foreign keys and one-adoption-
per-grant unique index. EF reports no pending model changes. The migration was
reviewed but was not applied to a local or production database in this slice.

Seven focused tests cover target Question creation and lineage, all composite
template kinds, dependency translation failure, stale/unauthorized grants,
source publication drift, duplicate adoption and target-area read scope. The
complete Full Procedures run after the Procedure-definition slice reports **91
passed, 10 explicitly database-gated skips and 0 failed**.

The focused tests use EF's in-memory provider. They verify service behavior but
do not prove PostgreSQL serializable-concurrency behavior; database integration
coverage for competing adoption/revocation transactions remains required.
