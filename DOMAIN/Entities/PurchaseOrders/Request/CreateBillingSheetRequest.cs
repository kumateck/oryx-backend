using DOMAIN.Entities.Charges;

namespace DOMAIN.Entities.PurchaseOrders.Request;

public class CreateBillingSheetRequest
{
    public string Code { get; set; }
    public string BillOfLading { get; set; }
    public Guid SupplierId { get; set; }
    public Guid InvoiceId { get; set; }
    public DateTime ExpectedArrivalDate { get; set; }
    public DateTime FreeTimeExpiryDate { get; set; }
    public string FreeTimeDuration { get; set; }
    public DateTime DemurrageStartDate { get; set; }
    public List<CreateChargeRequest> Charges { get; set; } = [];
    
    //container information
    public string ContainerNumber { get; set; }
    public string NumberOfPackages { get; set; } 
    public string PackageDescription { get; set; }
    public Guid ContainerPackageStyleId { get; set; }
}
