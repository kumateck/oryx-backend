using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.JobRequests;

public class CreateServiceMemoRequest
{
    [Required]
    public Guid JobOrderId { get; set; }
    
    [Required]
    public Guid ServiceQuotationId { get; set; }
    
    [Required]
    public Guid ServiceProviderId { get; set; }
    
    [Required]
    public DateTime IssuedDate { get; set; }
    
    [Required]
    public Guid IssuedById { get; set; }
    
    [Required, Range(0, double.MaxValue)]
    public decimal AgreedServiceCharge { get; set; }
    
    [Required, Range(0, double.MaxValue)]
    public decimal AgreedMaterialsCost { get; set; }
    
    [Required]
    public DateTime ExpectedStartDate { get; set; }
    
    [Required]
    public DateTime ExpectedCompletionDate { get; set; }
    
    [StringLength(2000)]
    public string TermsAndConditions { get; set; }
    
    [StringLength(2000)]
    public string SpecialInstructions { get; set; }
}

public class UpdateServiceMemoRequest
{
    public decimal? AgreedServiceCharge { get; set; }
    public decimal? AgreedMaterialsCost { get; set; }
    public DateTime? ExpectedStartDate { get; set; }
    public DateTime? ExpectedCompletionDate { get; set; }
    public string TermsAndConditions { get; set; }
    public string SpecialInstructions { get; set; }
}

public class IssueServiceMemoRequest
{
    [Required]
    public Guid ServiceMemoId { get; set; }
}

