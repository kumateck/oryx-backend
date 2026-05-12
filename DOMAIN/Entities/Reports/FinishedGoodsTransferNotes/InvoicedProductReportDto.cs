using DOMAIN.Entities.ProductionOrders;
using DOMAIN.Entities.Products;

namespace DOMAIN.Entities.Reports.FinishedGoodsTransferNotes;

public class InvoicedProductReportDto
{
    
}
public class InvoicedProductsSummaryReportDto
{
    public int No { get; set; }

    public string InvoiceNo { get; set; }
    public DateTime InvoiceDate { get; set; }

    public string CustomerName { get; set; }

    public string OrderNo { get; set; }
    public DateTime OrderDate { get; set; }

    public int NoOfBatches { get; set; }

    public decimal OrderQuantity { get; set; }
    public decimal QuantityAllocated { get; set; }
    public decimal OutstandingQuantity =>OrderQuantity - QuantityAllocated;

    public string ProductCode { get; set; }
    public string ProductName { get; set; }

    public string Uom { get; set; }

    public decimal UnitPrice { get; set; }
    public string PriceUoM { get; set; }
    public decimal ItemAmount => UnitPrice * QuantityAllocated;
}

public class InvoicedProductFilters
{
    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public Guid? ProductId { get; set; }

    public Guid? CustomerId { get; set; }

    public Division? WarehouseDivision { get; set; }
}

public class InvoicedProductsDetailedReportDto
{
    public int No { get; set; }

    public string InvoiceNo { get; set; }
    public DateTime InvoiceDate { get; set; }
    public string CustomerName { get; set; }
    public string OrderNo { get; set; }
    public DateTime OrderDate { get; set; }

    public string ProductName { get; set; }
    public string ProductCode { get; set; }
    public string BatchNo { get; set; }

    public decimal OrderQuantity { get; set; }        
    public decimal QuantityAllocated { get; set; }     
    public decimal OutstandingQuantity =>OrderQuantity - QuantityAllocated;

    
    public string Uom { get; set; }
    public decimal UnitPrice { get; set; }
    public string PriceUoM { get; set; }
    public decimal ItemAmount => QuantityAllocated * UnitPrice;

   
    public AllocateProductionOrderStatus AllocationStatus { get; set; }        
}