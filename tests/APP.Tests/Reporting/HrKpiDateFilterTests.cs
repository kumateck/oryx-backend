using APP.Repository;
using DOMAIN.Entities.Reports.HrDashboardKpi;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Reporting;

// HR KPI dates arrive from query strings such as ?date=2026-10-29, which bind with no time zone
// (DateTimeKind.Unspecified). Npgsql refuses to compare those with timestamptz columns.
public class HrKpiDateFilterTests
{
    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => "NonProduction";
    }

    [Fact]
    public void Filter_dates_are_held_as_UTC()
    {
        var filter = new HrKpiFilterDto
        {
            StartDate = new DateTime(2026, 10, 29),
            EndDate = new DateTime(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc),
        };
        Assert.Equal(DateTimeKind.Utc, filter.StartDate!.Value.Kind);
        Assert.Equal(new DateTime(2026, 10, 29, 0, 0, 0, DateTimeKind.Utc), filter.StartDate);
        Assert.Equal(new DateTime(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc), filter.EndDate);

        var local = new DateTime(2026, 10, 29, 8, 0, 0, DateTimeKind.Local);
        filter.StartDate = local;
        Assert.Equal(local.ToUniversalTime(), filter.StartDate);
        Assert.Equal(DateTimeKind.Utc, filter.StartDate!.Value.Kind);

        filter.StartDate = null;
        Assert.Null(filter.StartDate);
    }

    [LocalHrKpiFact]
    public async Task PostgreSQL_runs_every_date_filtered_HR_KPI_report_with_a_plain_query_date()
    {
        var connection = Environment.GetEnvironmentVariable("ORYX_HR_KPI_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Missing disposable fixture connection.");
        var parsed = new NpgsqlConnectionStringBuilder(connection);
        if (parsed.Host is not ("127.0.0.1" or "localhost") || parsed.Database != "oryx_hr_kpi_test")
            throw new InvalidOperationException("Only the dedicated local disposable fixture is permitted.");
        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection).Options, new Actor());
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var repository = new ReportRepository(context, null!, null!, NullLogger<ReportRepository>.Instance);
        // Exactly what model binding produces for ?date=2026-10-29 or ?startDate=...&endDate=...
        HrKpiFilterDto Day() => new() { StartDate = new DateTime(2026, 10, 29) };
        HrKpiFilterDto Range() => new() { StartDate = new DateTime(2026, 10, 1), EndDate = new DateTime(2026, 10, 29) };

        AssertSuccess(await repository.GetDailyAttendanceRate(Day(), null));
        AssertSuccess(await repository.GetDailyAttendanceRate(Day(), Guid.NewGuid()));
        AssertSuccess(await repository.GetLeaveRequestPipeline(Range(), null));
        AssertSuccess(await repository.GetOvertimeRequestActivity(Range(), null));
        AssertSuccess(await repository.GetStaffRequisitionPipeline(Range(), null));
        AssertSuccess(await repository.GetNewHiresThisPeriod(Range(), null));
        AssertSuccess(await repository.GetEmployeeTurnoverRate(Range(), null));
    }

    private static void AssertSuccess<T>(SHARED.Result<T> result) =>
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(error => error.Description)));
}

public sealed class LocalHrKpiFactAttribute : FactAttribute
{
    public LocalHrKpiFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ORYX_HR_KPI_TEST_CONNECTION")))
            Skip = "Set ORYX_HR_KPI_TEST_CONNECTION to the dedicated disposable local PostgreSQL fixture.";
    }
}
