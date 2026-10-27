using DOMAIN.Entities.Base;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.Materials;

/// <summary>Reads purchase-unit symbols from active material linkages in every department.</summary>
public sealed class MaterialPurchaseUnitsReader(ApplicationDbContext context)
{
    public async Task<List<UnitOfMeasureDto>> Read(Guid materialId, CancellationToken cancellationToken = default)
    {
        var units = await context.MaterialDepartments.AsNoTracking()
            .Where(link => link.MaterialId == materialId && !link.DeletedAt.HasValue
                && !link.Material.DeletedAt.HasValue && !link.Department.DeletedAt.HasValue
                && link.UoM != null && !link.UoM.DeletedAt.HasValue)
            .Select(link => new UnitOfMeasureDto
            {
                Id = link.UoM.Id, Name = link.UoM.Name, Symbol = link.UoM.Symbol,
                Description = link.UoM.Description, Category = link.UoM.Category,
                Type = link.UoM.Type, IsScalable = link.UoM.IsScalable,
                IsRawMaterial = link.UoM.IsRawMaterial, CreatedAt = link.UoM.CreatedAt,
            }).ToListAsync(cancellationToken);

        return units.Where(unit => !string.IsNullOrWhiteSpace(unit.Symbol))
            .OrderBy(unit => unit.Id)
            .DistinctBy(unit => unit.Symbol.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(unit => { unit.Symbol = unit.Symbol.Trim(); return unit; })
            .OrderBy(unit => unit.Symbol, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
