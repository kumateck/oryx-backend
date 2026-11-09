using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.StockMigrations;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class StockMigrationRepository
{
    private static string LinkKey(MaterialKind kind) => kind == MaterialKind.Raw
        ? PermissionKeys.CanLinkRawMaterials : PermissionKeys.CanLinkPackagingMaterials;

    private async Task<Error> MaterialLinkAccess(StockMigration migration, MaterialKind kind, Guid actor)
    {
        if (kind is not (MaterialKind.Raw or MaterialKind.Package))
            return Error.Validation("StockMigration.Kind", "Choose raw or packaging materials.");
        var department = await context.Users.AsNoTracking().Where(u => u.Id == actor).Select(u => u.DepartmentId).FirstOrDefaultAsync();
        if (department == null || department != migration.DepartmentId)
            return Error.Forbidden("StockMigration.Department", "Switch to the migration's department before linking materials.");
        var grants = await CurrentPermissionSet.Load(context, actor);
        return grants.Contains(PermissionKeys.CanViewOpeningBalances) && grants.Contains(PermissionKeys.CanCorrectOpeningBalances) && grants.Contains(LinkKey(kind))
            ? null : Error.Forbidden("StockMigration.LinkPermission", "Correction access and the material kind's link permission are required.");
    }

    public async Task<Result<List<StockMigrationMaterialLinkDto>>> PreviewMaterialLinks(Guid id, MaterialKind kind, Guid actor)
    {
        var draft = await Draft(id);
        if (draft.IsFailure) return draft.Error;
        if (await MaterialLinkAccess(draft.Value, kind, actor) is { } denied) return denied;
        await Check(draft.Value); // Re-resolve every row; never rely on the saved blocker text alone.
        return await MissingMaterialLinks(draft.Value, kind);
    }

    public async Task<Result<StockMigrationMaterialLinksResult>> CreateMaterialLinks(Guid id, MaterialKind kind, CreateStockMigrationMaterialLinksRequest request, Guid actor)
    {
        if (request.Materials is not { Count: > 0 and <= 1000 } || request.Materials.Any(m => m == null)
            || request.Materials.Select(m => m.MaterialId).Distinct().Count() != request.Materials.Count)
            return Error.Validation("StockMigration.Links", "Choose between 1 and 1,000 distinct materials.");
        await using var transaction = await context.Database.BeginTransactionAsync();
        await LockDraft(id); // Serializes with approval, discard, corrections and other linking requests.
        var draft = await Draft(id);
        if (draft.IsFailure) return draft.Error;
        var migration = draft.Value;
        if (await MaterialLinkAccess(migration, kind, actor) is { } denied) return denied;
        // The legacy department operation is an upsert. Block concurrent link writes while checking
        // absence and inserting; this operation never updates or revives an existing link.
        await context.Database.ExecuteSqlRawAsync("LOCK TABLE \"MaterialDepartments\" IN SHARE ROW EXCLUSIVE MODE");
        await Check(migration);
        var candidates = (await MissingMaterialLinks(migration, kind)).ToDictionary(m => m.MaterialId);
        var ids = request.Materials.Select(m => m.MaterialId).ToList();
        var existing = await context.MaterialDepartments.IgnoreQueryFilters().AsNoTracking()
            .Where(m => m.DepartmentId == migration.DepartmentId && ids.Contains(m.MaterialId)).Select(m => m.MaterialId).ToListAsync();
        var storedKinds = await context.Materials.IgnoreQueryFilters().AsNoTracking().Where(m => ids.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.Kind);
        var result = new StockMigrationMaterialLinksResult();
        foreach (var submitted in request.Materials)
        {
            if (!storedKinds.TryGetValue(submitted.MaterialId, out var storedKind) || storedKind != kind)
                return Error.Forbidden("StockMigration.LinkKind", "A material does not belong to this material kind.");
            if (existing.Contains(submitted.MaterialId)) { result.AlreadyLinked++; continue; }
            if (!candidates.TryGetValue(submitted.MaterialId, out var current) || current.Issue != null || current.UoMId == null
                || submitted.UoMId != current.UoMId || submitted.MinimumStockLevel != current.MinimumStockLevel
                || submitted.ReOrderLevel != current.ReOrderLevel || submitted.MaximumStockLevel != current.MaximumStockLevel)
                return Error.Conflict("StockMigration.LinkPreviewChanged", "The missing materials or units have changed. Refresh the preview before linking.");
            if (await MaterialLinkAccess(migration, storedKind, actor) is { } refused) return refused;
            context.MaterialDepartments.Add(new MaterialDepartment
            {
                MaterialId = current.MaterialId, DepartmentId = migration.DepartmentId, UoMId = current.UoMId.Value,
                MinimumStockLevel = current.MinimumStockLevel, ReOrderLevel = current.ReOrderLevel, MaximumStockLevel = current.MaximumStockLevel,
            });
            result.Created++;
        }
        if (await MaterialLinkAccess(migration, kind, actor) is { } beforeLinks) return beforeLinks;
        await context.SaveChangesAsync();
        await Check(migration);
        if (await MaterialLinkAccess(migration, kind, actor) is { } beforeRecheck) return beforeRecheck;
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return result;
    }

    private async Task<List<StockMigrationMaterialLinkDto>> MissingMaterialLinks(StockMigration migration, MaterialKind kind)
    {
        var rows = migration.Lines.Where(l => !l.Excluded && l.ResolvedMaterialId != null
            && l.Blockers?.Contains(" is not linked to ", StringComparison.Ordinal) == true).ToList();
        var ids = rows.Select(l => l.ResolvedMaterialId.Value).Distinct().ToList();
        var materials = await context.Materials.AsNoTracking().Where(m => ids.Contains(m.Id) && m.Kind == kind && !m.IsUnlimited).OrderBy(m => m.Code).ToListAsync();
        var units = await context.UnitOfMeasures.AsNoTracking().Where(u => u.DeletedAt == null).ToListAsync();
        // Even a retired link is preserved for a settings operator to review explicitly.
        var linked = await context.MaterialDepartments.IgnoreQueryFilters().Where(m => m.DepartmentId == migration.DepartmentId && ids.Contains(m.MaterialId)).Select(m => m.MaterialId).ToListAsync();
        return materials.Select(material =>
        {
            var symbols = rows.Where(l => l.ResolvedMaterialId == material.Id).Select(l => LinkStorageSymbol(l.UnitSymbol)).Distinct().ToList();
            var symbol = symbols.Count == 1 ? symbols[0] : null;
            var matches = units.Where(u => LinkStorageSymbol(u.Symbol) == symbol && symbol != null
                && (symbol == "piece" ? u.Name?.Trim().Equals("Piece", StringComparison.OrdinalIgnoreCase) == true : u.Symbol?.Trim().Equals(symbol, StringComparison.OrdinalIgnoreCase) == true)).ToList();
            var unit = matches.Count == 1 ? matches[0] : null;
            var source = symbol == "mg" ? "kg" : symbol == "ml" ? "L" : "piece";
            decimal Level(decimal value) => UomConverter.Convert(value, source, symbol ?? "") ?? value;
            return new StockMigrationMaterialLinkDto
            {
                MaterialId = material.Id, Code = material.Code, Name = material.Name, Kind = material.Kind,
                UoMId = unit?.Id, UnitName = unit?.Name, UnitSymbol = symbol,
                MinimumStockLevel = Level(100), ReOrderLevel = Level(1000), MaximumStockLevel = Level(10000),
                Issue = linked.Contains(material.Id) ? "A department link already exists, possibly retired. Review it in Material settings; its unit and levels will be preserved."
                    : symbol == null ? "Rows have missing, unsupported or conflicting units. Correct the rows first."
                    : unit == null ? $"No unique matching {symbol} unit exists. Review units in settings, then refresh this preview." : null,
            };
        }).ToList();
    }

    internal static string LinkStorageSymbol(string symbol) => symbol?.Trim().ToLowerInvariant() switch
    {
        "mg" or "g" or "kg" => "mg",
        "ml" or "l" => "ml",
        "pcs" or "piece" => "piece",
        _ => null,
    };
}
