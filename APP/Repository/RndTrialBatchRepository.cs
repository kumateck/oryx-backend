using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.RndTrialBatches;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class RndTrialBatchRepository(ApplicationDbContext context, IMapper mapper) : IRndTrialBatchRepository
{
    public async Task<Result<Guid>> CreateTrialBatch(
        Guid rndProjectId,
        CreateRndTrialBatchRequest request,
        Guid userId
    )
    {
        var project = await context.RndProjects.FirstOrDefaultAsync(p => p.Id == rndProjectId);
        if (project is null)
            return Error.NotFound("RndTrialBatch.ProjectNotFound", "R&D project not found.");

        var approvalGate = project.EnsureApprovedForProgression("R&D project");
        if (approvalGate.IsFailure)
            return approvalGate.Error;

        var formulation = await context.RndFormulations.FirstOrDefaultAsync(f =>
            f.Id == request.RndFormulationId
        );
        if (formulation is null)
            return Error.NotFound("RndTrialBatch.FormulationNotFound", "Formulation not found.");

        if (formulation.RndProjectId != rndProjectId)
            return Error.Validation(
                "RndTrialBatch.FormulationMismatch",
                "This formulation does not belong to the specified R&D project."
            );

        if (request.ProtocolFormId.HasValue && !await context.Forms.AnyAsync(f => f.Id == request.ProtocolFormId))
            return Error.NotFound("RndTrialBatch.FormNotFound", "Protocol form not found.");

        var year = DateTime.UtcNow.Year;
        var sequence = await context.RndTrialBatches.CountAsync(b => b.CreatedAt.Year == year) + 1;

        var trialBatch = mapper.Map<RndTrialBatch>(request);
        trialBatch.RndProjectId = rndProjectId;
        trialBatch.BatchCode = $"TRB-{year}-{sequence:D4}";
        trialBatch.Status = RndTrialBatchStatus.Planned;
        trialBatch.CreatedById = userId;

        await context.RndTrialBatches.AddAsync(trialBatch);
        await context.SaveChangesAsync();
        return trialBatch.Id;
    }

    public async Task<Result> UpdateStatus(Guid id, UpdateRndTrialBatchStatusRequest request, Guid userId)
    {
        var trialBatch = await context
            .RndTrialBatches.Include(b => b.RndProject)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (trialBatch is null)
            return Error.NotFound("RndTrialBatch.NotFound", "Trial batch not found.");

        var approvalGate = trialBatch.RndProject.EnsureApprovedForProgression("R&D project");
        if (approvalGate.IsFailure)
            return approvalGate.Error;

        if (trialBatch.Status is RndTrialBatchStatus.Completed or RndTrialBatchStatus.Aborted)
            return Error.Validation(
                "RndTrialBatch.InvalidStatus",
                $"This trial batch is already {trialBatch.Status} and cannot change status further."
            );

        trialBatch.Status = request.Status;
        trialBatch.Observations = request.Observations;
        trialBatch.LastUpdatedById = userId;

        context.RndTrialBatches.Update(trialBatch);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    private IQueryable<RndTrialBatch> TrialBatchDetailQuery() =>
        context
            .RndTrialBatches.AsSplitQuery()
            .Include(b => b.BatchSizeUoM)
            .Include(b => b.PerformedBy);

    public async Task<Result<RndTrialBatchDto>> GetTrialBatch(Guid id)
    {
        var trialBatch = await TrialBatchDetailQuery().FirstOrDefaultAsync(b => b.Id == id);
        return trialBatch is null
            ? Error.NotFound("RndTrialBatch.NotFound", "Trial batch not found.")
            : mapper.Map<RndTrialBatchDto>(trialBatch);
    }

    public async Task<Result<Paginateable<IEnumerable<RndTrialBatchDto>>>> GetTrialBatchesForProject(
        Guid rndProjectId,
        int page,
        int pageSize
    )
    {
        var query = TrialBatchDetailQuery()
            .Where(b => b.RndProjectId == rndProjectId)
            .OrderByDescending(b => b.CreatedAt)
            .AsQueryable();

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<RndTrialBatchDto>);
    }
}
