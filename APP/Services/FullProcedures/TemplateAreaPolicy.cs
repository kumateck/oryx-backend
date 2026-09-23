namespace APP.Services.FullProcedures;

public enum TemplateDefinitionKind
{
    Question = 0,
    Section = 1,
    Form = 2,
    Activity = 3,
    Workflow = 4,
}

public enum TemplateAreaBlocker
{
    InvalidDraft = 0,
    DuplicateName = 1,
    UnknownPurpose = 2,
    UnknownSubject = 3,
    UnknownCapability = 4,
    UnknownReviewPolicy = 5,
    PurposeSubjectMismatch = 6,
    PurposeCapabilityMismatch = 7,
    UnknownArea = 8,
    PurposeNotInArea = 9,
    SubjectNotInArea = 10,
    KindNotAllowed = 11,
    CapabilityNotInArea = 12,
    CapabilityNotInPurpose = 13,
    UnknownOwnerGroup = 14,
}

public sealed record TemplatePurposeDescriptor(
    string Id,
    IReadOnlySet<TemplateDefinitionKind> AllowedKinds,
    IReadOnlySet<string> SubjectTypeIds,
    IReadOnlySet<string> CapabilityIds
);

public sealed record TemplateAreaCatalog(
    IReadOnlyDictionary<string, TemplatePurposeDescriptor> Purposes,
    IReadOnlySet<string> SubjectTypeIds,
    IReadOnlySet<string> CapabilityIds,
    IReadOnlySet<string> ReviewPolicyIds,
    IReadOnlySet<Guid> OwnerGroupIds
);

public sealed record TemplateAreaDraft(
    string Name,
    Guid OwnerGroupId,
    IReadOnlyList<string> PurposeIds,
    IReadOnlyList<string> SubjectTypeIds,
    IReadOnlyList<string> CapabilityIds,
    string ReviewPolicyId
);

public sealed record TemplateAreaDefinition(
    Guid Id,
    string Name,
    Guid OwnerGroupId,
    IReadOnlyList<string> PurposeIds,
    IReadOnlyList<string> SubjectTypeIds,
    IReadOnlyList<string> CapabilityIds,
    string ReviewPolicyId
);

public sealed record TemplateDraftContext(
    Guid AreaId,
    string PurposeId,
    string SubjectTypeId,
    TemplateDefinitionKind Kind,
    IReadOnlyList<string> CapabilityIds
);

public static class TemplateAreaPolicy
{
    public static IReadOnlySet<TemplateAreaBlocker> ValidateDraft(
        TemplateAreaDraft draft,
        IReadOnlyCollection<TemplateAreaDefinition> existingAreas,
        TemplateAreaCatalog catalog,
        Guid? editingAreaId = null)
    {
        var blockers = new HashSet<TemplateAreaBlocker>();
        if (!IsStructurallyValid(draft))
            return new HashSet<TemplateAreaBlocker> { TemplateAreaBlocker.InvalidDraft };

        var normalizedName = draft.Name.Trim().ToUpperInvariant();
        if (existingAreas.Any(area =>
                area.Id != editingAreaId &&
                area.Name.Trim().ToUpperInvariant() == normalizedName))
            blockers.Add(TemplateAreaBlocker.DuplicateName);
        if (!catalog.OwnerGroupIds.Contains(draft.OwnerGroupId))
            blockers.Add(TemplateAreaBlocker.UnknownOwnerGroup);
        if (!catalog.ReviewPolicyIds.Contains(draft.ReviewPolicyId))
            blockers.Add(TemplateAreaBlocker.UnknownReviewPolicy);

        var allowedSubjects = new HashSet<string>();
        var allowedCapabilities = new HashSet<string>();
        foreach (var purposeId in draft.PurposeIds)
        {
            if (!catalog.Purposes.TryGetValue(purposeId, out var purpose))
            {
                blockers.Add(TemplateAreaBlocker.UnknownPurpose);
                continue;
            }
            allowedSubjects.UnionWith(purpose.SubjectTypeIds);
            allowedCapabilities.UnionWith(purpose.CapabilityIds);
            if (!draft.SubjectTypeIds.Any(purpose.SubjectTypeIds.Contains))
                blockers.Add(TemplateAreaBlocker.PurposeSubjectMismatch);
        }

        if (draft.SubjectTypeIds.Any(id => !catalog.SubjectTypeIds.Contains(id)))
            blockers.Add(TemplateAreaBlocker.UnknownSubject);
        if (draft.SubjectTypeIds.Any(id => !allowedSubjects.Contains(id)))
            blockers.Add(TemplateAreaBlocker.PurposeSubjectMismatch);
        if (draft.CapabilityIds.Any(id => !catalog.CapabilityIds.Contains(id)))
            blockers.Add(TemplateAreaBlocker.UnknownCapability);
        if (draft.CapabilityIds.Any(id => !allowedCapabilities.Contains(id)))
            blockers.Add(TemplateAreaBlocker.PurposeCapabilityMismatch);
        return blockers;
    }

    public static IReadOnlySet<TemplateAreaBlocker> ValidateContext(
        TemplateDraftContext context,
        IReadOnlyCollection<TemplateAreaDefinition> areas,
        TemplateAreaCatalog catalog)
    {
        var area = areas.SingleOrDefault(item => item.Id == context.AreaId);
        if (area is null)
            return new HashSet<TemplateAreaBlocker> { TemplateAreaBlocker.UnknownArea };
        var blockers = new HashSet<TemplateAreaBlocker>();
        catalog.Purposes.TryGetValue(context.PurposeId, out var purpose);
        if (purpose is null || !area.PurposeIds.Contains(context.PurposeId))
            blockers.Add(TemplateAreaBlocker.PurposeNotInArea);
        if (!area.SubjectTypeIds.Contains(context.SubjectTypeId))
            blockers.Add(TemplateAreaBlocker.SubjectNotInArea);
        if (purpose is not null && !purpose.SubjectTypeIds.Contains(context.SubjectTypeId))
            blockers.Add(TemplateAreaBlocker.PurposeSubjectMismatch);
        if (purpose is not null && !purpose.AllowedKinds.Contains(context.Kind))
            blockers.Add(TemplateAreaBlocker.KindNotAllowed);
        if (context.CapabilityIds.Any(id => !area.CapabilityIds.Contains(id)))
            blockers.Add(TemplateAreaBlocker.CapabilityNotInArea);
        if (purpose is not null && context.CapabilityIds.Any(id => !purpose.CapabilityIds.Contains(id)))
            blockers.Add(TemplateAreaBlocker.CapabilityNotInPurpose);
        return blockers;
    }

    private static bool IsStructurallyValid(TemplateAreaDraft draft) =>
        draft.Name.Trim().Length is >= 2 and <= 100 &&
        draft.OwnerGroupId != Guid.Empty &&
        IsUniqueNonEmpty(draft.PurposeIds, requireOne: true) &&
        IsUniqueNonEmpty(draft.SubjectTypeIds, requireOne: true) &&
        IsUniqueNonEmpty(draft.CapabilityIds, requireOne: false) &&
        !string.IsNullOrWhiteSpace(draft.ReviewPolicyId);

    private static bool IsUniqueNonEmpty(IReadOnlyList<string> values, bool requireOne) =>
        (!requireOne || values.Count > 0) &&
        values.All(value => !string.IsNullOrWhiteSpace(value)) &&
        values.Distinct(StringComparer.Ordinal).Count() == values.Count;
}
