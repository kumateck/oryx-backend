using APP.Services.Formulas;
using APP.Services.FullProcedures;
using DOMAIN.Entities.Formulas;
using DOMAIN.Entities.FullProcedures;
using DOMAIN.Entities.QcWorksheets;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.Approvals;

public sealed record PendingRevisionApprovalDto(
    Guid RevisionId, Guid ResourceId, string ResourceType, string Action,
    string Label, string ContentHash, DateTime CreatedAt, string ResourcePath);

public sealed record PendingRevisionPermissions(
    bool FormulaReview, bool FormulaApprove, bool TemplateReview,
    bool TemplatePublish, bool ProcedureReview, bool ProcedureApprove,
    bool OosDisposition);

public static class PendingRevisionApprovals
{
    public static async Task<IReadOnlyList<PendingRevisionApprovalDto>> GetAsync(
        ApplicationDbContext context, Guid actorId, IReadOnlyCollection<Guid> roleIds,
        PendingRevisionPermissions permissions, CancellationToken cancellationToken,
        string? resourceType = null)
    {
        var rows = new List<PendingRevisionApprovalDto>();
        if (resourceType is null or "FormulaRevision" &&
            (permissions.FormulaReview || permissions.FormulaApprove))
            await AddFormulasAsync(context, actorId, permissions, rows, cancellationToken);
        if (permissions.TemplateReview || permissions.TemplatePublish)
            await AddTemplatesAsync(context, actorId, roleIds, permissions, rows, cancellationToken, resourceType);
        if (resourceType is null or "ProcedureRevision" &&
            (permissions.ProcedureReview || permissions.ProcedureApprove))
            await AddProceduresAsync(context, actorId, roleIds, permissions, rows, cancellationToken);
        if (resourceType is null or "QcOosCase" && permissions.OosDisposition)
            await AddUnconfiguredOosAsync(context, rows, cancellationToken);
        return rows.OrderByDescending(item => item.CreatedAt).ToArray();
    }

    private static async Task AddFormulasAsync(ApplicationDbContext context, Guid actorId,
        PendingRevisionPermissions permissions, List<PendingRevisionApprovalDto> rows,
        CancellationToken cancellationToken)
    {
        var canReview = permissions.FormulaReview &&
            await FormulaApprovalAssignment.AllowsAsync(context, actorId, false, cancellationToken);
        var canApprove = permissions.FormulaApprove &&
            await FormulaApprovalAssignment.AllowsAsync(context, actorId, true, cancellationToken);
        if (!canReview && !canApprove) return;
        var candidates = await context.Set<FormulaRevision>().AsNoTracking()
            .Where(item => item.Status == FormulaRevisionStatus.InReview)
            .Join(context.Set<QuestionFormulaDefinition>().AsNoTracking(),
                revision => revision.FormulaDefinitionId,
                link => link.FormulaDefinitionId,
                (revision, link) => new { revision, link.QuestionId, link.Question.Label })
            .ToListAsync(cancellationToken);
        foreach (var item in candidates)
        {
            var revision = item.revision;
            if (revision.CreatedById == actorId) continue;
            var action = revision.ReviewedById.HasValue ? "approve" : "review";
            if (action == "review" && !canReview ||
                action == "approve" && (!canApprove || revision.ReviewedById == actorId))
                continue;
            rows.Add(new PendingRevisionApprovalDto(revision.Id, item.QuestionId,
                "FormulaRevision", action, item.Label, revision.DefinitionHash,
                revision.CreatedAt,
                $"qa/formula-{(action == "review" ? "reviews" : "approvals")}/{item.QuestionId}?revisionId={revision.Id}"));
        }
    }

