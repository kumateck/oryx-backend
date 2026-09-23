using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DOMAIN.Entities.FullProcedures;

namespace APP.Services.FullProcedures;

internal sealed record ResolvedTemplateRevision(Guid AreaId, string PurposeId,
    string SubjectTypeId, string ContentHash, bool IsPublished);

internal static class TemplateSharingServiceSupport
{
    internal static bool HasReason(string value) =>
        value?.Trim().Length is >= 10 and <= 4000;

    internal static TemplateSharingGrantAudit Audit(TemplateSharingGrant grant,
        TemplateSharingGrantStatus? prior, string action, string reason,
        Guid actorId, Guid correlationId)
    {
        var snapshot = SnapshotJson(grant);
        return new TemplateSharingGrantAudit
        {
            Id = Guid.NewGuid(), TemplateSharingGrantId = grant.Id,
            PriorStatus = prior, NewStatus = grant.Status, Action = action,
            Reason = reason.Trim(), Version = grant.Version, SnapshotJson = snapshot,
            SnapshotHash = Hash(snapshot), ActorId = actorId,
            OccurredAt = DateTime.UtcNow, CorrelationId = correlationId,
        };
    }

    internal static TemplateSharingGrantDto ToDto(TemplateSharingGrant grant) => new(
        grant.Id, grant.SourceAreaId, grant.SourceArea.Name,
        grant.TargetAreaId, grant.TargetArea.Name, grant.RequestedByAreaId,
        grant.TemplateKind, grant.DefinitionId, grant.RevisionId,
        grant.RevisionContentHash, grant.PurposeId, grant.SubjectTypeId,
        grant.Status, grant.Version, grant.RequestedById, grant.RequestedAt,
        grant.DecidedById, grant.DecidedAt, grant.RevokedById, grant.RevokedAt);

    private static string SnapshotJson(TemplateSharingGrant grant) =>
        JsonSerializer.Serialize(new
        {
            grant.Id, grant.SourceAreaId, grant.TargetAreaId,
            grant.RequestedByAreaId, grant.TemplateKind, grant.DefinitionId,
            grant.RevisionId, grant.RevisionContentHash, grant.PurposeId,
            grant.SubjectTypeId, grant.Status, grant.Version,
            grant.RequestedById, grant.RequestedAt, grant.DecidedById,
            grant.DecidedAt, grant.RevokedById, grant.RevokedAt,
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    private static string Hash(string value) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
