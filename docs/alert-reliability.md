# Alert reliability and Operations Watch

Backend-first hardening, 2026-10-24. This is a local implementation and audit; the migration has not been applied to a customer database and no production delivery claim is made.

## Delivery contract

The backend owns alert rules, recipient selection, priorities, record identity, and read state. The companion is presentation only. A notification is not an approval and must never perform a regulated transition.

1. `BackgroundWorkerService.EnqueueNotification` stages `AlertDispatchRequest`, retaining department and explicit assignee IDs instead of dropping them. With pending business changes the intent is saved by the caller's normal save/transaction; it does not commit unfinished work. Legacy post-save callers save only the intent immediately. The old process-local queue is no longer the normal producer path.
2. A fresh-scope request dispatcher claims pending intents using an optimistic concurrency lease. It resolves all active matching configurations, merges recipients per delivery channel, removes disabled/deleted users, and writes notifications plus channel delivery records. PostgreSQL transaction commit also completes the intent. Missing configuration or eligible recipients leaves the intent retryable with a failure code; explicitly disabled rules are recorded as suppressed.
3. `NotificationService` saves one notification and one delivery entry per distinct channel in one save. No RabbitMQ or SMTP work occurs before persistence. In-app and email failures are independent.
4. The in-app dispatcher publishes the stored notification ID and original sent time. SMTP runs on a separate worker so a slow mail server cannot delay real-time publication. Broker/SMTP failures retry with bounded exponential backoff (up to 256 seconds), durable attempt/error state, and expired-lease recovery after restart. Audit retry does not deliberately republish an acknowledged notification.
5. The unread API returns only in-app records addressed to the authenticated user, with the actual stored sent time. Mark-as-read checks recipient ownership and persists a targeted `NotificationReadEvent` delivery with the read record. Other users receive no read event.
6. Live Desktop's authenticated gateway forwards stable notification IDs, reports broker availability, and routes read events only to the user's room. On socket reconnect, broker recovery, or read events, clients reconcile the backend list immediately. Timer-based reconciliation remains a fallback.

The transport is **at least once**, not exactly once. A crash after external delivery but before database acknowledgement can repeat an event. Clients deduplicate stable IDs. SMTP can repeat after an ambiguous send result. Delivery records represent broker/SMTP acknowledgement, not proof a human saw or read a notification.

## Authentication and audit

Alert configuration endpoints now enforce existing `CanViewAlerts`, `CanCreateNewAlerts`, `CanEditAlerts`, `CanEnableDisableAlerts`, and `CanDeleteAlerts` policies. Personal list/read endpoints remain authenticated and recipient-scoped. No role-name bypass or new permission key is introduced.

Persisted intents, delivery IDs, attempts, leases, failure codes, publication timestamps, and completion timestamps provide a reconciliation trail. Channel delivery also produces the existing Mongo activity log, referencing the delivery ID. A crash during multi-recipient audit may repeat an audit entry; correlate by ID. Logs avoid message bodies, credentials, and recipient PII.

## Corrections found in the audit

| Finding | Local correction |
| --- | --- |
| Queued department/assignee information discarded | Preserve it through durable intent processing. |
| One in-memory notification every ten seconds, then dropped on error | Normal producers stage durable intents; legacy queue forwarding retains scope and retries. |
| Publish precedes save; pushed and persisted IDs differ | Save first; publish the stored ID and time. |
| SMTP can cause in-app duplication or block live delivery | Distinct persisted channel work and separate workers. |
| Only the first configuration for an event is used | Merge every active configuration, deduplicating users per channel. |
| Email records appear as extra unread in-app items | Filter by InApp. |
| List DTO fabricates sent time at request time | Project `SentAt` from storage. |
| Expiry scan waits until one month after expiry | Scan available expired batches against current UTC every hour; persist a unique batch/expiry source key. No inventory status is changed. |
| Authenticated users can administer alert rules without specific policies | Enforce existing configuration permissions. |
| Broker failure invisible to an otherwise connected socket | Propagate delivery availability and immediately reconcile after recovery. |

## Operational alert coverage to complete before companion launch

