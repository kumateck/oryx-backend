using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DOMAIN.Entities.FullProcedures;

#nullable enable

namespace APP.Services.FullProcedures;

internal sealed record TemplateSectionQuestionContent(
    Guid QuestionId, Guid RevisionId, int Order, string Wording,
    TemplateQuestionAnswerType AnswerType);
internal sealed record TemplateSectionConditionContent(
    Guid TargetQuestionId, Guid DependsOnQuestionId,
    TemplateSectionConditionOperator Operator, string? Value);
internal sealed record TemplateSectionContent(
    string Title, IReadOnlyList<TemplateSectionQuestionContent> Questions,
    IReadOnlyList<TemplateSectionConditionContent> ConditionalRules);

internal static class TemplateSectionServiceSupport
{
    internal static bool ValidShape(TemplateSectionContentRequest request)
    {
        var title = request?.Title?.Trim() ?? string.Empty;
        var questions = request?.Questions ?? [];
        var rules = request?.ConditionalRules ?? [];
        if (title.Length is < 2 or > 150 || questions.Count is < 1 or > 500 ||
            rules.Count > questions.Count || questions.Any(item =>
                item.QuestionId == Guid.Empty || item.RevisionId == Guid.Empty || item.Order < 0) ||
            questions.Select(item => item.QuestionId).Distinct().Count() != questions.Count ||
            questions.Select(item => item.RevisionId).Distinct().Count() != questions.Count ||
            questions.Select(item => item.Order).Distinct().Count() != questions.Count ||
            rules.Select(item => item.TargetQuestionId).Distinct().Count() != rules.Count)
            return false;
        var order = questions.ToDictionary(item => item.QuestionId, item => item.Order);
        return rules.All(item => ValidRule(item, order));
    }

    private static bool ValidRule(
        TemplateSectionConditionalRuleRequest rule, IReadOnlyDictionary<Guid, int> order)
    {
        if (rule.TargetQuestionId == Guid.Empty || rule.DependsOnQuestionId == Guid.Empty ||
            !Enum.IsDefined(rule.Operator) || rule.TargetQuestionId == rule.DependsOnQuestionId ||
            !order.TryGetValue(rule.TargetQuestionId, out var target) ||
            !order.TryGetValue(rule.DependsOnQuestionId, out var dependency) || dependency >= target)
            return false;
        var value = rule.Value?.Trim();
        return rule.Operator == TemplateSectionConditionOperator.IsAnswered
            ? value is null or ""
            : value?.Length is >= 1 and <= 500;
    }

    internal static TemplateSectionContent BuildContent(
        TemplateSectionContentRequest request,
        IReadOnlyDictionary<Guid, (Guid QuestionId, string Wording,
            TemplateQuestionAnswerType AnswerType)> resolved)
    {
        var questions = request.Questions.OrderBy(item => item.Order).Select(item =>
        {
            var source = resolved[item.RevisionId];
            return new TemplateSectionQuestionContent(
                source.QuestionId, item.RevisionId, item.Order,
                source.Wording, source.AnswerType);
        }).ToArray();
        var rules = request.ConditionalRules.OrderBy(item => item.TargetQuestionId).Select(item =>
            new TemplateSectionConditionContent(item.TargetQuestionId,
                item.DependsOnQuestionId, item.Operator,
                string.IsNullOrWhiteSpace(item.Value) ? null : item.Value.Trim())).ToArray();
        return new TemplateSectionContent(request.Title.Trim(), questions, rules);
    }

