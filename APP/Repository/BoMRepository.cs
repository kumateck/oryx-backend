using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using APP.Services.Authorization;
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
    public async Task<Result<Guid>> CreateBillOfMaterial(CreateBillOfMaterialRequest request, Guid userId, ProductContext productContext = ProductContext.Production,
        Guid? technologyTransferId = null)
    {
        if (context.Database.IsRelational() && context.Database.CurrentTransaction is null)
            return await ProductionMaterialTransaction.Run(context, () => CreateBillOfMaterial(request, userId, productContext, technologyTransferId));

        var product = await context.Products
            .AsSplitQuery()
            .Include(product => product.BillOfMaterials)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId);

        if (product is null) return ProductErrors.NotFound(request.ProductId);

        var deletedProduct = await DeletedProductGuard.Refuse(context, request.ProductId);
        if (deletedProduct is not null) return deletedProduct;

        foreach (var item in request.Items)
        {
            item.Substitutes ??= [];
            item.SubstituteMaterialIds ??= [];
            // With quantities the substitutes are the quantified list; the id-only list is kept for older callers.
            if (item.Substitutes.Count != 0) item.SubstituteMaterialIds = item.Substitutes.Select(substitute => substitute.MaterialId).ToList();
            if (item.SubstituteMaterialIds.Contains(item.MaterialId) || item.SubstituteMaterialIds.Distinct().Count() != item.SubstituteMaterialIds.Count)
                return Error.Validation("Bom.Substitute", "List each substitute once, and never the line's own material.");
            if (item.Substitutes.Any(substitute => substitute.BaseQuantity <= 0 || substitute.PrescribedQuantity <= 0))
                return Error.Validation("Bom.SubstituteQuantity", "Each substitute needs its own quantity greater than zero.");
            item.IsSubstitutable = item.SubstituteMaterialIds.Count != 0;
        }
        var materialIds = request.Items.SelectMany(item => item.SubstituteMaterialIds.Append(item.MaterialId)).Distinct().ToList();
        var links = await context.MaterialDepartments.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Include(link => link.Material).Include(link => link.UoM).Include(link => link.DensityUoM).Where(link => materialIds.Contains(link.MaterialId)
                && link.DepartmentId == product.DepartmentId && link.DeletedAt == null).ToListAsync();
        if (product.DepartmentId is null)
            return Error.Conflict("Bom.MaterialUnit", "The product must belong to a department with linked material units.");

        var normalized = await BomMaterialUnits.Normalize(context, request.Items, links);
        if (normalized.IsFailure) return normalized.Error;

        var verbs = await ProductSectionChanges.Bom(context, request);
        async Task<Result> AuthorizeWrite()
        {
            var grants = await CurrentPermissionSet.Load(context, userId);
            // Promotion is an existing separate task whose authorized side effect is creating the product BOM.
            // Only its stored transfer may select that task; the HTTP BOM action never supplies a transfer ID.
            if (technologyTransferId.HasValue)
            {
                var validTransfer = await context.RndTechnologyTransfers.AnyAsync(transfer => transfer.Id == technologyTransferId
                    && transfer.RndProject.ProductId == request.ProductId
                    && transfer.Status == DOMAIN.Entities.RndTechnologyTransfers.RndTechnologyTransferStatus.ExecutionInProgress);
                return validTransfer && grants.Contains(PermissionKeys.CanPromoteRndTechnologyTransfer) ? Result.Success()
                    : Error.Forbidden("Bom.Permission", "Your current permissions or transfer state do not allow this BOM promotion.");
            }
            var required = verbs.Select(verb => ProductPermissions.Key(productContext, ProductSection.Bom, verb)).ToList();
            return required.All(key => key is not null && grants.Contains(key)) ? Result.Success()
                : Error.Forbidden("Bom.Permission", "Your current permissions do not allow these BOM changes.");
        }
        var access = await AuthorizeWrite();
        if (access.IsFailure) return access.Error;

        if (product.BillOfMaterials.Count != 0)
        {
            await context.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"ProductBillOfMaterials\" WHERE \"ProductId\" = {0}", request.ProductId);

            var bomIds = product.BillOfMaterials.Select(p => p.BillOfMaterialId).ToList();

            if (bomIds.Count != 0)
            {
                access = await AuthorizeWrite();
                if (access.IsFailure) return access.Error;
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

        access = await AuthorizeWrite();
        if (access.IsFailure) return access.Error;
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

        var deletedProduct = await DeletedProductGuard.Refuse(context, existingBillOfMaterial.ProductId);
        if (deletedProduct is not null) return deletedProduct;

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
