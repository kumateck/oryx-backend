# Permission Keys

One key per state-machine transition, following the project's existing convention —
every action/view gets its own dedicated permission key, never shared (see
[lifecycle-and-governance.md](./lifecycle-and-governance.md#permission-granularity)).

```
CONFIGURATION
  qc.stp.view / .create / .edit / .approve / .supersede / .import
  qc.worksheetTemplate.view / .create / .edit / .approve / .supersede / .import
  qc.specification.view / .create / .edit / .approve / .supersede
  qc.monitoringProgram.view / .create / .edit / .pause
  qc.samplingPoint.view / .manage       (master data — view splits off, then create/edit/
                                         delete share one key, mirroring the pair Milestone 2
                                         gave SamplingPointGroup; neither has a lifecycle to
                                         gate transitions on, but reading reference data is a
                                         distinct concern from administering it)

WATER QUALITY
  qc.waterQuality.view
  qc.waterQuality.activatePeriod
  qc.waterQuality.holdPeriod
  qc.waterQuality.recordUse

OPERATIONS
  qc.testRequest.view
  qc.testRequest.createScheduled        (system-triggered, key exists for manual override)
  qc.testRequest.createUnscheduled      (requires Reason — mandatory)
  qc.sample.record
  qc.testRequest.assign

TEST ROOM
  qc.worksheet.assign
  qc.worksheet.reassign
  qc.worksheet.start.chemical / qc.worksheet.start.microbial
  qc.worksheet.enterResult.chemical / qc.worksheet.enterResult.microbial
  qc.worksheet.submit.chemical / qc.worksheet.submit.microbial
  qc.worksheet.review.chemical / qc.worksheet.review.microbial
  qc.worksheet.returnForCorrection

QUALITY REVIEW / OOS
  qc.oos.view                           (the queue and a single case — read splits off from
                                         investigate, because the retest authorizer and the QA
                                         signer both act on a case they would otherwise hold
                                         no key to load)
  qc.oos.investigate
  qc.oos.retest.authorize
  qc.oos.disposition

CERTIFICATES
  qc.coa.issue
  qc.coa.revise
  qc.coa.view

APPROVAL AUDIT
  qc.approvalHistory.view               (full signature trail for an arbitrary QC entity;
                                         distinct from the self-scoped "my pending" queue)
```

Split by `.chemical`/`.microbial` in Test Room follows directly from the
separate-tabs decision in [test-room-ux.md](./test-room-ux.md) — someone can be
granted Microbial Start/Submit without ever seeing Chemical actions, matching how
labs are actually staffed.

Holding `qc.worksheet.start.*`/`.enterResult.*`/`.submit.*` is necessary but not
sufficient: those actions also require the acting user to be the `WorksheetInstance`'s
current assignee (see
[lifecycle-and-governance.md](./lifecycle-and-governance.md#assignment-enforcement)).
`qc.worksheet.assign`/`.reassign` are separate keys, since deciding who works a test
is a different authority from doing the work.

`qc.stp.import` is a separate key from `qc.stp.create`, even though a successful
import produces the same thing a manual create would (a Draft) — bulk-importing the
existing STP library is a migration-scale activity worth being able to restrict
independently of everyday STP authoring, e.g. to a small migration team during
Phase 3 rollout without handing that same group ordinary STP-creation rights.

`qc.worksheetTemplate.import` (`CanImportQcWorksheetTemplates` in code) mirrors
`qc.stp.import` for the same reason: turning the lab's ARD Word worksheets into template
proposals ([build brief 07](./build-briefs/07-worksheet-docx-import.md)) is a migration-scale
activity, restricted separately from everyday template authoring. The import endpoint
(`POST qc/worksheets/templates/import`) writes nothing — it returns proposals only. Saving
one still goes through `qc.worksheetTemplate.create`, and accepting its SamplingPoint
proposals through `qc.samplingPoint.manage`, so the import key alone never creates anything.

See [roles-permission-matrix.md](./roles-permission-matrix.md) for which real job
roles (QC Officer, QA Executive, QC Manager, QA Manager/Deputy, QA Head) get which of
the keys above.

The centralized `GET qc/worksheets/approvals/my-pending` queue is the one confirmed
exception to the dedicated-view-key rule: it is inherently self-scoped and returns
only rows for which the caller is already a configured approver. The separate
`GET qc/worksheets/approvals/{entityType}/{entityId}` endpoint is not self-scoped —
it accepts any QC entity address and returns its full signature trail — so it requires
`qc.approvalHistory.view` (`CanViewQcApprovalHistory` in code).
