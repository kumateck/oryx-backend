using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.FullProcedures;

public sealed partial class TemplateWorkflowService
{
    public async Task<Result<IReadOnlyList<TemplateWorkflowDto>>> ListAsync(Guid areaId,
        IReadOnlyCollection<Guid> actorRoleIds, CancellationToken cancellationToken = default)
    {
        var area = await LoadAreaAsync(areaId, cancellationToken);
        if (area is null) return TemplateAreaErrors.NotFound;
        if (!TemplateDefinitionAuthorization.Allows(
                area, actorRoleIds, TemplateDefinitionOperation.View))
            return TemplateWorkflowErrors.AccessDenied;
        var items = await WorkflowQuery().AsNoTracking().Where(x => x.TemplateAreaId == areaId)
            .OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<TemplateWorkflowDto>>(items.Select(ToWorkflowDto).ToArray());
    }

    public async Task<Result<TemplateWorkflowDetailDto>> GetAsync(Guid workflowId,
        IReadOnlyCollection<Guid> actorRoleIds, CancellationToken cancellationToken = default)
    {
        var item = await WorkflowQuery().AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == workflowId, cancellationToken);
        if (item is null) return TemplateWorkflowErrors.NotFound;
        if (!TemplateDefinitionAuthorization.Allows(
                item.TemplateArea, actorRoleIds, TemplateDefinitionOperation.View))
            return TemplateWorkflowErrors.AccessDenied;
        return new TemplateWorkflowDetailDto(item.Id, item.TemplateAreaId, item.TemplateArea.Name,
            item.PurposeId, item.SubjectTypeId, item.Revisions.OrderByDescending(x => x.Sequence)
                .Select(TemplateWorkflowServiceSupport.ToDto).ToArray());
    }
}
