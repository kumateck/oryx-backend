using System.Data;
using System.Text.Json;
using APP.Services.Authorization;
using APP.Services.Formulas;
using DOMAIN.Entities.Formulas;
using APP.Utils;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products.Production;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
using SHARED.Services.Identity;

namespace APP.Services.Forms;

public sealed class TestCorrectionService(ApplicationDbContext db, ICurrentUserService actor)
{
    public static string ReturnKey(TemplateRepairTestKind kind) => kind switch {
        TemplateRepairTestKind.Product => PermissionKeys.CanReturnProductTestForCorrection,
        TemplateRepairTestKind.RawMaterial => PermissionKeys.CanReturnRawMaterialTestForCorrection,
        _ => PermissionKeys.CanReturnPackagingMaterialTestForCorrection };
    public static string HistoryKey(TemplateRepairTestKind kind) => kind switch {
        TemplateRepairTestKind.Product => PermissionKeys.CanViewProductTestCorrectionHistory,
        TemplateRepairTestKind.RawMaterial => PermissionKeys.CanViewRawMaterialTestCorrectionHistory,
        _ => PermissionKeys.CanViewPackagingMaterialTestCorrectionHistory };
    private static Error Denied() => Error.Forbidden("TestCorrection.Permission", "You cannot perform this action on this kind of test.");
    private static Error Blocked(string message) => Error.Conflict("TestCorrection.State", message);
    private IQueryable<Response> Cycle(Response source) => db.Responses.Where(r => r.FormId == source.FormId
        && r.MaterialBatchId == source.MaterialBatchId && r.MaterialSamplingId == source.MaterialSamplingId
        && r.BatchManufacturingRecordId == source.BatchManufacturingRecordId
        && r.ProductionActivityStepId == source.ProductionActivityStepId && r.RoutineTrackId == null);

    private async Task<bool> Matches(Response response, TemplateRepairTestKind kind)
    {
        if (response.RoutineTrackId != null) return false;
        if (kind == TemplateRepairTestKind.Product)
            return response.MaterialBatchId == null && response.BatchManufacturingRecordId != null
                && response.ProductionActivityStepId != null && await db.AnalyticalTestRequests.AnyAsync(a =>
                    a.BatchManufacturingRecordId == response.BatchManufacturingRecordId && a.ProductionActivityStepId == response.ProductionActivityStepId);
        return response.BatchManufacturingRecordId == null && response.MaterialBatchId != null
            && await db.MaterialBatches.AnyAsync(b => b.Id == response.MaterialBatchId && b.Material.Kind ==
                (kind == TemplateRepairTestKind.RawMaterial ? MaterialKind.Raw : MaterialKind.Package));
    }

    public async Task<Result<TestCorrectionStateDto>> History(Guid responseId, TemplateRepairTestKind kind)
    {
        var grants = await CurrentPermissionSet.Load(db, actor.UserId);
        if (!grants.Contains(HistoryKey(kind))) return Denied();
        var response = await db.Responses.AsNoTracking().SingleOrDefaultAsync(r => r.Id == responseId);
        if (response == null) return Error.NotFound("TestCorrection.Response", "The worksheet was not found.");
        if (!await Matches(response, kind)) return Denied();
        var cycleIds = Cycle(response).Select(r => r.Id);
        var history = await (from h in db.TestCorrectionReturns.AsNoTracking()
            join u in db.Users.IgnoreQueryFilters() on h.ReturnedById equals u.Id
            where cycleIds.Contains(h.SourceResponseId)
            orderby h.ReturnedAt descending
            select new TestCorrectionHistoryItemDto(h.Id, h.SourceResponseId, h.CorrectionResponseId, h.ReturnedById,
                u.FirstName + " " + u.LastName, h.ReturnedAt, h.Reason)).ToListAsync();
        var problem = await Problem(response, kind);
        return new TestCorrectionStateDto(responseId, grants.Contains(ReturnKey(kind)) && problem == null, problem, history);
    }

    private async Task<string> Problem(Response response, TemplateRepairTestKind kind)
    {
        if (response.CheckedAt != null || response.Approved || response.Rejected
            || await db.ResponseApprovals.AnyAsync(a => a.ResponseId == response.Id))
            return "A certificate or approval already exists. Use the formal rework workflow instead.";
        if (await db.ResponseReworkTasks.AnyAsync(t => t.NewResponseId == response.Id || t.SourceResponseId == response.Id))
            return "This worksheet belongs to a formal QA rework cycle.";
        var latest = await Cycle(response).OrderByDescending(r => r.ReworkSequence).ThenByDescending(r => r.CreatedAt).Select(r => r.Id).FirstAsync();
        if (latest != response.Id || await db.TestCorrectionReturns.AnyAsync(h => h.SourceResponseId == response.Id))
            return "This worksheet has already been returned. Reload the current worksheet.";
        if (kind == TemplateRepairTestKind.Product)
        {
            var tests = await db.AnalyticalTestRequests.Where(a => a.BatchManufacturingRecordId == response.BatchManufacturingRecordId
                && a.ProductionActivityStepId == response.ProductionActivityStepId).ToListAsync();
            var bmr = await db.BatchManufacturingRecords.SingleOrDefaultAsync(b => b.Id == response.BatchManufacturingRecordId);
            if (tests.Count != 1 || tests[0].Status != AnalyticalTestStatus.TestTaken || tests[0].ReleasedAt != null
                || bmr?.Status != BatchManufacturingStatus.TestTaken)
                return "Only submitted results awaiting their first certificate can be returned.";
        }
        else
        {
            var batch = await db.MaterialBatches.IgnoreAutoIncludes().SingleAsync(b => b.Id == response.MaterialBatchId);
            var retest = await APP.Services.Materials.MaterialRetestTesting.ActiveAsync(db, batch.Id);
            if (retest != null)
            {
                if (retest.Status != MaterialRetestStatus.TestTaken || retest.CoaId != null
                    || !await db.MaterialSamplings.AnyAsync(s => s.Id == response.MaterialSamplingId && s.MaterialRetestRequestId == retest.Id))
                    return "Only the current submitted retest can be returned before its certificate is generated.";
            }
            else if (batch.Status != BatchStatus.TestTaken)
                return "Only submitted results awaiting their first certificate can be returned.";
        }
        return null;
    }