    private static void AddTemplate(List<PendingRevisionApprovalDto> rows, Guid actorId,
        IReadOnlyCollection<Guid> roleIds, PendingRevisionPermissions permissions,
        TemplateArea area, Guid revisionId, Guid resourceId, Guid? createdById,
        Guid? reviewedById, string kind, string label, string hash, DateTime createdAt,
        string path)
    {
        if (!area.IsActive || createdById == actorId) return;
        var action = reviewedById.HasValue ? "publish" : "review";
        var operation = reviewedById.HasValue
            ? TemplateDefinitionOperation.Publish : TemplateDefinitionOperation.Review;
        if (action == "review" && !permissions.TemplateReview ||
            action == "publish" && (!permissions.TemplatePublish ||
                area.ReviewPolicyId == "regulated-three-person" && reviewedById == actorId) ||
            !TemplateDefinitionAuthorization.Allows(area, roleIds, operation)) return;
        rows.Add(new PendingRevisionApprovalDto(revisionId, resourceId, kind, action,
            label, hash, createdAt, path));
    }

    private static async Task AddTemplatesAsync(ApplicationDbContext context, Guid actorId,
        IReadOnlyCollection<Guid> roleIds, PendingRevisionPermissions permissions,
        List<PendingRevisionApprovalDto> rows, CancellationToken token,
        string? resourceType)
    {
        if (resourceType is not null && resourceType is not ("TemplateQuestionRevision" or
            "TemplateSectionRevision" or "TemplateFormRevision" or
            "TemplateActivityRevision" or "TemplateWorkflowRevision")) return;
        if (resourceType is null or "TemplateQuestionRevision")
        {
        var questions = await context.Set<TemplateQuestionRevision>().AsNoTracking()
            .Include(x => x.TemplateQuestion).ThenInclude(x => x.TemplateArea)
            .ThenInclude(x => x.RoleGrants)
            .Where(x => x.Status == TemplateQuestionRevisionStatus.InReview).ToListAsync(token);
        foreach (var x in questions)
            AddTemplate(rows, actorId, roleIds, permissions, x.TemplateQuestion.TemplateArea,
                x.Id, x.TemplateQuestionId, x.CreatedById, x.ReviewedById,
                "TemplateQuestionRevision", x.Wording, x.ContentHash, x.CreatedAt,
                $"settings/full-procedures/templates/questions/{x.TemplateQuestionId}/review?revisionId={x.Id}");

        }

        if (resourceType is null or "TemplateSectionRevision")
        {
        var sections = await context.Set<TemplateSectionRevision>().AsNoTracking()
            .Include(x => x.TemplateSection).ThenInclude(x => x.TemplateArea)
            .ThenInclude(x => x.RoleGrants)
            .Where(x => x.Status == TemplateSectionRevisionStatus.InReview).ToListAsync(token);
        foreach (var x in sections)
            AddTemplate(rows, actorId, roleIds, permissions, x.TemplateSection.TemplateArea,
                x.Id, x.TemplateSectionId, x.CreatedById, x.ReviewedById,
                "TemplateSectionRevision", x.Title, x.ContentHash, x.CreatedAt,
                $"settings/full-procedures/templates/sections/{x.TemplateSectionId}/review?revisionId={x.Id}");

        }

        if (resourceType is null or "TemplateFormRevision")
        {
        var forms = await context.Set<TemplateFormRevision>().AsNoTracking()
            .Include(x => x.TemplateForm).ThenInclude(x => x.TemplateArea)
            .ThenInclude(x => x.RoleGrants)
            .Where(x => x.Status == TemplateFormRevisionStatus.InReview).ToListAsync(token);
        foreach (var x in forms)
            AddTemplate(rows, actorId, roleIds, permissions, x.TemplateForm.TemplateArea,
                x.Id, x.TemplateFormId, x.CreatedById, x.ReviewedById,
                "TemplateFormRevision", x.Name, x.ContentHash, x.CreatedAt,
                $"settings/full-procedures/templates/forms/{x.TemplateFormId}/review?revisionId={x.Id}");

        }

        if (resourceType is null or "TemplateActivityRevision")
        {
        var activities = await context.Set<TemplateActivityRevision>().AsNoTracking()
            .Include(x => x.TemplateActivity).ThenInclude(x => x.TemplateArea)
            .ThenInclude(x => x.RoleGrants)
            .Where(x => x.Status == TemplateActivityRevisionStatus.InReview).ToListAsync(token);
        foreach (var x in activities)
            AddTemplate(rows, actorId, roleIds, permissions, x.TemplateActivity.TemplateArea,
                x.Id, x.TemplateActivityId, x.CreatedById, x.ReviewedById,
                "TemplateActivityRevision", x.Name, x.ContentHash, x.CreatedAt,
                $"settings/full-procedures/templates/activities/{x.TemplateActivityId}/review?revisionId={x.Id}");

        }

        if (resourceType is null or "TemplateWorkflowRevision")
        {
        var workflows = await context.Set<TemplateWorkflowRevision>().AsNoTracking()
            .Include(x => x.TemplateWorkflow).ThenInclude(x => x.TemplateArea)
            .ThenInclude(x => x.RoleGrants)
            .Where(x => x.Status == TemplateWorkflowRevisionStatus.InReview).ToListAsync(token);
        foreach (var x in workflows)
            AddTemplate(rows, actorId, roleIds, permissions, x.TemplateWorkflow.TemplateArea,
                x.Id, x.TemplateWorkflowId, x.CreatedById, x.ReviewedById,
                "TemplateWorkflowRevision", x.Name, x.ContentHash, x.CreatedAt,
                $"settings/full-procedures/workflows/{x.TemplateWorkflowId}/review?revisionId={x.Id}");
        }
    }

