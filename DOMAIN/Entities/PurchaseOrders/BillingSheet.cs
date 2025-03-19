using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Charges;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.Shipments;
using SHARED;

namespace DOMAIN.Entities.PurchaseOrders;

public class BillingSheet : BaseEntity
{
    [StringLength(1000)] public string Code { get; set; }
    [StringLength(1000)] public string BillOfLading { get; set; }
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; }
    public Guid InvoiceId { get; set; }
    public ShipmentInvoice Invoice { get; set; }
    public DateTime ExpectedArrivalDate { get; set; }
    public DateTime FreeTimeExpiryDate { get; set; }
    [StringLength(100)] public string FreeTimeDuration { get; set; }
    public DateTime DemurrageStartDate { get; set; }
    public BillingSheetStatus Status { get; set; }
    
    //container information
    [StringLength(100)] public string ContainerNumber { get; set; }
    [StringLength(1000)] public string NumberOfPackages { get; set; }
    [StringLength(1000)] public string PackageDescription { get; set; }
    public List<Charge> Charges { get; set; } = [];
}

public enum BillingSheetStatus
{
    Pending,
    Paid
}

public class BillingSheetDto : WithAttachment
{
    public string Code { get; set; }
    public string BillOfLading { get; set; }
    public SupplierDto Supplier { get; set; }
    public ShipmentInvoiceDto Invoice { get; set; }
    public DateTime ExpectedArrivalDate { get; set; }
    public BillingSheetStatus Status { get; set; }
    public DateTime FreeTimeExpiryDate { get; set; }
    public string FreeTimeDuration { get; set; }
    public DateTime DemurrageStartDate { get; set; }
    public List<ChargeDto> Charges { get; set; } 
    
    //container information
    public string ContainerNumber { get; set; }
    public string NumberOfPackages { get; set; }
    public string PackageDescription { get; set; }
}