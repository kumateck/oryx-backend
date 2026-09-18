using APP.IRepository;
using AutoMapper;
using DOMAIN.Entities.QcWorksheets;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository.QcWorksheets;

/// <summary>
/// Plain CRUD over the <see cref="SamplingPoint"/> master table — the correction this milestone
/// makes to a system where a sampling point was only ever a typed string.
/// <para>
/// No lifecycle, no versioning and no approval, for the same reason
/// <see cref="SamplingPointGroupRepository"/> has none: this is reference data a QC Manager
/// maintains, not a controlled document.
/// </para>
/// </summary>
public class SamplingPointRepository(ApplicationDbContext context, IMapper mapper)
    : ISamplingPointRepository
{
    public async Task<Result<List<SamplingPointDto>>> GetSamplingPoints(
        string searchQuery, SamplingPointType? type)
    {
        var query = context.QcSamplingPoints
            .AsNoTracking()
            .Include(item => item.CreatedBy)
            .Include(item => item.SamplingPointGroup)
            .AsQueryable();

        if (type.HasValue)
            query = query.Where(item => item.Type == type.Value);

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var term = searchQuery.Trim().ToLower();
            query = query.Where(item =>
                item.Code.ToLower().Contains(term)
                || item.Name.ToLower().Contains(term)
                || (item.Area != null && item.Area.ToLower().Contains(term)));
        }

        // Not paginated, and filtered by Type on purpose: this populates the point picker on a
        // monitoring program form and on a routine Subject, both of which are already scoped to
        // one discipline. A partial page would hide points the author needs.
        var points = await query.OrderBy(item => item.Code).ToListAsync();

        return Result.Success(points.Select(ToDto).ToList());
    }

    public async Task<Result<SamplingPointDto>> GetSamplingPoint(Guid id)
    {
        var point = await LoadDetail(id);

        return point is null
            ? Result.Failure<SamplingPointDto>(QcWorksheetErrors.SamplingPointNotFound(id))
            : Result.Success(ToDto(point));
    }

    public async Task<Result<SamplingPointDto>> CreateSamplingPoint(
        CreateSamplingPointRequest request, Guid userId)
    {
        var code = request.Code?.Trim();

        if (await CodeExists(code, null))
            return Result.Failure<SamplingPointDto>(QcWorksheetErrors.DuplicateSamplingPointCode(code));

        var groupCheck = await ValidateGroup(request.SamplingPointGroupId);
        if (!groupCheck.IsSuccess)
            return Result.Failure<SamplingPointDto>(groupCheck.Error);

        var point = new SamplingPoint
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = request.Name?.Trim(),
            Area = request.Area?.Trim(),

            // Model binding has already rejected an omitted Type — the request property is
            // nullable precisely so that omission cannot bind to Water by default.
            Type = request.Type!.Value,
            SamplingPointGroupId = request.SamplingPointGroupId,
            CreatedAt = DateTime.UtcNow,
            CreatedById = userId
        };

        context.QcSamplingPoints.Add(point);
        await context.SaveChangesAsync();

        return await GetSamplingPoint(point.Id);
    }

    public async Task<Result<SamplingPointDto>> UpdateSamplingPoint(
        Guid id, UpdateSamplingPointRequest request, Guid userId)
    {
        var point = await context.QcSamplingPoints.SingleOrDefaultAsync(item => item.Id == id);
        if (point is null)
            return Result.Failure<SamplingPointDto>(QcWorksheetErrors.SamplingPointNotFound(id));

        var code = request.Code?.Trim();

        if (await CodeExists(code, id))
            return Result.Failure<SamplingPointDto>(QcWorksheetErrors.DuplicateSamplingPointCode(code));

        var groupCheck = await ValidateGroup(request.SamplingPointGroupId);
        if (!groupCheck.IsSuccess)
            return Result.Failure<SamplingPointDto>(groupCheck.Error);

        // Type is effectively immutable once anything has resolved against it. A Water point that
        // became Environmental would leave rounds pinned to a Specification for the wrong
        // category and water validity windows hanging off a point that can no longer have them.
        if (request.Type.HasValue && request.Type.Value != point.Type && await IsReferenced(id))
            return Result.Failure<SamplingPointDto>(QcWorksheetErrors.SamplingPointTypeImmutable);

        point.Code = code;
        point.Name = request.Name?.Trim();
        point.Area = request.Area?.Trim();
        point.Type = request.Type ?? point.Type;
        point.SamplingPointGroupId = request.SamplingPointGroupId;
        point.UpdatedAt = DateTime.UtcNow;
        point.LastUpdatedById = userId;

        await context.SaveChangesAsync();

        return await GetSamplingPoint(id);
    }

    public async Task<Result> DeleteSamplingPoint(Guid id, Guid userId)
    {
        var point = await context.QcSamplingPoints.SingleOrDefaultAsync(item => item.Id == id);
        if (point is null)
            return QcWorksheetErrors.SamplingPointNotFound(id);

        // A program whose point vanished would have nothing to schedule, so a point still
        // scheduled is refused rather than detached silently — the same rule a sampling point
        // group in use gets.
        var programCount = await context.QcMonitoringPrograms
            .CountAsync(item => item.SamplingPointId == id);

        if (programCount > 0)
            return QcWorksheetErrors.SamplingPointInUse(point.Code, programCount);

        // Soft delete: the context rewrites a Deleted entry as Modified with DeletedAt set, so
        // rounds and windows that already reference this point keep resolving it.
        point.LastDeletedById = userId;
        context.QcSamplingPoints.Remove(point);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private async Task<Result> ValidateGroup(Guid? samplingPointGroupId)
    {
        if (!samplingPointGroupId.HasValue) return Result.Success();

        return await context.QcSamplingPointGroups.AnyAsync(item => item.Id == samplingPointGroupId.Value)
            ? Result.Success()
            : QcWorksheetErrors.SamplingPointGroupNotFoundOnSubject(samplingPointGroupId.Value);
    }

    /// <summary>
    /// Uniqueness among <b>live</b> points. Not a database unique index, because soft deletion
    /// would otherwise reserve a retired point's code for ever; the query filter makes this the
    /// check that matches the rule.
    /// </summary>
    private async Task<bool> CodeExists(string code, Guid? excludingId) =>
        !string.IsNullOrWhiteSpace(code)
        && await context.QcSamplingPoints.AnyAsync(item =>
            item.Code.ToLower() == code.ToLower()
            && (!excludingId.HasValue || item.Id != excludingId.Value));

    private async Task<bool> IsReferenced(Guid id) =>
        await context.QcMonitoringPrograms.AnyAsync(item => item.SamplingPointId == id)
        || await context.QcTestRequestSubjects.AnyAsync(item => item.SamplingPointId == id)
        || await context.QcWaterQualityPeriods.AnyAsync(item => item.SamplingPointId == id);

    private async Task<SamplingPoint> LoadDetail(Guid id) =>
        await context.QcSamplingPoints
            .AsNoTracking()
            .Include(item => item.CreatedBy)
            .Include(item => item.SamplingPointGroup)
            .SingleOrDefaultAsync(item => item.Id == id);

    internal SamplingPointDto ToDto(SamplingPoint point) => new()
    {
        Id = point.Id,
        Code = point.Code,
        Name = point.Name,
        Area = point.Area,
        Type = point.Type,
        SamplingPointGroupId = point.SamplingPointGroupId,
        SamplingPointGroup = point.SamplingPointGroup is null
            ? null
            : new SamplingPointGroupDto
            {
                Id = point.SamplingPointGroup.Id,
                Name = point.SamplingPointGroup.Name,
                Description = point.SamplingPointGroup.Description,
                CreatedAt = point.SamplingPointGroup.CreatedAt
            },
        CreatedAt = point.CreatedAt,
        CreatedBy = mapper.Map<UserDto>(point.CreatedBy)
    };
}
