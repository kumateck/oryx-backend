using DOMAIN.Entities.FullProcedures;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable

namespace APP.Services.FullProcedures;

public sealed partial class TemplateWorkflowService(ApplicationDbContext context)
    : ITemplateWorkflowService
{
    private IQueryable<TemplateArea> AreaQuery() => context.Set<TemplateArea>()
        .Include(x => x.RoleGrants).Include(x => x.Purposes)
        .Include(x => x.SubjectTypes).Include(x => x.Capabilities);

    private IQueryable<TemplateWorkflow> WorkflowQuery() => context.Set<TemplateWorkflow>()
        .AsSplitQuery().Include(x => x.TemplateArea).ThenInclude(x => x.RoleGrants)
        .Include(x => x.Revisions).ThenInclude(x => x.Nodes)
            .ThenInclude(x => x.TemplateActivityRevision)
        .Include(x => x.Revisions).ThenInclude(x => x.Edges)
        .Include(x => x.Revisions).ThenInclude(x => x.Layouts);

    private IQueryable<TemplateWorkflowRevision> RevisionQuery() =>
        context.Set<TemplateWorkflowRevision>().AsSplitQuery()
            .Include(x => x.TemplateWorkflow).ThenInclude(x => x.TemplateArea)
                .ThenInclude(x => x.RoleGrants)
            .Include(x => x.Nodes).ThenInclude(x => x.TemplateActivityRevision)
            .Include(x => x.Edges).Include(x => x.Layouts);

    private Task<TemplateArea?> LoadAreaAsync(Guid id, CancellationToken token) =>
        AreaQuery().SingleOrDefaultAsync(x => x.Id == id, token);
    private Task<TemplateWorkflowRevision?> LoadRevisionAsync(Guid id, CancellationToken token) =>
        RevisionQuery().SingleOrDefaultAsync(x => x.Id == id, token);

    private async Task<TemplateWorkflowContent?> ResolveContentAsync(
        TemplateWorkflowContentRequest request, TemplateArea area, string purposeId,
        string subjectTypeId, CancellationToken token)
    {
        if (!TemplateWorkflowServiceValidation.ValidShape(request)) return null;
        var revisionIds = request.Nodes.Where(x => x.TemplateActivityRevisionId.HasValue)
            .Select(x => x.TemplateActivityRevisionId!.Value).Distinct().ToArray();
        var activities = await context.Set<TemplateActivityRevision>().AsNoTracking()
            .Where(x => revisionIds.Contains(x.Id) &&
                x.Status == TemplateActivityRevisionStatus.Published &&
                x.TemplateActivity.TemplateAreaId == area.Id &&
                x.TemplateActivity.PurposeId == purposeId &&
                x.TemplateActivity.SubjectTypeId == subjectTypeId)
            .Select(x => new { x.Id, x.TemplateActivityId, x.Name }).ToListAsync(token);
        if (activities.Count != revisionIds.Length) return null;
        var resolved = activities.ToDictionary(x => x.Id, x => (x.TemplateActivityId, x.Name));
        if (request.Nodes.Where(x => x.TemplateActivityRevisionId.HasValue)
                .Any(x => resolved[x.TemplateActivityRevisionId!.Value].TemplateActivityId !=
                    x.TemplateActivityId)) return null;
        return TemplateWorkflowServiceSupport.BuildContent(request, resolved);
    }

    private static bool ValidContext(TemplateArea area, string purposeId, string subjectTypeId) =>
        area.Purposes.Any(x => x.PurposeId == purposeId) &&
        area.SubjectTypes.Any(x => x.SubjectTypeId == subjectTypeId);

    private static TemplateWorkflowRevision NewRevision(Guid workflowId, int sequence,
        Guid actorId, TemplateWorkflowContent content)
    {
        var revision = new TemplateWorkflowRevision
        {
            Id = Guid.NewGuid(), TemplateWorkflowId = workflowId, Sequence = sequence,
            Status = TemplateWorkflowRevisionStatus.Draft, CreatedById = actorId,
        };
        TemplateWorkflowServiceSupport.Apply(revision, content);
        return revision;
    }

    private async Task<bool> ReferencesAvailableAsync(TemplateWorkflowRevision revision,
        CancellationToken token)
    {
        var revisionIds = revision.Nodes.Where(x => x.TemplateActivityRevisionId.HasValue)
            .Select(x => x.TemplateActivityRevisionId!.Value).Distinct().ToArray();
        if (revisionIds.Length == 0) return true;
        var workflow = revision.TemplateWorkflow;
        var activities = await context.Set<TemplateActivityRevision>().AsNoTracking()
            .Where(x => revisionIds.Contains(x.Id) &&
                x.Status == TemplateActivityRevisionStatus.Published &&
                x.TemplateActivity.TemplateAreaId == workflow.TemplateAreaId &&
                x.TemplateActivity.PurposeId == workflow.PurposeId &&
                x.TemplateActivity.SubjectTypeId == workflow.SubjectTypeId)
            .Select(x => new { x.Id, x.TemplateActivityId }).ToListAsync(token);
        var activityMap = activities.ToDictionary(x => x.Id, x => x.TemplateActivityId);
        return revision.Nodes.Where(x => x.TemplateActivityRevisionId.HasValue).All(x =>
            activityMap.TryGetValue(x.TemplateActivityRevisionId!.Value, out var activityId) &&
            activityId == x.TemplateActivityId);
    }

    private async Task<Result<TemplateWorkflowRevisionDto>> SaveRevisionAsync(Guid revisionId,
        CancellationToken token)
    {
        try { await context.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException) { return TemplateWorkflowErrors.Conflict; }
        catch (DbUpdateException) { return TemplateWorkflowErrors.Conflict; }
        context.ChangeTracker.Clear();
        var saved = await RevisionQuery().AsNoTracking().SingleAsync(x => x.Id == revisionId, token);
        return TemplateWorkflowServiceSupport.ToDto(saved);
    }

    private static TemplateWorkflowDto ToWorkflowDto(TemplateWorkflow item)
    {
        var latest = item.Revisions.OrderByDescending(x => x.Sequence).FirstOrDefault();
        return new TemplateWorkflowDto(item.Id, item.TemplateAreaId, item.TemplateArea.Name,
            item.PurposeId, item.SubjectTypeId,
            latest is null ? null : TemplateWorkflowServiceSupport.ToDto(latest));
    }
}
