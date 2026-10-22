using INFRASTRUCTURE.Context;

namespace APP.Services.Stores;

/// <summary>Permission keys of the job request, job order and service procurement workflows.</summary>
public static class ServiceKeys
{
    public const string ViewJobRequests = "CanViewJobRequests", CreateJobRequest = "CanCreateJobRequest", EditJobRequest = "CanEditJobRequest",
        DeleteJobRequest = "CanDeleteJobRequest", AcknowledgeJobRequest = "CanAcknowledgeJobRequest", AssignJobRequest = "CanAssignJobRequest",
        StartJobRequest = "CanStartJobRequest", CompleteJobRequest = "CanCompleteJobRequest";
    public const string ViewJobOrders = "CanViewJobOrders", CreateJobOrder = "CanCreateJobOrder", EditJobOrder = "CanEditJobOrder", DeleteJobOrder = "CanDeleteJobOrder";
    public const string ViewQuotationResponses = "CanViewServicesQuotationResponses", SendQuotationResponse = "CanSendServicesQuotationResponse",
        ViewPricing = "CanViewServicesPricingSelection", ApplyPricing = "CanApplyChangesForServicesPricingSelection",
        ViewProformaRequests = "CanViewServicesProformaRequests", SendProformaRequest = "CanSendServicesProformaRequest",
        ViewProformaResponses = "CanViewServicesProformaResponses", SendProformaResponse = "CanSendServicesProformaResponse",
        ViewMemo = "CanViewServicesMemo", CreateMemo = "CanCreateServicesMemo",
        ViewPurchaseOrders = "CanViewServicesPurchaseOrders", CreatePurchaseOrder = "CanCreateServicesPurchaseOrder";
}

/// <summary>Current-grant checks for service workflows. Role names never bypass them.</summary>
public static class ServiceWorkflowAccess
{
    /// <summary>True when the actor holds every key.</summary>
    public static async Task<bool> All(ApplicationDbContext context, Guid? actor, params string[] keys)
    {
        if (!actor.HasValue) return false;
        var granted = await Authorization.CurrentPermissionSet.Load(context, actor);
        return keys.All(granted.Contains);
    }

    /// <summary>True when the actor holds at least one key.</summary>
    public static async Task<bool> Any(ApplicationDbContext context, Guid? actor, params string[] keys)
    {
        if (!actor.HasValue) return false;
        var granted = await Authorization.CurrentPermissionSet.Load(context, actor);
        return keys.Any(granted.Contains);
    }
}
