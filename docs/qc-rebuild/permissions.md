# Permission Keys

One key per state-machine transition, following the project's existing convention —
every action/view gets its own dedicated permission key, never shared (see
[lifecycle-and-governance.md](./lifecycle-and-governance.md#permission-granularity)).

```
CONFIGURATION
  qc.stp.view / .create / .edit / .approve / .supersede / .import
  qc.worksheetTemplate.view / .create / .edit / .approve / .supersede
  qc.specification.view / .create / .edit / .approve / .supersede
  qc.monitoringProgram.view / .create / .edit / .pause

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
  qc.oos.investigate
  qc.oos.retest.authorize
  qc.oos.disposition

CERTIFICATES
  qc.coa.issue
  qc.coa.revise
  qc.coa.view
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

See [roles-permission-matrix.md](./roles-permission-matrix.md) for which real job
roles (QC Officer, QA Executive, QC Manager, QA Manager/Deputy, QA Head) get which of
the keys above.

**Confirmed exception (2026-09-18):** the centralized `qc/worksheets/approvals` queue
itself has no dedicated view key, unlike every other screen in this list. This is
deliberate, not an oversight — the queue is inherently self-scoped to "my pending
approvals" (the backend only ever returns items the caller is a configured approver
for), so a separate view permission would gate nothing real; anyone who can see an
item in that queue is, by construction, someone already entitled to act on it via
that item's own `.approve` key (`qc.stp.approve`, `qc.worksheetTemplate.approve`,
etc.). The sidebar entry is gated on holding any one of those `.approve` keys, purely
as a navigational convenience, not as an access-control boundary.
