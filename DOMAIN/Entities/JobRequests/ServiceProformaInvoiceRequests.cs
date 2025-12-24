using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.JobRequests;

public class RequestServiceProformaInvoiceRequest
{
    [Required]
    public Guid JobOrderId { get; set; }

    [Required]
    public Guid ServiceQuotationId { get; set; }

    [StringLength(2000)]
    public string Notes { get; set; }
}

public class RespondServiceProformaInvoiceRequest
{
    [Required]
    public Guid ServiceProformaInvoiceId { get; set; }

    [StringLength(100)]
    public string InvoiceNumber { get; set; }

    [Required]
    public DateTime ResponseDate { get; set; }

    [StringLength(2000)]
    public string ResponseNotes { get; set; }

    [StringLength(1000)]
    public string ProformaInvoiceDocumentUrl { get; set; }

    // Optional: Allow contractor to update prices if needed
    public List<UpdateProformaInvoiceItemRequest> UpdatedItems { get; set; } = [];
}

public class UpdateProformaInvoiceItemRequest
{
    [Required]
    public Guid ServiceProformaInvoiceItemId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? UnitPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Quantity { get; set; }
}

public class ApproveServiceProformaInvoiceRequest
{
    [Required]
    public Guid ServiceProformaInvoiceId { get; set; }

    [Required]
    public Guid ApprovedById { get; set; }

    [StringLength(1000)]
    public string ApprovalComments { get; set; }
}

