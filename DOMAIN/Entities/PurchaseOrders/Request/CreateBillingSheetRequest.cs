using DOMAIN.Entities.Charges;

namespace DOMAIN.Entities.PurchaseOrders.Request;

public class CreateBillingSheetRequest : UpdateBillingSheetRequest
{
    public List<CreateBillingSheetCharge> Charges { get; set; } = [];
}

public class UpdateBillingSheetRequest
{
    public string Code { get; set; }
    public string BillOfLading { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid InvoiceId { get; set; }
    public DateTime ExpectedArrivalDate { get; set; }
    public DateTime FreeTimeExpiryDate { get; set; }
    public string FreeTimeDuration { get; set; }
    public DateTime DemurrageStartDate { get; set; }
    //container information
    public string ContainerNumber { get; set; }
    public string NumberOfPackages { get; set; } 
    public string PackageDescription { get; set; }
    public Guid? ContainerPackageStyleId { get; set; }
}

public class CreateBillingSheetCharge
{
    public Guid ChargeId { get; set; }
    public Guid? CurrencyId { get; set; }
    public decimal Amount { get; set; }
}

public class MarkBillingSheetCharge
{
    public List<Guid> BillingSheetChargeIds { get; set; } = [];
}