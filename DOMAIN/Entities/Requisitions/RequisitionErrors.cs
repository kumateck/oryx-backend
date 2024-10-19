using SHARED;

namespace DOMAIN.Entities.Requisitions;

public static class RequisitionErrors
{
    public static Error NotFound(Guid requisitionId) =>
        Error.NotFound("Requisition.NotFound", $"The requisition with the Id: {requisitionId} was not found");
    
    public static Error NoPendingApprovals =>
        Error.NotFound("Requisition.NoPendingApprovals", $"The requisition has no pending approvals.");
}