using System.Data;
using DOMAIN.Entities.Forms;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.QcWorksheets;

public interface IResponseReworkService
{
    Task<Result<List<ResponseReworkTaskDto>>> ListAsync();
    Task<Result<ResponseReworkDetailDto>> GetAsync(Guid id);
    Task<Result<Guid>> StartAsync(Guid id, Guid actorId);
}

public sealed record ResponseReworkTaskDto(Guid Id, Guid SourceResponseId, Guid RootResponseId,
    Guid? NewResponseId, Guid? MaterialBatchId, Guid? BatchManufacturingRecordId,
    string BatchNumber, string Reason, ResponseReworkStatus Status, DateTime RequestedAt);
public sealed record ResponseReworkDetailDto(ResponseReworkTaskDto Task, List<ResponseReworkTaskDto> History,
    Guid? ProductionActivityStepId, int? ProductStage);

public sealed class ResponseReworkService(ApplicationDbContext context) : IResponseReworkService
{
    public async Task<Result<List<ResponseReworkTaskDto>>> ListAsync()
    {
        var rows = await Query().OrderByDescending(item => item.RequestedAt).ToListAsync();
        return rows.Select(ToDto).ToList();
    }

    public async Task<Result<ResponseReworkDetailDto>> GetAsync(Guid id)
    {
        var task = await Query().SingleOrDefaultAsync(item => item.Id == id);
        if (task is null) return Error.NotFound("ResponseRework.NotFound", "Rework task was not found.");
        var history = await Query().Where(item => item.RootResponseId == task.RootResponseId)
            .OrderBy(item => item.RequestedAt).ToListAsync();
        var atr = task.BatchManufacturingRecordId.HasValue && task.SourceResponse.ProductionActivityStepId.HasValue
            ? await context.AnalyticalTestRequests.AsNoTracking()
                .Where(item => item.BatchManufacturingRecordId == task.BatchManufacturingRecordId
                    && item.ProductionActivityStepId == task.SourceResponse.ProductionActivityStepId)
                .Select(item => new { item.ProductionActivityStepId, item.Stage })
                .FirstOrDefaultAsync() : null;
        return new ResponseReworkDetailDto(ToDto(task), history.Select(ToDto).ToList(),
            atr?.ProductionActivityStepId, atr is null ? null : (int)atr.Stage);
    }

    public async Task<Result<Guid>> StartAsync(Guid id, Guid actorId)
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        var task = await context.ResponseReworkTasks
            .Include(item => item.SourceResponse)
            .SingleOrDefaultAsync(item => item.Id == id);
        if (task is null) return Error.NotFound("ResponseRework.NotFound", "Rework task was not found.");
        if (task.Status != ResponseReworkStatus.AwaitingQc || task.NewResponseId.HasValue)
            return Error.Conflict("ResponseRework.AlreadyStarted", "This task has already started.");
        var source = task.SourceResponse;
        if (!source.CheckedAt.HasValue || !source.Rejected
            || source.RejectionOutcome != ResponseRejectionOutcome.Rework)
            return Error.Conflict("ResponseRework.Source", "The source Response was not rejected for rework.");

        var now = DateTime.UtcNow;
        var next = new Response
        {
            Id = Guid.NewGuid(), FormId = source.FormId, FormRevisionId = source.FormRevisionId,
            MaterialBatchId = source.MaterialBatchId, MaterialSamplingId = source.MaterialSamplingId,
            BatchManufacturingRecordId = source.BatchManufacturingRecordId,
            ProductionActivityStepId = source.ProductionActivityStepId,
            ReworkSequence = source.ReworkSequence + 1,
            CreatedAt = now, CreatedById = actorId,
            FormResponses = []
        };
        context.Responses.Add(next);
        task.NewResponseId = next.Id;
        task.Status = ResponseReworkStatus.Testing;
        task.StartedAt = now;
        task.StartedById = actorId;
        await context.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        return next.Id;
    }

    private IQueryable<ResponseReworkTask> Query() => context.ResponseReworkTasks.AsNoTracking()
        .Include(item => item.SourceResponse).ThenInclude(item => item.MaterialBatch)
        .Include(item => item.SourceResponse).ThenInclude(item => item.BatchManufacturingRecord);

    private static ResponseReworkTaskDto ToDto(ResponseReworkTask item) => new(
        item.Id, item.SourceResponseId, item.RootResponseId, item.NewResponseId,
        item.MaterialBatchId, item.BatchManufacturingRecordId,
        item.SourceResponse.MaterialBatch?.BatchNumber
            ?? item.SourceResponse.BatchManufacturingRecord?.BatchNumber,
        item.Reason, item.Status, item.RequestedAt);
}
