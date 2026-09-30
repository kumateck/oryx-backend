using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DOMAIN.Entities.FullProcedures;

#nullable enable

namespace APP.Services.FullProcedures;

internal sealed record TemplateFormSectionContent(
    Guid SectionId, Guid RevisionId, int Order, bool IsRequired, string Title);
internal sealed record TemplateFormRuleContent(
    Guid TargetSectionId, Guid SourceSectionId, Guid SourceQuestionId,
    Guid SourceQuestionRevisionId, TemplateFormConditionOperator Operator, string? Value);
internal sealed record TemplateFormContent(
    string Name, string Description, bool RequiresEvidence, bool RequiresSignature,
    IReadOnlyList<TemplateFormSectionContent> Sections,
    IReadOnlyList<TemplateFormRuleContent> ConditionalRules);

internal static class TemplateFormServiceSupport
{
    internal static bool ValidShape(TemplateFormContentRequest request)
    {
        var name = request?.Name?.Trim() ?? string.Empty;
        var description = request?.Description?.Trim() ?? string.Empty;
        var sections = request?.Sections ?? [];
        var rules = request?.ConditionalRules ?? [];
        if (name.Length is < 2 or > 150 || description.Length > 4000 ||
            sections.Count is < 1 or > 100 || rules.Count > sections.Count ||
            sections.Any(item => item.SectionId == Guid.Empty ||
                item.RevisionId == Guid.Empty || item.Order < 0) ||
            sections.Select(item => item.SectionId).Distinct().Count() != sections.Count ||
            sections.Select(item => item.RevisionId).Distinct().Count() != sections.Count ||
            sections.Select(item => item.Order).Distinct().Count() != sections.Count ||
            rules.Select(item => item.TargetSectionId).Distinct().Count() != rules.Count)
            return false;
        var order = sections.ToDictionary(item => item.SectionId, item => item.Order);
        return rules.All(item => ValidRule(item, order));
    }

    private static bool ValidRule(
        TemplateFormConditionalRuleRequest rule, IReadOnlyDictionary<Guid, int> order)
    {
        if (rule.TargetSectionId == Guid.Empty || rule.SourceSectionId == Guid.Empty ||
            rule.SourceQuestionId == Guid.Empty || rule.SourceQuestionRevisionId == Guid.Empty ||
            !Enum.IsDefined(rule.Operator) || rule.TargetSectionId == rule.SourceSectionId ||
            !order.TryGetValue(rule.TargetSectionId, out var target) ||
            !order.TryGetValue(rule.SourceSectionId, out var source) || source >= target)
            return false;
        var value = rule.Value?.Trim();
        return rule.Operator == TemplateFormConditionOperator.IsAnswered
            ? value is null or ""
            : value?.Length is >= 1 and <= 500;
    }

    internal static TemplateFormContent BuildContent(
        TemplateFormContentRequest request,
        IReadOnlyDictionary<Guid, (Guid SectionId, string Title)> resolved)
    {
        var sections = request.Sections.OrderBy(item => item.Order).Select(item =>
        {
            var source = resolved[item.RevisionId];
            return new TemplateFormSectionContent(source.SectionId, item.RevisionId,
                item.Order, item.IsRequired, source.Title);
        }).ToArray();
        var rules = request.ConditionalRules.OrderBy(item => item.TargetSectionId).Select(item =>
            new TemplateFormRuleContent(item.TargetSectionId, item.SourceSectionId,
                item.SourceQuestionId, item.SourceQuestionRevisionId, item.Operator,
                string.IsNullOrWhiteSpace(item.Value) ? null : item.Value.Trim())).ToArray();
        return new TemplateFormContent(request.Name.Trim(), request.Description.Trim(),
            request.RequiresEvidence, request.RequiresSignature, sections, rules);
    }

