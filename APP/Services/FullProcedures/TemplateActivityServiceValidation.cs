using DOMAIN.Entities.FullProcedures;

#nullable enable

namespace APP.Services.FullProcedures;

internal static class TemplateActivityServiceValidation
{
    internal static bool ValidShape(TemplateActivityContentRequest request)
    {
        if (request is null || request.Name.Trim().Length is < 2 or > 150 ||
            request.Instructions.Trim().Length is < 1 or > 4000 ||
            request.Forms.Count > 20 || request.Actions.Count is < 1 or > 100 ||
            request.Resources.Count > 50 || request.DataBindings.Count > 100 ||
            request.CompletionRules.Count is < 1 or > 100)
            return false;
        return ValidForms(request.Forms) && ValidActions(request.Actions) &&
               ValidResources(request.Resources) && ValidData(request.DataBindings) &&
               ValidCompletion(request);
    }

    private static bool ValidForms(IReadOnlyList<TemplateActivityFormBindingRequest> items) =>
        items.All(item => item.FormId != Guid.Empty && item.RevisionId != Guid.Empty &&
            ValidKey(item.Key) && item.Order >= 0 && Enum.IsDefined(item.Usage)) &&
        Unique(items.Select(item => item.FormId)) && Unique(items.Select(item => item.RevisionId)) &&
        Unique(items.Select(item => item.Key.Trim()), StringComparer.OrdinalIgnoreCase) &&
        Unique(items.Select(item => item.Order));

    private static bool ValidActions(IReadOnlyList<TemplateActivityActionRequest> items) =>
        Unique(items.Select(item => item.Key.Trim()), StringComparer.OrdinalIgnoreCase) &&
        Unique(items.Select(item => item.Order)) && items.All(ValidAction);

    private static bool ValidAction(TemplateActivityActionRequest item)
    {
        if (!ValidKey(item.Key) || item.Name.Trim().Length is < 2 or > 150 || item.Order < 0 ||
            !Enum.IsDefined(item.ActionType) || item.PerformerRoleIds.Count == 0 ||
            item.PerformerRoleIds.Any(id => id == Guid.Empty) ||
            !Unique(item.PerformerRoleIds) || !Unique(item.CheckerRoleIds) ||
            !Unique(item.ApproverRoleIds) ||
            item.RequiresIndependentChecker != (item.CheckerRoleIds.Count > 0) ||
            item.RequiresApproval != (item.ApproverRoleIds.Count > 0) ||
            (item.ActionType == TemplateActivityActionType.ApproveRelease &&
             !item.RequiresApproval)) return false;
        var performers = item.PerformerRoleIds.ToHashSet();
        return !item.CheckerRoleIds.Any(performers.Contains) &&
               !item.ApproverRoleIds.Any(performers.Contains);
    }

    private static bool ValidResources(IReadOnlyList<TemplateActivityResourceRequest> items) =>
        items.All(item => item.CapabilityId.Trim().Length is >= 2 and <= 120 && item.Order >= 0) &&
        Unique(items.Select(item => item.CapabilityId.Trim()), StringComparer.OrdinalIgnoreCase) &&
        Unique(items.Select(item => item.Order));

    private static bool ValidData(IReadOnlyList<TemplateActivityDataBindingRequest> items) =>
        items.All(item => ValidKey(item.Key) && item.Order >= 0 &&
            Enum.IsDefined(item.Direction) && Enum.IsDefined(item.DataType)) &&
        Unique(items.Select(item => item.Key.Trim()), StringComparer.OrdinalIgnoreCase) &&
        Unique(items.Select(item => item.Order));

    private static bool ValidCompletion(TemplateActivityContentRequest request)
    {
        if (!Unique(request.CompletionRules.Select(item => item.Order)) ||
            request.CompletionRules.Any(item => item.Order < 0 || !Enum.IsDefined(item.RuleType)))
            return false;
        var actions = request.Actions.ToDictionary(item => item.Key.Trim(),
            StringComparer.OrdinalIgnoreCase);
        var forms = request.Forms.Select(item => item.Key.Trim()).ToHashSet(
            StringComparer.OrdinalIgnoreCase);
        if (request.CompletionRules.Count(item =>
                item.RuleType == TemplateActivityCompletionRuleType.AllActionsCompleted &&
                string.IsNullOrWhiteSpace(item.TargetKey)) != 1) return false;
        foreach (var rule in request.CompletionRules)
            if (!ValidRule(rule, actions, forms)) return false;
        return RequiredRuleCoverage(request.Actions, request.Forms, request.CompletionRules);
    }

    private static bool ValidRule(TemplateActivityCompletionRuleRequest rule,
        IReadOnlyDictionary<string, TemplateActivityActionRequest> actions,
        IReadOnlySet<string> forms)
    {
        var key = rule.TargetKey?.Trim();
        if (rule.RuleType == TemplateActivityCompletionRuleType.AllActionsCompleted)
            return string.IsNullOrEmpty(key);
        if (string.IsNullOrEmpty(key)) return false;
        if (rule.RuleType == TemplateActivityCompletionRuleType.FormSubmitted)
            return forms.Contains(key);
        if (!actions.TryGetValue(key, out var action)) return false;
        return rule.RuleType switch
        {
            TemplateActivityCompletionRuleType.EvidenceCaptured =>
                action.ActionType == TemplateActivityActionType.CaptureEvidence,
            TemplateActivityCompletionRuleType.ApprovalGranted => action.RequiresApproval,
            TemplateActivityCompletionRuleType.DomainReceiptRecorded =>
                action.ActionType == TemplateActivityActionType.PostTransaction,
            _ => false,
        };
    }

    private static bool RequiredRuleCoverage(IReadOnlyList<TemplateActivityActionRequest> actions,
        IReadOnlyList<TemplateActivityFormBindingRequest> forms,
        IReadOnlyList<TemplateActivityCompletionRuleRequest> rules) =>
        forms.Where(item => item.IsRequired).All(item => HasRule(rules,
            TemplateActivityCompletionRuleType.FormSubmitted, item.Key)) &&
        actions.Where(item => item.ActionType == TemplateActivityActionType.CaptureEvidence)
            .All(item => HasRule(rules, TemplateActivityCompletionRuleType.EvidenceCaptured, item.Key)) &&
        actions.Where(item => item.RequiresApproval).All(item => HasRule(rules,
            TemplateActivityCompletionRuleType.ApprovalGranted, item.Key)) &&
        actions.Where(item => item.ActionType == TemplateActivityActionType.PostTransaction)
            .All(item => HasRule(rules, TemplateActivityCompletionRuleType.DomainReceiptRecorded, item.Key));

    private static bool HasRule(IEnumerable<TemplateActivityCompletionRuleRequest> rules,
        TemplateActivityCompletionRuleType type, string key) => rules.Any(item =>
            item.RuleType == type && string.Equals(item.TargetKey?.Trim(), key.Trim(),
                StringComparison.OrdinalIgnoreCase));
    private static bool ValidKey(string value) => value.Trim().Length is >= 2 and <= 100;
    private static bool Unique<T>(IEnumerable<T> items, IEqualityComparer<T>? comparer = null)
    {
        var array = items.ToArray();
        return array.Distinct(comparer).Count() == array.Length;
    }
}
