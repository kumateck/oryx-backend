using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.ServiceProviders;
using DOMAIN.Entities.Services;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.JobRequests;

/// <summary>
/// Represents a job order sent to external service providers for quotation
/// </summary>
public class JobOrder : BaseEntity
{
    [StringLength(100)]
    public string Code { get; set; }

    public Guid JobRequestId { get; set; }
    public JobRequest JobRequest { get; set; }

    public Guid? ServiceId { get; set; }
    public Service Service { get; set; }

    public DateTime IssuedDate { get; set; }

    public Guid IssuedById { get; set; }
    public User IssuedBy { get; set; }

    [StringLength(2000)]
    public string Description { get; set; }

    [StringLength(100)]
    public string IssuedBySignature { get; set; }

    public JobOrderStatus Status { get; set; } = JobOrderStatus.Pending;

    // Service providers this job order was sent to
    public List<JobOrderServiceProvider> ServiceProviders { get; set; } = [];

    // Quotations received from contractors
    public List<ServiceQuotation> Quotations { get; set; } = [];

    // Selected quotation
    public Guid? SelectedQuotationId { get; set; }
    public ServiceQuotation SelectedQuotation { get; set; }

    // Proforma Invoice
    public Guid? ServiceProformaInvoiceId { get; set; }
    public ServiceProformaInvoice ServiceProformaInvoice { get; set; }

    // Service Memo
    public Guid? ServiceMemoId { get; set; }
    public ServiceMemo ServiceMemo { get; set; }

    // Execution by external contractor
    public JobOrderExecution Execution { get; set; }
}

/// <summary>
/// Junction table for many-to-many relationship between JobOrders and ServiceProviders
/// </summary>
public class JobOrderServiceProvider
{
    public Guid Id { get; set; }

    public Guid JobOrderId { get; set; }
    public JobOrder JobOrder { get; set; }

    public Guid ServiceProviderId { get; set; }
    public ServiceProvider ServiceProvider { get; set; }

    public DateTime SentAt { get; set; }

    public bool ResponseReceived { get; set; }

    public DateTime? ResponseDate { get; set; }
}

public enum JobOrderStatus
{
    Pending,
    SentToProviders,
    QuotationsReceived,
    QuotationSelected,
    ProformaInvoiceRequested,
    ProformaInvoiceReceived,
    MemoCreated,
    InProgress,
    Completed,
    Approved,
    Cancelled
}

