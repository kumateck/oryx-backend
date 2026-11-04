using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Runtime.CompilerServices;
using SHARED;

namespace APP.Repository;

/// <summary>
/// Serializes production allocation writers before they read. Table locks also coordinate with
/// legacy clients and master edits, which cannot opt out of PostgreSQL's write locks.
/// Nested callers share the outer transaction; only its owner commits or rolls back.
/// </summary>
internal sealed class ProductionMaterialTransaction(ApplicationDbContext context, IDbContextTransaction transaction) : IAsyncDisposable
{
    private static readonly ConditionalWeakTable<ApplicationDbContext, ProductionMaterialTransaction> Active = new();
    private readonly List<Func<Task>> afterCommit = [];
    private static readonly HashSet<string> Tables =
    [
        "ProductionMaterialConfiguration", "ProductionMaterialRepair", "ProductionScheduleRepair", "MaterialBatchReservedQuantity",
        "ProductionSchedule", "Route", "RouteResource", "RouteWorkCenter", "RouteResponsibleUser", "RouteResponsibleRole",
        "ProductionActivityStepResource", "ProductionActivityStepWorkCenter", "ProductionActivityStepUser", "RoleDepartment",
        "ProductAnalyticalRawData", "Operation", "Workflow",
        "ShelfMaterialBatch", "MaterialBatch", "Requisition", "RequisitionItem", "RequisitionApproval",
        "StockRequisitionIssue", "ProductionActivity", "ProductionActivityStep", "ProductionExtraPacking",
        "ProductionExtraPackingApproval", "MaterialReturnNote", "MaterialReturnNoteFullReturn",
        "MaterialReturnNotePartialReturn", "SwapRequest", "StockTransfer", "ProductionScheduleProduct",
        "Product", "ProductPacking", "ProductPackingList", "ProductPackage", "ProductPackageSubstitute", "ProductBillOfMaterial", "BillOfMaterial",
        "BillOfMaterialItem", "BillOfMaterialItemSubstitute", "MaterialDepartment", "UnitOfMeasure",
        "FinalPacking", "FinishedGoodsTransferNote", "MassMaterialBatchMovement", "BinCardInformation",
        "Approval", "ApprovalStage", "User", "Role", "PermissionType", "IdentityRoleClaim`1", "IdentityUserRole`1",
        "BatchManufacturingRecord", "BatchPackagingRecord", "MaterialReturnNoteFullReturn", "MaterialReturnNotePartialReturn",
        "IssuedBmrChange", "IssuedBmrRevision", "AnalyticalTestRequest", "TestRequestSubject", "OosInvestigation", "Coa", "CoaRow", "WorkOrder"
    ];

    public static async Task<ProductionMaterialTransaction> Begin(ApplicationDbContext context)
    {
        if (!context.Database.IsRelational() || context.Database.CurrentTransaction is not null)
            return new ProductionMaterialTransaction(context, null);
        var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            if (context.Database.ProviderName?.Contains("Npgsql") == true)
            {
                var names = context.Model.GetEntityTypes().Where(entity => Tables.Contains(entity.ClrType.Name))
                    .Select(entity => entity.GetTableName()).Where(name => name is not null)
                    .Distinct().OrderBy(name => name).Select(name => $"\"{name}\"");
                await context.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '3s'");
                await context.Database.ExecuteSqlRawAsync($"LOCK TABLE {string.Join(", ", names)} IN SHARE ROW EXCLUSIVE MODE");
            }
            var scope = new ProductionMaterialTransaction(context, transaction);
            Active.Add(context, scope);
            return scope;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    public async Task CommitAsync()
    {
        if (transaction is null) return;
        await transaction.CommitAsync();
        await transaction.DisposeAsync();
        transaction = null;
        Active.Remove(context);
        foreach (var publish in afterCommit) await publish();
    }
    public Task RollbackAsync() => transaction?.RollbackAsync() ?? Task.CompletedTask;
    public async ValueTask DisposeAsync()
    {
        if (transaction is null) return;
        Active.Remove(context);
        await transaction.DisposeAsync();
    }

    internal static bool DeferUntilCommit(ApplicationDbContext context, Func<Task> publish)
    {
        if (!Active.TryGetValue(context, out var scope)) return false;
        scope.afterCommit.Add(publish);
        return true;
    }

    public static async Task<Result> Run(ApplicationDbContext context, Func<Task<Result>> action)
    {
        await using var scope = await Begin(context);
        var result = await action();
        if (result.IsSuccess) await scope.CommitAsync();
        return result;
    }

    public static async Task<Result<T>> Run<T>(ApplicationDbContext context, Func<Task<Result<T>>> action)
    {
        await using var scope = await Begin(context);
        var result = await action();
        if (result.IsSuccess) await scope.CommitAsync();
        return result;
    }
}
