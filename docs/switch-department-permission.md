# Switch department permission — 2026-10-31

`POST /api/v1/user/switch-department/{departmentId}` moves the signed-in user's account to another department. It used to be allowed for the `super`, `admin` and `Warehouse Manager` roles by name. It now requires the permission **`CanSwitchDepartment`** (IT Support › Manage User Directory) and role names no longer grant it.

## Rollout

No role holds the permission when this ships, so **nobody can switch until an administrator grants it**: open Roles, edit each role that should switch (for example super, admin, Warehouse Manager), tick *Can Switch Department* and save. Users pick the change up on their next request once the permission cache refreshes, or after signing in again. There is no migration.

Callers without the permission get 403. Nothing else about the endpoint changed: it still saves the department on the account and clears the caller's cached token data.

## Tests

`tests/APP.Tests/Users/SwitchDepartmentPermissionTests.cs` checks that the endpoint's only authorization is the `CanSwitchDepartment` policy (no roles) and that the key is grantable under IT Support › Manage User Directory with the `Access` type.

The desktop app shows Switch department only to holders of the same permission (desktop `docs/session-and-access.md`).
