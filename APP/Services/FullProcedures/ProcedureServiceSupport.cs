using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DOMAIN.Entities.FullProcedures;

#nullable enable

namespace APP.Services.FullProcedures;

internal sealed record ProcedureApplicabilityContent(Guid ProductId, string ProductName,
    Guid SiteId, string SiteName, ProcedureBatchType BatchType);
internal sealed record ProcedureStageScopeContent(Guid NodeId, string NodeKey, string NodeName,
    int NodeOrder, ProcedureRecordScope RecordScope);
internal sealed record ProcedureContent(string Name, string Description,
    Guid AreaId, string PurposeId, string SubjectTypeId,
    Guid WorkflowId, Guid WorkflowRevisionId, string WorkflowName, string WorkflowContentHash,
    string ParameterSchemaJson, IReadOnlyList<ProcedureApplicabilityContent> Applicabilities,
    IReadOnlyList<ProcedureStageScopeContent> StageScopes);

internal static class ProcedureServiceSupport
{
    internal static bool HasReason(string? value) => value?.Trim().Length is >= 10 and <= 4000;

    internal static void Apply(ProcedureRevision revision, ProcedureContent content)
    {
        revision.Name = content.Name;
        revision.Description = content.Description;
        revision.TemplateWorkflowId = content.WorkflowId;
        revision.TemplateWorkflowRevisionId = content.WorkflowRevisionId;
        revision.TemplateWorkflowName = content.WorkflowName;
        revision.TemplateWorkflowContentHash = content.WorkflowContentHash;
        revision.ParameterSchemaJson = content.ParameterSchemaJson;
        revision.Applicabilities = content.Applicabilities.Select(x => new ProcedureApplicability
        {
            Id = Guid.NewGuid(), ProcedureRevisionId = revision.Id,
            ProductId = x.ProductId, SiteId = x.SiteId, BatchType = x.BatchType,
        }).ToList();
        revision.StageScopes = content.StageScopes.Select(x => new ProcedureStageScope
        {
            Id = Guid.NewGuid(), ProcedureRevisionId = revision.Id,
            TemplateWorkflowRevisionId = content.WorkflowRevisionId,
            TemplateWorkflowNodeId = x.NodeId, WorkflowNodeKey = x.NodeKey,
            WorkflowNodeName = x.NodeName, WorkflowNodeOrder = x.NodeOrder,
            RecordScope = x.RecordScope,
        }).ToList();
        revision.ContentHash = Hash(SnapshotJson(revision.ProcedureDefinitionId, content));
    }

    internal static ProcedureRevisionAudit Audit(ProcedureRevision revision,
        ProcedureRevisionStatus? prior, string action, string reason,
        Guid actorId, Guid correlationId) => new()
        {
            Id = Guid.NewGuid(), ProcedureRevisionId = revision.Id,
            PriorStatus = prior, NewStatus = revision.Status, Action = action,
            Reason = reason.Trim(), ContentHash = revision.ContentHash,
            SnapshotJson = SnapshotJson(revision), ActorId = actorId,
            OccurredAt = DateTime.UtcNow, CorrelationId = correlationId,
        };

    internal static ProcedureRevisionDto ToDto(ProcedureRevision revision) => new(
        revision.Id, revision.ProcedureDefinitionId, revision.Sequence, revision.Status,
        revision.Name, revision.Description, revision.TemplateWorkflowId,
        revision.TemplateWorkflowRevisionId, revision.TemplateWorkflowName,
        revision.TemplateWorkflowContentHash, revision.ParameterSchemaJson,
        revision.Applicabilities.OrderBy(x => x.Product.Name).ThenBy(x => x.Site.Name)
            .ThenBy(x => x.BatchType).Select(x => new ProcedureApplicabilityDto(
                x.ProductId, x.Product.Name, x.SiteId, x.Site.Name, x.BatchType)).ToArray(),
        revision.StageScopes.OrderBy(x => x.WorkflowNodeOrder)
            .Select(x => new ProcedureStageScopeDto(x.WorkflowNodeKey,
                x.WorkflowNodeName, x.RecordScope)).ToArray(),
        revision.ContentHash, revision.CreatedById, revision.ReviewedById,
        revision.ReviewedAt, revision.ApprovedById, revision.ApprovedAt, revision.RetiredAt);

    private static string SnapshotJson(ProcedureRevision revision)
    {
        var content = new ProcedureContent(revision.Name, revision.Description,
            revision.ProcedureDefinition.TemplateAreaId, revision.ProcedureDefinition.PurposeId,
            revision.ProcedureDefinition.SubjectTypeId,
            revision.TemplateWorkflowId, revision.TemplateWorkflowRevisionId,
            revision.TemplateWorkflowName, revision.TemplateWorkflowContentHash,
            revision.ParameterSchemaJson,
            revision.Applicabilities.Select(x => new ProcedureApplicabilityContent(
                x.ProductId, x.Product?.Name ?? string.Empty, x.SiteId,
                x.Site?.Name ?? string.Empty, x.BatchType)).ToArray(),
            revision.StageScopes.Select(x => new ProcedureStageScopeContent(
                x.TemplateWorkflowNodeId, x.WorkflowNodeKey, x.WorkflowNodeName,
                x.WorkflowNodeOrder, x.RecordScope)).ToArray());
        return SnapshotJson(revision.ProcedureDefinitionId, content);
    }

    private static string SnapshotJson(Guid definitionId, ProcedureContent content) =>
        JsonSerializer.Serialize(new
        {
            definitionId, content.Name, content.Description, content.AreaId,
            content.PurposeId, content.SubjectTypeId,
            templateWorkflowId = content.WorkflowId,
            templateWorkflowRevisionId = content.WorkflowRevisionId,
            workflowContentHash = content.WorkflowContentHash,
            parameterSchema = JsonSerializer.Deserialize<JsonElement>(content.ParameterSchemaJson),
            applicabilities = content.Applicabilities
                .OrderBy(x => x.ProductId).ThenBy(x => x.SiteId).ThenBy(x => x.BatchType)
                .Select(x => new { x.ProductId, x.SiteId, x.BatchType }),
            stageScopes = content.StageScopes.OrderBy(x => x.NodeKey, StringComparer.OrdinalIgnoreCase)
                .Select(x => new { x.NodeId, x.NodeKey, x.RecordScope }),
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    private static string Hash(string value) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
