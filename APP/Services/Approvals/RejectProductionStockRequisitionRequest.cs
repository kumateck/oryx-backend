using System.ComponentModel.DataAnnotations;
namespace APP.Services.Approvals;
public sealed record RejectProductionStockRequisitionRequest(
    [property: Required, MaxLength(1000)] string Reason,
    [property: Range(0, int.MaxValue)] int ContentRevision);
