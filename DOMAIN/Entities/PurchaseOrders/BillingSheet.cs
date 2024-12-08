using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Procurement.Suppliers;
using SHARED;

namespace DOMAIN.Entities.PurchaseOrders;

public class BillingSheet : BaseEntity
{
    [StringLength(1000)] public string Code { get; set; }
    [StringLength(1000)] public string BillOfLading { get; set; }
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; }
    public Guid InvoiceId { get; set; }
    public PurchaseOrderInvoice Invoice { get; set; }
    public DateTime ExpectedArrivalDate { get; set; }
    public DateTime FreeTimeExpiryDate { get; set; }
    public TimeSpan FreeTimeDuration { get; set; }
    public DateTime DemurrageStartDate { get; set; }
    
    //container information
    [StringLength(100)] public string ContainerNumber { get; set; }
    [StringLength(1000)] public string NumberOfPackages { get; set; }
    [StringLength(1000)] public string PackageDescription { get; set; }
}

public class BillingSheetDto : WithAttachment
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string BillOfLading { get; set; }
    public CollectionItemDto Supplier { get; set; }
    public CollectionItemDto Invoice { get; set; }
    public DateTime ExpectedArrivalDate { get; set; }
    public DateTime FreeTimeExpiryDate { get; set; }
    public TimeSpan FreeTimeDuration { get; set; }
    public DateTime DemurrageStartDate { get; set; }
    
    //container information
    public string ContainerNumber { get; set; }
    public string NumberOfPackages { get; set; }
    public string PackageDescription { get; set; }
}