using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Customers;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Payments;
using DOMAIN.Entities.ProformaInvoices;

namespace DOMAIN.Entities.Invoices;

public class CreateInvoice
{
    public Guid ProformaInvoiceId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? TermsOfPaymentId { get; set; }
    public List<CreateInvoiceAmount> Amounts { get; set; } = [];
}

public class CreateInvoiceAmount
{
    public Guid CurrencyId { get; set; }
    public decimal Amount { get; set; }
}
public class Invoice : BaseEntity
{
    public Guid ProformaInvoiceId { get; set; }
    public ProformaInvoice ProformaInvoice { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; }
    public InvoiceStatus Status { get; set; }
    public Guid? TermsOfPaymentId { get; set; }
    public TermsOfPayment TermsOfPayment { get; set; }
    public DateTime? DueDate { get; set; }
    public List<InvoiceAmount> Amounts { get; set; } = [];
}

public class InvoiceAmount : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; }
    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; }
    public decimal Amount { get; set; }
}

public enum InvoiceStatus
{
    Pending,
    Approved,
}

public class InvoiceDto : WithAttachment
{
    public ProformaInvoiceDto ProformaInvoice { get; set; }
    public CustomerDto Customer { get; set; }
    public InvoiceStatus Status { get; set; }
    public TermsOfPaymentDto TermsOfPayment { get; set; }
    public DateTime? DueDate { get; set; }
    public List<InvoiceAmountDto> Amounts { get; set; } = [];
    public List<PayableBalanceDto> Balances { get; set; } = [];
}

public class InvoiceAmountDto : BaseDto
{
    public CurrencyDto Currency { get; set; }
    public decimal Amount { get; set; }
}