    internal static void Apply(TemplateFormRevision revision, TemplateFormContent content)
    {
        revision.Name = content.Name;
        revision.Description = content.Description;
        revision.RequiresEvidence = content.RequiresEvidence;
        revision.RequiresSignature = content.RequiresSignature;
        revision.Sections = content.Sections.Select(item => new TemplateFormSection
        {
            Id = Guid.NewGuid(), TemplateFormRevisionId = revision.Id,
            TemplateSectionId = item.SectionId, TemplateSectionRevisionId = item.RevisionId,
            Order = item.Order, IsRequired = item.IsRequired,
        }).ToList();
        var bySection = revision.Sections.ToDictionary(item => item.TemplateSectionId);
        revision.ConditionalRules = content.ConditionalRules.Select(item =>
            new TemplateFormConditionalRule
            {
                Id = Guid.NewGuid(), TemplateFormRevisionId = revision.Id,
                TargetFormSectionId = bySection[item.TargetSectionId].Id,
                SourceFormSectionId = bySection[item.SourceSectionId].Id,
                SourceQuestionId = item.SourceQuestionId,
                SourceQuestionRevisionId = item.SourceQuestionRevisionId,
                Operator = item.Operator, ComparisonValue = item.Value,
            }).ToList();
        revision.ContentHash = Hash(SnapshotJson(revision.TemplateFormId, content));
    }

    internal static TemplateFormRevisionAudit Audit(
        TemplateFormRevision revision, TemplateFormRevisionStatus? prior,
        string action, string reason, Guid actorId, Guid correlationId) => new()
        {
            Id = Guid.NewGuid(), TemplateFormRevisionId = revision.Id,
            PriorStatus = prior, NewStatus = revision.Status, Action = action,
            Reason = reason.Trim(), ContentHash = revision.ContentHash,
            SnapshotJson = SnapshotJson(revision), ActorId = actorId,
            OccurredAt = DateTime.UtcNow, CorrelationId = correlationId,
        };

    internal static bool HasReason(string? reason) =>
        reason?.Trim().Length is >= 10 and <= 4000;

    internal static TemplateFormRevisionDto ToDto(TemplateFormRevision item)
    {
        var byId = item.Sections.ToDictionary(section => section.Id);
        return new TemplateFormRevisionDto(
            item.Id, item.TemplateFormId, item.Sequence, item.Status, item.Name,
            item.Description, item.RequiresEvidence, item.RequiresSignature,
            item.Sections.OrderBy(section => section.Order).Select(section =>
                new TemplateFormSectionDto(section.TemplateSectionId,
                    section.TemplateSectionRevisionId, section.Order, section.IsRequired,
                    section.TemplateSectionRevision.Title)).ToArray(),
            item.ConditionalRules.OrderBy(rule => byId[rule.TargetFormSectionId].Order)
                .Select(rule => new TemplateFormConditionalRuleDto(
                    byId[rule.TargetFormSectionId].TemplateSectionId,
                    byId[rule.SourceFormSectionId].TemplateSectionId,
                    rule.SourceQuestionId, rule.SourceQuestionRevisionId,
                    rule.Operator, rule.ComparisonValue)).ToArray(),
            item.ContentHash, item.CreatedById, item.ReviewedById, item.ReviewedAt,
            item.PublishedById, item.PublishedAt, item.RetiredAt);
    }

    internal static string SnapshotJson(TemplateFormRevision revision)
    {
        var byId = revision.Sections.ToDictionary(item => item.Id);
        var content = new TemplateFormContent(revision.Name, revision.Description,
            revision.RequiresEvidence, revision.RequiresSignature,
            revision.Sections.OrderBy(item => item.Order).Select(item =>
                new TemplateFormSectionContent(item.TemplateSectionId,
                    item.TemplateSectionRevisionId, item.Order, item.IsRequired,
                    item.TemplateSectionRevision?.Title ?? string.Empty)).ToArray(),
            revision.ConditionalRules.Select(item => new TemplateFormRuleContent(
                byId[item.TargetFormSectionId].TemplateSectionId,
                byId[item.SourceFormSectionId].TemplateSectionId,
                item.SourceQuestionId, item.SourceQuestionRevisionId,
                item.Operator, item.ComparisonValue)).ToArray());
        return SnapshotJson(revision.TemplateFormId, content);
    }

    private static string SnapshotJson(Guid formId, TemplateFormContent content) =>
        JsonSerializer.Serialize(new
        {
            formId, content.Name, content.Description, content.RequiresEvidence,
            content.RequiresSignature,
            sections = content.Sections.OrderBy(item => item.Order).Select(item => new
                { item.SectionId, item.RevisionId, item.Order, item.IsRequired }),
            conditionalRules = content.ConditionalRules.OrderBy(item => item.TargetSectionId),
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    private static string Hash(string value) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
