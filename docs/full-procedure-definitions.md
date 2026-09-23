# Full Procedure definitions

Date: 2026-09-16. Status: immutable backend definition and governance layer
implemented; BMR/BPR masters, release bundles, effective assignments and runtime
execution are not implemented by this slice.

## Definition boundary

A Procedure revision pins one exact Published Workflow revision and its content
hash. It adds the product/site/Batch Type context needed to specialize the
reusable Workflow and assigns every Activity node to exactly one record scope:
Manufacturing, Packaging, Shared or Development. A Production Procedure cannot
use Development scope.

The supported Batch Type enum is Trial, Validation, Commercial and Scale Up.
Applicability rows are declarations on the revision; they are not effective-
dated product assignments and cannot issue a batch.

The parameter schema is a bounded canonical JSON object. Duplicate properties,
invalid JSON and non-decimal-compatible numbers are rejected. This slice does
not yet validate the object against a governed JSON Schema meta-schema.

## API and permissions

- `GET /api/v1/procedures/area/{areaId}` and `GET /api/v1/procedures/{id}` use
  `CanViewProcedures` plus area visibility.
- `POST /api/v1/procedures` and `POST /api/v1/procedures/{id}/revisions` use
  `CanCreateProcedure` plus target-area Author assignment.
- `PUT /api/v1/procedures/revisions/{id}` uses `CanEditProcedureDraft`.
- `POST .../validate` uses `CanValidateProcedureDraft` and is side-effect-free.
- `POST .../submit-review`, `/record-review`, `/return-draft`, `/approve` and
  `/retire` use their distinct Procedure permission keys and area roles.

Every content write is hash-fenced and reasoned. Review and approval enforce the
configured actor-segregation policy. Approval rechecks the exact Workflow/hash,
all Activity scope mappings and referenced Product/Site rows. Approving a
replacement retires the prior Approved revision transactionally.

## Persistence and integrity

Migration `20260916234859_AddFullProcedureDefinitions` adds only Procedure
definitions, revisions, product/site/Batch Type applicability, Activity record
scopes and append-only audit tables. Composite foreign keys ensure a stage scope
belongs to both its parent Procedure's pinned Workflow revision and an actual
node in that Workflow revision. Unique indexes permit one open and one Approved
revision per stable Procedure identity.

The migration is split into small partial helper files to retain the repository
line limit. EF reports no pending model changes. Neither this migration nor the
regenerated adoption migration was applied to a database in this slice.

## Verification and limits

Six focused tests cover exact Workflow/applicability/scope pins, canonical JSON,
scope completeness/uniqueness, area authorization, segregation of duties,
Workflow retirement drift, replacement retirement and soft-deleted Product
validation. The complete Full Procedures run reports **91 passed, 10 explicitly
database-gated skips and 0 failed**.

The focused tests use EF's in-memory provider. They do not prove PostgreSQL
concurrency or migration application. This module creates no BMR/BPR master,
release bundle, effective product assignment, issue manifest, run, evidence,
inventory transaction or quality disposition. Legacy Routes remain unchanged.
