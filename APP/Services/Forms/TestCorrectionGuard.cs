using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Materials.Batch;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
namespace APP.Services.Forms;
public static class TestCorrectionGuard
{
    public static async Task Lock(ApplicationDbContext db, Guid? material, Guid? bmr, Guid? step)
    {
        if (db.Database.ProviderName?.Contains("Npgsql") != true || db.Database.CurrentTransaction == null) return;
        await db.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '3s'");
        var key = $"test-correction:{material}:{bmr}:{step}";
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))");
    }
    public static Task<bool> IsReturned(ApplicationDbContext db, Guid id) => db.TestCorrectionReturns.AnyAsync(h => h.SourceResponseId == id);
    public static async Task<Result> Writer(ApplicationDbContext db, Response response, Guid actorId)
    {
        if (!await db.TestCorrectionReturns.AnyAsync(h => h.CorrectionResponseId == response.Id)) return Result.Success();
        var key = APP.Utils.PermissionKeys.CanStartProductTest;
        if (response.MaterialBatchId.HasValue)
        {
            var kind = await db.MaterialBatches.Where(b => b.Id == response.MaterialBatchId).Select(b => b.Material.Kind).SingleAsync();
            key = kind == DOMAIN.Entities.Materials.MaterialKind.Raw ? APP.Utils.PermissionKeys.CanStartRawMaterialTest : APP.Utils.PermissionKeys.CanStartPackagingMaterialTest;
        }
        if (!(await APP.Services.Authorization.CurrentPermissionSet.Load(db, actorId)).Contains(key)
            || !await db.FormFieldAssignees.AnyAsync(a => a.AssigneeId == actorId && a.FormAssignee.FormId == response.FormId
                && a.FormAssignee.MaterialBatchId == response.MaterialBatchId && a.FormAssignee.MaterialSamplingId == response.MaterialSamplingId
                && a.FormAssignee.BatchManufacturingRecordId == response.BatchManufacturingRecordId
                && a.FormAssignee.ProductionActivityStepId == response.ProductionActivityStepId))
            return Error.Forbidden("TestCorrection.Analyst", "Only an assigned analyst with current testing permission can save or submit this correction.");
        return Result.Success();
    }
    public static async Task<Result> CanGenerate(ApplicationDbContext db, Response response)
    {
        if (await IsReturned(db, response.Id)) return Error.Conflict("TestCorrection.Superseded", "This worksheet was returned for correction. Reload the current worksheet.");
        // Limit the additional submission gate to correction cycles; existing legacy certificate rules remain in force.
        if (!await db.TestCorrectionReturns.AnyAsync(h => h.CorrectionResponseId == response.Id)) return Result.Success();
        if (response.MaterialBatchId is Guid batchId)
        {
            var retest = await APP.Services.Materials.MaterialRetestTesting.ActiveAsync(db, batchId);
            if (retest != null ? retest.Status != MaterialRetestStatus.TestTaken
                : !await db.MaterialBatches.AnyAsync(b => b.Id == batchId && b.Status == BatchStatus.TestTaken))
                return Error.Conflict("TestCorrection.NotSubmitted", "The analyst must resubmit the corrected results before a certificate can be generated.");
        }
        else if (!await db.AnalyticalTestRequests.AnyAsync(a => a.BatchManufacturingRecordId == response.BatchManufacturingRecordId
            && a.ProductionActivityStepId == response.ProductionActivityStepId && a.Status == AnalyticalTestStatus.TestTaken))
            return Error.Conflict("TestCorrection.NotSubmitted", "The analyst must resubmit the corrected results before a certificate can be generated.");
        return Result.Success();
    }
}
