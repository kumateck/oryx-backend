# Formula migration operator tool

## Scope

`TOOLS/FormulaMigration` is the controller-free operator boundary for formula-v1 migration.
It creates and validates reviewable packages, exports live dry-run evidence, records an approved
dry-run ledger, and invokes the controlled definition importer. It does not migrate form
placements, response snapshots, executions, or enable formula-v1 runtime behavior.

No command changes legacy `QuestionOption`, `FormField`, `FormResponse`, or completed response
data. `seal`, `validate`, and `dry-run` never write database rows. `record-dry-run` writes only
the immutable migration evidence ledger. `apply` writes only approved canonical definitions,
revision/audit rows, question links, reviewed key mappings, and reconciliation evidence.

## Security boundary

Database connection strings are read only from `ORYX_FORMULA_DB_CONNECTION`; they are rejected
as command arguments. Every database command requires both `--expect-database` and
`--expect-server HOST:PORT`; it fails when either differs. This is required because the Docker
and local PostgreSQL instances may both contain a database named `entrancedb`.

The two write commands authenticate the importer from a signed, non-expired application JWT in
`ORYX_FORMULA_OPERATOR_TOKEN`. They validate it with `ORYX_FORMULA_JWT_KEY`, require its
`environment` claim to equal `ORYX_FORMULA_ENVIRONMENT`, and require the active user to hold the
dedicated `CanApplyFormulaMigration` permission. The actor is never supplied as a free-form GUID.
Reviewer, approver, and importer must remain three different active users.

Store these environment variables in the approved deployment secret mechanism. Do not paste
their values into a command, package, report, shell transcript, or source-controlled file.

## Artifact flow

1. Produce a `FormulaMigrationDryRunRequest` JSON from the classified inventory. It must cover
   every live legacy artifact and carry the approved corpus checksum, target hashes, actions,
   approval references, and approval scopes.
2. Run `dry-run`. Review its item diagnostics, reconciliation controls, `CanApply`, and source
   fingerprint. The command writes a new output file and prints the exact recording token.
3. After review, run `record-dry-run` with that exact token. A non-ready report cannot be recorded.
4. Prepare a `FormulaMigrationApplyDraft` JSON. `DryRunId` is the recorded run ID. `Targets`
   contain canonical definitions and approved test cases; `DecisionEvidence` contains one entry
   for each executable artifact plus reviewed path-aware key mappings.
5. QA/SME signs the external approval report. Run `seal`; it hashes the report bytes, computes
   a canonical manifest hash that also binds the report digest/location, validates target
   hashes/versions/revisions, and creates a new
   package file without overwriting an existing artifact.
6. Run `validate --signed-report` on the sealed package. This verifies package structure,
   canonical hashes, separation of reviewer/approver, and the exact report-byte digest offline.
7. Apply first on an approved restored clone. Run `apply` only with the exact full manifest token
   printed by `seal`. Apply repeats validation against the recorded dry run and current source in
   one serializable transaction.
8. Review stored reconciliation results and rerun the identical Apply package. The second run
   must report `alreadyApplied=true` and create zero rows before any shared-environment rollout.

Offline validation cannot prove that a report's human or digital signature is legally valid;
that verification stays in the approved QA document workflow. It proves only that the exact
reviewed report bytes are bound to the immutable migration package.

## Commands

Run from the backend repository root:

```text
dotnet run --project TOOLS/FormulaMigration -- seal \
  --draft APPLY_DRAFT.json --signed-report SIGNED_REPORT.pdf \
  --report-location QUALITY_SYSTEM_REFERENCE --output SEALED_PACKAGE.json

dotnet run --project TOOLS/FormulaMigration -- validate \
  --package SEALED_PACKAGE.json --signed-report SIGNED_REPORT.pdf

dotnet run --project TOOLS/FormulaMigration -- dry-run \
  --request DRY_RUN_REQUEST.json --expect-database entrancedb \
  --expect-server EXPECTED_HOST:EXPECTED_PORT --output DRY_RUN_REPORT.json

dotnet run --project TOOLS/FormulaMigration -- record-dry-run \
  --request DRY_RUN_REQUEST.json --expect-database entrancedb \
  --expect-server EXPECTED_HOST:EXPECTED_PORT \
  --confirm RECORD:RELEASE_ID:FULL_SOURCE_FINGERPRINT

dotnet run --project TOOLS/FormulaMigration -- apply \
  --package SEALED_PACKAGE.json --signed-report SIGNED_REPORT.pdf \
  --expect-database entrancedb --expect-server EXPECTED_HOST:EXPECTED_PORT \
  --confirm APPLY:FULL_MANIFEST_HASH
```

Output paths use create-new semantics. Move an obsolete artifact into the controlled archive or
choose a new release path; the tool will not overwrite evidence.

## Go/no-go boundary

Do not run `record-dry-run` or `apply` against either local or shared `entrancedb` until the
classified decision JSON, golden expected results, numeric policy, high-risk scientific
decisions, user permission, external approval report, backup, restored-clone rehearsal, and
change record are all approved. Building or running `seal`, `validate`, and a read-only
`dry-run` does not authorize a migration.

## Verification

Package tests prove deterministic sealing, manifest tamper rejection, and signed-report digest
rejection. Formula migration tests cover dry-run evidence, source drift, Apply idempotency,
separation of duties, legacy-source preservation, and persistence guards.
