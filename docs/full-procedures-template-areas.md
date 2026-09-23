# Full Procedures template-area registry

Date: 2026-09-16. Status: implemented backend configuration foundation; no
Procedure execution or template revision publishing is enabled by this slice.

## Purpose and ownership

Template Areas are deployment-configured workspaces for reusable questions,
sections, forms, activities and workflows. They let each installed company
configure HR, Production, QC, Microbiology, R&D, Warehouse, Maintenance or
future reviewed uses without company-name branches and without introducing a
new tenant identifier. The existing authenticated deployment context remains
the company boundary.

An area stores an owner role, one or more reviewed purposes and subject types,
allowed declarative capabilities, a review policy, role grants, active state
and an optimistic version. `Viewer`, `Author`, `Reviewer`, `Publisher` and
`Administrator` are numeric access levels. These grants are resource scope;
the caller must also hold the endpoint's global permission key.

## API

All routes are under `/api/v1/template-areas`:

- `GET /catalog` requires `CanViewQuestionTemplates` and returns reviewed
  purposes, subject types, capability descriptors, review policies and active
  roles that may own or receive area access.
- `GET /` and `GET /{id}` require `CanViewQuestionTemplates` and return only
  areas assigned through an owner role or explicit role grant.
- `POST /`, `PUT /{id}` and `PUT /{id}/active` require
  `CanManageTemplateAreas` plus area-level administration. An owner role is an
  implicit area administrator. Creation requires the actor's role to be the
  owner or receive an explicit Administrator grant in the new area.

The catalog is deliberately server-owned. A configuration can select reviewed
capabilities such as approval, electronic signature, material action, quality
gate, R&D abort and redevelopment; it cannot define arbitrary executable code
or URLs. Actual action execution remains a separate, versioned Procedure
contract and permission boundary.

## Data integrity and audit

Migration `20260916064646_AddFullProcedureTemplateAreas` creates
`TemplateAreas`, purpose/subject/capability bindings, role grants and governed
audit rows. Database constraints enforce normalized active-name uniqueness,
one binding/grant per area, access-level range, positive versions, one audit
snapshot per area version and SHA-256 hash shape.

Every create, meaningful update, activation or deactivation requires a reason
and writes the new configuration plus a canonical JSON snapshot and SHA-256
digest in the same `SaveChanges` transaction. Updates require
`expectedVersion`; stale writers receive `409 TemplateArea.VersionConflict`.
There is no hard-delete endpoint. Inactive areas are retained and excluded from
ordinary lists unless explicitly requested.

## Validation and release boundary

Purpose/subject/capability combinations are validated on the server against the
reviewed descriptor catalog. Owner and grant roles must exist and be active;
duplicate grants, unsupported enum values and unknown references fail closed.
403 responses are represented by the shared `Error.Forbidden` result type.

This slice does not yet persist question/form/activity/workflow revisions, grant
access to completed HR/QC/Microbiology responses, publish a Procedure, bind a
BMR/BPR revision, issue a batch or mutate inventory. Legacy Routes remain
unchanged.

## Verification

- Backend API build: succeeded.
- At completion of the Area slice, focused Full Procedures tests: 29 passed, 10 explicitly PostgreSQL-gated
  spike tests skipped, 0 failed.
- Migration was first generated in an isolated repository copy. Its `Up` SQL
  contains only the six Template Area tables, constraints and indexes; `Down`
  removes only those tables.
- Generated PostgreSQL SQL was inspected from the preceding water-quality
  migration through the Template Area migration. No migration was applied to a
  database automatically.
