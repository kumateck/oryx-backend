using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Materials.Batch;

public class UpdateBatchStatusRequest
{
    [RegularExpression("^(Received|Quarantine|Testing|Available|Rejected|Retest)$", 
        ErrorMessage = "Invalid batch status.")]
    public string Status { get; set; }

    public List<Guid> MaterialBatchIds { get; set; }
}