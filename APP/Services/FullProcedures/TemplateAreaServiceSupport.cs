using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DOMAIN.Entities.FullProcedures;

namespace APP.Services.FullProcedures;

internal static class TemplateAreaServiceSupport
{
    internal static bool HasReason(string reason) =>
        !string.IsNullOrWhiteSpace(reason) && reason.Trim().Length <= 2000;

    internal static string NormalizeName(string name) => name.Trim().ToUpperInvariant();

    internal static TemplateAreaDraft ToDraft(TemplateAreaDraftRequest request) => new(
        request.Name, request.OwnerRoleId, request.PurposeIds, request.SubjectTypeIds,
        request.CapabilityIds, request.ReviewPolicyId);

    internal static void ReplaceBindings(TemplateArea area, TemplateAreaDraftRequest request)
    {
        area.Purposes.Clear();
        area.SubjectTypes.Clear();
        area.Capabilities.Clear();
        area.RoleGrants.Clear();
        area.Purposes.AddRange(request.PurposeIds.Select(id => new TemplateAreaPurpose
            { Id = Guid.NewGuid(), TemplateAreaId = area.Id, PurposeId = id }));
        area.SubjectTypes.AddRange(request.SubjectTypeIds.Select(id => new TemplateAreaSubjectType
            { Id = Guid.NewGuid(), TemplateAreaId = area.Id, SubjectTypeId = id }));
        area.Capabilities.AddRange(request.CapabilityIds.Select(id => new TemplateAreaCapability
            { Id = Guid.NewGuid(), TemplateAreaId = area.Id, CapabilityId = id }));
        area.RoleGrants.AddRange(request.RoleGrants.Select(grant => new TemplateAreaRoleGrant
        {
            Id = Guid.NewGuid(), TemplateAreaId = area.Id,
            RoleId = grant.RoleId, AccessLevel = grant.AccessLevel,
        }));
    }

    internal static TemplateAreaAudit CreateAudit(
        TemplateArea area, Guid actorId, string action, string reason)
    {
        var snapshot = JsonSerializer.Serialize(new
        {
            area.Id, area.Name, area.OwnerRoleId,
            purposeIds = area.Purposes.Select(item => item.PurposeId).Order().ToArray(),
            subjectTypeIds = area.SubjectTypes.Select(item => item.SubjectTypeId).Order().ToArray(),
            capabilityIds = area.Capabilities.Select(item => item.CapabilityId).Order().ToArray(),
            area.ReviewPolicyId,
            roleGrants = area.RoleGrants.OrderBy(item => item.RoleId).Select(item => new
                { item.RoleId, accessLevel = (int)item.AccessLevel }).ToArray(),
            area.IsActive, area.Version,
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new TemplateAreaAudit
        {
            Id = Guid.NewGuid(), TemplateAreaId = area.Id, ActorId = actorId,
            Action = action, Reason = reason.Trim(), Version = area.Version,
            SnapshotJson = snapshot,
            SnapshotHash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(snapshot))).ToLowerInvariant(),
            OccurredAt = DateTime.UtcNow,
        };
    }

    internal static TemplateAreaDto ToDto(TemplateArea area) => new(
        area.Id, area.Name, area.OwnerRoleId,
        area.OwnerRole?.DisplayName ?? area.OwnerRole?.Name ?? area.OwnerRoleId.ToString(),
        area.Purposes.Select(item => item.PurposeId).Order().ToArray(),
        area.SubjectTypes.Select(item => item.SubjectTypeId).Order().ToArray(),
        area.Capabilities.Select(item => item.CapabilityId).Order().ToArray(),
        area.ReviewPolicyId,
        area.RoleGrants.OrderBy(item => item.Role?.DisplayName ?? item.Role?.Name)
            .Select(item => new TemplateAreaRoleGrantDto(item.RoleId,
                item.Role?.DisplayName ?? item.Role?.Name ?? item.RoleId.ToString(), item.AccessLevel)).ToArray(),
        area.IsActive, area.Version, area.CreatedAt, area.UpdatedAt);
}
