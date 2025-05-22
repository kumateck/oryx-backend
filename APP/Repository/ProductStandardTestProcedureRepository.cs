using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.ProductStandardTestProcedures;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ProductStandardTestProcedureRepository(ApplicationDbContext context, IMapper mapper) : IProductStandardTestProcedureRepository
{
     public async Task<Result<Guid>> CreateProductStandardTestProcedure(CreateProductStandardTestProcedureRequest request)
    {
        var existingProcedure = await context.ProductStandardTestProcedures
            .FirstOrDefaultAsync(stp => stp.StpNumber == request.StpNumber && stp.LastDeletedById == null);

        if (existingProcedure != null)
        {
            return Error.Validation("ProductStandardTestProcedure.Exists", "Product Standard test procedure already exists.");
        }
        
        var product = await context.Products.FirstOrDefaultAsync(m => m.Id == request.ProductId);

        if (product == null)
        {
            return Error.Validation("Invalid.Product", "Invalid Product");
        }
        
        var productStandardTestProcedure = mapper.Map<ProductStandardTestProcedure>(request);
        await context.ProductStandardTestProcedures.AddAsync(productStandardTestProcedure);
        
        await context.SaveChangesAsync();
        return productStandardTestProcedure.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<ProductStandardTestProcedureDto>>>> GetProductStandardTestProcedures(int page, int pageSize, string searchQuery)
    {
        var query = context.ProductStandardTestProcedures
            .AsQueryable()
            .Include(stp => stp.Product)
            .Where(stp => stp.DeletedAt == null)
            .AsSplitQuery();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, stp => stp.StpNumber);
        }
        
        return await PaginationHelper
            .GetPaginatedResultAsync(query,
                page,
                pageSize,
                mapper.Map<ProductStandardTestProcedureDto>);
        
    }

    public async Task<Result<ProductStandardTestProcedureDto>> GetProductStandardTestProcedure(Guid id)
    {
        var procedure = await context.ProductStandardTestProcedures
            .Include(stp => stp.Product)
            .FirstOrDefaultAsync(stp => stp.Id == id && stp.LastDeletedById == null);
        
        return procedure is null ? 
            Error.NotFound("ProductStandardTestProcedure.NotFound", "Product Standard test procedure not found") : 
            mapper.Map<ProductStandardTestProcedureDto>(procedure
            , opts => {opts.Items[AppConstants.ModelType] = nameof(ProductStandardTestProcedure);});
    }

    public async Task<Result> UpdateProductStandardTestProcedure(Guid id, CreateProductStandardTestProcedureRequest request)
    {
        var procedure = await context.ProductStandardTestProcedures
            .FirstOrDefaultAsync(stp => stp.Id == id && stp.LastDeletedById == null);
        
        if (procedure is null)
        {
            return Error.NotFound("ProductStandardTestProcedure.NotFound", "Product Standard test procedure not found");
        }
        
        mapper.Map(request, procedure);
        
        context.ProductStandardTestProcedures.Update(procedure);
        await context.SaveChangesAsync();
        
        return Result.Success();
    }

    public async Task<Result> DeleteProductStandardTestProcedure(Guid id, Guid userId)
    {
        var procedure = await context.ProductStandardTestProcedures
            .FirstOrDefaultAsync(stp => stp.Id == id && stp.LastDeletedById == null);
        if (procedure is null)
        {
            return Error.NotFound("ProductStandardTestProcedure.NotFound", "Product Standard test procedure not found");
        }
        
        procedure.DeletedAt = DateTime.UtcNow;
        procedure.LastDeletedById = userId;
        
        context.ProductStandardTestProcedures.Update(procedure);
        await context.SaveChangesAsync();
        
        return Result.Success();
    }
}