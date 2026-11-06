using DOMAIN.Entities.Employees;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace APP.Tests.Repository;

public sealed partial class EmployeeAssignmentTests
{
    private sealed class PostgresFactAttribute : FactAttribute
    {
        public PostgresFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ORYX_EMPLOYEE_ASSIGNMENT_TEST_CONNECTION")))
                Skip = "Set ORYX_EMPLOYEE_ASSIGNMENT_TEST_CONNECTION to a disposable PostgreSQL server with CREATE DATABASE access.";
        }
    }

    [PostgresFact]
    public async Task OmittedManagerAssignmentPassesTheRealPostgresForeignKey()
    {
        var database = "employee_assignment_" + Guid.NewGuid().ToString("N");
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ORYX_EMPLOYEE_ASSIGNMENT_TEST_CONNECTION"));
        await using var admin = new NpgsqlConnection(connection.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin))
            await create.ExecuteNonQueryAsync();
        connection.Database = database;
        try
        {
            await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(connection.ConnectionString).Options, new AnonymousActor());
            await db.Database.EnsureCreatedAsync();
            var (employee, request) = await Seed(db);
            // Before the fix this call throws 23503 / FK_Employees_users_ReportingManagerId.
            var result = await Repository(db).AssignEmployee(employee.Id, request);
            Assert.True(result.IsSuccess);
            db.ChangeTracker.Clear();
            var saved = await db.Employees.SingleAsync();
            Assert.Null(saved.ReportingManagerId);
            Assert.Equal(EmployeeStatus.Active, saved.Status);
            Assert.Equal("EBO", saved.StaffNumber);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
