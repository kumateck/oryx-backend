using DOMAIN.Entities.ProductsSampling;
using SHARED;

namespace APP.IRepository;

public interface IProductSamplingRepository
{
    Task<Result<Guid>> CreateProductSampling(CreateProductSamplingRequest productSampling);
    
    Task<Result<ProductSamplingDto>> GetProductSamplingByProductId(Guid id);
}