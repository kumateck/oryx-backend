using DOMAIN.Entities.FullProcedures;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Sites;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable

namespace APP.Services.FullProcedures;

public sealed partial class ProcedureService(ApplicationDbContext context) : IProcedureService
{
    private IQueryable<TemplateArea> AreaQuery() => context.Set<TemplateArea>()
        .Include(x => x.RoleGrants);

    private IQueryable<ProcedureDefinition> DefinitionQuery() =>
        context.Set<ProcedureDefinition>().AsSplitQuery()
            .Include(x => x.TemplateArea).ThenInclude(x => x.RoleGrants)
            .Include(x => x.Revisions).ThenInclude(x => x.TemplateWorkflowRevision)
            .Include(x => x.Revisions).ThenInclude(x => x.Applicabilities).ThenInclude(x => x.Product)
            .Include(x => x.Revisions).ThenInclude(x => x.Applicabilities).ThenInclude(x => x.Site)
            .Include(x => x.Revisions).ThenInclude(x => x.StageScopes)
                .ThenInclude(x => x.TemplateWorkflowNode);

    private IQueryable<ProcedureRevision> RevisionQuery() =>
        context.Set<ProcedureRevision>().AsSplitQuery()
            .Include(x => x.ProcedureDefinition).ThenInclude(x => x.TemplateArea)
                .ThenInclude(x => x.RoleGrants)
            .Include(x => x.TemplateWorkflowRevision).ThenInclude(x => x.TemplateWorkflow)
            .Include(x => x.Applicabilities).ThenInclude(x => x.Product)
            .Include(x => x.Applicabilities).ThenInclude(x => x.Site)
            .Include(x => x.StageScopes).ThenInclude(x => x.TemplateWorkflowNode);

    private IQueryable<TemplateWorkflowRevision> WorkflowRevisionQuery() =>
        context.Set<TemplateWorkflowRevision>().AsSplitQuery()
            .Include(x => x.TemplateWorkflow).ThenInclude(x => x.TemplateArea)
                .ThenInclude(x => x.RoleGrants)
            .Include(x => x.Nodes);

    private Task<ProcedureRevision?> LoadRevisionAsync(Guid id, CancellationToken token) =>
        RevisionQuery().SingleOrDefaultAsync(x => x.Id == id, token);

    private async Task<ProcedureContent?> ResolveContentAsync(ProcedureContentRequest request,
        Guid? requiredAreaId, string? requiredPurposeId, string? requiredSubjectTypeId,
        CancellationToken token)
    {
        if (!ValidShape(request)) return null;
        var workflow = await WorkflowRevisionQuery().AsNoTracking().SingleOrDefaultAsync(x =>
            x.Id == request.TemplateWorkflowRevisionId &&
            x.TemplateWorkflowId == request.TemplateWorkflowId &&
            x.Status == TemplateWorkflowRevisionStatus.Published, token);
        if (workflow is null || !workflow.TemplateWorkflow.TemplateArea.IsActive ||
            !IsProcedurePurpose(workflow.TemplateWorkflow.PurposeId)) return null;
        if (workflow.TemplateWorkflow.PurposeId == "production-procedure" &&
            request.StageScopes.Any(x => x.RecordScope == ProcedureRecordScope.Development))
            return null;
        if (requiredAreaId.HasValue && (workflow.TemplateWorkflow.TemplateAreaId != requiredAreaId ||
            workflow.TemplateWorkflow.PurposeId != requiredPurposeId ||
            workflow.TemplateWorkflow.SubjectTypeId != requiredSubjectTypeId)) return null;

        string schema;
        try { schema = ProcedureCanonicalJson.Object(request.ParameterSchemaJson); }
        catch (ArgumentException) { return null; }
        catch (System.Text.Json.JsonException) { return null; }

        var productIds = request.Applicabilities.Select(x => x.ProductId).Distinct().ToArray();
        var siteIds = request.Applicabilities.Select(x => x.SiteId).Distinct().ToArray();
        var products = await context.Set<Product>().AsNoTracking()
            .Where(x => productIds.Contains(x.Id) && !x.DeletedAt.HasValue)
            .Select(x => new { x.Id, x.Name }).ToListAsync(token);
        var sites = await context.Set<Site>().AsNoTracking()
            .Where(x => siteIds.Contains(x.Id) && !x.DeletedAt.HasValue)
            .Select(x => new { x.Id, x.Name }).ToListAsync(token);
        if (products.Count != productIds.Length || sites.Count != siteIds.Length) return null;
        var productNames = products.ToDictionary(x => x.Id, x => x.Name);
        var siteNames = sites.ToDictionary(x => x.Id, x => x.Name);

        var activityNodes = workflow.Nodes.Where(x => x.NodeType == TemplateWorkflowNodeType.Activity)
            .ToArray();
        var nodeByKey = activityNodes.ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
        if (request.StageScopes.Count != activityNodes.Length || request.StageScopes.Any(x =>
                !nodeByKey.ContainsKey(x.WorkflowNodeKey.Trim()))) return null;
        var content = new ProcedureContent(request.Name.Trim(), request.Description.Trim(),
            workflow.TemplateWorkflow.TemplateAreaId, workflow.TemplateWorkflow.PurposeId,
            workflow.TemplateWorkflow.SubjectTypeId, workflow.TemplateWorkflowId, workflow.Id,
            workflow.Name, workflow.ContentHash, schema,
            request.Applicabilities.Select(x => new ProcedureApplicabilityContent(x.ProductId,
                productNames[x.ProductId], x.SiteId, siteNames[x.SiteId], x.BatchType)).ToArray(),
            request.StageScopes.Select(x => new ProcedureStageScopeContent(
                nodeByKey[x.WorkflowNodeKey.Trim()].Id, nodeByKey[x.WorkflowNodeKey.Trim()].Key,
                nodeByKey[x.WorkflowNodeKey.Trim()].Name,
                nodeByKey[x.WorkflowNodeKey.Trim()].Order, x.RecordScope)).ToArray());
        return content;
    }

