# Refresh token ownership — 2026-10-21

Refresh tokens issued by `JwtService.SaveRefreshToken` now explicitly assign `UserId` to the authenticated user. `AuthRepository.LoginWithRefreshToken` already uses this field to find the account; previously the field remained an empty GUID, so later renewal could fail even when an encrypted desktop token was retained correctly.

`CreatedById` is an audit field, overwritten by the context's current-user hook. Anonymous sign-in cannot use it as token ownership. Ownership must not be inferred from another field or guessed for existing rows.

The fix covers both normal authentication and new-user authentication because both issue tokens through the shared helper. Existing endpoints, token lifetime, approval/authorization behavior and client token storage contracts are unchanged. There is no database schema change, backfill or deployment. Existing ownerless tokens remain unusable; after a corrected server is running, a new sign-in issues an owned token. Do not bulk-assign old tokens to an account.

`RefreshTokenOwnershipTests` uses an isolated in-memory database, an anonymous audit user, actual JWT issuance and the refresh repository, then a new database context to verify owner preservation and successful renewal. No live credentials, database writes, messages or network services are used. Server expiry/disabled-user validation is a separate pre-existing concern; this narrowly scoped repair does not extend acceptance of old tokens.

Verification: `dotnet test tests/APP.Tests/APP.Tests.csproj --no-restore --filter FullyQualifiedName~RefreshTokenOwnershipTests` passed (1 test). The build reports existing AutoMapper advisory and existing nullable/analyzer warnings; this patch does not change dependencies. No deployment was performed.
