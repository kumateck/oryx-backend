using System.Data;
using APP.IRepository;
using APP.Services.QcWorksheets;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.QcWorksheets;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Materials;

public interface IMaterialRetestService
{
    Task<Result<Guid>> RequestAsync(Guid batchId, string reason, Guid? actorId, bool scheduled, DateTime? asOf = null);
    Task<Result<MaterialRetestDetailDto>> GetAsync(Guid id);
    Task<Result<List<MaterialRetestDetailDto>>> ListAsync(Guid? batchId);
    Task<Result> DecideAsync(Guid id, Guid actorId, List<Guid> roleIds, MaterialRetestDecision decision, bool approve);
    Task<Result> RecordSampleAsync(Guid id, RecordMaterialRetestSample sample, Guid actorId);
    Task<Result> AcknowledgeAsync(Guid id, Guid actorId);
    Task<Result> LinkTestRequestAsync(Guid id, Guid testRequestId, Guid actorId);
    Task<Result<TestRequestDetailDto>> CreateTestingAsync(Guid id, CreateTestRequestRequest body, Guid actorId);
    Task<int> ScanDueAsync(DateTime asOf, CancellationToken cancellationToken);
}

public sealed class MaterialRetestDetailDto
{
    public Guid Id { get; set; }
    public Guid MaterialBatchId { get; set; }
    public string BatchNumber { get; set; }
    public DOMAIN.Entities.Materials.MaterialKind MaterialKind { get; set; }
    public MaterialRetestOrigin Origin { get; set; }
    public MaterialRetestStatus Status { get; set; }
    public bool Approved { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public DateTime? OriginalExpiryDate { get; set; }
    public DateTime? ProposedExpiryDate { get; set; }
    public string Reason { get; set; }
    public DateTime? SampleTakenAt { get; set; }
    public decimal? SampleQuantity { get; set; }
    public int? SampledContainers { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public Guid? TestRequestId { get; set; }
    public Guid? CoaId { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<MaterialRetestEventDto> Events { get; set; } = [];
}

public sealed record MaterialRetestEventDto(string Action, string Detail, Guid? ActorId, DateTime OccurredAt);

public sealed class MaterialRetestService(
    ApplicationDbContext context,
    IApprovalRepository approvals,
    IQcSignatureService signatures, ITestRequestRepository testing = null) : IMaterialRetestService
{
    public async Task<Result<Guid>> RequestAsync(Guid batchId, string reason, Guid? actorId, bool scheduled, DateTime? asOf = null)
    {
        var today = (asOf ?? DateTime.UtcNow).Date;
        var batch = await context.MaterialBatches.SingleOrDefaultAsync(item => item.Id == batchId);
        if (batch is null) return Error.NotFound("MaterialRetest.BatchMissing", "Material batch was not found.");
        if (batch.Status is not (BatchStatus.Available or BatchStatus.Approved or BatchStatus.Rejected))
            return Error.Conflict("MaterialRetest.BatchStatus", "Batch status does not allow a new retest request.");
        if (scheduled && (batch.RetestDate is null || batch.RetestDate.Value.Year <= 1900 || batch.RetestDate.Value.Date > today))
            return Error.Validation("MaterialRetest.NotDue", "The scheduled retest date is not due.");
        if (!scheduled && batch.RetestDate is not null)
            return Error.Validation("MaterialRetest.Scheduled", "This batch has a scheduled retest date; the due-date scan creates its request.");
        if (!scheduled && (actorId is null || string.IsNullOrWhiteSpace(reason)))
            return Error.Validation("MaterialRetest.Reason", "A manual retest requires an actor and reason.");

        var sourceDate = (batch.RetestDate ?? batch.ExpiryDate ?? batch.DateReceived).Date;
        var sourceKey = $"material-retest:{batch.Id}:{(scheduled ? "scheduled" : "manual")}:{sourceDate:yyyyMMdd}";
        if (await context.MaterialRetestRequests.AnyAsync(item => item.SourceKey == sourceKey))
            return Error.Conflict("MaterialRetest.Duplicate", "A retest request already exists for this batch cycle.");

        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync() : null;
        var now = DateTime.UtcNow;
        var request = new MaterialRetestRequest
        {
            Id = Guid.NewGuid(), MaterialBatchId = batchId, SourceKey = sourceKey,
            Origin = scheduled ? MaterialRetestOrigin.Scheduled : MaterialRetestOrigin.Manual,
            Status = MaterialRetestStatus.PendingApproval, PreviousBatchStatus = batch.Status,
            ScheduledFor = scheduled ? batch.RetestDate : null,
            OriginalExpiryDate = batch.ExpiryDate,
            Reason = scheduled ? $"Scheduled retest due {sourceDate:yyyy-MM-dd}" : reason.Trim(),
            CreatedAt = now, CreatedById = actorId
        };
        batch.Status = BatchStatus.Frozen;
        batch.UpdatedAt = now;
        batch.LastUpdatedById = actorId;
        context.MaterialRetestRequests.Add(request);
        AddEvent(request.Id, "Requested", request.Reason, actorId);
        await context.SaveChangesAsync();
        await approvals.CreateInitialApprovalsAsync(QcWorksheetModelTypes.MaterialRetest, request.Id, actorId);
        if (request.Approved)
            AddEvent(request.Id, "AutoApproved", "No Retest approval document is configured; central approval policy auto-approved the request.", null);
        await context.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        return request.Id;
    }

    public async Task<Result<MaterialRetestDetailDto>> GetAsync(Guid id)
    {
        var item = await context.MaterialRetestRequests.AsNoTracking()
            .Include(request => request.MaterialBatch).ThenInclude(batch => batch.Material)
            .Include(request => request.Events)
            .SingleOrDefaultAsync(request => request.Id == id);
        return item is null
            ? Error.NotFound("MaterialRetest.NotFound", "Retest request was not found.")
            : ToDto(item);
    }

    public async Task<Result<List<MaterialRetestDetailDto>>> ListAsync(Guid? batchId)
    {
        var query = context.MaterialRetestRequests.AsNoTracking()
            .Include(item => item.MaterialBatch).ThenInclude(batch => batch.Material)
            .Include(item => item.Events).AsQueryable();
        if (batchId.HasValue) query = query.Where(item => item.MaterialBatchId == batchId.Value);
        var rows = await query.OrderByDescending(item => item.CreatedAt).ToListAsync();
        return rows.Select(ToDto).ToList();
    }

    public async Task<Result> DecideAsync(Guid id, Guid actorId, List<Guid> roleIds, MaterialRetestDecision decision, bool approve)
    {
        var request = await context.MaterialRetestRequests.SingleOrDefaultAsync(item => item.Id == id);
        if (request is null) return Error.NotFound("MaterialRetest.NotFound", "Retest request was not found.");
        if (request.Status != MaterialRetestStatus.PendingApproval)
            return Error.Conflict("MaterialRetest.NotPending", "This request is no longer pending approval.");
        if (string.IsNullOrWhiteSpace(decision?.Comments))
            return Error.Validation("MaterialRetest.DecisionReason", "A decision comment is required.");
        var result = approve
            ? await signatures.SignAndApproveAsync(QcWorksheetModelTypes.MaterialRetest, id, actorId, roleIds, decision.Password, decision.Comments.Trim())
            : await signatures.SignAndRejectAsync(QcWorksheetModelTypes.MaterialRetest, id, actorId, roleIds, decision.Password, decision.Comments.Trim());
        if (result.IsFailure) return result;
        AddEvent(id, approve ? "ApprovalSigned" : "Rejected", decision.Comments.Trim(), actorId);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> RecordSampleAsync(Guid id, RecordMaterialRetestSample sample, Guid actorId)
    {
        var request = await context.MaterialRetestRequests
            .Include(item => item.MaterialBatch).ThenInclude(item => item.ReservedQuantities)
            .SingleOrDefaultAsync(item => item.Id == id);
        if (request is null) return Error.NotFound("MaterialRetest.NotFound", "Retest request was not found.");
        if (request.Status != MaterialRetestStatus.Authorized || !request.Approved)
            return Error.Conflict("MaterialRetest.NotAuthorized", "Approval is required before sampling.");
        if (sample is null || sample.SampleQuantity <= 0 || sample.SampledContainers <= 0
            || sample.SampleQuantity > request.MaterialBatch.RemainingQuantity)
            return Error.Validation("MaterialRetest.Sample", "Sample quantity and container count must be positive and within remaining stock.");
        if (sample.ProposedExpiryDate.Date <= DateTime.UtcNow.Date
            || (request.OriginalExpiryDate.HasValue && sample.ProposedExpiryDate.Date <= request.OriginalExpiryDate.Value.Date))
            return Error.Validation("MaterialRetest.ProposedExpiry", "Proposed expiry must be later than today and the original expiry.");
        request.SampleQuantity = sample.SampleQuantity;
        request.SampledContainers = sample.SampledContainers;
        request.ProposedExpiryDate = sample.ProposedExpiryDate.Date;
        request.SampleTakenAt = DateTime.UtcNow;
        request.SampleTakenById = actorId;
        request.Status = MaterialRetestStatus.SampleTaken;
        AddEvent(id, "SampleTaken", $"Quantity {sample.SampleQuantity}; containers {sample.SampledContainers}; proposed expiry {sample.ProposedExpiryDate:yyyy-MM-dd}", actorId);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> AcknowledgeAsync(Guid id, Guid actorId)
    {
        var request = await context.MaterialRetestRequests.SingleOrDefaultAsync(item => item.Id == id);
        if (request is null) return Error.NotFound("MaterialRetest.NotFound", "Retest request was not found.");
        if (request.Status != MaterialRetestStatus.SampleTaken)
            return Error.Conflict("MaterialRetest.SampleRequired", "The sample must be recorded before acknowledgement.");
        request.Status = MaterialRetestStatus.Acknowledged;
        request.AcknowledgedAt = DateTime.UtcNow;
        request.AcknowledgedById = actorId;
        AddEvent(id, "Acknowledged", "QC acknowledged the retest sample.", actorId);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> LinkTestRequestAsync(Guid id, Guid testRequestId, Guid actorId)
    {
        var request = await context.MaterialRetestRequests.Include(item => item.MaterialBatch).ThenInclude(item => item.Material)
            .SingleOrDefaultAsync(item => item.Id == id);
        if (request is null) return Error.NotFound("MaterialRetest.NotFound", "Retest request was not found.");
        if (request.Status != MaterialRetestStatus.Acknowledged)
            return Error.Conflict("MaterialRetest.NotAcknowledged", "The sample must be acknowledged before testing is linked.");
        var test = await context.QcTestRequests.Include(item => item.Subjects)
            .SingleOrDefaultAsync(item => item.Id == testRequestId);
        var expectedType = request.MaterialBatch.Material.Kind == DOMAIN.Entities.Materials.MaterialKind.Raw
            ? TestRequestType.RawMaterial : TestRequestType.PackagingMaterial;
        if (test is null || test.Type != expectedType || test.Subjects.Count != 1
            || test.Subjects[0].MaterialBatchId != request.MaterialBatchId)
            return Error.Validation("MaterialRetest.TestRequestMismatch", "The QC test request must test this batch and material kind only.");
        if (await context.MaterialRetestRequests.AnyAsync(item => item.TestRequestId == testRequestId))
            return Error.Conflict("MaterialRetest.TestRequestUsed", "This QC test request is already linked to a retest.");
        request.TestRequestId = testRequestId;
        request.Status = MaterialRetestStatus.Testing;
        AddEvent(id, "TestingLinked", $"QC test request {testRequestId} linked.", actorId);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<TestRequestDetailDto>> CreateTestingAsync(Guid id, CreateTestRequestRequest body, Guid actorId)
    {
        if (testing is null) return Error.Conflict("MaterialRetest.TestingUnavailable", "Testing service is unavailable.");
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        var retest = await context.MaterialRetestRequests.Include(item => item.MaterialBatch).ThenInclude(item => item.Material)
            .SingleOrDefaultAsync(item => item.Id == id);
        if (retest is null) return Error.NotFound("MaterialRetest.NotFound", "Retest request was not found.");
        if (retest.Status != MaterialRetestStatus.Acknowledged || retest.TestRequestId.HasValue)
            return Error.Conflict("MaterialRetest.NotAcknowledged", "Only an acknowledged, unlinked retest can create testing.");
        var kind = retest.MaterialBatch.Material.Kind == DOMAIN.Entities.Materials.MaterialKind.Raw
            ? TestRequestType.RawMaterial : TestRequestType.PackagingMaterial;
        if (body is null || body.Type != kind || body.ScheduleOrigin != TestRequestScheduleOrigin.Unscheduled ||
            body.Subjects is not { Count: 1 } || body.Subjects[0].MaterialBatchId != retest.MaterialBatchId)
            return Error.Validation("MaterialRetest.TestRequestMismatch", "Create an unscheduled test for this batch and material kind only.");
        var result = await testing.CreateTestRequest(body, actorId);
        if (result.IsFailure) return result.Errors;
        var linked = await LinkTestRequestAsync(id, result.Value.Id, actorId);
        if (linked.IsFailure) return linked.Errors;
        if (transaction is not null) await transaction.CommitAsync();
        return result.Value;
    }

    public async Task<int> ScanDueAsync(DateTime asOf, CancellationToken cancellationToken)
    {
        var due = await context.MaterialBatches.AsNoTracking()
            .Where(item => item.RetestDate.HasValue && item.RetestDate.Value.Date <= asOf.Date
                && (item.Status == BatchStatus.Available || item.Status == BatchStatus.Approved || item.Status == BatchStatus.Rejected)
                && !context.MaterialRetestRequests.Any(request => request.MaterialBatchId == item.Id
                    && request.Origin == MaterialRetestOrigin.Scheduled && request.ScheduledFor == item.RetestDate))
            .Select(item => item.Id).ToListAsync(cancellationToken);
        var created = 0;
        foreach (var batchId in due)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await RequestAsync(batchId, "Scheduled retest due", null, true, asOf);
            if (result.IsSuccess) created++;
        }
        return created;
    }

    private void AddEvent(Guid requestId, string action, string detail, Guid? actorId) =>
        context.MaterialRetestEvents.Add(new MaterialRetestEvent
        {
            Id = Guid.NewGuid(), MaterialRetestRequestId = requestId,
            Action = action, Detail = detail, ActorId = actorId, OccurredAt = DateTime.UtcNow
        });

    private static MaterialRetestDetailDto ToDto(MaterialRetestRequest item) => new()
    {
        Id = item.Id, MaterialBatchId = item.MaterialBatchId, BatchNumber = item.MaterialBatch?.BatchNumber,
        MaterialKind = item.MaterialBatch.Material.Kind,
        Origin = item.Origin, Status = item.Status, Approved = item.Approved,
        ScheduledFor = item.ScheduledFor, OriginalExpiryDate = item.OriginalExpiryDate,
        ProposedExpiryDate = item.ProposedExpiryDate, Reason = item.Reason,
        SampleTakenAt = item.SampleTakenAt, SampleQuantity = item.SampleQuantity,
        SampledContainers = item.SampledContainers, AcknowledgedAt = item.AcknowledgedAt,
        TestRequestId = item.TestRequestId, CoaId = item.CoaId, CompletedAt = item.CompletedAt,
        Events = item.Events.OrderBy(entry => entry.OccurredAt)
            .Select(entry => new MaterialRetestEventDto(entry.Action, entry.Detail, entry.ActorId, entry.OccurredAt)).ToList()
    };
}