    public async Task<Result<Guid>> Return(Guid responseId, TemplateRepairTestKind kind, ReturnTestForCorrectionRequest request)
    {
        if (!(await CurrentPermissionSet.Load(db, actor.UserId)).Contains(ReturnKey(kind))) return Denied();
        var reason = request.Reason?.Trim() ?? "";
        if (reason.Length is < 1 or > 2000) return Error.Validation("TestCorrection.Reason", "Enter a reason of 1–2000 characters.");
        await using var tx = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        var source = await db.Responses.Include(r => r.FormResponses).SingleOrDefaultAsync(r => r.Id == responseId);
        if (source == null) return Error.NotFound("TestCorrection.Response", "The worksheet was not found.");
        if (!await Matches(source, kind)) return Denied();
        await TestCorrectionGuard.Lock(db, source.MaterialBatchId, source.BatchManufacturingRecordId, source.ProductionActivityStepId);
        var problem = await Problem(source, kind);
        if (problem != null) return Blocked(problem);
        var next = new Response {
            Id = Guid.NewGuid(), FormId = source.FormId, FormRevisionId = source.FormRevisionId,
            MaterialBatchId = source.MaterialBatchId, MaterialSamplingId = source.MaterialSamplingId,
            BatchManufacturingRecordId = source.BatchManufacturingRecordId, ProductionActivityStepId = source.ProductionActivityStepId,
            ReworkSequence = source.ReworkSequence + 1,
            FormResponses = source.FormResponses.GroupBy(a => a.FormFieldId).Select(g => g.OrderByDescending(a => a.CreatedAt).First()).Select(a => new FormResponse {
                Id = Guid.NewGuid(), FormFieldId = a.FormFieldId, FormFieldRevisionId = a.FormFieldRevisionId,
                Value = a.Value, Complies = a.Complies, CreatedById = a.CreatedById, CreatedAt = a.CreatedAt }).ToList()
        };
        string previous;
        if (kind == TemplateRepairTestKind.Product)
        {
            var test = await db.AnalyticalTestRequests.SingleAsync(a => a.BatchManufacturingRecordId == source.BatchManufacturingRecordId
                && a.ProductionActivityStepId == source.ProductionActivityStepId);
            var bmr = await db.BatchManufacturingRecords.SingleAsync(b => b.Id == source.BatchManufacturingRecordId);
            previous = JsonSerializer.Serialize(new { test.Status, test.TestedAt, test.TestedById, BatchStatus = bmr.Status });
            test.Status = AnalyticalTestStatus.Testing; test.TestedAt = null; test.TestedById = null;
            bmr.Status = BatchManufacturingStatus.Testing;
        }
        else
        {
            var batch = await db.MaterialBatches.IgnoreAutoIncludes().SingleAsync(b => b.Id == source.MaterialBatchId);
            var retest = await APP.Services.Materials.MaterialRetestTesting.ActiveAsync(db, batch.Id);
            previous = JsonSerializer.Serialize(new { batch.Status, RetestId = retest?.Id, RetestStatus = retest?.Status });
            if (retest != null) await APP.Services.Materials.MaterialRetestTesting.AdvanceAsync(db, batch.Id, MaterialRetestStatus.Testing, actor.UserId, reason);
            else batch.Status = BatchStatus.Testing;
        }
        db.Responses.Add(next);
        // Carry repaired definitions into the new draft, but never copy a prior finalization or calculated execution.
        foreach (var pin in await LegacyFormulaConfigurationReader.Latest(db, [source.Id]))
            db.LegacyFormulaConfigurationPins.Add(new LegacyFormulaConfigurationPin {
                Id = Guid.NewGuid(), ResponseId = next.Id, FormFieldId = pin.FormFieldId, Sequence = 1,
                RepairId = pin.RepairId, Definition = pin.Definition, DefinitionHash = pin.DefinitionHash,
                RequiresRecalculation = true, CapturedAt = DateTime.UtcNow });
        db.TestCorrectionReturns.Add(new TestCorrectionReturn {
            Id = Guid.NewGuid(), SourceResponseId = source.Id, CorrectionResponseId = next.Id,
            ReturnedById = actor.UserId.Value, ReturnedAt = DateTime.UtcNow, Reason = reason, PreviousState = previous });
        if (!(await CurrentPermissionSet.Load(db, actor.UserId)).Contains(ReturnKey(kind))) { db.ChangeTracker.Clear(); return Denied(); }
        await db.SaveChangesAsync();
        if (tx != null) await tx.CommitAsync();
        return next.Id;
    }
}
