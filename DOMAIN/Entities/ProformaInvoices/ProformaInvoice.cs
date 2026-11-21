using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.ProductionOrders;
using DOMAIN.Entities.Products;

namespace DOMAIN.Entities.ProformaInvoices;

public class CreateProformaInvoice
{
    public string Code { get; set; }
    public Guid AllocateProductionOrderId { get; set; }
    [MinLength(1, ErrorMessage = "At least one product must be included in the proforma invoice.")]
    public List<CreateProformaInvoiceProduct> Products { get; set; } = [];
}

public class CreateProformaInvoiceProduct
{
    public Guid? ProductPackingId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
}

public class ProformaInvoice : BaseEntity, IRequireApproval
{
    public Guid? IssuedInvoiceId { get; set; }
    public string IssuedInvoiceCode { get; set; }
    public DateTime? InvoiceIssuedAt { get; set; }

    public string CustomerPoNumber { get; set; }
    public DateTime? CustomerPoDate { get; set; }
    public Guid? CustomerPoAttachmentId { get; set; }
    public DateTime? CustomerPoVerifiedAt { get; set; }
    public Guid? CustomerPoVerifiedById { get; set; }

    [StringLength(1000)] public string Code { get; set; }
    public Guid AllocateProductionOrderId { get; set; }
    public AllocateProductionOrder AllocateProductionOrder { get; set; }
    public ProformaInvoiceStatus Status { get; set; }
    public List<ProformaInvoiceProduct> Products { get; set; } = [];
    public List<ProformaInvoiceApproval> Approvals { get; set; } = [];
    public bool Approved { get; set; }
}

public class ProformaInvoiceApproval : ResponsibleApprovalStage
{
    public Guid Id { get; set; }
    public Guid ProformaInvoiceId { get; set; }
    public ProformaInvoice ProformaInvoice { get; set; }
    public Guid ApprovalId { get; set; }
    public Approval Approval { get; set; }
}

public enum ProformaInvoiceStatus
{
    Pending = 0,
    SentToCustomer = 1,
    Invoice = 2,
}

public class ProformaInvoiceProduct : BaseEntity
{
    public Guid? ProductPackingId { get; set; }
    public decimal? UnitPrice { get; set; }
    public Guid? PriceCurrencyId { get; set; }
    public Guid? PriceListRevisionId { get; set; }
    public Guid ProformaInvoiceId { get; set; }
    public ProformaInvoice ProformaInvoice { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    public decimal Quantity { get; set; }
}

public class ProformaInvoiceDto : BaseDto
{
    public bool Approved { get; set; }

    public Guid? IssuedInvoiceId { get; set; }
    public string IssuedInvoiceCode { get; set; }
    public DateTime? InvoiceIssuedAt { get; set; }

    public string CustomerPoNumber { get; set; }
    public DateTime? CustomerPoDate { get; set; }
    public Guid? CustomerPoAttachmentId { get; set; }
    public DateTime? CustomerPoVerifiedAt { get; set; }
    public Guid? CustomerPoVerifiedById { get; set; }

    public string Code { get; set; }
    public AllocateProductionOrderDto AllocateProductionOrder { get; set; }
    public ProformaInvoiceStatus Status { get; set; }
    public List<ProformaInvoiceProductDto> Products { get; set; } = [];
}

public class ProformaInvoiceProductDto : BaseDto
{
    public Guid? ProductPackingId { get; set; }
    public decimal? UnitPrice { get; set; }
    public Guid? PriceCurrencyId { get; set; }
    public Guid? PriceListRevisionId { get; set; }
    public ProductListDto Product { get; set; }
    public decimal Quantity { get; set; }
}
public class VerifyFinishedGoodsCustomerPoRequest
{
    public string CustomerPoNumber { get; set; }
    public DateTime CustomerPoDate { get; set; }
    public Guid AttachmentId { get; set; }
    public List<VerifyFinishedGoodsCustomerPoLine> Lines { get; set; } = [];
}
public class VerifyFinishedGoodsCustomerPoLine
{
    public Guid ProformaInvoiceProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public Guid CurrencyId { get; set; }
}
