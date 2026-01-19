using DOMAIN.Entities.Base;
using DOMAIN.Entities.ServiceProviders;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.JobRequests;

public class ServiceMemoDto : BaseDto
{
    public string MemoNumber { get; set; }
    public Guid JobOrderId { get; set; }
    public Guid ServiceQuotationId { get; set; }
    public ServiceProviderReducedDto ServiceProvider { get; set; }
    public DateTime IssuedDate { get; set; }
    public UserDto IssuedBy { get; set; }
    public decimal AgreedServiceCharge { get; set; }
    public decimal AgreedMaterialsCost { get; set; }
    public decimal TotalAgreedCost { get; set; }
    public DateTime ExpectedStartDate { get; set; }
    public DateTime ExpectedCompletionDate { get; set; }
    public string TermsAndConditions { get; set; }
    public string SpecialInstructions { get; set; }
    public ServiceMemoStatus Status { get; set; }
    public bool Approved { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public bool Paid { get; set; }
}

