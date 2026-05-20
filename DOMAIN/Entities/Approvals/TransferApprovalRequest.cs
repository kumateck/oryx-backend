using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Approvals;

public class TransferApprovalRequest
{
    [Required] public Guid FromUserId { get; set; }
    [Required] public Guid ToUserId { get; set; }
}
