using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DOMAIN.Entities.FullProcedures;

#nullable enable

namespace APP.Services.FullProcedures;

internal sealed record TemplateQuestionContent(
    string Wording,
    TemplateQuestionAnswerType AnswerType,
    string InputType,
    Guid? UnitOfMeasureId,
    IReadOnlyList<TemplateQuestionOptionRequest> Options,
    bool Required,
    string? HelpText,
    decimal? Minimum,
    decimal? Maximum,
    IReadOnlyList<TemplateQuestionReference> CalculationReferences,
    TemplateQuestionSensitivity Sensitivity);

internal sealed record TemplateQuestionReference(Guid QuestionId, Guid RevisionId);

internal static class TemplateQuestionServiceSupport
{
    private static readonly IReadOnlyDictionary<TemplateQuestionAnswerType, HashSet<string>> Inputs =
        new Dictionary<TemplateQuestionAnswerType, HashSet<string>>
        {
            [TemplateQuestionAnswerType.ShortText] = ["text"],
            [TemplateQuestionAnswerType.LongText] = ["textarea", "richtext"],
            [TemplateQuestionAnswerType.Number] = ["number", "slider"],
            [TemplateQuestionAnswerType.Date] = ["date"],
            [TemplateQuestionAnswerType.Time] = ["time", "clock"],
            [TemplateQuestionAnswerType.Month] = ["month"],
            [TemplateQuestionAnswerType.SingleChoice] =
                ["radio", "select", "async-select", "paginated-select", "special-select"],
            [TemplateQuestionAnswerType.MultipleChoice] = ["multi", "async-multi", "multiple"],
            [TemplateQuestionAnswerType.Boolean] = ["switch", "checkbox"],
            [TemplateQuestionAnswerType.Signature] = ["signature"],
            [TemplateQuestionAnswerType.Image] = ["image", "upload", "drag-n-drop"],
            [TemplateQuestionAnswerType.Table] = ["table"],
            [TemplateQuestionAnswerType.Calculation] = ["formula"],
        };

    internal static TemplateQuestionContent? Prepare(TemplateQuestionContentRequest request)
    {
        if (request is null || !Enum.IsDefined(request.AnswerType) ||
            !Enum.IsDefined(request.Sensitivity)) return null;
        var rawOptions = request.Options ?? [];
        var rawReferences = request.CalculationQuestionIds ?? [];
        var wording = request.Wording?.Trim() ?? string.Empty;
        var inputType = request.InputType?.Trim() ?? string.Empty;
        var helpText = string.IsNullOrWhiteSpace(request.HelpText) ? null : request.HelpText.Trim();
        if (wording.Length is < 3 or > 500 || helpText?.Length > 1000 ||
            !Inputs[request.AnswerType].Contains(inputType) ||
            request.Minimum > request.Maximum || rawOptions.Count > 200 ||
            rawReferences.Count > 100) return null;
        var options = rawOptions.Select(item => new TemplateQuestionOptionRequest
        {
            Value = item.Value?.Trim() ?? string.Empty,
            Label = item.Label?.Trim() ?? string.Empty,
        }).ToArray();
        if (options.Any(item => item.Value.Length is < 1 or > 200 ||
                item.Label.Length is < 1 or > 500) ||
            options.Select(item => item.Value).Distinct(StringComparer.Ordinal).Count() != options.Length ||
            rawReferences.Any(item => item == Guid.Empty) ||
            rawReferences.Distinct().Count() != rawReferences.Count)
            return null;
        var isChoice = request.AnswerType is TemplateQuestionAnswerType.SingleChoice or
            TemplateQuestionAnswerType.MultipleChoice;
        var isCalculation = request.AnswerType == TemplateQuestionAnswerType.Calculation;
        if (isChoice != (options.Length > 0) ||
            isCalculation != (rawReferences.Count > 0)) return null;
        return new TemplateQuestionContent(
            wording, request.AnswerType, inputType, request.UnitOfMeasureId, options,
            request.Required, helpText, request.Minimum, request.Maximum,
            rawReferences.Order().Select(id => new TemplateQuestionReference(id, Guid.Empty)).ToArray(),
            request.Sensitivity);
    }

