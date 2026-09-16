using Xunit;

namespace APP.Tests.FullProcedures;

/// <summary>
/// Gates tests that actually stop/start the disposable PostgreSQL SERVER
/// process (not just a client connection or worker process), proving
/// ADR-001's still-open "database-server-restart recovery" boundary.
/// Requires ORYX_PROCEDURE_SPIKE_TEST_DATABASE_URL and
/// ORYX_PROCEDURE_SPIKE_TEST_PGDATA_DIR (the disposable cluster's own data
/// directory, never a shared/production one). Skipped by default so CI and
/// normal runs never touch a real postgres process.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class DisposablePostgresServerControlFactAttribute : FactAttribute
{
    public DisposablePostgresServerControlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(
                "ORYX_PROCEDURE_SPIKE_TEST_DATABASE_URL")) ||
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(
                "ORYX_PROCEDURE_SPIKE_TEST_PGDATA_DIR")))
            Skip = "Requires a disposable PostgreSQL spike URL and its own data directory";
    }
}
