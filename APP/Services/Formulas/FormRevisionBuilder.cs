using System.Text.Json;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Formulas;

#nullable enable

namespace APP.Services.Formulas;

internal static class FormRevisionBuilder
{
    public static FormRevision Build(
        Form form,
        IReadOnlyList<FormField> fields,
        IReadOnlyDictionary<Guid, (FormulaPlacementDraftRequest Request,
            FormulaRevision Revision)> placements,
        int sequence,
        Guid actorId)
    {
        var revision = new FormRevision
        {
            Id = Guid.NewGuid(), FormId = form.Id, Sequence = sequence,
            Status = FormRevisionStatus.Draft, CreatedById = actorId
        };
        foreach (var field in fields.OrderBy(item => item.FormSection.Order)
                     .ThenBy(item => item.Rank).ThenBy(item => item.Id))
        {
            placements.TryGetValue(field.Id, out var placement);
            var fieldRevision = new FormFieldRevision
            {
                Id = Guid.NewGuid(), FormRevisionId = revision.Id,
                PlacementKey = placement.Request?.PlacementKey ?? $"field-{field.Id:N}",
                FormFieldId = field.Id, QuestionId = field.QuestionId,
                Required = field.Required, Rank = field.Rank,
                Description = field.Description,
                FieldHash = FieldHash(field)
            };
            if (placement.Request is not null)
                fieldRevision.FormulaConfiguration = Configuration(
                    fieldRevision.Id, placement.Request, placement.Revision, actorId);
            revision.Fields.Add(fieldRevision);
        }
        revision.ContentHash = ContentHash(revision);
        return revision;
    }

    public static IReadOnlyList<FormFieldRevision> RefreshDraft(
        FormRevision revision,
        IReadOnlyList<FormField> fields,
        IReadOnlyDictionary<Guid, (FormulaPlacementDraftRequest Request,
            FormulaRevision Revision)> placements,
        Guid actorId)
    {
        var replacement = Build(
            revision.Form, fields, placements, revision.Sequence, actorId);
        foreach (var field in replacement.Fields)
            field.FormRevisionId = revision.Id;
        revision.ContentHash = ContentHash(replacement);
        return replacement.Fields;
    }

    public static FormRevisionAudit Audit(
        FormRevision revision, FormRevisionStatus? prior, string action,
        string reason, Guid actorId, Guid correlationId) => new()
    {
        Id = Guid.NewGuid(), FormRevisionId = revision.Id, PriorStatus = prior,
        NewStatus = revision.Status, Action = action, Reason = reason,
        ContentHash = revision.ContentHash, ActorId = actorId,
        OccurredAt = DateTime.UtcNow, CorrelationId = correlationId
    };

    public static FormRevisionDto ToDto(FormRevision revision) => new(
        revision.Id, revision.FormId, revision.Sequence, (int)revision.Status,
        revision.Status.ToString(), revision.ContentHash, revision.CreatedById,
        revision.ReviewedById, revision.ReviewedAt, revision.ApprovedById,
        revision.ApprovedAt, revision.Fields.Where(item => item.FormulaConfiguration is not null)
            .OrderBy(item => item.PlacementKey).Select(item => new FormRevisionPlacementDto(
                item.Id, item.FormFieldId, item.QuestionId, item.PlacementKey,
                item.FormulaConfiguration.FormulaRevisionId,
                item.FormulaConfiguration.FormulaRevision.DefinitionHash,
                item.FormulaConfiguration.ConfigurationHash,
                ParseClone(item.FormulaConfiguration.BindingsJson),
                item.FormulaConfiguration.MethodReference)).ToList());

    private static FormFieldFormulaConfiguration Configuration(
        Guid fieldRevisionId, FormulaPlacementDraftRequest request,
        FormulaRevision revision, Guid actorId) => new()
    {
        Id = Guid.NewGuid(), FormFieldRevisionId = fieldRevisionId,
        FormulaRevisionId = revision.Id, FormulaRevision = revision,
        BindingsJson = Canonical(request.Bindings, 1_048_576),
        ResultTargetsJson = Canonical(request.ResultTargets, 1_048_576),
        DisplayPolicyJson = Canonical(request.DisplayPolicy, 262_144),
        MethodReference = string.IsNullOrWhiteSpace(request.MethodReference)
            ? null : request.MethodReference.Trim(),
        ConfigurationHash = request.ConfigurationHash, CreatedById = actorId
    };

    private static string FieldHash(FormField field) => FormulaCanonicalJson.HashJson(
        "oryx:form-field-revision:v1", JsonSerializer.Serialize(new
        {
            formFieldId = field.Id, field.QuestionId, field.Required,
            field.Rank, description = field.Description ?? string.Empty
        }), 262_144);

    private static string ContentHash(FormRevision revision) => FormulaCanonicalJson.HashJson(
        "oryx:form-revision:v1", JsonSerializer.Serialize(revision.Fields
            .OrderBy(item => item.PlacementKey).Select(item => new
            {
                item.PlacementKey, item.FieldHash,
                configurationHash = item.FormulaConfiguration?.ConfigurationHash
            })), 1_048_576);

    private static string Canonical(JsonElement element, int limit) =>
        FormulaCanonicalJson.Canonicalize(element.GetRawText(), limit);

    private static JsonElement ParseClone(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
