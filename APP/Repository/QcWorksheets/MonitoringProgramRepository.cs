using APP.IRepository;
using AutoMapper;
using DOMAIN.Entities.QcWorksheets;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository.QcWorksheets;

/// <summary>
/// The per-point testing schedule.
/// <para>
/// Plain CRUD plus Pause/Resume, and deliberately no approval lifecycle: a monitoring program is
/// operational configuration, not a controlled document. Nobody signs for how often a tap is
/// sampled the way they sign for the Specification it is sampled against.
/// </para>
/// <para>
/// <b>Hard version pinning</b> applies here as it does everywhere else in this module. A program
/// names one exact Specification version row, <see cref="MonitoringProgram.SpecificationVersion"/>
/// records which, and nothing resolves forward through <c>SupersedesId</c>. Repointing a program
/// at a newer version is an ordinary edit somebody makes on purpose.
/// </para>
/// </summary>
public class MonitoringProgramRepository(ApplicationDbContext context, IMapper mapper)
    : IMonitoringProgramRepository
{
    public async Task<Result<List<MonitoringProgramDto>>> GetMonitoringPrograms(
        string searchQuery,
        MonitoringProgramStatus? status,
        SamplingPointType? samplingPointType,
        Guid? samplingPointId)
    {
        var query = context.QcMonitoringPrograms
            .AsNoTracking()
            .Include(item => item.CreatedBy)
            .Include(item => item.SamplingPoint)
                .ThenInclude(point => point.SamplingPointGroup)
            .Include(item => item.Specification)
            .AsQueryable();

        if (status.HasValue) query = query.Where(item => item.Status == status.Value);
        if (samplingPointId.HasValue) query = query.Where(item => item.SamplingPointId == samplingPointId.Value);

        if (samplingPointType.HasValue)
            query = query.Where(item => item.SamplingPoint.Type == samplingPointType.Value);

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var term = searchQuery.Trim().ToLower();
            query = query.Where(item =>
                item.SamplingPoint.Code.ToLower().Contains(term)
                || item.SamplingPoint.Name.ToLower().Contains(term)
                || item.Specification.Code.ToLower().Contains(term));
        }

        // Unpaginated and ordered by due date: this feeds the calendar view, whose whole job is
        // to show Overdue and Due Today first. Paginating it would push overdue points onto a
        // second page nobody looks at.
        var programs = await query
            .OrderBy(item => item.NextDueDate)
            .ThenBy(item => item.SamplingPoint.Code)
            .AsSplitQuery()
            .ToListAsync();

        var today = DateTime.UtcNow.Date;
        return Result.Success(programs.Select(program => ToDto(program, today)).ToList());
    }

    public async Task<Result<MonitoringProgramDto>> GetMonitoringProgram(Guid id)
    {
        var program = await LoadDetail(id);

        return program is null
            ? Result.Failure<MonitoringProgramDto>(QcWorksheetErrors.MonitoringProgramNotFound(id))
            : Result.Success(ToDto(program, DateTime.UtcNow.Date));
    }

    public async Task<Result<MonitoringProgramDto>> CreateMonitoringProgram(
        CreateMonitoringProgramRequest request, Guid userId)
    {
        var validation = await Validate(request, null);
        if (!validation.IsSuccess)
            return Result.Failure<MonitoringProgramDto>(validation.Error);

        var specification = validation.Value;

        var program = new MonitoringProgram
        {
            Id = Guid.NewGuid(),
            SamplingPointId = request.SamplingPointId,
            SpecificationId = specification.Id,

            // Pinned here, once, from the specification row itself — never client-supplied.
            SpecificationVersion = specification.Version,
            Frequency = request.Frequency!.Value,
            CustomIntervalDays = request.Frequency.Value == MonitoringFrequency.Custom
                ? request.CustomIntervalDays
                : null,
            LeadTimeDays = request.LeadTimeDays,
            NextDueDate = request.NextDueDate,
            Status = MonitoringProgramStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedById = userId
        };

        context.QcMonitoringPrograms.Add(program);
        await context.SaveChangesAsync();

        return await GetMonitoringProgram(program.Id);
    }

    public async Task<Result<MonitoringProgramDto>> UpdateMonitoringProgram(
        Guid id, UpdateMonitoringProgramRequest request, Guid userId)
    {
        var program = await context.QcMonitoringPrograms.SingleOrDefaultAsync(item => item.Id == id);
        if (program is null)
            return Result.Failure<MonitoringProgramDto>(QcWorksheetErrors.MonitoringProgramNotFound(id));

        var validation = await Validate(request, id);
        if (!validation.IsSuccess)
            return Result.Failure<MonitoringProgramDto>(validation.Error);

        var specification = validation.Value;

        program.SamplingPointId = request.SamplingPointId;
        program.SpecificationId = specification.Id;

        // Re-pinned from the row now named. This is the deliberate human act hard version pinning
        // requires when a Specification is revised: the program does not follow the revision on
        // its own, somebody repoints it here.
        program.SpecificationVersion = specification.Version;
        program.Frequency = request.Frequency!.Value;
        program.CustomIntervalDays = request.Frequency.Value == MonitoringFrequency.Custom
            ? request.CustomIntervalDays
            : null;
        program.LeadTimeDays = request.LeadTimeDays;
        program.NextDueDate = request.NextDueDate;
        program.UpdatedAt = DateTime.UtcNow;
        program.LastUpdatedById = userId;

        await context.SaveChangesAsync();

        return await GetMonitoringProgram(id);
    }

    public async Task<Result<MonitoringProgramDto>> Pause(Guid id, Guid userId)
    {
        var program = await context.QcMonitoringPrograms.SingleOrDefaultAsync(item => item.Id == id);
        if (program is null)
            return Result.Failure<MonitoringProgramDto>(QcWorksheetErrors.MonitoringProgramNotFound(id));

        if (program.Status != MonitoringProgramStatus.Active)
            return Result.Failure<MonitoringProgramDto>(QcWorksheetErrors.PauseRequiresActive(program.Status));

        program.Status = MonitoringProgramStatus.Paused;
        program.UpdatedAt = DateTime.UtcNow;
        program.LastUpdatedById = userId;

        await context.SaveChangesAsync();

        return await GetMonitoringProgram(id);
    }

    /// <summary>
    /// Resumes a paused schedule.
    /// <para>
    /// <see cref="MonitoringProgram.NextDueDate"/> is deliberately left exactly where it was.
    /// A program paused through three due dates comes back overdue, which is the truth and which
    /// the calendar view files under Overdue; silently rolling it forward to the next future slot
    /// would erase the fact that three occurrences were never tested.
    /// </para>
    /// </summary>
    public async Task<Result<MonitoringProgramDto>> Resume(Guid id, Guid userId)
    {
        var program = await context.QcMonitoringPrograms.SingleOrDefaultAsync(item => item.Id == id);
        if (program is null)
            return Result.Failure<MonitoringProgramDto>(QcWorksheetErrors.MonitoringProgramNotFound(id));

        if (program.Status != MonitoringProgramStatus.Paused)
            return Result.Failure<MonitoringProgramDto>(QcWorksheetErrors.ResumeRequiresPaused(program.Status));

        program.Status = MonitoringProgramStatus.Active;
        program.UpdatedAt = DateTime.UtcNow;
        program.LastUpdatedById = userId;

        await context.SaveChangesAsync();

        return await GetMonitoringProgram(id);
    }

    // -----------------------------------------------------------------------
    // Validation
    // -----------------------------------------------------------------------

    /// <summary>
    /// Resolves the exact Specification version row the program will pin to — the row the given
    /// id names, with no walk forward through <c>SupersedesId</c> — and checks the whole triple
    /// (point, specification, frequency) hangs together.
    /// </summary>
    private async Task<Result<Specification>> Validate(CreateMonitoringProgramRequest request, Guid? excludingId)
    {
        if (!request.Frequency.HasValue || !Enum.IsDefined(request.Frequency.Value))
            return Result.Failure<Specification>(Error.Validation(
                "QcMonitoringProgram.FrequencyRequired",
                "A monitoring program must declare how often it comes round."));

        var frequency = request.Frequency.Value;

        if (frequency == MonitoringFrequency.Custom
            && (!request.CustomIntervalDays.HasValue || request.CustomIntervalDays.Value <= 0))
            return Result.Failure<Specification>(QcWorksheetErrors.CustomIntervalRequired);

        if (frequency != MonitoringFrequency.Custom && request.CustomIntervalDays.HasValue)
            return Result.Failure<Specification>(QcWorksheetErrors.CustomIntervalNotApplicable(frequency));

        var point = await context.QcSamplingPoints
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.SamplingPointId);

        if (point is null)
            return Result.Failure<Specification>(QcWorksheetErrors.SamplingPointNotFound(request.SamplingPointId));

        var specification = await context.QcSpecifications
            .AsNoTracking()
            .Include(item => item.WorksheetLinks)
            .SingleOrDefaultAsync(item => item.Id == request.SpecificationId);

        if (specification is null)
            return Result.Failure<Specification>(
                QcWorksheetErrors.MonitoringProgramSpecificationNotFound(request.SpecificationId));

        // The same gate TestRequestRepository applies at round creation: real testing runs
        // against a controlled document somebody signed for.
        if (specification.Status != QcDocumentStatus.Effective)
            return Result.Failure<Specification>(
                QcWorksheetErrors.MonitoringProgramSpecificationNotEffective(
                    specification.Code, specification.Status));

        if (specification.AppliesTo != QcSamplingPointTypes.ToAppliesTo(point.Type))
            return Result.Failure<Specification>(
                QcWorksheetErrors.MonitoringProgramSpecificationTypeMismatch(
                    point.Type, specification.AppliesTo));

        if (specification.WorksheetLinks.Count == 0)
            return Result.Failure<Specification>(
                QcWorksheetErrors.MonitoringProgramSpecificationHasNoWorksheetLinks(specification.Code));

        // One live schedule per (point, specification). A duplicate would simply raise the same
        // round twice, or — worse — be silently absorbed into the same generated round.
        var duplicate = await context.QcMonitoringPrograms.AnyAsync(item =>
            item.SamplingPointId == request.SamplingPointId
            && item.SpecificationId == request.SpecificationId
            && (!excludingId.HasValue || item.Id != excludingId.Value));

        if (duplicate)
            return Result.Failure<Specification>(
                QcWorksheetErrors.DuplicateMonitoringProgram(point.Code, specification.Code));

        return Result.Success(specification);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private async Task<MonitoringProgram> LoadDetail(Guid id) =>
        await context.QcMonitoringPrograms
            .AsNoTracking()
            .Include(item => item.CreatedBy)
            .Include(item => item.SamplingPoint)
                .ThenInclude(point => point.SamplingPointGroup)
            .Include(item => item.Specification)
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.Id == id);

    /// <summary>
    /// Which calendar bucket a program falls in, computed against today rather than stored — it
    /// is a rendering of the clock against the schedule, not a fact about the program.
    /// </summary>
    internal static MonitoringDueBucket BucketFor(MonitoringProgram program, DateTime today)
    {
        if (program.Status == MonitoringProgramStatus.Paused) return MonitoringDueBucket.Paused;

        var due = program.NextDueDate.Date;

        if (due < today) return MonitoringDueBucket.Overdue;
        if (due == today) return MonitoringDueBucket.DueToday;

        return due <= today.AddDays(7) ? MonitoringDueBucket.DueThisWeek : MonitoringDueBucket.Scheduled;
    }

    private MonitoringProgramDto ToDto(MonitoringProgram program, DateTime today) => new()
    {
        Id = program.Id,
        SamplingPointId = program.SamplingPointId,
        SamplingPoint = program.SamplingPoint is null
            ? null
            : new SamplingPointDto
            {
                Id = program.SamplingPoint.Id,
                Code = program.SamplingPoint.Code,
                Name = program.SamplingPoint.Name,
                Area = program.SamplingPoint.Area,
                Type = program.SamplingPoint.Type,
                SamplingPointGroupId = program.SamplingPoint.SamplingPointGroupId,
                SamplingPointGroup = program.SamplingPoint.SamplingPointGroup is null
                    ? null
                    : new SamplingPointGroupDto
                    {
                        Id = program.SamplingPoint.SamplingPointGroup.Id,
                        Name = program.SamplingPoint.SamplingPointGroup.Name,
                        Description = program.SamplingPoint.SamplingPointGroup.Description,
                        CreatedAt = program.SamplingPoint.SamplingPointGroup.CreatedAt
                    },
                CreatedAt = program.SamplingPoint.CreatedAt
            },
        SpecificationId = program.SpecificationId,

        // Reported as stored, deliberately not re-read from the specification row — a drifted
        // pin should be visible rather than papered over.
        SpecificationVersion = program.SpecificationVersion,
        SpecificationCode = program.Specification?.Code,
        SpecificationName = program.Specification?.Name,
        Frequency = program.Frequency,
        CustomIntervalDays = program.CustomIntervalDays,
        LeadTimeDays = program.LeadTimeDays,
        NextDueDate = program.NextDueDate,
        Status = program.Status,
        DueBucket = BucketFor(program, today),
        CreatedAt = program.CreatedAt,
        CreatedBy = mapper.Map<UserDto>(program.CreatedBy)
    };
}
