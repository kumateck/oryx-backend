using DOMAIN.Entities.FullProcedures;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable

namespace APP.Services.FullProcedures;

public sealed partial class TemplateFormService(ApplicationDbContext context)
    : ITemplateFormService
{
    private IQueryable<TemplateArea> AreaQuery() => context.Set<TemplateArea>()
        .Include(item => item.RoleGrants).Include(item => item.Purposes)
        .Include(item => item.SubjectTypes);

    private IQueryable<TemplateForm> FormQuery() => context.Set<TemplateForm>()
        .AsSplitQuery().Include(item => item.TemplateArea).ThenInclude(item => item.RoleGrants)
        .Include(item => item.Revisions).ThenInclude(item => item.Sections)
            .ThenInclude(item => item.TemplateSectionRevision)
        .Include(item => item.Revisions).ThenInclude(item => item.ConditionalRules)
            .ThenInclude(item => item.TargetFormSection)
        .Include(item => item.Revisions).ThenInclude(item => item.ConditionalRules)
            .ThenInclude(item => item.SourceFormSection);

    private IQueryable<TemplateFormRevision> RevisionQuery() =>
        context.Set<TemplateFormRevision>().AsSplitQuery()
            .Include(item => item.TemplateForm).ThenInclude(item => item.TemplateArea)
                .ThenInclude(item => item.RoleGrants)
            .Include(item => item.Sections).ThenInclude(item => item.TemplateSectionRevision)
            .Include(item => item.ConditionalRules).ThenInclude(item => item.TargetFormSection)
            .Include(item => item.ConditionalRules).ThenInclude(item => item.SourceFormSection);

    private Task<TemplateArea?> LoadAreaAsync(Guid id, CancellationToken cancellationToken) =>
        AreaQuery().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    private Task<TemplateFormRevision?> LoadRevisionAsync(
        Guid id, CancellationToken cancellationToken) =>
        RevisionQuery().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    private async Task<TemplateFormContent?> ResolveContentAsync(
        TemplateFormContentRequest request, Guid areaId, string purposeId,
        string subjectTypeId, CancellationToken cancellationToken)
    {
        if (!TemplateFormServiceSupport.ValidShape(request)) return null;
        var revisionIds = request.Sections.Select(item => item.RevisionId).ToArray();
        var resolved = await context.Set<TemplateSectionRevision>().AsNoTracking()
            .Include(item => item.Questions)
            .Where(item => revisionIds.Contains(item.Id) &&
                item.Status == TemplateSectionRevisionStatus.Published &&
                item.TemplateSection.TemplateAreaId == areaId &&
                item.TemplateSection.PurposeId == purposeId &&
                item.TemplateSection.SubjectTypeId == subjectTypeId)
            .ToListAsync(cancellationToken);
        if (resolved.Count != revisionIds.Length) return null;
        var byRevision = resolved.ToDictionary(item => item.Id,
            item => (item.TemplateSectionId, item.Title));
        if (request.Sections.Any(item =>
                byRevision[item.RevisionId].TemplateSectionId != item.SectionId))
            return null;
        var revisionBySection = request.Sections.ToDictionary(
            item => item.SectionId, item => item.RevisionId);
        foreach (var rule in request.ConditionalRules)
        {
            if (!revisionBySection.TryGetValue(rule.SourceSectionId, out var sourceRevisionId))
                return null;
            var source = resolved.Single(item => item.Id == sourceRevisionId);
            if (!source.Questions.Any(item => item.TemplateQuestionId == rule.SourceQuestionId &&
                    item.TemplateQuestionRevisionId == rule.SourceQuestionRevisionId))
                return null;
        }
        return TemplateFormServiceSupport.BuildContent(request, byRevision);
    }

    private static bool ValidContext(
        TemplateArea area, string purposeId, string subjectTypeId) =>
        area.Purposes.Any(item => item.PurposeId == purposeId) &&
        area.SubjectTypes.Any(item => item.SubjectTypeId == subjectTypeId);

    private static TemplateFormRevision NewRevision(
        Guid formId, int sequence, Guid actorId, TemplateFormContent content)
    {
        var revision = new TemplateFormRevision
        {
            Id = Guid.NewGuid(), TemplateFormId = formId, Sequence = sequence,
            Status = TemplateFormRevisionStatus.Draft, CreatedById = actorId,
        };
        TemplateFormServiceSupport.Apply(revision, content);
        return revision;
    }

    private async Task<bool> ReferencesArePublishedAsync(
        TemplateFormRevision revision, CancellationToken cancellationToken)
    {
        var revisionIds = revision.Sections.Select(item => item.TemplateSectionRevisionId)
            .ToArray();
        var pairs = await context.Set<TemplateSectionRevision>().AsNoTracking()
            .Where(item => revisionIds.Contains(item.Id) &&
                item.Status == TemplateSectionRevisionStatus.Published &&
                item.TemplateSection.TemplateAreaId == revision.TemplateForm.TemplateAreaId &&
                item.TemplateSection.PurposeId == revision.TemplateForm.PurposeId &&
                item.TemplateSection.SubjectTypeId == revision.TemplateForm.SubjectTypeId)
            .Select(item => new { item.Id, item.TemplateSectionId })
            .ToListAsync(cancellationToken);
        var resolved = pairs.ToDictionary(item => item.Id, item => item.TemplateSectionId);
        return revision.Sections.All(item => resolved.TryGetValue(
            item.TemplateSectionRevisionId, out var sectionId) &&
            sectionId == item.TemplateSectionId);
    }

    private async Task<Result<TemplateFormRevisionDto>> SaveRevisionAsync(
        Guid revisionId, CancellationToken cancellationToken)
    {
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { return TemplateFormErrors.Conflict; }
        catch (DbUpdateException) { return TemplateFormErrors.Conflict; }
        context.ChangeTracker.Clear();
        var saved = await RevisionQuery().AsNoTracking()
            .SingleAsync(item => item.Id == revisionId, cancellationToken);
        return TemplateFormServiceSupport.ToDto(saved);
    }

    private static TemplateFormDto ToFormDto(TemplateForm form)
    {
        var latest = form.Revisions.OrderByDescending(item => item.Sequence).FirstOrDefault();
        return new TemplateFormDto(form.Id, form.TemplateAreaId, form.TemplateArea.Name,
            form.PurposeId, form.SubjectTypeId,
            latest is null ? null : TemplateFormServiceSupport.ToDto(latest));
    }
}
