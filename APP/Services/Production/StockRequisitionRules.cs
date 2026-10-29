using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Routes;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Production;

/// <summary>
/// A batch gets one raw material ("RM") and one packaging ("PM") stock requisition. Raw material is
/// requested first; packaging can follow at any point until final packing is recorded. Anything more
/// is an extra material request, which needs the main requisition of that kind.
/// </summary>
public static class StockRequisitionRules
{
    public const string RawPrefix = "RM";
    public const string PackagingPrefix = "PM";

    private static Task<bool> Raised(ApplicationDbContext context, Guid batchId, string prefix) =>
        context.Requisitions.AnyAsync(r =>
            r.ProductionScheduleProductId == batchId
            && r.RequisitionType == RequisitionType.Stock
            && r.Code.StartsWith(prefix));

    public static async Task<Result> CanRaise(ApplicationDbContext context, Guid batchId, bool raw, bool packaging)
    {
        var rawRaised = await Raised(context, batchId, RawPrefix);
        if (raw && rawRaised)
            return Error.Conflict("StockRequisition.RawAlreadyRaised",
                "The raw material stock requisition for this batch has already been raised. Request extra material instead.");
        if (!packaging)
            return Result.Success();
        if (await Raised(context, batchId, PackagingPrefix))
            return Error.Conflict("StockRequisition.PackagingAlreadyRaised",
                "The packaging material stock requisition for this batch has already been raised. Request extra material instead.");
        if (!raw && !rawRaised)
            return Error.Validation("StockRequisition.RawFirst",
                "Raise the raw material stock requisition before requesting packaging material.");
        if (await context.FinalPackings.AnyAsync(p => p.ProductionScheduleProductId == batchId))
            return Error.Conflict("StockRequisition.FinalPackingRecorded",
                "Final packing has been recorded for this batch, so packaging material can no longer be requisitioned.");
        return Result.Success();
    }

    /// <summary>Extra material of a kind can only be asked for once the batch's main requisition of that kind exists.</summary>
    public static async Task<Result> CanRequestExtra(ApplicationDbContext context, Guid batchId, IEnumerable<MaterialKind> kinds)
    {
        foreach (var kind in kinds.Distinct())
        {
            var raw = kind == MaterialKind.Raw;
            if (!await Raised(context, batchId, raw ? RawPrefix : PackagingPrefix))
                return Error.Validation("ExtraPacking.MainRequisitionMissing",
                    $"Raise the main {(raw ? "raw material" : "packaging material")} stock requisition for this batch before requesting extra.");
        }
        return Result.Success();
    }

    /// <summary>The batch's packaging requisition has been raised and every item of it issued.</summary>
    public static Task<bool> PackagingIssued(ApplicationDbContext context, Guid batchId) =>
        context.Requisitions.AnyAsync(r =>
            r.ProductionScheduleProductId == batchId
            && r.RequisitionType == RequisitionType.Stock
            && r.Code.StartsWith(PackagingPrefix)
            && r.Status == RequestStatus.Completed);

    /// <summary>
    /// Why a step cannot be completed by hand yet, as far as packaging goes: the step right before final packing,
    /// and any step packaging was requested from, wait until the packaging requisition is issued.
    /// </summary>
    public static async Task<Result> CanCompleteStep(ApplicationDbContext context, Guid stepId, Guid activityId, int order, Guid batchId)
    {
        var requestedHere = await context.Requisitions.AnyAsync(r =>
            r.ProductionActivityStepId == stepId
            && r.RequisitionType == RequisitionType.Stock
            && r.Code.StartsWith(PackagingPrefix)
            && r.Status != RequestStatus.Completed);
        if (requestedHere)
            return Error.Conflict("ProductActivity.PackagingNotIssued",
                "The packaging material requested from this step has not been issued yet. Complete the step once it is issued.");
        if (await NextIsFinalPacking(context, activityId, order) && !await PackagingIssued(context, batchId))
            return Error.Conflict("ProductActivity.PackagingNotIssued",
                "Final packing comes next. The batch's packaging material must be requested and issued before this step is completed.");
        return Result.Success();
    }

    /// <summary>Whether the step that follows this one in its production run is the final packing step.</summary>
    public static async Task<bool> NextIsFinalPacking(ApplicationDbContext context, Guid activityId, int order)
    {
        var next = await context.ProductionActivitySteps
            .Where(s => s.ProductionActivityId == activityId && s.Order > order)
            .OrderBy(s => s.Order)
            .Select(s => new
            {
                FinalPacking = s.ResponsibleUsers.Any(u => u.Action == OperationAction.FinalPackingAndPartialReturn),
                Name = s.Operation.Name,
            })
            .FirstOrDefaultAsync();
        // Older procedures recognise the final packing step by its operation name (see material consumption on start).
        return next is not null && (next.FinalPacking || (next.Name ?? "").StartsWith("Final Packing"));
    }

    /// <summary>
    /// Whether a requisition starts and finishes its step: only on the batch's stock requisition step (one set up for
    /// stock requisition, or the one the raw requisition was raised from). There every requisition on the step must be
    /// issued before it finishes, as before. Packaging requested later from any other step never moves that step.
    /// </summary>
    public static async Task<bool> DrivesStep(ApplicationDbContext context, Guid stepId) =>
        await context.Requisitions.AnyAsync(r =>
            r.ProductionActivityStepId == stepId && r.RequisitionType == RequisitionType.Stock && r.Code.StartsWith(RawPrefix))
        || await context.ProductionActivitySteps.Where(s => s.Id == stepId)
            .AnyAsync(s => s.ResponsibleUsers.Any(u => u.Action == OperationAction.StockRequisitionOrIssue));
}
