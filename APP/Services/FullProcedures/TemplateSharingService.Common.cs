using DOMAIN.Entities.FullProcedures;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.FullProcedures;

public sealed partial class TemplateSharingService(ApplicationDbContext context)
    : ITemplateSharingService
{
    private IQueryable<TemplateArea> AreaQuery() => context.Set<TemplateArea>()
        .Include(x => x.RoleGrants).Include(x => x.Purposes).Include(x => x.SubjectTypes);

    private IQueryable<TemplateSharingGrant> GrantQuery() =>
        context.Set<TemplateSharingGrant>().AsSplitQuery()
            .Include(x => x.SourceArea).ThenInclude(x => x.RoleGrants)
            .Include(x => x.SourceArea).ThenInclude(x => x.Purposes)
            .Include(x => x.SourceArea).ThenInclude(x => x.SubjectTypes)
            .Include(x => x.TargetArea).ThenInclude(x => x.RoleGrants)
            .Include(x => x.TargetArea).ThenInclude(x => x.Purposes)
            .Include(x => x.TargetArea).ThenInclude(x => x.SubjectTypes);

    private Task<TemplateArea> LoadAreaAsync(Guid id, CancellationToken token) =>
        AreaQuery().SingleOrDefaultAsync(x => x.Id == id, token);

    private async Task<ResolvedTemplateRevision> ResolveRevisionAsync(
        TemplateRevisionKind kind, Guid definitionId, Guid revisionId,
        CancellationToken token)
    {
        return kind switch
        {
            TemplateRevisionKind.Question => await context.Set<TemplateQuestionRevision>()
                .AsNoTracking().Where(x => x.Id == revisionId &&
                    x.TemplateQuestionId == definitionId)
                .Select(x => new ResolvedTemplateRevision(x.TemplateQuestion.TemplateAreaId,
                    x.TemplateQuestion.PurposeId, x.TemplateQuestion.SubjectTypeId,
                    x.ContentHash, x.Status == TemplateQuestionRevisionStatus.Published))
                .SingleOrDefaultAsync(token),
            TemplateRevisionKind.Section => await context.Set<TemplateSectionRevision>()
                .AsNoTracking().Where(x => x.Id == revisionId &&
                    x.TemplateSectionId == definitionId)
                .Select(x => new ResolvedTemplateRevision(x.TemplateSection.TemplateAreaId,
                    x.TemplateSection.PurposeId, x.TemplateSection.SubjectTypeId,
                    x.ContentHash, x.Status == TemplateSectionRevisionStatus.Published))
                .SingleOrDefaultAsync(token),
            TemplateRevisionKind.Form => await context.Set<TemplateFormRevision>()
                .AsNoTracking().Where(x => x.Id == revisionId && x.TemplateFormId == definitionId)
                .Select(x => new ResolvedTemplateRevision(x.TemplateForm.TemplateAreaId,
                    x.TemplateForm.PurposeId, x.TemplateForm.SubjectTypeId,
                    x.ContentHash, x.Status == TemplateFormRevisionStatus.Published))
                .SingleOrDefaultAsync(token),
            TemplateRevisionKind.Activity => await context.Set<TemplateActivityRevision>()
                .AsNoTracking().Where(x => x.Id == revisionId &&
                    x.TemplateActivityId == definitionId)
                .Select(x => new ResolvedTemplateRevision(x.TemplateActivity.TemplateAreaId,
                    x.TemplateActivity.PurposeId, x.TemplateActivity.SubjectTypeId,
                    x.ContentHash, x.Status == TemplateActivityRevisionStatus.Published))
                .SingleOrDefaultAsync(token),
            TemplateRevisionKind.Workflow => await context.Set<TemplateWorkflowRevision>()
                .AsNoTracking().Where(x => x.Id == revisionId &&
                    x.TemplateWorkflowId == definitionId)
                .Select(x => new ResolvedTemplateRevision(x.TemplateWorkflow.TemplateAreaId,
                    x.TemplateWorkflow.PurposeId, x.TemplateWorkflow.SubjectTypeId,
                    x.ContentHash, x.Status == TemplateWorkflowRevisionStatus.Published))
                .SingleOrDefaultAsync(token),
            _ => null,
        };
    }

    private async Task<int> CountDirectReferencesAsync(TemplateRevisionKind kind,
        Guid revisionId, CancellationToken token) => kind switch
        {
            TemplateRevisionKind.Question =>
                await context.Set<TemplateSectionQuestion>().AsNoTracking()
                    .CountAsync(x => x.TemplateQuestionRevisionId == revisionId, token) +
                await context.Set<TemplateFormConditionalRule>().AsNoTracking()
                    .CountAsync(x => x.SourceQuestionRevisionId == revisionId, token) +
                await context.Set<TemplateQuestionCalculationReference>().AsNoTracking()
                    .CountAsync(x => x.ReferencedQuestionRevisionId == revisionId, token),
            TemplateRevisionKind.Section => await context.Set<TemplateFormSection>().AsNoTracking()
                .CountAsync(x => x.TemplateSectionRevisionId == revisionId, token),
            TemplateRevisionKind.Form => await context.Set<TemplateActivityFormBinding>().AsNoTracking()
                .CountAsync(x => x.TemplateFormRevisionId == revisionId, token),
            TemplateRevisionKind.Activity => await context.Set<TemplateWorkflowNode>().AsNoTracking()
                .CountAsync(x => x.TemplateActivityRevisionId == revisionId, token),
            TemplateRevisionKind.Workflow => 0,
            _ => 0,
        };

    private static bool ContextAllowed(TemplateArea area, ResolvedTemplateRevision revision) =>
        area.Purposes.Any(x => x.PurposeId == revision.PurposeId) &&
        area.SubjectTypes.Any(x => x.SubjectTypeId == revision.SubjectTypeId);
}
