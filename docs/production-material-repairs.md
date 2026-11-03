# Repair a running batch's BOM or packaging

Implemented in source on 2026-11-02. This does not deploy the feature or change a
customer database. The desktop study is in the companion repository at
`oryx-live-desktop/docs/production-configuration-repair-study.md`.

## Operator workflow

1. Correct and **save** the product's BOM or packaging through its usual editor.
2. Open the affected running batch. In its header choose **Repair → BOM** or
   **Repair → Packaging**. **Procedure** retains the existing procedure action.
3. Review the material requirements and reservations before and after, shortages,
   existing requisition and approval consequences. Preview makes no writes.
4. Enter a reason and apply. The product and operational evidence are re-read
   under database write locks. A changed source, stock balance, reservation,
   approval or production state requires a fresh preview.
5. If requisition quantities/materials/units changed, have the corrected document
   approved through its normal workflow before warehouse issue. Its displayed
   voucher reference includes the content revision. Repair never issues stock,
   approves a requisition or advances a production step.

Each operation targets **one batch and one material kind**. Raw material already
issued does not prevent repair of still-unissued packaging. A terminal/cancelled
run, affected issued/consumed material, return/final-packing/finished-goods history,
overlapping extra material, unresolved transfer/swap evidence or ambiguous allocation history
blocks repair. An issued item, issue-check record or linked bin-card issue blocks
repair even when the requisition header still says Pending. Deleted history is
inspected. The user explicitly chose to refuse issued/consumed repairs.

Insufficient stock refuses the entire change. The preview states each shortage;
existing requirements, reservations and documents remain intact. Resolve stock
through its normal authorized workflow, then refresh the preview. The repair
does not make a partially allocated batch appear ready for issue.

## Permissions and deployment

- Procedure: existing `CanRepairProcedure`.
- BOM: `CanRepairBom`.
- Packaging: `CanRepairPackaging`.

The backend permission catalogue exposes the new grants for normal role
assignment. No role receives them automatically. Product editing and batch repair
remain separate permissions. Both material endpoints check current grants, active
actor, production department and exact batch; a browser-supplied actor is never
accepted. The client checks access on selection, after preview and before apply.

Deploy migration **20261102182519_AddProductionMaterialRepairs** and the matching
API before activating the desktop UI. It adds applied configurations, repair audit,
reservation release markers and requisition/approval content revisions. Existing
rows start at revision 0; no legacy batch is silently rebased by the migration.
Older clients cannot approve a repaired stock requisition without its current
`contentRevision`; they receive a conflict and must reload using a compatible
client. Retain the existing customer release compatibility review; generation is
not permission to approve a new deployment fingerprint.

Do not roll this migration down after repairs have been used: that would discard
applied definitions and repair/approval revision evidence. Back up and rehearse
the forward migration against the customer's migrated schema before rollout.
No migration or release has been applied to customer data by this implementation.

## Data integrity rules

Raw material stock units come from the product department's material linkage,
including counted units such as `piece`. Saving a BOM rejects a stale or different
unit before removing any existing formula. Stock requisition creation and issue
also reject raw lines, applied units or live reservations that disagree with that
linkage. For a legacy batch, incompatible BOM units or a mismatch between the
requested quantity and its existing reservation require the operator to save the
correct product BOM and apply **Repair BOM**, then reload the batch. Repair keeps
its existing preview, permission, reason, audit and renewed approval requirements.
Physical stock units are never silently relabelled or converted from mass to count;
incompatible stock evidence still blocks repair and needs reconciliation.

New production runs capture raw and packaging requirements when they start. Later
product edits do not change those applied requirements until explicit repair.
The raw/package stock readers use the same applied version as requisition
validation. Planned-batch forecasts still read the product's current definitions.

For legacy unissued runs, preview labels its baseline as the **existing
requisition or reservations**. It does not invent an original BOM. Apply adopts
the reviewed, saved product definition and records the operational baseline.
Missing/incompatible unit evidence or ambiguous main/substitute allocations are
refused. Existing valid substitute selections are preserved; new requirements
use their primary material rather than silently allocating every substitute.
A valid selection recorded only on the current requisition is preserved even if
its reservation is missing. Conflicting selections are refused.

