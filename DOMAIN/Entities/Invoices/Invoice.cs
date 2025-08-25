using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Customers;
using DOMAIN.Entities.ProformaInvoices;

namespace DOMAIN.Entities.Invoices;

public class CreateInvoice
{
    public Guid ProformaInvoiceId { get; set; }
    public Guid CustomerId { get; set; }
}
public class Invoice : BaseEntity
{
    public Guid ProformaInvoiceId { get; set; }
    public ProformaInvoice ProformaInvoice { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; }
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
    public CustomerDto Customer { get; set; }
    public InvoiceStatus Status { get; set; }
}