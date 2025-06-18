using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.ProductAnalyticalRawData;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
using ZstdSharp.Unsafe;

namespace APP.Repository;

public class ProductAnalyticalRawDataRepository(ApplicationDbContext context, IMapper mapper) : IProductAnalyticalRawDataRepository
{
    public async Task<Result<Guid>> CreateAnalyticalRawData(CreateProductAnalyticalRawDataRequest request)
    {
        var existingAnalyticalRawData = await context.ProductAnalyticalRawData.FirstOrDefaultAsync(ad => ad.SpecNumber == request.SpecNumber);
        if (existingAnalyticalRawData is not null)
        {
            return Error.Validation("ProductAnalyticalRawData.Exists", "Analytical raw data already exists.");
        }
        
        var form = await context.Forms.FirstOrDefaultAsync(f => f.Id == request.FormId);

        if (form is null)
        {
            return Error.Validation("Form.Invalid", "Form is invalid.");
        }
        
        var stpNumber = await context.ProductStandardTestProcedures
            .AnyAsync(mstp => mstp.StpNumber == request.StpNumber);

        if (!stpNumber)
        {
            return Error.Validation("ProductAnalyticalRawData.StpNumberNotFound", "Stp number not found.");
        }
        
        var analyticalRawData = mapper.Map<ProductAnalyticalRawData>(request);
        
        await context.ProductAnalyticalRawData.AddAsync(analyticalRawData);
        await context.SaveChangesAsync();
        
        return analyticalRawData.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<ProductAnalyticalRawDataDto>>>> GetAnalyticalRawData(int page, int pageSize, string searchQuery)
    {
        var query = context.ProductAnalyticalRawData
            .AsSplitQuery()
            .Include(ad => ad.ProductStandardTestProcedure)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery,
                ad => ad.SpecNumber
                ,ad => ad.StpNumber);
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            if (Enum.TryParse<Stage>(searchQuery, true, out var stage))
                query = query.Where(ad => ad.Stage == stage);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<ProductAnalyticalRawDataDto>);
    }

    public async Task<Result<ProductAnalyticalRawDataDto>> GetAnalyticalRawData(Guid id)
    {
        var analyticalRawData = await context.ProductAnalyticalRawData
            .AsSplitQuery()
            .Include(ad => ad.ProductStandardTestProcedure)
            .ThenInclude(ad => ad.Product)
            .FirstOrDefaultAsync(ad => ad.Id == id);
        
        return analyticalRawData is null ?
            Error.NotFound("ProductAnalyticalRawData.NotFound", "Product analytical raw data not found") : 
            mapper.Map<ProductAnalyticalRawDataDto>(analyticalRawData, opts =>
            {
                opts.Items[AppConstants.ModelType] = nameof(ProductAnalyticalRawData);
            });
    }

    public async Task<Result> UpdateAnalyticalRawData(Guid id, CreateProductAnalyticalRawDataRequest request)
    {
        var analyticalRawData = await context.ProductAnalyticalRawData
            .FirstOrDefaultAsync(ad => ad.Id == id);

        if (analyticalRawData is null)
        {
            return Error.NotFound("ProductAnalyticalRawData.NotFound", "Product analytical raw data not found");
        }
        
        mapper.Map(request, analyticalRawData);
        
        context.ProductAnalyticalRawData.Update(analyticalRawData);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteAnalyticalRawData(Guid id, Guid userId)
    {
        var analyticalRawData = await context.ProductAnalyticalRawData
            .FirstOrDefaultAsync(ad => ad.Id == id);
        if (analyticalRawData is null)
        {
            return Error.NotFound("ProductAnalyticalRawData.NotFound", "Product analytical raw data not found");
        }
        
        analyticalRawData.DeletedAt = DateTime.UtcNow;
        analyticalRawData.LastDeletedById = userId;
        
        context.ProductAnalyticalRawData.Update(analyticalRawData);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