Quantities preserve the existing full/half rules: raw prescribed quantity is
halved for a half batch; packaging halves the prescribed quantity and then adds
the complete `Loose` allowance. `PackingExcessMargin` is not added a second time.
Only the selected packing style plus common packaging lines apply.

BOM units are explicitly converted into the department's stock unit. This first
implementation requires selected shelf, physical batch and department stock unit
IDs to agree; differing stock units require stock reconciliation rather than a
silent change to existing voucher quantity semantics. Missing or dimensionally
incompatible conversions are refused. Changes to an already applied unit's
symbol/meaning also block repair until reconciled.

Allocation preserves usable existing physical batch/shelf rows when possible,
adjusts their quantities, adds only the missing allocations and releases obsolete
unissued rows using `ReleasedAt`. Release is not deletion or consumption. Active
reservation queries exclude releases; consumed-history queries explicitly exclude
them too. Other batches' **unissued** holds are subtracted from shelf availability;
issued reservations are not subtracted again from stock which already left storage.
Current availability, reserved reports and forecasts exclude released holds even
when they read historical rows. Material trace retains the original reservation
and adds a distinct **Reservation released** event with quantity, unit, schedule
and actor. Release does not post a stock-ledger or consumption entry.

An open purchase requisition for the affected kind blocks repair until resolved;
completed purchase receipts are supply evidence, not proof that stock was issued
to production. After adoption, new purchase/transfer requests must use the adopted
materials, compatible units and quantities bounded by the requirement plus approved
unissued extras. This bounds each request; it does not introduce a procurement
budget across all historical purchase documents.

For adopted runs, linked stock swaps must select complete current reservation rows
and replace equal quantities of the same material/unit with eligible stock that
respects other batches' holds. Partial-row swaps are refused in this version.
Creation and approval both revalidate; issued/consumed allocations cannot be
retargeted even when the requisition header is stale. Rejected swap decisions
cannot be reopened through approval.

An altered unissued main requisition keeps its ID/code and increments
`ContentRevision`. Previous approval rows remain in the database with their old
revision. New pending stages authorize only the new revision, and old pending
decisions are filtered out of queues. Retired material lines keep their IDs and a
`RetiredByRepairId`; current document reads omit those historical lines. Complete
before/after quantities, source hashes and prior approval decisions remain in the
repair audit. A configured stock-requisition workflow with a required stage is
necessary before repair can change a requisition; repair cannot fall through to
automatic approval because the workflow is absent.

After writing, apply rechecks reservation totals and the corrected requisition
against the adopted requirements before committing. Any failure, including audit
or approval creation, rolls back the entire repair.

## Concurrency, audit and failure handling

`ProductionMaterialTransaction` acquires PostgreSQL table write locks **before**
cooperating production writers read their evidence. Start, repair, requisition
creation/issue, stock approval decisions, swap creation/approval, production consumption,
extra-material issue, returns and final packing use this protocol. Locks also
coordinate with legacy master/shelf writes. Stock issue loads its lines and
reservations after locking, validates exact normalized quantities, checks usable
physical stock and deducts from the allocated shelf. Approval submissions carry
the reviewed content revision, so a stale approval screen cannot approve a later
repair's quantities.

Locks currently serialize a broad set of production/source/identity tables, with
a 3-second acquisition timeout. This is a deliberate integrity tradeoff, not a
measured performance budget. Rehearse contention and duration on realistic data
before rollout; do not run repairs during busy stock processing. Never retry an
uncertain write blindly. Production status notifications collected by this scope
are published only after commit and discarded on rollback.

