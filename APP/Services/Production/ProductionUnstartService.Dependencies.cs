using System.Data;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products.Production;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace APP.Services.Production;

public sealed partial class ProductionUnstartService
{
    private static readonly Type[] Principals = [typeof(ProductionScheduleProduct), typeof(ProductionActivity),
        typeof(ProductionActivityStep), typeof(BatchManufacturingRecord), typeof(BatchPackagingRecord), typeof(MaterialBatchReservedQuantity)];
    private static readonly Type[] InitialRecords = [typeof(ProductionActivity), typeof(ProductionActivityStep),
        typeof(BatchManufacturingRecord), typeof(BatchPackagingRecord), typeof(MaterialBatchReservedQuantity),
        typeof(ProductionActivityStepResource), typeof(ProductionActivityStepWorkCenter), typeof(ProductionActivityStepUser),
        typeof(ProductionActivityLog), typeof(ProductionUnstartRepair)];

    private IEnumerable<IForeignKey> EvidenceKeys() => db.Model.GetEntityTypes().SelectMany(type => type.GetForeignKeys())
        .Where(key => Principals.Contains(key.PrincipalEntityType.ClrType) && key.Properties.Count == 1);
    private static string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";
    private static string Table(IEntityType type) => Quote(type.GetSchema() ?? "public") + "." + Quote(type.GetTableName());

    private async Task LockEvidence()
    {
        var types = EvidenceKeys().Select(key => key.DeclaringEntityType)
            .Concat(Principals.Select(type => db.Model.FindEntityType(type))).ToList();
        var names = types.Select(Table).Append("public.\"ProductionSchedules\"").Append("public.\"ShelfMaterialBatches\"")
            .Append("public.\"MaterialBatches\"").Append("public.\"ProductionUnstartRepairs\"").Distinct().Order(StringComparer.Ordinal);
        await db.Database.ExecuteSqlRawAsync("LOCK TABLE " + string.Join(", ", names) + " IN SHARE ROW EXCLUSIVE MODE");
    }

    private async Task<List<string>> Dependencies(ProductionActivity run, List<ProductionActivityStep> steps,
        List<BatchManufacturingRecord> bmrs, List<BatchPackagingRecord> bprs)
    {
        var reservationIds = await db.MaterialBatchReservedQuantities.IgnoreQueryFilters().Where(item => item.ProductionScheduleProductId == run.ProductionScheduleProductId)
            .Select(item => item.Id).ToArrayAsync();
        var ids = new Dictionary<Type, Guid[]> { [typeof(ProductionScheduleProduct)] = [run.ProductionScheduleProductId],
            [typeof(ProductionActivity)] = [run.Id], [typeof(ProductionActivityStep)] = steps.Select(item => item.Id).ToArray(),
            [typeof(BatchManufacturingRecord)] = bmrs.Select(item => item.Id).ToArray(), [typeof(BatchPackagingRecord)] = bprs.Select(item => item.Id).ToArray(),
            [typeof(MaterialBatchReservedQuantity)] = reservationIds };
        var blockers = new List<string>();
        foreach (var key in EvidenceKeys().Where(key => !InitialRecords.Contains(key.DeclaringEntityType.ClrType)))
        {
            var values = ids[key.PrincipalEntityType.ClrType];
            if (values.Length == 0) continue;
            var table = StoreObjectIdentifier.Table(key.DeclaringEntityType.GetTableName(), key.DeclaringEntityType.GetSchema());
            var column = key.Properties[0].GetColumnName(table);
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.Transaction = db.Database.CurrentTransaction.GetDbTransaction();
            command.CommandText = $"SELECT EXISTS (SELECT 1 FROM {Table(key.DeclaringEntityType)} WHERE {Quote(column)} = ANY(@ids))";
            var parameter = command.CreateParameter(); parameter.ParameterName = "ids"; parameter.Value = values; command.Parameters.Add(parameter);
            if (await command.ExecuteScalarAsync() is true)
                blockers.Add($"Existing {key.DeclaringEntityType.GetTableName()} records depend on this batch or run. Reconcile them before unstarting.");
        }
        return blockers;
    }
}
