using APP.Utils;
using DOMAIN.Entities.JobRequests;
using SHARED;

namespace APP.IRepository;

public interface IServiceProformaInvoiceRepository
{
    Task<Result<Guid>> RequestServiceProformaInvoice(RequestServiceProformaInvoiceRequest request, Guid requestedById);
    Task<Result> RespondServiceProformaInvoice(RespondServiceProformaInvoiceRequest request);
    Task<Result> ApproveServiceProformaInvoice(ApproveServiceProformaInvoiceRequest request);
    Task<Result<Paginateable<IEnumerable<ServiceProformaInvoiceDto>>>> GetServiceProformaInvoices(int page, int pageSize,
        ServiceProformaInvoiceStatus? status = null, Guid? jobOrderId = null, Guid? serviceProviderId = null);
    Task<Result<ServiceProformaInvoiceDto>> GetServiceProformaInvoice(Guid id);
}

