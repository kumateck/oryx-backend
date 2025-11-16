using DOMAIN.Entities.ProductsSampling;
using SHARED;

namespace APP.IRepository;

public interface IProductSamplingRepository
{ 
    Task<Result<Guid>> CreateProductSampling(CreateProductSamplingRequest productSampling);
    Task<Result<ProductSamplingDto>> GetProductSamplingByBmrId(Guid batchManufacturingRecordId);
    Task<Result> AddIssueNumberToProductSample(Guid productSampleId, string issueNumber, Guid userId);
}