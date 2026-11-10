# Implementation report — 10 November 2026

This report records the fixes prepared for review across `oryx-backend` and
`oryx-live-desktop`. No customer server was accessed or changed.

## BOM density conversion

The product BOM editor now allows an authorized user to enable mass-to-volume
conversion for a raw-material line. The material's department link must have a
measured density in g/mL and a liquid stock unit. For density `ρ`:

- `mL = mg ÷ (ρ × 1,000)`
- `mg = mL × ρ × 1,000`

The backend validates the BOM and all substitute-material links. It preserves
the authored quantity in mg and snapshots the density, units and converted
stock requirement into the production batch. Reservations and production
requirements use the department's stock unit; warehouse stock and picking
remain in that original unit. Existing BOM lines default to conversion off.
The database change is additive (`AddBomDensityConversion`), and no permission
keys or authorization contracts were added.

The desktop implementation is in the companion `oryx-live-desktop`
`codex/bom-density-conversion` branch. Configuration and rollback guidance is
in [BOM mass-to-volume conversion](bom-density-conversion.md); the desktop repo
also carries the UI/API contract notes and its own feature guide.

## Production-start and requisition HTTP 500s

The customer report did not include an exception or trace for either failed
request, so the underlying database/application cause is not established. The
backend now logs the full exception with batch, user and trace identifiers when
production start or a stock/production requisition fails. The API returns a
safe support message instead of exposing an internal exception to the user.
These changes make a future reproduction diagnosable; they do not claim to fix
an unknown database failure.

The separate database-timeout report lists
`approval/my-pending/stats` in its summary but `/api/v1/customers?pageSize=1000`
in its request details. Because those identify different requests, no query or
customer data repair is inferred from that report.

## Employee account creation and date update

The backend now rejects user-account creation for an employee without a
nonblank email before calling ASP.NET Identity. For the reported employee,
enter a valid email on the employee record before retrying account creation; no
user account was created by the failed request.

The desktop employee edit form now serializes year-one date-only values with a
four-digit year and without applying a local-timezone date shift. This addresses
the reported `dateOfBirth` JSON conversion failure. No employee data migration
is needed. The desktop change is in
`oryx-live-desktop` branch `codex/fix-employee-year-one-date`.

## Invalid production packing-style repair

The screenshot's blocked batch was caused by a saved packing-style reference
that was missing or no longer belonged to the product. The merged backend fix
prevents deleting any style still referenced by a scheduled batch and validates
that new schedule batches use an active style for their product. For a legacy
running batch, the packaging-repair preview proposes the first active style in
alphabetical order (ID breaks ties), names it for operator review, and includes
the choice in the preview hash and audit evidence. Applying the repair updates
the style and packaging allocation atomically under the existing
`CanRepairPackaging`, stock and approval rules. No schema migration or new
permission was added.

This work is already merged in [backend PR #1784](https://github.com/kumateck/oryx-backend/pull/1784)
and [desktop PR #172](https://github.com/kumateck/oryx-live-desktop/pull/172).
The backend PR reported `git diff --check` passing; the backend build did not
finish within five minutes and no tests were run. The desktop web typecheck and
diff check passed; no desktop tests were run.

## Related work from this conversation already merged

These changes are already in their target branches and have their own detailed
PR descriptions and feature documentation:

- Customer formula runtime ownership and hardened backend installation:
  [backend #1759](https://github.com/kumateck/oryx-backend/pull/1759),
  [desktop #154](https://github.com/kumateck/oryx-live-desktop/pull/154), plus
  [backend hardening #1762](https://github.com/kumateck/oryx-backend/pull/1762)
  and [desktop hardening #158](https://github.com/kumateck/oryx-live-desktop/pull/158).
- Signed desktop update mirroring for customer LAN servers:
  [desktop #168](https://github.com/kumateck/oryx-live-desktop/pull/168).
- Migration top-up quantity adds to a matching existing batch:
  [backend #1776](https://github.com/kumateck/oryx-backend/pull/1776) and
  [desktop #163](https://github.com/kumateck/oryx-live-desktop/pull/163).
- Missing material department links can be created from migration drafts:
  [backend #1779](https://github.com/kumateck/oryx-backend/pull/1779) and
  [desktop #167](https://github.com/kumateck/oryx-live-desktop/pull/167).
- Employee reporting-manager assignment validation:
  [backend #1758](https://github.com/kumateck/oryx-backend/pull/1758).

## Verification recorded

- Backend focused tests: 32 passed; API build completed with no errors.
- EF Core reports no pending model changes after the migration was generated.
- Desktop full web suite: 2,116 tests across 367 files passed.
- Desktop typecheck, route check, generated API check, and focused conversion
  tests passed.
- Required exact-commit customer-release Docker validation passed for desktop
  commits `70a686e41f641c2e0bc39786ebc5658f28412457` and
  `6e8ac300e3461e3569867a40767255170f07dcec`, including Nginx, health/version,
  SPA routes, HTTP API proxy, compatibility and runtime smoke checks.
- Employee year-one date test and employee typecheck passed.

## Limits and operational notes

- The production-start/requisition exception cause and the conflicting timeout
  report still require a captured server exception to diagnose.
- The SMTP connection timeout diagnosis was explicitly left out of this work.
- The density conversion is opt-in per BOM line. Deploy the backend migration
  before releasing the desktop contract that sends the new flag.
- No real server or customer stock was modified. No stock-data repair is
  indicated by these code changes.
