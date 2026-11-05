# Oryx backend agent guide

This guide applies to every change in `oryx-backend`. Follow the user's current instructions when they supersede it.

## Branches and pull requests

- Branch from `demo` (pull local `demo` and `main` first). Pull requests go into `desmond-latest`. If `origin/desmond-latest..HEAD` shows `demo` merge commits, rebase onto `origin/desmond-latest` so the PR holds only your change.
- `oryx-next` is obsolete and kept for reference only. Never change it.

## Permissions: required for every task

A task is not finished until its permissions are done. The server is the authority. The desktop hiding a button is not protection.

### Every endpoint names one permission

- Each new or changed controller action carries exactly one specific key: `[Authorize(PermissionKeys.X)]`.
- `[Authorize]` alone is only for reviewed self-service actions on the caller's own data: own profile, own avatar, own notifications, own password, own payslip. The action must refuse another user's id.
- `[AllowAnonymous]` is only for sign-in, token refresh, password reset, invitation onboarding and signed-callback flows that verify their own token. Never use it for uploads, reads of business data or writes.
- Do not leave a commented-out `[Authorize(...)]`. Do not rely on the fallback "signed in" policy for a business task.

### One key per task

- Name keys `Can{Verb}{Context}{Variant}{Resource}{Section}`, for example `CanEditProductionProductBom`, `CanViewRawMaterialRequisitions` or `CanApproveLocalPurchaseOrder`.
- View, create, edit, delete, approve, verify, issue, assign and export are separate tasks with separate keys. Do not cover them with one `CanManageX` or `CanEditPlan` key.
- Variants get their own keys: raw/packaging, foreign/local and each store type.
  - When an endpoint takes a variant parameter, check the key for that variant.
  - When an endpoint acts on an existing record, load the record and check the key for its stored kind. Never trust the request for this.
- Never reuse an existing key for a different task. Never add an alias, meaning a second name for the same permission.

### Adding a key

1. Add a `public const string` to `PermissionKeys` in `APP/Utils/PermissionUtils.cs`, with a one-line `<summary>` saying what it allows.
2. Register it exactly once in `GeneratePermissions()` with `addPermission(module, submodule, key)`, under the module and submodule where an administrator expects to find it.
3. Use the same key on the desktop: the module manifest, action `require`, and `usePermissionAction`. Never use a desktop key the backend does not define.

### Check again before every write

- Services and repositories that change data reload the actor's grants with `CurrentPermissionSet.Load(context, actorId)`. Do this immediately before the write, inside the transaction, and again before each later write in multi-step or bulk work.
- Permission checks never replace ownership, assignment, record-state, stock, approval-stage or department rules. Do both.
- Never grant access by role name, never add an administrator bypass, and never infer a permission from another one.

### Granting access (users, roles, permissions)

Use `APP/Services/Authorization/AccessGuard.cs`:

- Nobody changes their own role, deletes their own account, or blocks or unblocks themselves (`AccessGuard.Self`).
- Nobody grants a permission they do not hold. This applies to assigning a role and to editing a role's permissions (`AccessGuard.RoleAssignment`, `AccessGuard.Escalation`).
- Only keys that exist in `GeneratePermissions()` can be stored.

### Existing roles must keep working

When an endpoint gets a key for the first time, or a key is split into several, add a data-only EF migration. It grants the new key, with the same access types, to every role that holds the key guarding that screen today, so nobody loses a task on deploy. Follow `20261105170000_GrantAssignUserRoleToUserEditors` and `GrantManageCostingToContractManagers`. A key for a sensitive new task, such as deleting master data, is granted to nobody until an administrator assigns it. Say so in the PR.

### Tests

For each new or changed endpoint, add:

- a reflection test asserting its `[Authorize]` policy and that it is not `[AllowAnonymous]`;
- a test that the key is registered exactly once;
- a deny test (caller without the key, or with the wrong variant key, is refused);
- an allow test.

For access management, also test the self-change and escalation refusals. `tests/APP.Tests/Authorization/AccessControlTests.cs` shows the pattern.

### Pull request

The description lists:

- every new or changed key, with its module and submodule;
- which endpoints use it;
- which roles receive it through the migration, or "granted to nobody";
- the matching desktop change.

## Data and migrations

- Never edit an applied migration. Add a new one, and keep `dotnet ef migrations has-pending-model-changes` clean.
- Postgres-only behaviour (triggers, savepoints, concurrency) is tested against a real Postgres, using `ORYX_ISSUE_REPAIR_TEST_CONNECTION` or the test's own variable.
