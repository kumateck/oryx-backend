using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Products;

namespace DOMAIN.Entities.Customers;

public class CustomerQuotation : BaseEntity, IRequireApproval
{
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; }
    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; }
    [Required, StringLength(100)] public string Code { get; set; }
    public CustomerQuotationStatus Status { get; set; }
    public DateTime ValidUntil { get; set; }
    public bool Approved { get; set; }
    public List<CustomerQuotationItem> Items { get; set; } = [];
    public List<CustomerQuotationApproval> Approvals { get; set; } = [];
}

public class CustomerQuotationItem : BaseEntity
{
    public Guid CustomerQuotationId { get; set; }
    public CustomerQuotation CustomerQuotation { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    public int Quantity { get; set; }
    public Guid ProductPackingId { get; set; }
    public ProductPacking ProductPacking { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal TotalValue => Quantity * UnitPrice * (1 - DiscountPercent / 100m);
}

public class CustomerQuotationApproval : ResponsibleApprovalStage
{
    public Guid Id { get; set; }
    public Guid CustomerQuotationId { get; set; }
    public CustomerQuotation CustomerQuotation { get; set; }
    public Guid ApprovalId { get; set; }
    public Approval Approval { get; set; }
}

public enum CustomerQuotationStatus
{
    Draft = 0,
    Sent = 1,
    Accepted = 2,
    Rejected = 3,
    Expired = 4,
    ConvertedToOrder = 5,
}
