# Employee assignment reporting-manager foreign key

## Cause and scope

The latest occurrence in the pasted production report is **PUT
`/api/v1/employee/{id}/assign`**, not the form-draft endpoint in the report header.
The decisive error is SQLSTATE `23503`, constraint
`FK_Employees_users_ReportingManagerId`. The report also groups database
availability errors (`3D000`, `55000`) and a response uniqueness error (`23505`);
those have separate causes and are not repaired by this change.

At production commit `59c045dac4ec175a91ef6a7bf52665026a45064c`,
`AssignEmployeeDto.ReportingManagerId` was a non-nullable `Guid`, while
`Employee.ReportingManagerId` was nullable and referenced **users**, not employees.
The submitted body omitted the manager member. JSON binding therefore supplied
`Guid.Empty`, and the `AssignEmployeeDto -> Employee` AutoMapper convention copied
that all-zero value into the optional foreign key. The first employee save failed
because no user had that ID. This also happened for an explicit all-zero ID.
A supplied nonexistent user ID (including an employee ID mistaken for a user ID)
was not validated and could hit the same constraint. Explicit JSON null could
not bind to the old non-nullable request property.

After fetching demo/main on 2026-11-06, main was still the reported `59c045da`;
`git log 59c045da..main` was empty. The affected DTO, repository and mapper were
also unchanged in refreshed demo `adf1b682559a699c8b3fbedbcf2e83403b0d175f`.
Work is isolated on `codex/fix-employee-assignment-manager`, based on that demo.

## Correction

The request manager ID is now nullable. Omitted/null and legacy all-zero values
mean **no manager**. Assignment is a replacement operation: these values clear an
existing manager; a supplied valid user ID sets/retains it. The current desktop
form already omits the field when no manager is selected and sends the selected
user ID otherwise, so its request construction needs no change.

Before changing any tracked assignment/warehouse state, the repository validates
nonempty IDs against the existing, non-soft-deleted user query. Invalid IDs return
HTTP 400 (`Employee.ReportingManager`) rather than reaching a failing save.
AutoMapper ignores this property; the repository assigns only the validated,
normalized ID. The existing FK remains authoritative. No schema migration,
permission key, role bypass or production data change is introduced. Department,
designation, staff-number, employee-level, warehouse and account-update behavior
remain in the existing flow; audit writes still use `ApplicationDbContext`.

The request contract now accepts JSON null as well as omission. Its response and
route are unchanged. Generated API files have not been hand-edited.

## Similar-pattern check

Searched every reporting-manager writer and compared explicit request-to-entity
AutoMapper mappings for non-nullable request GUIDs mapped to nullable entity GUIDs.
This is the only request writer for `ReportingManagerId`. Employee create/update
profile DTOs do not carry it; employee/designation response DTOs already use
nullable GUIDs. Assignment department/designation IDs have existence checks.
Creating an employee user also verifies its employee and role IDs.

Two additional mappings have the same risk shape and lack corresponding guards
in their create paths: `CreateProductRequest.CategoryId -> Product.CategoryId`
and nested `CreateProductionStepRequest.ResourceId -> ProductionStep.ResourceId`
in work-order creation. Omission supplies zero although the destination FK is
nullable. These are separate product/work-order input-validation issues, found
by source inspection, not reproduced production occurrences from this report.
They are documented for focused follow-up rather than changing those workflows
as part of the employee fix. This was a targeted mapping check, not a complete
validation audit of all repositories.

## Customer data and rollout

The FK refusal aborts the first employee `SaveChangesAsync`. EF's transactional
save rolls back its employee/warehouse writes; the later user-department update
and welcome email are not reached. Ordinarily no corrupt manager link was saved
and no database repair/migration is needed. After deploying the reviewed fix,
HR should check the employee's current assignment and retry the intended request.
If a manager is selected, use a real user account ID, not an employee ID.

If integrity was previously bypassed or the FK was unvalidated, an operator can
run this **read-only** check through the normal approved database access process:

```sql
SELECT e."Id", e."ReportingManagerId"
FROM public."Employees" AS e
LEFT JOIN public.users AS u ON u."Id" = e."ReportingManagerId"
WHERE e."ReportingManagerId" IS NOT NULL AND u."Id" IS NULL;
```

Any results need HR-reviewed correction with the normal audit process; do not
create an all-zero dummy user, disable the FK, or guess a manager. Soft-deleted
manager accounts may still satisfy the FK but need review before reassigning.
No customer database or server was inspected or changed during this work.

## Regression verification

The tests were written first. On the unfixed code seven failed and the existing
user-ID success case passed. A separate PostgreSQL 16 run reproduced exactly
`23503 / FK_Employees_users_ReportingManagerId` from the same omitted-member
request shape, including permanent employee level and assignment fields.

Tests cover omission, explicit null, the legacy all-zero ID, a valid user ID,
missing/deleted user IDs, an employee ID mistaken for a user ID, unchanged state
on validation refusal, and a real PostgreSQL save. HR permission coverage includes
`AssignEmployee` requiring `CanUpdateEmployeeInfo`.

```bash
# Optional real-database regression: use only an isolated disposable PostgreSQL
# server. The test creates and drops its own random database; CREATE DATABASE is required.
export ORYX_EMPLOYEE_ASSIGNMENT_TEST_CONNECTION='<local scratch connection string>'
dotnet test tests/APP.Tests/APP.Tests.csproj \
  --filter 'FullyQualifiedName~EmployeeAssignmentTests|FullyQualifiedName~HrOwnershipAndOnboardingTests|FullyQualifiedName~HrPermissionTests'
```

Without that variable, the PostgreSQL test is explicitly skipped; the in-memory
regressions still run. No full-suite, deployment or real-server acceptance result
is implied by these focused checks. This change does not deploy or migrate customer servers.

Verified after the correction on 2026-11-06: all **37** focused tests passed,
including the real PostgreSQL 16 regression (no skipped tests), existing HR
ownership/onboarding and HR permission coverage. The test command also compiled
the API and its project dependencies. `git diff --check` passed. Existing package
advisory, nullable/XML and analyzer warnings were not changed by this fix.
Include the corrected nullable request property in the normal desktop API-contract
review when publishing/importing the released Swagger document.
