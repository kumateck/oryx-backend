using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DOMAIN.Entities.FullProcedures;

#nullable enable

namespace APP.Services.FullProcedures;

internal sealed record ActivityFormContent(Guid FormId, Guid RevisionId, string Key,
    int Order, TemplateActivityFormUsage Usage, bool IsRequired, string Name);
internal sealed record ActivityActionContent(string Key, string Name, int Order,
    TemplateActivityActionType Type, bool Checker, bool Approval,
    IReadOnlyList<Guid> Performers, IReadOnlyList<Guid> Checkers,
    IReadOnlyList<Guid> Approvers);
internal sealed record ActivityResourceContent(string CapabilityId, int Order, bool IsRequired);
internal sealed record ActivityDataContent(string Key, int Order,
    TemplateActivityDataDirection Direction, TemplateActivityDataType Type, bool IsRequired);
internal sealed record ActivityRuleContent(int Order,
    TemplateActivityCompletionRuleType Type, string? TargetKey);
internal sealed record TemplateActivityContent(string Name, string Instructions,
    IReadOnlyList<ActivityFormContent> Forms, IReadOnlyList<ActivityActionContent> Actions,
    IReadOnlyList<ActivityResourceContent> Resources,
    IReadOnlyList<ActivityDataContent> DataBindings,
    IReadOnlyList<ActivityRuleContent> CompletionRules);

internal static class TemplateActivityServiceSupport
{
    internal static TemplateActivityContent BuildContent(TemplateActivityContentRequest request,
        IReadOnlyDictionary<Guid, (Guid FormId, string Name)> forms)
    {
        var resolvedForms = request.Forms.OrderBy(item => item.Order).Select(item =>
        {
            var form = forms[item.RevisionId];
            return new ActivityFormContent(form.FormId, item.RevisionId, item.Key.Trim(),
                item.Order, item.Usage, item.IsRequired, form.Name);
        }).ToArray();
        var actions = request.Actions.OrderBy(item => item.Order).Select(item =>
            new ActivityActionContent(item.Key.Trim(), item.Name.Trim(), item.Order,
                item.ActionType, item.RequiresIndependentChecker, item.RequiresApproval,
                item.PerformerRoleIds.Order().ToArray(), item.CheckerRoleIds.Order().ToArray(),
                item.ApproverRoleIds.Order().ToArray())).ToArray();
        var resources = request.Resources.OrderBy(item => item.Order).Select(item =>
            new ActivityResourceContent(item.CapabilityId.Trim(), item.Order,
                item.IsRequired)).ToArray();
        var data = request.DataBindings.OrderBy(item => item.Order).Select(item =>
            new ActivityDataContent(item.Key.Trim(), item.Order, item.Direction,
                item.DataType, item.IsRequired)).ToArray();
        var rules = request.CompletionRules.OrderBy(item => item.Order).Select(item =>
            new ActivityRuleContent(item.Order, item.RuleType,
                string.IsNullOrWhiteSpace(item.TargetKey) ? null : item.TargetKey.Trim())).ToArray();
        return new TemplateActivityContent(request.Name.Trim(), request.Instructions.Trim(),
            resolvedForms, actions, resources, data, rules);
    }

    internal static void Apply(TemplateActivityRevision revision, TemplateActivityContent content)
    {
        revision.Name = content.Name;
        revision.Instructions = content.Instructions;
        revision.Forms = content.Forms.Select(item => new TemplateActivityFormBinding
        {
            Id = Guid.NewGuid(), TemplateActivityRevisionId = revision.Id,
            TemplateFormId = item.FormId, TemplateFormRevisionId = item.RevisionId,
            Key = item.Key, Order = item.Order, Usage = item.Usage, IsRequired = item.IsRequired,
        }).ToList();
        revision.Actions = content.Actions.Select(item =>
        {
            var action = new TemplateActivityAction
            {
                Id = Guid.NewGuid(), TemplateActivityRevisionId = revision.Id,
                Key = item.Key, Name = item.Name, Order = item.Order, ActionType = item.Type,
                RequiresIndependentChecker = item.Checker, RequiresApproval = item.Approval,
            };
            action.Roles = Roles(action.Id, item.Performers, TemplateActivityActionRoleKind.Performer)
                .Concat(Roles(action.Id, item.Checkers, TemplateActivityActionRoleKind.Checker))
                .Concat(Roles(action.Id, item.Approvers, TemplateActivityActionRoleKind.Approver))
                .ToList();
            return action;
        }).ToList();
        revision.Resources = content.Resources.Select(item => new TemplateActivityResourceRequirement
        {
            Id = Guid.NewGuid(), TemplateActivityRevisionId = revision.Id,
            CapabilityId = item.CapabilityId, Order = item.Order, IsRequired = item.IsRequired,
        }).ToList();
        revision.DataBindings = content.DataBindings.Select(item => new TemplateActivityDataBinding
        {
            Id = Guid.NewGuid(), TemplateActivityRevisionId = revision.Id, Key = item.Key,
            Order = item.Order, Direction = item.Direction, DataType = item.Type,
            IsRequired = item.IsRequired,
        }).ToList();
        revision.CompletionRules = content.CompletionRules.Select(item =>
            new TemplateActivityCompletionRule
            {
                Id = Guid.NewGuid(), TemplateActivityRevisionId = revision.Id,
                Order = item.Order, RuleType = item.Type, TargetKey = item.TargetKey,
            }).ToList();
        revision.ContentHash = Hash(SnapshotJson(revision.TemplateActivityId, content));
    }