    internal static void Apply(TemplateSectionRevision revision, TemplateSectionContent content)
    {
        revision.Title = content.Title;
        revision.Questions = content.Questions.Select(item => new TemplateSectionQuestion
        {
            Id = Guid.NewGuid(), TemplateSectionRevisionId = revision.Id,
            TemplateQuestionId = item.QuestionId,
            TemplateQuestionRevisionId = item.RevisionId, Order = item.Order,
        }).ToList();
        var byQuestion = revision.Questions.ToDictionary(item => item.TemplateQuestionId);
        revision.ConditionalRules = content.ConditionalRules.Select(item =>
            new TemplateSectionConditionalRule
            {
                Id = Guid.NewGuid(), TemplateSectionRevisionId = revision.Id,
                TargetSectionQuestionId = byQuestion[item.TargetQuestionId].Id,
                DependsOnSectionQuestionId = byQuestion[item.DependsOnQuestionId].Id,
                Operator = item.Operator, ComparisonValue = item.Value,
            }).ToList();
        revision.ContentHash = Hash(SnapshotJson(revision.TemplateSectionId, content));
    }

    internal static TemplateSectionRevisionAudit Audit(
        TemplateSectionRevision revision, TemplateSectionRevisionStatus? prior,
        string action, string reason, Guid actorId, Guid correlationId) => new()
        {
            Id = Guid.NewGuid(), TemplateSectionRevisionId = revision.Id,
            PriorStatus = prior, NewStatus = revision.Status, Action = action,
            Reason = reason.Trim(), ContentHash = revision.ContentHash,
            SnapshotJson = SnapshotJson(revision), ActorId = actorId,
            OccurredAt = DateTime.UtcNow, CorrelationId = correlationId,
        };

    internal static bool HasReason(string? reason) =>
        reason?.Trim().Length is >= 10 and <= 4000;

    internal static TemplateSectionRevisionDto ToDto(TemplateSectionRevision item) => new(
        item.Id, item.TemplateSectionId, item.Sequence, item.Status, item.Title,
        item.Questions.OrderBy(question => question.Order).Select(question =>
            new TemplateSectionQuestionDto(question.TemplateQuestionId,
                question.TemplateQuestionRevisionId, question.Order,
                question.TemplateQuestionRevision.Wording,
                question.TemplateQuestionRevision.AnswerType)).ToArray(),
        item.ConditionalRules.OrderBy(rule => rule.TargetSectionQuestion.Order).Select(rule =>
            new TemplateSectionConditionalRuleDto(
                rule.TargetSectionQuestion.TemplateQuestionId,
                rule.DependsOnSectionQuestion.TemplateQuestionId,
                rule.Operator, rule.ComparisonValue)).ToArray(),
        item.ContentHash, item.CreatedById, item.ReviewedById, item.ReviewedAt,
        item.PublishedById, item.PublishedAt, item.RetiredAt);

    internal static string SnapshotJson(TemplateSectionRevision revision) =>
        SnapshotJson(revision.TemplateSectionId, FromRevision(revision));

    private static TemplateSectionContent FromRevision(TemplateSectionRevision revision)
    {
        var byId = revision.Questions.ToDictionary(item => item.Id);
        return new TemplateSectionContent(
            revision.Title, revision.Questions.OrderBy(item => item.Order).Select(item =>
                new TemplateSectionQuestionContent(item.TemplateQuestionId,
                    item.TemplateQuestionRevisionId, item.Order,
                    item.TemplateQuestionRevision?.Wording ?? string.Empty,
                    item.TemplateQuestionRevision?.AnswerType ?? default)).ToArray(),
            revision.ConditionalRules.Select(item => new TemplateSectionConditionContent(
                byId[item.TargetSectionQuestionId].TemplateQuestionId,
                byId[item.DependsOnSectionQuestionId].TemplateQuestionId,
                item.Operator, item.ComparisonValue)).ToArray());
    }

    private static string SnapshotJson(Guid sectionId, TemplateSectionContent content) =>
        JsonSerializer.Serialize(new
        {
            sectionId, content.Title,
            questions = content.Questions.OrderBy(item => item.Order).Select(item => new
                { item.QuestionId, item.RevisionId, item.Order }),
            conditionalRules = content.ConditionalRules.OrderBy(item => item.TargetQuestionId)
                .Select(item => new
                {
                    item.TargetQuestionId, item.DependsOnQuestionId,
                    @operator = (int)item.Operator, item.Value,
                }),
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    private static string Hash(string value) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