`ProductionMaterialRepairs` contains one immutable operational record per repair
request ID: actor, batch/kind, reason, preview hash, before/after evidence and saved
result. A normal production activity log references that ID. Repeating the same
request ID/body returns its saved result after authorization; changing its body or
using another actor/batch is refused. A genuine no-op adds no repair audit or new
reservation. A timeout in the client leaves the confirmation unable to resend;
inspect the batch activity log and get a fresh preview before another action.

## API and source map

| Operation | Contract |
| --- | --- |
| BOM preview | `GET /api/v1/production-schedule/activity/{activityId}/repair-bom/preview` |
| BOM apply | `POST /api/v1/production-schedule/activity/{activityId}/repair-bom` |
| Packaging preview | `GET /api/v1/production-schedule/activity/{activityId}/repair-packaging/preview` |
| Packaging apply | `POST /api/v1/production-schedule/activity/{activityId}/repair-packaging` |

Apply requires `requestId`, the exact 64-character `previewHash`, and a reason of
5–2000 characters. Quantities and actor/department are computed/verified on the
server. Stable operation IDs are `PreviewProductionBomRepair`,
`ApplyProductionBomRepair`, `PreviewProductionPackagingRepair` and
`ApplyProductionPackagingRepair`.

The implementation is split into `APP/Repository/ProductionMaterialSource.cs`
(authorization/source), `ProductionMaterialAllocation.cs` (allocation),
`ProductionMaterialRepairPlan.cs` (evidence/preview), `ProductionMaterialRepairs.cs`
(apply/audit/start adoption), `ProductionMaterialQueries.cs` (applied reads),
`ProductionMaterialRequisitionRules.cs` (stale supply request protection),
`ProductionMaterialSwapRules.cs` (current allocation/issue checks) and
`ProductionMaterialTransaction.cs` (writer serialization/notification timing).
Models live in `DOMAIN/Entities/Products/Production/ProductionMaterialConfiguration.cs`.

## Verification

The opt-in PostgreSQL tests enforce an exact local disposable database name:

```sh
ORYX_MATERIAL_REPAIR_TEST_CONNECTION='Host=/tmp;Port=55441;Database=oryx_material_repair_test;Username=TEST_USER' \
dotnet test tests/APP.Tests/APP.Tests.csproj --filter FullyQualifiedName~ProductionMaterialRepairPostgresTests
```

They reset only that named test database. Never use a customer connection for
tests. Coverage includes read-only preview, atomic reconciliation/reapproval,
retained old decisions, stale approvals, idempotency/no-op, saved-source changes,
other-batch holds, issued/consumed refusal, independent packaging repair,
reservation release versus consumption, half-batch/style selection, grant and
department revocation, audit-failure rollback, post-commit notification timing and
an issue committed while repair waits for a lock. Supply/swap cases cover open
purchase refusal, adopted request bounds, completed purchase versus stock issue,
full-allocation swaps and preservation of a requisition's valid substitute when
its reservation is missing.

Final local regression: **70 passed, 4 skipped**, including all **16** repair
PostgreSQL tests. The four skips are unrelated opt-in reporting database tests.
API build and EF model-drift checks passed. Generated SQL for the new migration
was run down and up against the disposable test schema; this does not rehearse
the customer's entire migration history. Existing dependency/compiler warnings,
including the AutoMapper 14 vulnerability advisory, remain outside this change.

The companion desktop checks generated-client reproducibility, route generation,
strict typechecks, build, menu grants, preview/reason submission, stale evidence,
issue blockers and existing voucher/approval behaviour. Local visual checks use
synthetic data and long names at laptop sizes/scaling equivalents. Customer-schema
migration rehearsal, authenticated customer end-to-end acceptance, physical stock
reconciliation and Windows installed-client acceptance remain release checks.
Desktop results: **37 feature tests** and **23 contract/cache compatibility tests**
passed; strict typechecks, web build, API reproducibility and route checks passed.
React Doctor reported no issues (91/100), and the targeted UI detector returned no
findings. Synthetic visual checks covered 1366×768, 1280×720, 1024×576 and 854×480;
the smaller viewports simulate scaling, not native Windows acceptance.
