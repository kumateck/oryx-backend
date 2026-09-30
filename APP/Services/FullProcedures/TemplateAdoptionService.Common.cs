using DOMAIN.Entities.FullProcedures;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.FullProcedures;

public sealed partial class TemplateAdoptionService(
    ApplicationDbContext context,
    ITemplateQuestionService questions,
    ITemplateSectionService sections,
    ITemplateFormService forms,
    ITemplateActivityService activities,
    ITemplateWorkflowService workflows) : ITemplateAdoptionService
{
    private IQueryable<TemplateSharingGrant> GrantQuery() =>
        context.Set<TemplateSharingGrant>().AsSplitQuery()
            .Include(x => x.SourceArea).ThenInclude(x => x.RoleGrants)
            .Include(x => x.TargetArea).ThenInclude(x => x.RoleGrants)
            .Include(x => x.TargetArea).ThenInclude(x => x.Purposes)
            .Include(x => x.TargetArea).ThenInclude(x => x.SubjectTypes);

    private Task<TemplateSharingGrant> LoadGrantAsync(Guid id, CancellationToken token) =>
        GrantQuery().SingleOrDefaultAsync(x => x.Id == id, token);

    private static TemplateAdoptionDependencyMappingRequest FindDependency(
        AdoptTemplateRevisionRequest request, Guid definitionId, Guid revisionId) =>
        request.DependencyMappings.SingleOrDefault(x =>
            x.SourceDefinitionId == definitionId && x.SourceRevisionId == revisionId);

    private static Guid MapRole(AdoptTemplateRevisionRequest request, Guid sourceRoleId) =>
        request.RoleMappings.SingleOrDefault(x => x.SourceRoleId == sourceRoleId)?.TargetRoleId ??
        Guid.Empty;

    private static bool ExactDependencySet(AdoptTemplateRevisionRequest request,
        IEnumerable<(Guid DefinitionId, Guid RevisionId)> expected)
    {
        var actual = request.DependencyMappings
            .Select(x => (x.SourceDefinitionId, x.SourceRevisionId)).ToHashSet();
        return actual.SetEquals(expected.ToHashSet());
    }

    private static bool ExactRoleSet(AdoptTemplateRevisionRequest request,
        IEnumerable<Guid> expected) => request.RoleMappings.Select(x => x.SourceRoleId)
        .ToHashSet().SetEquals(expected.ToHashSet());
}
