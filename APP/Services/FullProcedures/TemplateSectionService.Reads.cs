using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.FullProcedures;

public sealed partial class TemplateSectionService
{
    public async Task<Result<IReadOnlyList<TemplateSectionDto>>> ListAsync(
        Guid areaId, IReadOnlyCollection<Guid> actorRoleIds,
        CancellationToken cancellationToken = default)
    {
        var area = await LoadAreaAsync(areaId, cancellationToken);
        if (area is null) return TemplateAreaErrors.NotFound;
        if (!TemplateDefinitionAuthorization.Allows(
                area, actorRoleIds, TemplateDefinitionOperation.View))
            return TemplateSectionErrors.AccessDenied;
        var sections = await SectionQuery().AsNoTracking()
            .Where(item => item.TemplateAreaId == areaId)
            .OrderBy(item => item.PurposeId).ThenBy(item => item.SubjectTypeId)
            .ThenBy(item => item.Id).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<TemplateSectionDto>>(
            sections.Select(ToSectionDto).ToArray());
    }

    public async Task<Result<TemplateSectionDetailDto>> GetAsync(
        Guid sectionId, IReadOnlyCollection<Guid> actorRoleIds,
        CancellationToken cancellationToken = default)
    {
        var section = await SectionQuery().AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == sectionId, cancellationToken);
        if (section is null) return TemplateSectionErrors.NotFound;
        if (!TemplateDefinitionAuthorization.Allows(
                section.TemplateArea, actorRoleIds, TemplateDefinitionOperation.View))
            return TemplateSectionErrors.AccessDenied;
        return new TemplateSectionDetailDto(
            section.Id, section.TemplateAreaId, section.TemplateArea.Name,
            section.PurposeId, section.SubjectTypeId,
            section.Revisions.OrderByDescending(item => item.Sequence)
                .Select(TemplateSectionServiceSupport.ToDto).ToArray());
    }
}
