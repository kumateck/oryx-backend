# Admin user password reset — 2026-10-31

One account, `dkadusei@kumateck.com`, can set a new password for any other user. This backs the "Change User Password" page in the desktop/web app.

## Endpoint

`POST /api/v1/auth/admin/user-password` (requires a signed-in user)

```json
{ "userId": "<target user id>", "newPassword": "<new password>" }
```

| Result | When |
| --- | --- |
| 204 No Content | Password changed |
| 400 Bad Request | Password breaks a rule; `errors[]` has one entry per broken rule (codes `UserPassword.PasswordTooShort`, `UserPassword.PasswordRequiresDigit`, `UserPassword.PasswordRequiresUpper`, `UserPassword.PasswordRequiresNonAlphanumeric`) |
| 403 Forbidden | Caller is not the password administrator |
| 404 Not Found | Target user does not exist or is deleted |

## Rules

- **Who may call it.** `UserPasswordAdminRepository` loads the caller from the database by the token's subject and compares the stored email with `dkadusei@kumateck.com` (case-insensitive). Email claims sent by the client are not used. A disabled or deleted admin account is refused too. To change the allowed account, edit `UserPasswordAdminRepository.PasswordAdminEmail` and the matching constant in the web app.
- **Password rules.** The reset goes through Identity (`GeneratePasswordResetTokenAsync` + `ResetPasswordAsync`), so the rules in `API/Program.cs` apply: at least 8 characters, a digit, an uppercase letter and a symbol. The security stamp rotates as part of the reset.
- **Sessions.** The target's refresh tokens are removed. Removal is a soft delete, so `AuthRepository.LoginWithRefreshToken` now ignores refresh tokens with `DeletedAt` set; before this change a deleted token could still renew a session. Access tokens already issued stay valid until they expire.
- **Audit.** Each successful reset writes an activity-log entry (Module `Settings`, SubModule `User Password`, Action `Reset password for <email> (<id>)`) attributed to the admin, plus a warning-level server log line. Neither contains the password. If the activity log store is down the reset still succeeds and the failure is logged.
- **Keeping the password out of logs.** `ActivityLogMiddleware` stores request bodies for most routes. This route lives under `/auth`, which the middleware already skips, and the middleware now also skips any path containing `password`.

## Tests

`tests/APP.Tests/Repository/UserPasswordAdminRepositoryTests.cs` (in-memory database, real `UserManager` and password validator, recording activity log):

- the admin (any email casing) resets the password, the target's refresh token can no longer renew, and the audit entry names the target without the password;
- another user, an unknown caller and a disabled admin are forbidden and nothing changes;
- a weak password returns one validation error per broken rule and leaves the hash unchanged;
- a missing target returns 404.

```bash
dotnet test tests/APP.Tests --filter "FullyQualifiedName~UserPasswordAdminRepositoryTests|FullyQualifiedName~RefreshTokenOwnershipTests"
```
