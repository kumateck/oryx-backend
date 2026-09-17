# Roles → Permission-Key Matrix

Maps the permission keys in [permissions.md](./permissions.md) against the real QC/QA
job roles already seeded in the system (`entrancedb.sql`), rather than generic
placeholder titles. `admin`/`Super` are system-wide roles and are not itemized —
they carry every key implicitly, as elsewhere in Oryx.

Five roles are QC/QA-relevant:

| Role | Day-to-day function |
|---|---|
| **QC Officer** | Executes worksheets in the Test Room — sampling, data entry, submission. |
| **QA Executive** | First-line review of submitted results; initiates OOS investigations. |
| **QC Manager** | Owns worksheet/specification authoring and monitoring-program configuration; assigns work. |
| **QA Manager** / **Deputy QA Manager** | Approval authority — worksheet/specification sign-off, OOS disposition, COA issuance. Deputy carries the same grants for coverage. |
| **QA Head** | Same grants as QA Manager — top of the approval chain, organizational oversight rather than a distinct permission tier. |

## Configuration

| Key | QC Officer | QA Executive | QC Manager | QA Manager / Deputy | QA Head |
|---|:-:|:-:|:-:|:-:|:-:|
| `qc.stp.view` | ✓ | ✓ | ✓ | ✓ | ✓ |
| `qc.stp.create` / `.edit` | | | ✓ | | |
| `qc.stp.import` | | | ✓ | | |
| `qc.stp.approve` / `.supersede` | | | | ✓ | ✓ |
| `qc.worksheetTemplate.view` | | ✓ | ✓ | ✓ | ✓ |
| `qc.worksheetTemplate.create` / `.edit` | | | ✓ | | |
| `qc.worksheetTemplate.approve` / `.supersede` | | | | ✓ | ✓ |
| `qc.specification.view` | | ✓ | ✓ | ✓ | ✓ |
| `qc.specification.create` / `.edit` | | | ✓ | | |
| `qc.specification.approve` / `.supersede` | | | | ✓ | ✓ |
| `qc.monitoringProgram.view` | ✓ | ✓ | ✓ | ✓ | ✓ |
| `qc.monitoringProgram.create` / `.edit` / `.pause` | | | ✓ | | |

STP grants mirror WorksheetTemplate/Specification exactly — same authoring tier
(QC Manager), same approval tier (QA Manager/Deputy/Head) — since all three share the
same Draft→UnderReview→Approved→Effective→Superseded lifecycle. `qc.stp.view` is open
to QC Officer too, unlike the other two: an analyst executing a worksheet needs to be
able to read the STP it implements without needing configuration access to it.
`qc.stp.import` sits with QC Manager by default here, but is deliberately a separate
key — see [permissions.md](./permissions.md) — so it can be granted to a smaller
migration-specific group during Phase 3 rollout without also handing that group
ordinary STP-authoring rights.

## Water Quality

| Key | QC Officer | QA Executive | QC Manager | QA Manager / Deputy | QA Head |
|---|:-:|:-:|:-:|:-:|:-:|
| `qc.waterQuality.view` | ✓ | ✓ | ✓ | ✓ | ✓ |
| `qc.waterQuality.activatePeriod` | | | ✓ | ✓ | ✓ |
| `qc.waterQuality.holdPeriod` | | ✓ | ✓ | ✓ | ✓ |
| `qc.waterQuality.recordUse` | ✓ | | | | |

Activating a period is a release-adjacent decision (it's what lets production start
drawing on that point's water as compliant), so it sits with QC Manager and above,
matching `qc.testRequest.assign`'s tier. Holding a period is closer to raising an
alarm than approving one — QA Executive can hold on suspicion without waiting for a
manager, consistent with `qc.oos.investigate` being open at that tier too. Recording
actual water use against an active period is an operational log entry, not a quality
decision, so it sits with whoever's on the floor — QC Officer.

## Operations

| Key | QC Officer | QA Executive | QC Manager | QA Manager / Deputy | QA Head |
|---|:-:|:-:|:-:|:-:|:-:|
| `qc.testRequest.view` | ✓ | ✓ | ✓ | ✓ | ✓ |
| `qc.testRequest.createScheduled` | | | ✓ | | |
| `qc.testRequest.createUnscheduled` | ✓ | ✓ | ✓ | | |
| `qc.sample.record` | ✓ | | | | |
| `qc.testRequest.assign` | | | ✓ | | |

`createUnscheduled` is granted to QC Officer and QA Executive too, not just QC
Manager — an unscheduled/emergency test (contamination suspicion, a deviation
trigger) needs to be raisable by whoever notices the problem, not gated behind a
manager. The mandatory `Reason` field (see
[lifecycle-and-governance.md](./lifecycle-and-governance.md)) is the actual control,
not the permission gate.

## Test Room

| Key | QC Officer | QA Executive | QC Manager | QA Manager / Deputy | QA Head |
|---|:-:|:-:|:-:|:-:|:-:|
| `qc.worksheet.assign` | | | ✓ | | |
| `qc.worksheet.reassign` | | | ✓ | | |
| `qc.worksheet.start.{chemical,microbial}` | ✓ * | | | | |
| `qc.worksheet.enterResult.{chemical,microbial}` | ✓ * | | | | |
| `qc.worksheet.submit.{chemical,microbial}` | ✓ * | | | | |
| `qc.worksheet.review.{chemical,microbial}` | | ✓ | ✓ | ✓ | ✓ |
| `qc.worksheet.returnForCorrection` | | ✓ | ✓ | ✓ | ✓ |

Chemical/Microbial-specific grants (`.chemical` vs `.microbial`) are assigned per
individual QC Officer based on which lab they're staffed in, not by role alone — the
role only determines *whether* someone can hold a Test Room key at all.

\* Holding the key is necessary but not sufficient — the QC Officer must also be the
current assignee of that specific `WorksheetInstance` (see
[lifecycle-and-governance.md](./lifecycle-and-governance.md#assignment-enforcement)).
Assignment and reassignment sit with QC Manager, matching the existing
`qc.testRequest.assign` ownership above — the same person who assigns the overall
work assigns its individual tests.

## Quality Review / OOS

| Key | QC Officer | QA Executive | QC Manager | QA Manager / Deputy | QA Head |
|---|:-:|:-:|:-:|:-:|:-:|
| `qc.oos.investigate` | ✓ | ✓ | | | |
| `qc.oos.retest.authorize` | | | ✓ | ✓ | ✓ |
| `qc.oos.disposition` | | | | ✓ | ✓ |

Investigation (Phase 1 lab-error check) is done by whoever is closest to the bench —
the QC Officer who ran the test or the QA Executive reviewing it. Disposition — the
decision that closes an `OOSCase` and releases or rejects the linked batch — stays
with QA Manager/Deputy/Head only, matching the live `OosInvestigation.ReviewByQa`
authority today.

## Certificates

| Key | QC Officer | QA Executive | QC Manager | QA Manager / Deputy | QA Head |
|---|:-:|:-:|:-:|:-:|:-:|
| `qc.coa.view` | | ✓ | ✓ | ✓ | ✓ |
| `qc.coa.issue` | | | | ✓ | ✓ |
| `qc.coa.revise` | | | | ✓ | ✓ |

## Open question

This matrix assumes QC Manager and QA Manager/Deputy/Head are distinct chains (QC
owns configuration + day-to-day assignment, QA owns approval + disposition +
certificates) — matching how `QC Manager` and `QA Manager` exist as separate seeded
roles today. If in practice one person holds both roles, or QC Manager also needs
disposition authority in a small team, that's a role-assignment decision (who gets
which role), not a change to this matrix.
