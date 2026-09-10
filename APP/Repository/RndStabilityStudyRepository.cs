using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.RndStabilityStudies;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class RndStabilityStudyRepository(ApplicationDbContext context, IMapper mapper)
    : IRndStabilityStudyRepository
{
    public async Task<Result<Guid>> CreateChamber(CreateRndStabilityChamberRequest request, Guid userId)
    {
        var chamber = new RndStabilityChamber
        {
            Code = request.Code,
            Name = request.Name,
            ConditionType = request.ConditionType,
            TargetTemperature = request.TargetTemperature,
            TargetHumidity = request.TargetHumidity,
            CalibrationDueDate = request.CalibrationDueDate,
            LastCalibratedAt = request.LastCalibratedAt,
            CreatedById = userId,
        };

        await context.RndStabilityChambers.AddAsync(chamber);
        await context.SaveChangesAsync();
        return chamber.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<RndStabilityChamberDto>>>> GetChambers(int page, int pageSize)
    {
        var query = context.RndStabilityChambers.OrderBy(c => c.Code).AsQueryable();
        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<RndStabilityChamberDto>
        );
    }

    public async Task<Result<Guid>> CreateStudy(
        Guid rndProjectId,
        CreateRndStabilityStudyRequest request,
        Guid userId
    )
    {
        var project = await context.RndProjects.FirstOrDefaultAsync(p => p.Id == rndProjectId);
        if (project is null)
            return Error.NotFound("RndStabilityStudy.ProjectNotFound", "R&D project not found.");

        var approvalGate = project.EnsureApprovedForProgression("R&D project");
        if (approvalGate.IsFailure)
            return approvalGate.Error;

        var trialBatch = await context.RndTrialBatches.FirstOrDefaultAsync(b => b.Id == request.RndTrialBatchId);
        if (trialBatch is null)
            return Error.NotFound("RndStabilityStudy.TrialBatchNotFound", "Trial batch not found.");

        if (trialBatch.RndProjectId != rndProjectId)
            return Error.Validation(
                "RndStabilityStudy.TrialBatchMismatch",
                "This trial batch does not belong to the specified R&D project."
            );

        if (!await context.RndStabilityChambers.AnyAsync(c => c.Id == request.RndStabilityChamberId))
            return Error.NotFound("RndStabilityStudy.ChamberNotFound", "Stability chamber not found.");

        if (
            request.ProtocolFormId.HasValue
            && !await context.Forms.AnyAsync(f => f.Id == request.ProtocolFormId.Value)
        )
            return Error.NotFound("RndStabilityStudy.FormNotFound", "Protocol form not found.");

        if (request.PullPointTimePointsMonths.Count == 0)
            return Error.Validation(
                "RndStabilityStudy.PullPoints",
                "At least one pull-point time point is required."
            );

        var study = new RndStabilityStudy
        {
            RndProjectId = rndProjectId,
            RndTrialBatchId = request.RndTrialBatchId,
            RndStabilityChamberId = request.RndStabilityChamberId,
            ProtocolFormId = request.ProtocolFormId,
            StartDate = request.StartDate,
            Status = RndStabilityStudyStatus.Active,
            PullPoints = request
                .PullPointTimePointsMonths.Distinct()
                .OrderBy(months => months)
                .Select(months => new RndStabilityPullPoint
                {
                    TimePointMonths = months,
                    DueDate = request.StartDate.AddMonths(months),
                    Status = RndStabilityPullPointStatus.Scheduled,
                })
                .ToList(),
            CreatedById = userId,
        };

        await context.RndStabilityStudies.AddAsync(study);
        await context.SaveChangesAsync();
        return study.Id;
    }

    public async Task<Result> UpdateStudyStatus(Guid id, UpdateRndStabilityStudyStatusRequest request, Guid userId)
    {
        var study = await context.RndStabilityStudies.FirstOrDefaultAsync(s => s.Id == id);
        if (study is null)
            return Error.NotFound("RndStabilityStudy.NotFound", "Stability study not found.");

        if (study.Status != RndStabilityStudyStatus.Active)
            return Error.Validation(
                "RndStabilityStudy.InvalidStatus",
                $"This study is already {study.Status} and cannot change status further."
            );

        study.Status = request.Status;
        study.LastUpdatedById = userId;

        context.RndStabilityStudies.Update(study);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> RecordPullPointResult(
        Guid pullPointId,
        RecordPullPointResultRequest request,
        Guid userId
    )
    {
        var pullPoint = await context.RndStabilityPullPoints.FirstOrDefaultAsync(p => p.Id == pullPointId);
        if (pullPoint is null)
            return Error.NotFound("RndStabilityStudy.PullPointNotFound", "Pull point not found.");

        if (pullPoint.Status == RndStabilityPullPointStatus.Reported)
            return Error.Validation(
                "RndStabilityStudy.PullPointAlreadyReported",
                "This pull point has already been reported."
            );

        pullPoint.ResultsSummary = request.ResultsSummary;
        pullPoint.PulledAt ??= DateTime.UtcNow;
        pullPoint.PulledById ??= userId;
        pullPoint.Status = RndStabilityPullPointStatus.Reported;
        pullPoint.LastUpdatedById = userId;

        context.RndStabilityPullPoints.Update(pullPoint);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    private IQueryable<RndStabilityStudy> StudyDetailQuery() =>
        context
            .RndStabilityStudies.AsSplitQuery()
            .Include(s => s.RndStabilityChamber)
            .Include(s => s.PullPoints)
                .ThenInclude(p => p.PulledBy);

    private RndStabilityStudyDto MapStudy(RndStabilityStudy study) => new()
    {
        Id = study.Id,
        CreatedAt = study.CreatedAt,
        RndProjectId = study.RndProjectId,
        RndTrialBatchId = study.RndTrialBatchId,
        RndStabilityChamber = mapper.Map<CollectionItemDto>(study.RndStabilityChamber),
        ProtocolFormId = study.ProtocolFormId,
        StartDate = study.StartDate,
        Status = study.Status,
        PullPoints = study
            .PullPoints.OrderBy(p => p.TimePointMonths)
            .Select(p => new RndStabilityPullPointDto
            {
                Id = p.Id,
                CreatedAt = p.CreatedAt,
                TimePointMonths = p.TimePointMonths,
                DueDate = p.DueDate,
                PulledAt = p.PulledAt,
                PulledBy = mapper.Map<UserDto>(p.PulledBy),
                Status = p.Status,
                ResultsSummary = p.ResultsSummary,
            })
            .ToList(),
    };

    public async Task<Result<RndStabilityStudyDto>> GetStudy(Guid id)
    {
        var study = await StudyDetailQuery().FirstOrDefaultAsync(s => s.Id == id);
        return study is null
            ? Error.NotFound("RndStabilityStudy.NotFound", "Stability study not found.")
            : MapStudy(study);
    }

    public async Task<Result<Paginateable<IEnumerable<RndStabilityStudyDto>>>> GetStudiesForProject(
        Guid rndProjectId,
        int page,
        int pageSize
    )
    {
        var query = StudyDetailQuery()
            .Where(s => s.RndProjectId == rndProjectId)
            .OrderByDescending(s => s.StartDate)
            .AsQueryable();

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, MapStudy);
    }
}
