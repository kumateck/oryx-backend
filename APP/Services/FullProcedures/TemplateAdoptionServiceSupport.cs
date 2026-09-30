using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DOMAIN.Entities.FullProcedures;

namespace APP.Services.FullProcedures;

internal sealed record AdoptedDraft(Guid DefinitionId, Guid RevisionId, string ContentHash);
internal sealed record AdoptionDraftRequest(TemplateRevisionKind Kind, object Request);

internal static class TemplateAdoptionServiceSupport
{
    internal static bool Valid(AdoptTemplateRevisionRequest request)
    {
        if (request is null || request.ExpectedGrantVersion < 1 ||
            request.Reason?.Trim().Length is not (>= 10 and <= 4000) ||
            request.DependencyMappings is null || request.RoleMappings is null)
            return false;
        return ValidDependencies(request.DependencyMappings) && ValidRoles(request.RoleMappings);
    }

    internal static TemplateAdoption New(TemplateSharingGrant grant, AdoptedDraft draft,
        AdoptTemplateRevisionRequest request, Guid actorId, Guid correlationId)
    {
        var dependencies = JsonSerializer.Serialize(request.DependencyMappings
            .OrderBy(x => x.SourceRevisionId).Select(ToDto).ToArray());
        var roles = JsonSerializer.Serialize(request.RoleMappings
            .OrderBy(x => x.SourceRoleId).Select(ToDto).ToArray());
        var item = new TemplateAdoption
        {
            Id = Guid.NewGuid(), TemplateSharingGrantId = grant.Id,
            TemplateKind = grant.TemplateKind, SourceDefinitionId = grant.DefinitionId,
            SourceRevisionId = grant.RevisionId, SourceContentHash = grant.RevisionContentHash,
            GrantVersion = grant.Version, TargetAreaId = grant.TargetAreaId,
            TargetDefinitionId = draft.DefinitionId, TargetRevisionId = draft.RevisionId,
            TargetContentHash = draft.ContentHash, DependencyMappingsJson = dependencies,
            RoleMappingsJson = roles, Reason = request.Reason.Trim(), AdoptedById = actorId,
            AdoptedAt = DateTime.UtcNow, CorrelationId = correlationId, CreatedById = actorId,
        };
        item.SnapshotHash = Hash(Snapshot(item));
        return item;
    }

    internal static TemplateAdoptionDto ToDto(TemplateAdoption item) => new(
        item.Id, item.TemplateSharingGrantId, item.TemplateKind,
        item.SourceDefinitionId, item.SourceRevisionId, item.SourceContentHash,
        item.GrantVersion, item.TargetAreaId, item.TargetDefinitionId,
        item.TargetRevisionId, item.TargetContentHash,
        JsonSerializer.Deserialize<TemplateAdoptionDependencyMappingDto[]>(
            item.DependencyMappingsJson) ?? [],
        JsonSerializer.Deserialize<TemplateAdoptionRoleMappingDto[]>(item.RoleMappingsJson) ?? [],
        item.Reason, item.SnapshotHash, item.AdoptedById, item.AdoptedAt, item.CorrelationId);

    internal static TemplateAdoptionDependencyMappingDto ToDto(
        TemplateAdoptionDependencyMappingRequest item) => new(item.SourceDefinitionId,
            item.SourceRevisionId, item.TargetDefinitionId, item.TargetRevisionId);
    internal static TemplateAdoptionRoleMappingDto ToDto(
        TemplateAdoptionRoleMappingRequest item) => new(item.SourceRoleId, item.TargetRoleId);

    private static bool ValidDependencies(
        IReadOnlyCollection<TemplateAdoptionDependencyMappingRequest> mappings) =>
        mappings.All(x => x.SourceDefinitionId != Guid.Empty &&
            x.SourceRevisionId != Guid.Empty && x.TargetDefinitionId != Guid.Empty &&
            x.TargetRevisionId != Guid.Empty) &&
        mappings.Select(x => (x.SourceDefinitionId, x.SourceRevisionId)).Distinct().Count() ==
            mappings.Count &&
        mappings.Select(x => (x.TargetDefinitionId, x.TargetRevisionId)).Distinct().Count() ==
            mappings.Count;

    private static bool ValidRoles(IReadOnlyCollection<TemplateAdoptionRoleMappingRequest> mappings) =>
        mappings.All(x => x.SourceRoleId != Guid.Empty && x.TargetRoleId != Guid.Empty) &&
        mappings.Select(x => x.SourceRoleId).Distinct().Count() == mappings.Count;

    private static string Snapshot(TemplateAdoption item) => JsonSerializer.Serialize(new
    {
        item.Id, item.TemplateSharingGrantId, item.TemplateKind,
        item.SourceDefinitionId, item.SourceRevisionId, item.SourceContentHash,
        item.GrantVersion, item.TargetAreaId, item.TargetDefinitionId,
        item.TargetRevisionId, item.TargetContentHash, item.DependencyMappingsJson,
        item.RoleMappingsJson, item.Reason, item.AdoptedById, item.AdoptedAt,
        item.CorrelationId,
    }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    private static string Hash(string value) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
