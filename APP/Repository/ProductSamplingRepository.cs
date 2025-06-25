using APP.IRepository;
using AutoMapper;
using DOMAIN.Entities.ProductsSampling;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ProductSamplingRepository(ApplicationDbContext context, IMapper mapper) : IProductSamplingRepository
{
    public async Task<Result<Guid>> CreateProductSampling(CreateProductSamplingRequest productSampling)
    {
        var analyticalTestRequest = context.AnalyticalTestRequests.FirstOrDefault(atr =>
                atr.Id == productSampling.AnalyticalTestRequestId);

        if (analyticalTestRequest == null)
        {
            return Error.Validation("ATR.Invalid", "Invalid analytical test request");
        }
        
        var productSample =  context.ProductSamplings
            .FirstOrDefault(ps => ps.AnalyticalTestRequestId == analyticalTestRequest.Id);

        if (productSample != null)
        {
            return Error.Validation("ProductSampling", "Product Sampling already exists");
        }
        
        var request = mapper.Map<ProductSampling>(productSampling);
        
        await context.ProductSamplings.AddAsync(request);
        await context.SaveChangesAsync();
        
        return request.Id;
    }

    public async Task<Result<ProductSamplingDto>> GetProductSamplingByProductId(Guid id)
    {
        var productSampling =  await context.ProductSamplings
            .AsSplitQuery()
            .Include(ps => ps.AnalyticalTestRequest)
            .Include(ps => ps.CreatedBy)
            .FirstOrDefaultAsync(ps => ps.Id == id);

        return productSampling == null ? 
            Error.Validation("ProductSampling", "Product Sampling not found") 
            : Result.Success(mapper.Map<ProductSamplingDto>(productSampling));
    }
}