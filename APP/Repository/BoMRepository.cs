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
        var billOfMaterial = mapper.Map<BillOfMaterial>(request); 
        billOfMaterial.CreatedById = userId; 
        
        // foreach (var bomItem in request.Items.Select(mapper.Map<BillOfMaterialItem>))
        // {
        //     bomItem.BillOfMaterialId = billOfMaterial.Id;
        //     await context.BillOfMaterialItems.AddAsync(bomItem);
        // }
        await context.BillOfMaterials.AddAsync(billOfMaterial);

        await context.ProductBillOfMaterials.AddAsync(new ProductBillOfMaterial
        {
            ProductId = request.ProductId,
            BillOfMaterialId = billOfMaterial.Id,
            EffectiveDate = DateTime.Now
        });
        //await context.BillOfMaterialItems.AddRangeAsync(billOfMaterial.Items); 
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
            .AsSplitQuery().Include(b => b.Items).ThenInclude(i => i.ComponentMaterial)
            .Include(b => b.Items).ThenInclude(i => i.ComponentProduct)
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