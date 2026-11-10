using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.BillOfMaterials;
using DOMAIN.Entities.BillOfMaterials.Request;
using DOMAIN.Entities.Products;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class BoMRepository(ApplicationDbContext context, IMapper mapper) : IBoMRepository
{
    public async Task<Result<Guid>> CreateBillOfMaterial(CreateBillOfMaterialRequest request, Guid userId)
    {
        if (context.Database.IsRelational() && context.Database.CurrentTransaction is null)
            return await ProductionMaterialTransaction.Run(context, () => CreateBillOfMaterial(request, userId));

        var product = await context.Products
            .AsSplitQuery()
            .Include(product => product.BillOfMaterials)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId);

        if (product is null) return ProductErrors.NotFound(request.ProductId);

        var materialIds = request.Items.SelectMany(item => item.SubstituteMaterialIds.Append(item.MaterialId)).Distinct().ToList();
        var links = await context.MaterialDepartments.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Include(link => link.UoM).Where(link => materialIds.Contains(link.MaterialId)
                && link.DepartmentId == product.DepartmentId && link.DeletedAt == null).ToListAsync();
        if (product.DepartmentId is null)
            return Error.Conflict("Bom.MaterialUnit", "The product must belong to a department with linked material units.");

        var densityItems = request.Items.Where(item => item.UseDensityConversion).ToList();
        DOMAIN.Entities.Base.UnitOfMeasure? densitySourceUnit = null;
        if (densityItems.Count != 0)
        {
            var candidates = await context.UnitOfMeasures.IgnoreQueryFilters().AsNoTracking()
                .Where(unit => unit.DeletedAt == null && unit.Symbol.ToLower() == "mg").ToListAsync();
            densitySourceUnit = candidates.Count == 1 ? candidates[0] : null;
            if (densitySourceUnit is null)
                return Error.Conflict("Bom.DensityUnit", "The canonical mg unit must exist exactly once before density conversion can be enabled.");
        }

        foreach (var item in request.Items)
        {
            var matching = links.Where(link => link.MaterialId == item.MaterialId && link.UoM != null && !link.UoM.DeletedAt.HasValue).ToList();
            if (matching.Count != 1)
                return Error.Conflict("Bom.MaterialUnit", "A BOM material needs exactly one active department stock-unit link.");
            var link = matching[0];
            if (item.UseDensityConversion)
            {
                foreach (var candidateId in item.SubstituteMaterialIds.Append(item.MaterialId).Distinct())
                {
                    var candidateLinks = links.Where(candidate => candidate.MaterialId == candidateId
                        && candidate.UoM != null && !candidate.UoM.DeletedAt.HasValue).ToList();
                    if (candidateLinks.Count != 1)
                        return Error.Conflict("Bom.MaterialUnit", "A BOM material and each substitute need exactly one active department stock-unit link.");
                    var candidate = candidateLinks[0];
                    if (candidate.Density <= 0 || UomConverter.ConvertWithDensity(1m, "mg", candidate.UoM.Symbol, candidate.Density) is null)
                        return Error.Conflict("Bom.Density", $"Configure a positive density in g/mL and a liquid stock unit for material {candidate.MaterialId} before enabling conversion.");
                }
                // The conversion mode has one defined source unit: BOM quantity is authored in mg.
                item.BaseUoMId = densitySourceUnit.Id;
            }
            else if (link.UoMId != item.BaseUoMId)
            {
                return Error.Conflict("Bom.MaterialUnit", "A BOM material unit no longer matches the department material linkage. Reload the linked units and review the quantities before saving.");
            }
        }

        if (product.BillOfMaterials.Count != 0)
        {
            await context.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"ProductBillOfMaterials\" WHERE \"ProductId\" = {0}", request.ProductId);

            var bomIds = product.BillOfMaterials.Select(p => p.BillOfMaterialId).ToList();

            if (bomIds.Count != 0)
            {
                await context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM \"BillOfMaterials\" WHERE \"Id\" = ANY({0})", bomIds.ToArray());
            }
        }

        var billOfMaterial = mapper.Map<BillOfMaterial>(request);
        billOfMaterial.CreatedById = userId;

        await context.BillOfMaterials.AddAsync(billOfMaterial);

        await context.ProductBillOfMaterials.AddAsync(new ProductBillOfMaterial
        {
            ProductId = request.ProductId,
            BillOfMaterialId = billOfMaterial.Id,
            IsActive = true,
            Version = product.BillOfMaterials.Count != 0 ? product.BillOfMaterials.MaxBy(p => p.Version).Version + 1 : 1,
            EffectiveDate = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        return billOfMaterial.Id;
    }

    public async Task<Result<BillOfMaterialDto>> GetBillOfMaterial(Guid billOfMaterialId)
    {
        var billOfMaterial = await context.BillOfMaterials
            .AsSplitQuery()
            .Include(b => b.Items)
            .ThenInclude(i => i.Material)
            .FirstOrDefaultAsync(b => b.Id == billOfMaterialId);

        return billOfMaterial is null ? BillOfMaterialErrors.NotFound(billOfMaterialId) : mapper.Map<BillOfMaterialDto>(billOfMaterial);
    }

    public async Task<Result<Paginateable<IEnumerable<BillOfMaterialDto>>>> GetBillOfMaterials(int page, int pageSize, string searchQuery)
    {
        var query = context.BillOfMaterials
            .AsSplitQuery()
            .Include(b => b.Items).ThenInclude(i => i.Material)
            .Include(b => b.Items).ThenInclude(i => i.MaterialType)
            .Include(b => b.Items).ThenInclude(i => i.BaseUoM)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, f => f.Product.Name, f => f.Product.Code);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<BillOfMaterialDto>
        );
    }

    public async Task<Result> UpdateBillOfMaterial(CreateProductBillOfMaterialRequest request, Guid billOfMaterialId, Guid userId)
    {
        var existingBillOfMaterial = await context.BillOfMaterials
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == billOfMaterialId);

        if (existingBillOfMaterial is null)
        {
            return BillOfMaterialErrors.NotFound(billOfMaterialId);
        }

        // Remove old items if they exist
        context.BillOfMaterialItems.RemoveRange(existingBillOfMaterial.Items);
        await context.SaveChangesAsync();

        mapper.Map(request, existingBillOfMaterial);
        existingBillOfMaterial.LastUpdatedById = userId;

        context.BillOfMaterials.Update(existingBillOfMaterial);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ArchiveBillOfMaterial(Guid productId, Guid userId)
    {
        var product = await context.Products.Include(product => product.BillOfMaterials)
            .FirstOrDefaultAsync(p => p.Id == productId);
        if (product is null) return ProductErrors.NotFound(productId);


        var bom = product.BillOfMaterials.FirstOrDefault(p => p.IsActive);

        if (bom is not null)
        {
            bom.IsActive = false;
            context.ProductBillOfMaterials.Update(bom);
            await context.SaveChangesAsync();
        }

        return Result.Success();
    }

    public async Task<Result> DeleteBillOfMaterial(Guid billOfMaterialId, Guid userId)
    {
        var billOfMaterial = await context.BillOfMaterials.FirstOrDefaultAsync(b => b.Id == billOfMaterialId);
        if (billOfMaterial is null)
        {
            return BillOfMaterialErrors.NotFound(billOfMaterialId);
        }

        billOfMaterial.DeletedAt = DateTime.UtcNow;
        billOfMaterial.LastDeletedById = userId;
        context.BillOfMaterials.Update(billOfMaterial);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
