using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.ProductionOrders;
using DOMAIN.Entities.Products;

namespace DOMAIN.Entities.ProformaInvoices;

public class CreateProformaInvoice
{
    public Guid ProductionOrderId { get; set; }
    [MinLength(1, ErrorMessage = "At least one product must be included in the proforma invoice.")]
    public List<CreateProformaInvoiceProduct> Products { get; set; } = [];
}

public class CreateProformaInvoiceProduct
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
}

public class ProformaInvoice : BaseEntity
{
    public Guid ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; }
    public List<ProformaInvoiceProduct> Products { get; set; } = [];
}

public class ProformaInvoiceProduct : BaseEntity
{
    public Guid ProformaInvoiceId { get; set; }
    public ProformaInvoice ProformaInvoice { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    public decimal Quantity { get; set; }
}

public class ProformaInvoiceDto : BaseDto
{
    public ProductionOrderDto ProductionOrder { get; set; }
    public List<ProformaInvoiceDto> Products { get; set; } = [];
}

public class ProformaInvoiceProductDto : BaseDto
{
    public ProductDto Product { get; set; }
    public decimal Quantity { get; set; }
}