    internal static void Apply(
        TemplateQuestionRevision revision, TemplateQuestionContent content)
    {
        revision.Wording = content.Wording;
        revision.AnswerType = content.AnswerType;
        revision.InputType = content.InputType;
        revision.UnitOfMeasureId = content.UnitOfMeasureId;
        revision.Required = content.Required;
        revision.HelpText = content.HelpText;
        revision.Minimum = content.Minimum;
        revision.Maximum = content.Maximum;
        revision.Sensitivity = content.Sensitivity;
        revision.Options = content.Options.Select((item, rank) => new TemplateQuestionOption
        {
            Id = Guid.NewGuid(), TemplateQuestionRevisionId = revision.Id,
            Value = item.Value, Label = item.Label, Rank = rank,
        }).ToList();
        revision.CalculationReferences = content.CalculationReferences.Select(reference =>
            new TemplateQuestionCalculationReference
            {
                Id = Guid.NewGuid(), TemplateQuestionRevisionId = revision.Id,
                ReferencedQuestionId = reference.QuestionId,
                ReferencedQuestionRevisionId = reference.RevisionId,
            }).ToList();
        revision.ContentHash = Hash(SnapshotJson(revision.TemplateQuestionId, content));
    }

    internal static TemplateQuestionRevisionAudit Audit(
        TemplateQuestionRevision revision, TemplateQuestionRevisionStatus? prior,
        string action, string reason, Guid actorId, Guid correlationId)
    {
        var snapshot = SnapshotJson(revision);
        return new TemplateQuestionRevisionAudit
        {
            Id = Guid.NewGuid(), TemplateQuestionRevisionId = revision.Id,
            PriorStatus = prior, NewStatus = revision.Status, Action = action,
            Reason = reason.Trim(), ContentHash = revision.ContentHash,
            SnapshotJson = snapshot, ActorId = actorId, OccurredAt = DateTime.UtcNow,
            CorrelationId = correlationId,
        };
    }

    internal static bool HasReason(string? reason) => reason?.Trim().Length is >= 10 and <= 4000;

    internal static TemplateQuestionRevisionDto ToRevisionDto(TemplateQuestionRevision item) => new(
        item.Id, item.TemplateQuestionId, item.Sequence, item.Status, item.Wording,
        item.AnswerType, item.InputType, item.UnitOfMeasureId,
        item.UnitOfMeasure?.Symbol ?? item.UnitOfMeasure?.Name,
        item.Options.OrderBy(option => option.Rank)
            .Select(option => new TemplateQuestionOptionDto(
                option.Value, option.Label, option.Rank)).ToArray(),
        item.Required, item.HelpText, item.Minimum, item.Maximum,
        item.CalculationReferences.OrderBy(reference => reference.ReferencedQuestionId)
            .Select(reference => new TemplateQuestionReferenceDto(
                reference.ReferencedQuestionId, reference.ReferencedQuestionRevisionId)).ToArray(),
        item.Sensitivity, item.ContentHash, item.CreatedById,
        item.ReviewedById, item.ReviewedAt, item.PublishedById, item.PublishedAt, item.RetiredAt);

    internal static string SnapshotJson(TemplateQuestionRevision revision) =>
        SnapshotJson(revision.TemplateQuestionId, FromRevision(revision));

    internal static TemplateQuestionContent FromRevision(TemplateQuestionRevision revision) => new(
            revision.Wording, revision.AnswerType, revision.InputType,
            revision.UnitOfMeasureId,
            revision.Options.OrderBy(item => item.Rank).Select(item =>
                new TemplateQuestionOptionRequest { Value = item.Value, Label = item.Label }).ToArray(),
            revision.Required, revision.HelpText, revision.Minimum, revision.Maximum,
            revision.CalculationReferences.OrderBy(item => item.ReferencedQuestionId).Select(item =>
                new TemplateQuestionReference(item.ReferencedQuestionId,
                    item.ReferencedQuestionRevisionId)).ToArray(),
            revision.Sensitivity);

    private static string SnapshotJson(Guid questionId, TemplateQuestionContent content) =>
        JsonSerializer.Serialize(new
        {
            questionId, content.Wording, answerType = (int)content.AnswerType,
            content.InputType, content.UnitOfMeasureId,
            options = content.Options.Select((item, rank) => new { item.Value, item.Label, rank }),
            content.Required, content.HelpText, content.Minimum, content.Maximum,
            calculationReferences = content.CalculationReferences
                .OrderBy(item => item.QuestionId).Select(item => new
                    { item.QuestionId, item.RevisionId }),
            sensitivity = (int)content.Sensitivity,
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    private static string Hash(string value) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
