using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

/// <summary>
/// An isolated PostgreSQL database with the deployed formula and form-revision integrity triggers, so tests
/// exercise the same commit-time audit checks as customer servers. Requires ORYX_ISSUE_REPAIR_TEST_CONNECTION.
/// </summary>
internal sealed class FormulaTriggerDatabase : IAsyncDisposable
{
    private readonly string database = "oryx_formula_trigger_test_" + Guid.NewGuid().ToString("N");
    private NpgsqlConnection? admin;
    private string connection = "";

    public ApplicationDbContext Open(ICurrentUserService user) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection).Options, user);

    public async Task CreateAsync(ICurrentUserService user, params Guid[] users)
    {
        var builder = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ORYX_ISSUE_REPAIR_TEST_CONNECTION"));
        // Never allow this fixture to use a remote customer server.
        Assert.Contains(builder.Host, new[] { "localhost", "127.0.0.1", "::1", "/tmp" });
        admin = new NpgsqlConnection(builder.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin)) await create.ExecuteNonQueryAsync();
        builder.Database = database;
        connection = builder.ConnectionString;
        await using var db = Open(user);
        await db.Database.EnsureCreatedAsync();
        // EnsureCreated builds tables only; apply the triggers and functions exactly as the migrations deploy them.
        Migration[] migrations =
        [
            new INFRASTRUCTURE.Migrations.AddFormulaV1IntegrityTriggers(),
            new INFRASTRUCTURE.Migrations.ProtectFormulaMigrationEvidence(),
            new INFRASTRUCTURE.Migrations.AddFormulaMigrationApplyProvenance(),
            new INFRASTRUCTURE.Migrations.AddGovernedFormRevisionLifecycle(),
        ];
        var generator = db.GetService<IMigrationsSqlGenerator>();
        await using var sql = new NpgsqlConnection(connection);
        await sql.OpenAsync();
        foreach (var migration in migrations)
            foreach (var command in generator.Generate(migration.UpOperations.OfType<SqlOperation>().ToList(), db.Model))
            {
                // Raw command: the trigger bodies contain braces that EF would read as format placeholders.
                await using var run = new NpgsqlCommand(command.CommandText, sql);
                await run.ExecuteNonQueryAsync();
            }
        // Audit columns reference users, so the acting people must exist.
        foreach (var id in users.Distinct())
            db.Users.Add(new DOMAIN.Entities.Users.User { Id = id, UserName = $"user-{id:N}", Email = $"{id:N}@example.test" });
        await db.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (admin is null) return;
        NpgsqlConnection.ClearAllPools();
        await using (var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE)", admin)) await drop.ExecuteNonQueryAsync();
        await admin.DisposeAsync();
    }
}
