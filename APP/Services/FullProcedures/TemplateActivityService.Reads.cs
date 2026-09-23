using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.FullProcedures;

public sealed partial class TemplateActivityService
{
    public async Task<Result<IReadOnlyList<TemplateActivityDto>>> ListAsync(Guid areaId,
        IReadOnlyCollection<Guid> actorRoleIds, CancellationToken cancellationToken = default)
    {
        var area = await LoadAreaAsync(areaId, cancellationToken);
        if (area is null) return TemplateAreaErrors.NotFound;
        if (!TemplateDefinitionAuthorization.Allows(
                area, actorRoleIds, TemplateDefinitionOperation.View))
            return TemplateActivityErrors.AccessDenied;
        var items = await ActivityQuery().AsNoTracking().Where(x => x.TemplateAreaId == areaId)
            .OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<TemplateActivityDto>>(items.Select(ToActivityDto).ToArray());
    }

    public async Task<Result<TemplateActivityDetailDto>> GetAsync(Guid activityId,
        IReadOnlyCollection<Guid> actorRoleIds, CancellationToken cancellationToken = default)
    {
        var item = await ActivityQuery().AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == activityId, cancellationToken);
        if (item is null) return TemplateActivityErrors.NotFound;
        if (!TemplateDefinitionAuthorization.Allows(
                item.TemplateArea, actorRoleIds, TemplateDefinitionOperation.View))
            return TemplateActivityErrors.AccessDenied;
        return new TemplateActivityDetailDto(item.Id, item.TemplateAreaId, item.TemplateArea.Name,
            item.PurposeId, item.SubjectTypeId, item.Revisions.OrderByDescending(x => x.Sequence)
                .Select(TemplateActivityServiceSupport.ToDto).ToArray());
    }
}
