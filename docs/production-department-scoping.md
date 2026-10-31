# Production department scoping (2026-10-31)

Production departments (Beta Lactam, Oral Solid, Oral Liquid, Ointment) work apart, and so
do the warehouses inside them. A role of the **Production** type is held by people in several of
those departments (for example nine General Manager Production users across four departments),
so anything assigned to that role is meant for the holders in the record's own department.

The rule, applied everywhere below: a person who sits in one production department does not
receive another production department's work through a Production-type role. Work assigned to a
person by name, roles of other types (QA, QC, procurement, finance), people with no department
or outside production departments, and records that do not belong to a production department
are unchanged, so cross-department flows keep working.

| Area | Where | Change |
| --- | --- | --- |
| My pending approvals, counts, approve/reject | `ApprovalDepartmentScope`, `PendingApprovalQueries` | A document whose author sits in another production department is left out when the user reaches its active stage only through a Production-type role. Approve and reject already require the item to be in the caller's queue (including delegations), so they refuse it too. |
| Alerts | `AlertProcessor` | Batch manufacturing/packaging record and production step alerts resolve the product's department and skip configured recipients from other production departments. Named assignees are kept. Alerts that already carry a department are filtered as before. |
| Production board | Migration `ScopeProductionStepAssigneesToDepartment`, step-user query filter | Runs started before role scoping (May 2026) listed every production manager. Unfinished steps of unfinished runs drop people from another production department when someone who stays covers the same action. Removed step assignees are now hidden, which they previously were not. Down restores exactly the rows the migration hid. |

New runs were already scoped when steps are created (`ProductionScheduleRepository`, Aug 2026),
and products are already filtered to the user's department by the global query filter.

Checked against the 2026-10-30 client copy (in a rolled-back transaction): the repair hides 555
assignments on 3 May runs, and all 70 step/action pairs remain covered.

Not changed: expired-material alerts carry no department (a batch can sit in several
departments' warehouses); users assigned explicitly on a product route are kept as configured.
