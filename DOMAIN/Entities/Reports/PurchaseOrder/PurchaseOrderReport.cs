namespace DOMAIN.Entities.Reports.PurchaseOrder;

public class PurchaseOrderReportDto
{
    public int No {get; set;}
    public string SupplierName {get; set;}
    public string ProformaInvoiceNumber {get; set;}
    public string PurchaseOrderNumber {get; set;}
    public string MaterialName {get; set;}
    public decimal OrderQuantity {get; set;}
    public string UomName {get; set;}
    public decimal UnitPrice {get; set;}
    public string CurrencySymbol {get; set;}
    public decimal MaterialValue=> OrderQuantity * UnitPrice;
    public DateTime  PurchaseOrderDate {get; set;}
    public DateTime? ExpectedDeliverydate {get; set;}
}


public class PurchaseOrderFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<Guid>? SupplierIds { get; set; }
    public string PoNumber {get; set;}
}