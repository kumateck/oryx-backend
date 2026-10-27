using System.Data;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SHARED;

namespace APP.Services.Transactions;

public static class ReferenceMaintenanceTransaction
{
    // Reference validation and historical checks share the definition write's transaction.
    // A serialization conflict is returned for review; no business mutation is automatically replayed.
    public static async Task<Result<T>> Run<T>(ApplicationDbContext context, Func<Task<Result<T>>> operation)
    {
        await using var transaction = context.Database.IsRelational() && context.Database.CurrentTransaction == null
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        try
        {
            var result = await operation();
            if (result.IsSuccess && transaction != null) await transaction.CommitAsync();
            return result;
        }
        catch (DbUpdateConcurrencyException) { return Conflict<T>(context); }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: "40001" or "40P01" or "23505" })
        { return Conflict<T>(context); }
        catch (PostgresException e) when (e.SqlState is "40001" or "40P01" or "23505")
        { return Conflict<T>(context); }
    }
    public static async Task<Result> Run(ApplicationDbContext context, Func<Task<Result>> operation) =>
        await Run<bool>(context, async () => {
            var result = await operation();
            return result.IsSuccess ? Result.Success(true) : Result.Failure<bool>(result.Error);
        });
    private static Result<T> Conflict<T>(ApplicationDbContext context)
    {
        context.ChangeTracker.Clear();
        return Error.Conflict("Maintenance.Changed", "The record or its references changed during validation. Review current records before another attempt.");
    }
}
