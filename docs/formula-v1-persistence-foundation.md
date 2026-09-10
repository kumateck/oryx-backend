# Formula v1 persistence foundation

## Scope and deployment state

This change adds the persistence and application-service boundary for the versioned formula
redesign. Governed definition, template-revision, snapshot, evaluation, and submission APIs are
implemented, but disabled runtime flags still prevent an uncontrolled cutover. It does not
migrate or rewrite legacy formula payloads.
The migrations are committed for controlled deployment and are not applied automatically
at application startup.

The schema is introduced by:

1. `20260905005056_AddFormulaV1Foundation`
2. `20260905005333_AddFormulaV1IntegrityTriggers`
3. `20260905012738_AllowUnclassifiedFormulaMigrationItems`
4. `20260905013841_ProtectFormulaMigrationEvidence`
5. `20260905023922_AddFormulaMigrationApplyProvenance`
6. `20260905201314_AddFormulaSnapshotExecutionFields`
7. `20260905202029_AddGovernedFormRevisionLifecycle`
8. `20260905202958_AddFormulaSubmissionSetIdempotency`

The foundation migration is additive. It creates 16 formula/revision/evidence tables and
adds nullable `FormRevisionId`, `FormFieldRevisionId`, and `FormulaSubmissionSetId` links to
existing response tables. It performs no legacy-row update, deletion, or reinterpretation.

## Persistence guarantees

- Formula content remains editable only in `Draft`. Definition, tests, hashes, language,
  and numeric-policy identity freeze when review begins.
- Formula revision status may follow only `Draft -> InReview`, `InReview -> Draft`,
  `InReview -> Approved`, or `Approved -> Retired`.
- Every status transition requires a matching append-only audit row in the same database
  transaction, including actor, reason, definition hash, and correlation ID.
- Legacy artifacts/key mappings, snapshots, executions, submission evidence, revision
  audits, and reconciliation results are append-only at both EF and PostgreSQL boundaries.
- Recorded dry-run items are append-only and a completed dry-run header cannot be changed or
  deleted. An unclassified item remains explicitly `null`; it is never mislabeled unrecoverable.
- Apply runs separately retain the canonical Apply-manifest hash and the external signed-report
  hash/location. Their identity/provenance fields are immutable in both EF and PostgreSQL,
  including while a run is pending or running.
- Snapshot generation 1 must be `Initial`. Later generations must reference the immediately
  preceding snapshot for the same response placement and carry an approval reference.
- A snapshot may use only an approved revision whose definition hash matches the snapshot.
- A valid execution must retain raw, rounded, and displayed results plus their result hash.
  Superseding executions must stay within one response placement and move forward in time.
- A final submission set accepts only `Valid`, `AuthoritativeServer`, `FinalSubmission`
  executions belonging to the same response and placement.
- An approval may link only to a formula submission set for its own response, and a linked
  submission set cannot later be replaced or cleared.

Application guards provide early failures for tracked writes; PostgreSQL triggers protect
the same evidence from repository omissions, maintenance scripts, and direct SQL clients.

## Read-only migration inventory service

`FormulaMigrationInventoryService` is registered for internal use but has no controller.
It reads formula questions, option history, active placements, and response evidence without
tracking or writing rows. Response payloads contribute only SHA-256 hashes to the report.

The service produces a deterministic source fingerprint and UUIDv5 artifact/target IDs. A
manifest decision is matched by question ID, option ID, legacy path, and raw source SHA-256;
revision IDs derive from the canonical target definition hash, so identical approved bodies
reuse one proposed revision instead of being duplicated per question.
Apply readiness remains false until the caller supplies the expected fingerprint, every
artifact has exactly one matching decision, executable decisions carry approval references,
and no active placement is classified unrecoverable. An actual Apply operation intentionally
does not change these read-only semantics.

## Dry-run evidence recording

`FormulaMigrationEvidenceService` can persist an authenticated dry-run report into only the
new migration-evidence tables. It reruns inventory inside a repeatable-read transaction, stores
the exact legacy option payload in a valid JSON envelope, records every decision and diagnostic,
and writes the reconciliation controls atomically. It is idempotent for the same release,
database fingerprint, corpus, code version, and decisions; a conflicting replay is rejected.

The recorder does not update `Questions`, `QuestionOptions`, `Forms`, `FormFields`, `Responses`,
or `FormResponses`. It has no controller, and therefore is not remotely callable. Deterministic
legacy-artifact IDs are source-evidence based and canonical target-revision IDs are definition-
hash based, allowing safe reuse instead of per-question duplication.

## Controlled definition importer

`FormulaMigrationApplyService` imports only approved canonical formula definitions, revisions,
question-to-definition links, and reviewed legacy-key mappings. It is internal and has no HTTP
controller. It does not rewrite or delete legacy question options, migrate template placements,
create response snapshots, execute formulas, or enable runtime cutover.

