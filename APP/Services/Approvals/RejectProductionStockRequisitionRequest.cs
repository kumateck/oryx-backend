using System.ComponentModel.DataAnnotations;
namespace APP.Services.Approvals;
public sealed record RejectProductionStockRequisitionRequest(
    [Required, MaxLength(1000)] string Reason,
    [Range(0, int.MaxValue)] int ContentRevision);
