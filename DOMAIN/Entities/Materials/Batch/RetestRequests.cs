using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Materials.Batch;

public class RequestRetestDto
{
    public string Reason { get; set; }
}

public class CompleteRetestDto
{
    [Required]
    public DateTime ExtendedExpiryDate { get; set; }
    public string Notes { get; set; }
}
