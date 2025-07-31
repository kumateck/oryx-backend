using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.ProformaInvoices;

namespace DOMAIN.Entities.Invoices;

public class CreateInvoice
{
    public Guid ProformaInvoiceId { get; set; }
    public string CustomerPoNumber { get; set; }
}
public class Invoice : BaseEntity
{
    public Guid ProformaInvoiceId { get; set; }
    public ProformaInvoice ProformaInvoice { get; set; }
    public string CustomerPoNumber { get; set; }
    public InvoiceStatus Status { get; set; }
}

public enum InvoiceStatus
{
    Pending,
    Completed,
}

public class InvoiceDto : WithAttachment
{
    public ProformaInvoiceDto ProformaInvoice { get; set; }
    public string CustomerPoNumber { get; set; }
    public InvoiceStatus Status { get; set; }
}