| Area | Required source event | State / next work |
| --- | --- | --- |
| QA/QC | OOS opened/requiring review; critical | OOS domain exists, but notification type/producer and authorized reviewer resolution are not yet wired. |
| Inventory | Expired stock; high | Existing available-batch expiry scan repaired. Approaching-expiry lead time must come from configured business policy, not an invented threshold. |
| Inventory | Reorder/min/max | Existing stock scanner is commented out and refers to an obsolete stock API. Rebuild against authoritative usable-stock units and configured per-department thresholds; do not simply uncomment it. |
| Production | Stage changes; exception/blocked work; high | Stage-change producer exists. Explicit exception/block events and priority metadata still need a contract. |
| Approvals | Purchase requisition/other assigned decisions; medium | Existing leave/overtime/staff/BMR events retained. Add per-assignee approval-created/reassigned/escalated events at the approval transaction boundary. |
| Procurement | Delayed PO/unresolved receipt; medium | Shipment arrival exists; delay/grace-period and unresolved-receipt rules need authoritative state/due dates and deduplication. |
| Warehousing | Stock discrepancy; high | Add an explicit discrepancy-created/investigation-required event; never infer from displayed balances. |
| Quality | COA/ARD/worksheet review; medium | Add events at actual review transitions with reviewer authorization and record identity. |
| Maintenance | Calibration due/overdue; high | Calibration date data exists; add policy-driven due scan, recipients, and repeat/escalation rules. |
| Assigned work | Shifts, tickets, job assignment | Existing types retained. Migrate direct post-save producers to transactional intents and add record metadata. |

These new business events are **not claimed implemented** by delivery hardening. Stock checks remain inactive. No priorities or deadlines are guessed from alert text.

## Record links and remaining reliability gates

The current Notification DTO has no event code, priority, source record type/id, occurrence key, or escalation/resolution state. Add versioned backend-owned metadata before routing a companion alert directly to a specific ERP record. Existing type-based links lead to module lists only. Do not parse record IDs from message text or reuse the notification ID as an ERP record ID.

Move legacy post-save enqueue calls inside the domain commit for strict business-event atomicity. Immediate intent persistence closes the background-queue loss window, but cannot remove a process crash between an already completed business save and the later enqueue call. Direct `ProcessAlert` callers must also migrate to staged transactional intents. Pending business callers must perform their normal save; disposing an unsaved scope intentionally discards both work and staged alert.

Before production acceptance: extend PostgreSQL coverage to full domain transactions and unique expiry races; RabbitMQ disconnect/restart and API process death; gateway disconnect/recovery; token expiry/disablement; parallel sessions and read synchronization; backlog replay; SMTP outage; and each domain's allowed/denied recipients. Verify target customer OS notification settings. Neither an in-memory test nor a local native screenshot proves a customer deployment is real-time.

Alert configuration is required. The old notification seed file is commented out; do not auto-enable rules or infer recipients from role names. Missing configuration must be addressed by an authorized administrator. Monitor pending intents/deliveries, oldest pending age, attempt counts, and error codes; investigate poison items instead of deleting them.

## Setup and checks

Apply `AddNotificationDeliveryOutbox` through the normal reviewed migration process **before** running this backend version; it creates `AlertDispatchRequests` and `NotificationDeliveries`. No data cleanup or replay of historical email is performed. Keep completed intents with source keys for expiry deduplication; any future retention job must preserve that deduplication state.

Run `dotnet test tests/APP.Tests/APP.Tests.csproj --filter FullyQualifiedName~APP.Tests.Notifications`. Tests cover persistence before delivery, retry/restart, leases, independent channel storage, disabled recipients, staging without premature commits, stable identity/time, personal read authorization, and configuration endpoint policies. Gateway and web tests live in `oryx-live-desktop`.

## Verification recorded in this session

- 22 backend notification tests passed, including a disposable PostgreSQL test for real migration application, injected publish failure, context restart, and simultaneous optimistic claims (one publication under the original ID).
- PostgreSQL ran only in `/tmp/oryx-alert-pg` on `127.0.0.1:55439`, database `oryx_alert_delivery_test`; it was stopped afterwards. No existing/customer database was changed.
- 12 gateway tests and 12 focused web tests passed in Live Desktop; desktop typecheck/package and native canvas isolation/background smoke checks passed. React Doctor on changed code reported no findings.
- RabbitMQ/SMTP process-death and outage tests against actual transports, full business-transaction crash windows, signed Windows notification-center behavior, and new business-event coverage remain acceptance work. The PostgreSQL test injects a failing transport; it does not run a real broker.
- Documentation updated: this report, README, services and workflows here; Live Desktop companion/notification docs, README, services, workflows and plan.

To reproduce the optional PostgreSQL test, provision the disposable tables using the generated migration SQL and the minimal Notifications/users prerequisites described by the test, then set `ORYX_ALERT_TEST_CONNECTION`. The test refuses any host/port/database other than the isolated local tuple above. Ordinary runs skip it when no connection is provided.
