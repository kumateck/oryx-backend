using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Products;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

#nullable enable
namespace APP.Repository;

/// <summary>
/// Which approval workflow a test result (<see cref="Response"/>) goes through. QA routes results by what was tested:
/// material (raw or packaging) results to the Material ATR workflow, finished products to the Product ATR workflow, and
/// intermediate or bulk products to the Beta or non-Beta (tablet, syrup, ointment) workflow. A result with no route, or
/// whose route has no configured stage, uses the general "Response" workflow, as before.
/// </summary>
internal static class ResponseApprovalRoute
{
    public const string General = nameof(Response);
    public const string Material = "ResponseMaterial";
    public const string ProductFinished = "ResponseProductFinished";
    public const string IntermediateGeneral = "ResponseProductIntermediateGeneral";
    public const string IntermediateBeta = "ResponseProductIntermediateBeta";

    /// <summary>The route for the result, before checking whether its workflow is configured.</summary>
    public static async Task<string> RouteAsync(ApplicationDbContext db, Guid responseId, CancellationToken cancellationToken = default)
    {
        var response = await db.Responses.AsNoTracking().Where(item => item.Id == responseId)
            .Select(item => new { item.RoutineTrackId, item.MaterialBatchId, item.MaterialSamplingId, item.BatchManufacturingRecordId, item.ProductionActivityStepId })
            .SingleOrDefaultAsync(cancellationToken);
        if (response is null || response.RoutineTrackId.HasValue) return General;
        if (response.MaterialBatchId.HasValue || response.MaterialSamplingId.HasValue) return Material;
        if (!response.BatchManufacturingRecordId.HasValue) return General;
        var test = await db.AnalyticalTestRequests.AsNoTracking()
            .Where(item => item.BatchManufacturingRecordId == response.BatchManufacturingRecordId && item.ProductionActivityStepId == response.ProductionActivityStepId)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new { item.Stage, Division = (Division?)item.ProductionScheduleProduct.Product.Division })
            .FirstOrDefaultAsync(cancellationToken);
        if (test is null) return General;
        if (test.Stage == TestStage.Finished) return ProductFinished;
        return test.Division == Division.BetaLactam ? IntermediateBeta : IntermediateGeneral;
    }

    /// <summary>The approval configuration type for the result: its route when that workflow has a stage, otherwise the general one.</summary>
    public static async Task<string> ConfigurationTypeAsync(ApplicationDbContext db, Guid responseId, CancellationToken cancellationToken = default)
    {
        var route = await RouteAsync(db, responseId, cancellationToken);
        if (route == General) return General;
        var configured = await db.Approvals.AsNoTracking()
            .AnyAsync(item => item.ItemType == route && item.ApprovalStages.Any(), cancellationToken);
        return configured ? route : General;
    }
}