    private static async Task AddProceduresAsync(ApplicationDbContext context, Guid actorId,
        IReadOnlyCollection<Guid> roleIds, PendingRevisionPermissions permissions,
        List<PendingRevisionApprovalDto> rows, CancellationToken token)
    {
        var revisions = await context.Set<ProcedureRevision>().AsNoTracking()
            .Include(x => x.ProcedureDefinition).ThenInclude(x => x.TemplateArea)
            .ThenInclude(x => x.RoleGrants)
            .Where(x => x.Status == ProcedureRevisionStatus.InReview).ToListAsync(token);
        foreach (var x in revisions)
        {
            var area = x.ProcedureDefinition.TemplateArea;
            if (!area.IsActive || x.CreatedById == actorId) continue;
            var action = x.ReviewedById.HasValue ? "approve" : "review";
            var operation = x.ReviewedById.HasValue
                ? TemplateDefinitionOperation.Publish : TemplateDefinitionOperation.Review;
            if (action == "review" && !permissions.ProcedureReview ||
                action == "approve" && (!permissions.ProcedureApprove ||
                    area.ReviewPolicyId == "regulated-three-person" && x.ReviewedById == actorId) ||
                !TemplateDefinitionAuthorization.Allows(area, roleIds, operation)) continue;
            rows.Add(new PendingRevisionApprovalDto(x.Id, x.ProcedureDefinitionId,
                "ProcedureRevision", action, x.Name, x.ContentHash, x.CreatedAt,
                $"settings/full-procedures/procedures/{x.ProcedureDefinitionId}/review?revisionId={x.Id}"));
        }
    }
    private static async Task AddUnconfiguredOosAsync(ApplicationDbContext context,
        List<PendingRevisionApprovalDto> rows, CancellationToken token)
    {
        var configured = await context.ApprovalStages.AsNoTracking().AnyAsync(
            item => item.Approval.ItemType == QcWorksheetModelTypes.OosCase, token);
        if (configured) return;
        var cases = await context.Set<OosCase>().AsNoTracking()
            .Where(item => item.Status == OosCaseStatus.PendingQaDisposition)
            .Select(item => new { item.Id, item.FieldKey, item.CreatedAt })
            .ToListAsync(token);
        foreach (var item in cases)
            rows.Add(new PendingRevisionApprovalDto(item.Id, item.Id,
                "QcOosCase", "disposition", item.FieldKey, string.Empty,
                item.CreatedAt, $"qc/worksheets/oos-cases/{item.Id}"));
    }
}
