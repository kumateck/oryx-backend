using DOMAIN.Entities.FullProcedures;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable

namespace APP.Services.FullProcedures;

public sealed partial class TemplateActivityService(ApplicationDbContext context)
    : ITemplateActivityService
{
    private IQueryable<TemplateArea> AreaQuery() => context.Set<TemplateArea>()
        .Include(x => x.RoleGrants).Include(x => x.Purposes)
        .Include(x => x.SubjectTypes).Include(x => x.Capabilities);

    private IQueryable<TemplateActivity> ActivityQuery() => context.Set<TemplateActivity>()
        .AsSplitQuery().Include(x => x.TemplateArea).ThenInclude(x => x.RoleGrants)
        .Include(x => x.TemplateArea).ThenInclude(x => x.Capabilities)
        .Include(x => x.Revisions).ThenInclude(x => x.Forms).ThenInclude(x => x.TemplateFormRevision)
        .Include(x => x.Revisions).ThenInclude(x => x.Actions).ThenInclude(x => x.Roles)
        .Include(x => x.Revisions).ThenInclude(x => x.Resources)
        .Include(x => x.Revisions).ThenInclude(x => x.DataBindings)
        .Include(x => x.Revisions).ThenInclude(x => x.CompletionRules);

    private IQueryable<TemplateActivityRevision> RevisionQuery() =>
        context.Set<TemplateActivityRevision>().AsSplitQuery()
            .Include(x => x.TemplateActivity).ThenInclude(x => x.TemplateArea)
                .ThenInclude(x => x.RoleGrants)
            .Include(x => x.TemplateActivity).ThenInclude(x => x.TemplateArea)
                .ThenInclude(x => x.Capabilities)
            .Include(x => x.Forms).ThenInclude(x => x.TemplateFormRevision)
            .Include(x => x.Actions).ThenInclude(x => x.Roles)
            .Include(x => x.Resources).Include(x => x.DataBindings)
            .Include(x => x.CompletionRules);

    private Task<TemplateArea?> LoadAreaAsync(Guid id, CancellationToken token) =>
        AreaQuery().SingleOrDefaultAsync(x => x.Id == id, token);
    private Task<TemplateActivityRevision?> LoadRevisionAsync(Guid id, CancellationToken token) =>
        RevisionQuery().SingleOrDefaultAsync(x => x.Id == id, token);

    private async Task<TemplateActivityContent?> ResolveContentAsync(
        TemplateActivityContentRequest request, TemplateArea area, string purposeId,
        string subjectTypeId, CancellationToken token)
    {
        if (!TemplateActivityServiceValidation.ValidShape(request)) return null;
        var revisionIds = request.Forms.Select(x => x.RevisionId).ToArray();
        var forms = await context.Set<TemplateFormRevision>().AsNoTracking()
            .Where(x => revisionIds.Contains(x.Id) &&
                x.Status == TemplateFormRevisionStatus.Published &&
                x.TemplateForm.TemplateAreaId == area.Id &&
                x.TemplateForm.PurposeId == purposeId &&
                x.TemplateForm.SubjectTypeId == subjectTypeId)
            .Select(x => new { x.Id, x.TemplateFormId, x.Name }).ToListAsync(token);
        if (forms.Count != revisionIds.Length) return null;
        var resolved = forms.ToDictionary(x => x.Id, x => (x.TemplateFormId, x.Name));
        if (request.Forms.Any(x => resolved[x.RevisionId].TemplateFormId != x.FormId)) return null;
        var allowedRoles = area.RoleGrants.Select(x => x.RoleId).Append(area.OwnerRoleId).ToHashSet();
        var requestedRoles = request.Actions.SelectMany(x => x.PerformerRoleIds
            .Concat(x.CheckerRoleIds).Concat(x.ApproverRoleIds)).Distinct();
        if (requestedRoles.Any(id => !allowedRoles.Contains(id))) return null;
        var capabilities = area.Capabilities.Select(x => x.CapabilityId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (request.Resources.Any(x => !capabilities.Contains(x.CapabilityId.Trim())) ||
            request.Actions.Any(x => !HasActionCapability(x.ActionType, capabilities))) return null;
        return TemplateActivityServiceSupport.BuildContent(request, resolved);
    }

    private static bool HasActionCapability(TemplateActivityActionType type,
        IReadOnlySet<string> capabilities) => type switch
        {
            TemplateActivityActionType.CaptureEvidence => capabilities.Contains("capture-response"),
            TemplateActivityActionType.ApproveRelease => capabilities.Contains("approval"),
            TemplateActivityActionType.PostTransaction => capabilities.Contains("material-action"),
            _ => true,
        };

    private static bool ValidContext(TemplateArea area, string purposeId, string subjectTypeId) =>
        TemplateAreaCatalogProvider.AllowsTemplateContext(
            area, purposeId, subjectTypeId, TemplateDefinitionKind.Activity);

    private static TemplateActivityRevision NewRevision(Guid activityId, int sequence,
        Guid actorId, TemplateActivityContent content)
    {
        var revision = new TemplateActivityRevision
        {
            Id = Guid.NewGuid(), TemplateActivityId = activityId, Sequence = sequence,
            Status = TemplateActivityRevisionStatus.Draft, CreatedById = actorId,
        };
        TemplateActivityServiceSupport.Apply(revision, content);
        return revision;
    }

    private async Task<bool> ReferencesAvailableAsync(TemplateActivityRevision revision,
        CancellationToken token)
    {
        var formIds = revision.Forms.Select(x => x.TemplateFormRevisionId).ToArray();
        var forms = await context.Set<TemplateFormRevision>().AsNoTracking()
            .Where(x => formIds.Contains(x.Id) && x.Status == TemplateFormRevisionStatus.Published &&
                x.TemplateForm.TemplateAreaId == revision.TemplateActivity.TemplateAreaId &&
                x.TemplateForm.PurposeId == revision.TemplateActivity.PurposeId &&
                x.TemplateForm.SubjectTypeId == revision.TemplateActivity.SubjectTypeId)
            .Select(x => new { x.Id, x.TemplateFormId }).ToListAsync(token);
        var formMap = forms.ToDictionary(x => x.Id, x => x.TemplateFormId);
        if (!revision.Forms.All(x => formMap.TryGetValue(x.TemplateFormRevisionId,
                out var formId) && formId == x.TemplateFormId)) return false;
        var area = revision.TemplateActivity.TemplateArea;
        var allowedRoles = area.RoleGrants.Select(x => x.RoleId).Append(area.OwnerRoleId).ToHashSet();
        if (revision.Actions.SelectMany(x => x.Roles).Any(x => !allowedRoles.Contains(x.RoleId)))
            return false;
        var capabilities = area.Capabilities.Select(x => x.CapabilityId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return revision.Resources.All(x => capabilities.Contains(x.CapabilityId)) &&
               revision.Actions.All(x => HasActionCapability(x.ActionType, capabilities));
    }

    private async Task<Result<TemplateActivityRevisionDto>> SaveRevisionAsync(Guid revisionId,
        CancellationToken token)
    {
        try { await context.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException) { return TemplateActivityErrors.Conflict; }
        catch (DbUpdateException) { return TemplateActivityErrors.Conflict; }
        context.ChangeTracker.Clear();
        var saved = await RevisionQuery().AsNoTracking().SingleAsync(x => x.Id == revisionId, token);
        return TemplateActivityServiceSupport.ToDto(saved);
    }

    private static TemplateActivityDto ToActivityDto(TemplateActivity item)
    {
        var latest = item.Revisions.OrderByDescending(x => x.Sequence).FirstOrDefault();
        return new TemplateActivityDto(item.Id, item.TemplateAreaId, item.TemplateArea.Name,
            item.PurposeId, item.SubjectTypeId,
            latest is null ? null : TemplateActivityServiceSupport.ToDto(latest));
    }
}
