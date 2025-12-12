using APP.Utils;
using DOMAIN.Entities.JobRequests;
using SHARED;

namespace APP.IRepository;

public interface IServiceQuotationRepository
{
    Task<Result<Guid>> CreateServiceQuotation(CreateServiceQuotationRequest request);
    Task<Result<Paginateable<IEnumerable<ServiceQuotationDto>>>> GetServiceQuotations(int page, int pageSize,
        QuotationStatus? status = null, Guid? jobOrderId = null, Guid? serviceProviderId = null);
    Task<Result<ServiceQuotationDto>> GetServiceQuotation(Guid id);
    Task<Result> UpdateServiceQuotation(Guid id, UpdateServiceQuotationRequest request);
    Task<Result> NegotiateQuotation(NegotiateQuotationRequest request);
    Task<Result<List<ServiceQuotationDto>>> CompareQuotations(CompareQuotationsRequest request);
}

