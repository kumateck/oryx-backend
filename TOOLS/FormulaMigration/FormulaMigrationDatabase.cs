using INFRASTRUCTURE.Context;
using APP.Utils;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FormulaMigration;

internal static class FormulaMigrationDatabase
{
    private const string ConnectionVariable = "ORYX_FORMULA_DB_CONNECTION";

    public static async Task<ApplicationDbContext> OpenAsync(Guid? actorId,
        string expectedDatabase, string expectedServer,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(expectedDatabase))
            throw new ArgumentException("Expected database name is required.");
        if (string.IsNullOrWhiteSpace(expectedServer))
            throw new ArgumentException("Expected database server is required as host:port.");
        var connection = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException(
                $"Set {ConnectionVariable}; connection strings are not accepted as command arguments.");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connection).Options;
        var context = new ApplicationDbContext(options, new OperatorCurrentUser(actorId));
        try
        {
            await context.Database.OpenConnectionAsync(cancellationToken);
            var actual = context.Database.GetDbConnection().Database;
            var npgsql = (NpgsqlConnection)context.Database.GetDbConnection();
            var actualServer = $"{npgsql.Host}:{npgsql.Port}";
            if (!string.Equals(actual, expectedDatabase, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Connected database '{actual}' does not match expected database '{expectedDatabase}'.");
            if (!string.Equals(actualServer, expectedServer,
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Connected server '{actualServer}' does not match expected server '{expectedServer}'.");
            return context;
        }
        catch
        {
            await context.DisposeAsync();
            throw;
        }
    }

    public static async Task RequireAuthorizedOperatorAsync(ApplicationDbContext context,
        Guid actorId, CancellationToken cancellationToken)
    {
        var active = await context.Users.IgnoreQueryFilters().AsNoTracking().AnyAsync(
            item => item.Id == actorId && !item.IsDisabled && item.DeletedAt == null,
            cancellationToken);
        var roleIds = await context.UserRoles.AsNoTracking()
            .Where(item => item.UserId == actorId)
            .Select(item => item.RoleId).ToListAsync(cancellationToken);
        var claimIds = await context.RoleClaims.AsNoTracking()
            .Where(item => roleIds.Contains(item.RoleId) &&
                item.ClaimValue == PermissionKeys.CanApplyFormulaMigration)
            .Select(item => item.Id).ToListAsync(cancellationToken);
        var permitted = await context.PermissionTypes.AsNoTracking().AnyAsync(
            item => claimIds.Contains(item.RoleClaimId) &&
                item.Key == PermissionKeys.CanApplyFormulaMigration,
            cancellationToken);
        if (!active || !permitted)
            throw new UnauthorizedAccessException(
                $"The authenticated operator requires {PermissionKeys.CanApplyFormulaMigration}.");
    }
}