    private static bool ValidShape(ProcedureContentRequest request) =>
        request.Name.Trim().Length is >= 3 and <= 150 && request.Description.Trim().Length <= 4000 &&
        request.TemplateWorkflowId != Guid.Empty && request.TemplateWorkflowRevisionId != Guid.Empty &&
        request.Applicabilities.Count > 0 && request.StageScopes.Count > 0 &&
        request.Applicabilities.All(x => x.ProductId != Guid.Empty && x.SiteId != Guid.Empty &&
            Enum.IsDefined(x.BatchType)) &&
        request.Applicabilities.DistinctBy(x => (x.ProductId, x.SiteId, x.BatchType)).Count() ==
            request.Applicabilities.Count &&
        request.StageScopes.All(x => !string.IsNullOrWhiteSpace(x.WorkflowNodeKey) &&
            Enum.IsDefined(x.RecordScope)) &&
        request.StageScopes.Select(x => x.WorkflowNodeKey.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase).Count() == request.StageScopes.Count;

    private static bool IsProcedurePurpose(string purposeId) =>
        purposeId is "production-procedure" or "research-development";

    private async Task<bool> DependenciesAvailableAsync(ProcedureRevision revision,
        CancellationToken token)
    {
        var workflow = await context.Set<TemplateWorkflowRevision>().AsNoTracking()
            .Include(x => x.TemplateWorkflow).Include(x => x.Nodes)
            .SingleOrDefaultAsync(x => x.Id == revision.TemplateWorkflowRevisionId &&
                x.TemplateWorkflowId == revision.TemplateWorkflowId &&
                x.Status == TemplateWorkflowRevisionStatus.Published, token);
        if (workflow is null || workflow.ContentHash != revision.TemplateWorkflowContentHash ||
            workflow.TemplateWorkflow.TemplateAreaId != revision.ProcedureDefinition.TemplateAreaId)
            return false;
        var nodeIds = workflow.Nodes.Where(x => x.NodeType == TemplateWorkflowNodeType.Activity)
            .Select(x => x.Id).Order().ToArray();
        var mappedIds = await context.Set<ProcedureStageScope>().AsNoTracking()
            .Where(x => x.ProcedureRevisionId == revision.Id)
            .Select(x => x.TemplateWorkflowNodeId).OrderBy(x => x).ToArrayAsync(token);
        if (!nodeIds.SequenceEqual(mappedIds)) return false;
        var applicability = await context.Set<ProcedureApplicability>().AsNoTracking()
            .Where(x => x.ProcedureRevisionId == revision.Id)
            .Select(x => new { x.ProductId, x.SiteId }).ToListAsync(token);
        if (applicability.Count == 0) return false;
        var productIds = applicability.Select(x => x.ProductId).Distinct().ToArray();
        var siteIds = applicability.Select(x => x.SiteId).Distinct().ToArray();
        return await context.Set<Product>().CountAsync(x => productIds.Contains(x.Id) &&
                   !x.DeletedAt.HasValue, token) == productIds.Length &&
               await context.Set<Site>().CountAsync(x => siteIds.Contains(x.Id) &&
                   !x.DeletedAt.HasValue, token) == siteIds.Length;
    }

    private async Task<Result<ProcedureRevisionDto>> SaveRevisionAsync(Guid revisionId,
        CancellationToken token)
    {
        try { await context.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException) { return ProcedureErrors.Conflict; }
        catch (DbUpdateException) { return ProcedureErrors.Conflict; }
        context.ChangeTracker.Clear();
        var saved = await RevisionQuery().AsNoTracking().SingleAsync(x => x.Id == revisionId, token);
        return ProcedureServiceSupport.ToDto(saved);
    }
}