    internal static TemplateActivityRevisionAudit Audit(TemplateActivityRevision revision,
        TemplateActivityRevisionStatus? prior, string action, string reason,
        Guid actorId, Guid correlationId) => new()
        {
            Id = Guid.NewGuid(), TemplateActivityRevisionId = revision.Id,
            PriorStatus = prior, NewStatus = revision.Status, Action = action,
            Reason = reason.Trim(), ContentHash = revision.ContentHash,
            SnapshotJson = SnapshotJson(revision), ActorId = actorId,
            OccurredAt = DateTime.UtcNow, CorrelationId = correlationId,
        };

    internal static bool HasReason(string? value) => value?.Trim().Length is >= 10 and <= 4000;

    internal static TemplateActivityRevisionDto ToDto(TemplateActivityRevision item)
    {
        return new TemplateActivityRevisionDto(item.Id, item.TemplateActivityId, item.Sequence,
            item.Status, item.Name, item.Instructions,
            item.Forms.OrderBy(x => x.Order).Select(x => new TemplateActivityFormBindingDto(
                x.TemplateFormId, x.TemplateFormRevisionId, x.Key, x.Order, x.Usage,
                x.IsRequired, x.TemplateFormRevision.Name)).ToArray(),
            item.Actions.OrderBy(x => x.Order).Select(ToActionDto).ToArray(),
            item.Resources.OrderBy(x => x.Order).Select(x => new TemplateActivityResourceDto(
                x.CapabilityId, x.Order, x.IsRequired)).ToArray(),
            item.DataBindings.OrderBy(x => x.Order).Select(x => new TemplateActivityDataBindingDto(
                x.Key, x.Order, x.Direction, x.DataType, x.IsRequired)).ToArray(),
            item.CompletionRules.OrderBy(x => x.Order).Select(x =>
                new TemplateActivityCompletionRuleDto(x.Order, x.RuleType, x.TargetKey)).ToArray(),
            item.ContentHash, item.CreatedById, item.ReviewedById, item.ReviewedAt,
            item.PublishedById, item.PublishedAt, item.RetiredAt);
    }

    private static TemplateActivityActionDto ToActionDto(TemplateActivityAction action) => new(
        action.Key, action.Name, action.Order, action.ActionType,
        action.RequiresIndependentChecker, action.RequiresApproval,
        RoleIds(action, TemplateActivityActionRoleKind.Performer),
        RoleIds(action, TemplateActivityActionRoleKind.Checker),
        RoleIds(action, TemplateActivityActionRoleKind.Approver));

    private static Guid[] RoleIds(TemplateActivityAction action,
        TemplateActivityActionRoleKind kind) => action.Roles.Where(x => x.RoleKind == kind)
        .Select(x => x.RoleId).Order().ToArray();

    private static IEnumerable<TemplateActivityActionRole> Roles(Guid actionId,
        IEnumerable<Guid> roleIds, TemplateActivityActionRoleKind kind) => roleIds.Select(id =>
            new TemplateActivityActionRole
                { Id = Guid.NewGuid(), TemplateActivityActionId = actionId, RoleId = id, RoleKind = kind });

    private static string SnapshotJson(TemplateActivityRevision revision)
    {
        var content = new TemplateActivityContent(revision.Name, revision.Instructions,
            revision.Forms.Select(x => new ActivityFormContent(x.TemplateFormId,
                x.TemplateFormRevisionId, x.Key, x.Order, x.Usage, x.IsRequired,
                x.TemplateFormRevision?.Name ?? string.Empty)).ToArray(),
            revision.Actions.Select(x => new ActivityActionContent(x.Key, x.Name, x.Order,
                x.ActionType, x.RequiresIndependentChecker, x.RequiresApproval,
                RoleIds(x, TemplateActivityActionRoleKind.Performer),
                RoleIds(x, TemplateActivityActionRoleKind.Checker),
                RoleIds(x, TemplateActivityActionRoleKind.Approver))).ToArray(),
            revision.Resources.Select(x => new ActivityResourceContent(
                x.CapabilityId, x.Order, x.IsRequired)).ToArray(),
            revision.DataBindings.Select(x => new ActivityDataContent(
                x.Key, x.Order, x.Direction, x.DataType, x.IsRequired)).ToArray(),
            revision.CompletionRules.Select(x => new ActivityRuleContent(
                x.Order, x.RuleType, x.TargetKey)).ToArray());
        return SnapshotJson(revision.TemplateActivityId, content);
    }

    private static string SnapshotJson(Guid activityId, TemplateActivityContent content) =>
        JsonSerializer.Serialize(new { activityId, content.Name, content.Instructions,
            forms = content.Forms.OrderBy(x => x.Order).Select(x => new
                { x.FormId, x.RevisionId, x.Key, x.Order, x.Usage, x.IsRequired }),
            actions = content.Actions.OrderBy(x => x.Order),
            resources = content.Resources.OrderBy(x => x.Order),
            dataBindings = content.DataBindings.OrderBy(x => x.Order),
            completionRules = content.CompletionRules.OrderBy(x => x.Order) },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    private static string Hash(string value) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
