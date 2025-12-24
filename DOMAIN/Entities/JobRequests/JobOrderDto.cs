using DOMAIN.Entities.Base;
using DOMAIN.Entities.ServiceProviders;
using DOMAIN.Entities.Services;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.JobRequests;

public class JobOrderDto : BaseDto
{
    public string Code { get; set; }
    public Guid JobRequestId { get; set; }
    public ServiceDto Service { get; set; }
    public DateTime IssuedDate { get; set; }
    public UserDto IssuedBy { get; set; }
    public string Description { get; set; }
    public JobOrderStatus Status { get; set; }
    public List<JobOrderServiceProviderDto> ServiceProviders { get; set; } = [];
    public List<ServiceQuotationDto> Quotations { get; set; } = [];
    public Guid? SelectedQuotationId { get; set; }
    public ServiceQuotationDto SelectedQuotation { get; set; }
    public Guid? ServiceProformaInvoiceId { get; set; }
    public ServiceProformaInvoiceDto ServiceProformaInvoice { get; set; }
    public ServiceMemoDto ServiceMemo { get; set; }
    public JobOrderExecutionDto Execution { get; set; }
}

public class JobOrderServiceProviderDto
{
    public Guid Id { get; set; }
    public Guid JobOrderId { get; set; }
    public ServiceProviderDto ServiceProvider { get; set; }
    public DateTime SentAt { get; set; }
    public bool ResponseReceived { get; set; }
    public DateTime? ResponseDate { get; set; }
}