The Apply request is one canonical, size-bounded manifest whose computed SHA-256 must match its
declared manifest hash and binds its signed-report digest/location. The separate signed-report
fields identify external approval
evidence; the importer preserves them but does not claim that hashing alone verifies a human or
digital signature. Before writing, the service replays the completed dry run against the live
legacy source, requires every prior reconciliation control to pass, verifies exact target/evidence
coverage, enforces active importer/reviewer/approver identities and separation of duties, and
requires individual approval for every corrective migration. Logical definitions use stable keys;
revision numbers must be contiguous from one; canonical body hashes and embedded language/numeric
policy versions must agree.

All writes occur in one serializable transaction. New revisions pass through `Draft -> InReview ->
Approved` with two append-only audit records. Deterministic IDs make an identical signed replay
idempotent, while any key, revision, source, evidence, or provenance conflict fails closed. Apply
completion requires a second live-source check plus stored reconciliation controls. Historical
legacy source remains evidence and is never silently corrected in place; an approved correction is
a new canonical revision linked to the preserved defective source.

## Operator package workflow

`TOOLS/FormulaMigration` exposes the internal services without creating an administrative HTTP
endpoint. It can seal a draft against the SHA-256 of the external signed report, validate both
artifacts offline, export a read-only live dry run, record an approved dry-run ledger, and invoke
Apply. Output files are create-only. Connection strings stay in an environment variable.

Write commands do not accept a caller-supplied actor ID. They validate a signed, non-expired,
environment-matched application token and derive its user ID, then require that active user to
hold `CanApplyFormulaMigration`. Database commands require the exact database name and host/port,
which distinguishes the two `entrancedb` instances. Recording requires the exact release/source-fingerprint token;
Apply requires the exact manifest-hash token and the signed report file so its bytes can be
rehashed. Full database/source/approval checks still run inside the application services.

See `docs/formula-migration-operator-tool.md` for the command contract and no-go boundary.

## Controlled rollout

Before applying these migrations to any shared environment:

1. Confirm the phase-1 inventory source fingerprint and approved golden-corpus checksum.
2. Confirm the numeric-policy and canonical-hashing versions named by migrated revisions.
3. Back up the target database and record its migration-history fingerprint.
4. Generate and review the idempotent SQL script for that exact database state.
5. Apply first to a restored clone; run schema, constraint, trigger, and reconciliation checks.
6. Apply the eight formula migrations during an approved change window.
7. Leave formula feature flags off until the repositories, migration runner, authoritative
   evaluator, and finalization integration have independently passed validation.

Do not point `dotnet ef database update` at a shared `entrancedb` merely to test this schema.
Use a disposable database or a controlled restore. Startup migration remains disabled.

## Verification completed

The full migration chain, including governed runtime persistence, was applied to a disposable
PostgreSQL 18 clone of local `entrancedb`. The three latest formula migrations were rolled back
and reapplied successfully. The
positive lifecycle (review, approval, snapshot, authoritative execution, submission set)
succeeded. Negative checks confirmed rejection of append-only mutation, post-review content
mutation, illegal status changes, missing transition audit, unapproved snapshot rebase,
incomplete valid execution, and provisional submission evidence. The integrity migration
latest migration was rolled back and reapplied successfully. Schema inspection confirmed the
stable-key column, approval-scope, manifest-hash, and signed-report-hash columns, all five
constraints, and the database
trigger's manifest-hash/signed-report provenance rules. The disposable test did not modify either local
database named `entrancedb`.

Backend verification at implementation time:

- `dotnet build Oryx.sln -m:1 --disable-build-servers --no-restore`: passed.
- `dotnet test Oryx.sln --no-restore -m:1 --disable-build-servers`: 225 passed.
- Formula-focused tests after adding package sealing and the operator boundary: 36 passed.
- `dotnet ef migrations has-pending-model-changes ...`: no model drift.

## Formula question revision edit locking

Quality Assurance queue endpoints expose the governed review lifecycle without granting editor access:

- `GET /api/v1/formula-definitions/review-queue` lists `InReview` revisions with no recorded reviewer and requires `CanViewReviewFormulaRevision`.
- `GET /api/v1/formula-definitions/approval-queue` lists `InReview` revisions with a recorded reviewer and requires `CanViewApproveFormulaRevision`.
- The corresponding detail reads use the same view permissions; `CanReviewFormulaRevision` and `CanApproveFormulaRevision` are action permissions only.

The frontend QC detail pages load the immutable revision and invoke only the existing validated transition endpoints, preserving the revision hash and audit trail.

Formula question updates reject an existing `InReview` revision with the explicit
`FormulaDefinition.RevisionInReview` conflict. Draft content remains editable; reviewed,
approved, and retired revisions are not mutated in place. A correction must proceed through
the controlled new-draft workflow so the reviewed revision remains auditable.

## Rollback boundary

The Apply-provenance migration may be rolled back independently before canonical definitions are
written. It deliberately refuses to invent stable keys if the pre-release definition table was
manually populated; those rows require controlled reconciliation first. The integrity-trigger
migration may be rolled back independently before formula evidence is
written. The foundation migration may be rolled back only while all new tables are empty and
the three nullable legacy links remain unused. Once migration evidence, snapshots, executions,
or submission sets exist, rollback is a controlled data-migration decision—not a routine EF
down migration—because dropping the tables would destroy regulated evidence.
