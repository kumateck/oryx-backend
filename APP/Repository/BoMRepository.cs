using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.BillOfMaterials;
using DOMAIN.Entities.Products;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class BoMRepository(ApplicationDbContext context, IMapper mapper) : IBoMRepository
{
    public async Task<Result<Guid>> CreateBillOfMaterial(CreateBillOfMaterialRequest request, Guid userId)
    {
        var product = await context.Products
            .Include(product => product.BillOfMaterials).FirstOrDefaultAsync(p => p.Id == request.ProductId);
        if (product is null) return ProductErrors.NotFound(request.ProductId);

        if (product.BillOfMaterials.Count != 0)
        {
            context.ProductBillOfMaterials.RemoveRange(product.BillOfMaterials);
            var boms = context.BillOfMaterials
                .Where(b => product.BillOfMaterials.Select(p => p.BillOfMaterialId).Contains(b.Id));
            context.BillOfMaterials.RemoveRange(boms);
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
            .Include(b => b.Items)
            .ThenInclude(i => i.ComponentMaterial)
            .Include(b => b.Items)
            .ThenInclude(i => i.ComponentProduct)
            .FirstOrDefaultAsync(b => b.Id == billOfMaterialId);

        return billOfMaterial is null ? BillOfMaterialErrors.NotFound(billOfMaterialId) : mapper.Map<BillOfMaterialDto>(billOfMaterial);
    }
    
    public async Task<Result<Paginateable<IEnumerable<BillOfMaterialDto>>>> GetBillOfMaterials(int page, int pageSize, string searchQuery) 
    { 
        var query = context.BillOfMaterials
            .AsSplitQuery()
            .Include(b => b.Items).ThenInclude(i => i.ComponentMaterial)
            .Include(b => b.Items).ThenInclude(i => i.ComponentProduct)
            .Include(b => b.Items).ThenInclude(i => i.MaterialType)
            .Include(b => b.Items).ThenInclude(i => i.UoM)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        { 
            query = query.WhereSearch(searchQuery, f => f.Product.Name);
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