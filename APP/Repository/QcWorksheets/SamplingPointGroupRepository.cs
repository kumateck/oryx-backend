using APP.IRepository;
using AutoMapper;
using DOMAIN.Entities.QcWorksheets;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository.QcWorksheets;

/// <summary>
/// Plain CRUD over the <see cref="SamplingPointGroup"/> reference table. No lifecycle, no
/// versioning and no approval: this is reference data a QC Manager maintains, not a
/// controlled document, which is also why it sits behind one permission key rather than the
/// five a controlled document gets.
/// </summary>
public class SamplingPointGroupRepository(ApplicationDbContext context, IMapper mapper)
    : ISamplingPointGroupRepository
{
    public async Task<Result<List<SamplingPointGroupDto>>> GetSamplingPointGroups(string searchQuery)
    {
        var query = context.QcSamplingPointGroups
            .Include(item => item.CreatedBy)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var term = searchQuery.Trim().ToLower();
            query = query.Where(item =>
                item.Name.ToLower().Contains(term)
                || (item.Description != null && item.Description.ToLower().Contains(term)));
        }

        // Not paginated on purpose: this list populates a dropdown, and a partial page would
        // silently hide groups an author needs to pick from.
        var groups = await query.OrderBy(item => item.Name).ToListAsync();

        return Result.Success(groups.Select(ToDto).ToList());
    }

    public async Task<Result<SamplingPointGroupDto>> GetSamplingPointGroup(Guid id)
    {
        var group = await context.QcSamplingPointGroups
            .Include(item => item.CreatedBy)
            .SingleOrDefaultAsync(item => item.Id == id);

        return group is null
            ? Result.Failure<SamplingPointGroupDto>(QcWorksheetErrors.SamplingPointGroupNotFound(id))
            : Result.Success(ToDto(group));
    }

    public async Task<Result<SamplingPointGroupDto>> CreateSamplingPointGroup(
        CreateSamplingPointGroupRequest request, Guid userId)
    {
        var name = request.Name?.Trim();

        if (await NameExists(name, null))
            return Result.Failure<SamplingPointGroupDto>(
                QcWorksheetErrors.DuplicateSamplingPointGroupName(name));

        var group = new SamplingPointGroup
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            CreatedById = userId
        };

        context.QcSamplingPointGroups.Add(group);
        await context.SaveChangesAsync();

        return await GetSamplingPointGroup(group.Id);
    }

    public async Task<Result<SamplingPointGroupDto>> UpdateSamplingPointGroup(
        Guid id, UpdateSamplingPointGroupRequest request, Guid userId)
    {
        var group = await context.QcSamplingPointGroups.SingleOrDefaultAsync(item => item.Id == id);
        if (group is null)
            return Result.Failure<SamplingPointGroupDto>(QcWorksheetErrors.SamplingPointGroupNotFound(id));

        var name = request.Name?.Trim();

        if (await NameExists(name, id))
            return Result.Failure<SamplingPointGroupDto>(
                QcWorksheetErrors.DuplicateSamplingPointGroupName(name));

        group.Name = name;
        group.Description = request.Description;
        group.UpdatedAt = DateTime.UtcNow;
        group.LastUpdatedById = userId;

        await context.SaveChangesAsync();
        return await GetSamplingPointGroup(id);
    }

    public async Task<Result> DeleteSamplingPointGroup(Guid id, Guid userId)
    {
        var group = await context.QcSamplingPointGroups.SingleOrDefaultAsync(item => item.Id == id);
        if (group is null)
            return QcWorksheetErrors.SamplingPointGroupNotFound(id);

        // A characteristic's Alert/Action tier is meaningful only through its group, so a
        // group still in use is refused rather than detached silently.
        var inUse = await context.QcSpecificationCharacteristics
            .CountAsync(item => item.SamplingPointGroupId == id);

        if (inUse > 0)
            return QcWorksheetErrors.SamplingPointGroupInUse(group.Name, inUse);

        // Soft delete: the context rewrites a Deleted entry as Modified with DeletedAt set.
        group.LastDeletedById = userId;
        context.QcSamplingPointGroups.Remove(group);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    private async Task<bool> NameExists(string name, Guid? excludingId) =>
        !string.IsNullOrWhiteSpace(name)
        && await context.QcSamplingPointGroups.AnyAsync(item =>
            item.Name.ToLower() == name.ToLower()
            && (!excludingId.HasValue || item.Id != excludingId.Value));

    private SamplingPointGroupDto ToDto(SamplingPointGroup group) => new()
    {
        Id = group.Id,
        Name = group.Name,
        Description = group.Description,
        CreatedAt = group.CreatedAt,
        CreatedBy = mapper.Map<UserDto>(group.CreatedBy)
    };
}
