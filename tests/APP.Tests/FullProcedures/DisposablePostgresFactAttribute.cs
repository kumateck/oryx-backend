using Xunit;

namespace APP.Tests.FullProcedures;

[AttributeUsage(AttributeTargets.Method)]
public sealed class DisposablePostgresFactAttribute : FactAttribute
{
    public DisposablePostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(
            "ORYX_PROCEDURE_SPIKE_TEST_DATABASE_URL")))
            Skip = "Requires disposable PostgreSQL spike database URL";
    }
}
