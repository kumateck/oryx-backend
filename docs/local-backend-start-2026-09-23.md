# Local backend startup — 2026-09-23

The API was started in Development mode on `http://127.0.0.1:5270` with the local `entrancedb` PostgreSQL instance (port 5433), local Redis (port 6380), and the local RabbitMQ container's configured account. Startup migrations were disabled for this run.

`JobRequestRepository` now has its existing `IJobRequestAssignmentNotifier` implementation registered as scoped in `APP/DependencyInjection.cs`. This resolves the startup dependency-injection failure and preserves assignment notifications through the existing notification service. No API contract changed.

The Swagger landing page and `/swagger/v1/swagger.json` returned HTTP 200. The local database still lacks `QcMonitoringPrograms`, so QC monitoring background queries report a missing-table error. Align the local schema through a reviewed migration update before testing that feature. No migration was applied during this startup.
