using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.ProductionOrders;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ProductionOrderRepository(ApplicationDbContext context, IMapper mapper) : IProductionOrderRepository
{
    public async Task<Result<Guid>> CreateProductionOrder(CreateProductionOrderRequest request)
    {
        var productionOrder = mapper.Map<ProductionOrder>(request);
        await context.AddAsync(productionOrder);
        await context.SaveChangesAsync();
        return productionOrder.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<ProductionOrderDto>>>> GetProductionOrders(int page, int pageSize, string searchQuery)
    {
        var query = context.ProductionOrders.AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.ProductionOrderCode);
        }
        
        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<ProductionOrderDto>);
    }

    public async Task<Result<ProductionOrderDto>> GetProductionOrder(Guid id)
    {
        var productionOrder = await context.ProductionOrders.FirstOrDefaultAsync(po => po.Id == id);
        return productionOrder is null ? 
            Error.NotFound("ProductionOrder.NotFound", "Production Order not found") 
            : mapper.Map<ProductionOrderDto>(productionOrder);
    }

    public async Task<Result> UpdateProductionOrder(Guid id, CreateProductionOrderRequest missing_name)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> DeleteProductionOrder(Guid id, Guid userId)
    {
        var productionOrder = await context.ProductionOrders.FirstOrDefaultAsync(po => po.Id == id);
        if (productionOrder == null) return Error.NotFound("ProductionOrder.NotFound", "Production Order not found");
        
        productionOrder.DeletedAt = DateTime.Now;
        productionOrder.LastDeletedById = userId;
        context.ProductionOrders.Update(productionOrder);
            
        await context.SaveChangesAsync();
        return Result.Success();

    }
}