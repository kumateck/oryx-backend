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

public class PurchasedPoReportDto
{
    public int No { get; set; }

    public string SupplierType { get; set; }  
    public string SupplierName { get; set; }

    public string PoNumber { get; set; }
    public string InvoiceNumber { get; set; }

    public string MaterialName { get; set; }

    public decimal OrderedQuantity { get; set; }
    public string OrderedUom { get; set; }

    public decimal QuantityReceived { get; set; }
    public string ReceivedUom { get; set; }

    public decimal UnitCost { get; set; }
    public string CurrencySymbol { get; set; }

    public decimal MaterialPurchaseValue => QuantityReceived * UnitCost;

    public DateTime InvoiceDate { get; set; }
}


public class PurchaseOrderFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<Guid> SupplierIds { get; set; }
    public string PoNumber {get; set;}
}