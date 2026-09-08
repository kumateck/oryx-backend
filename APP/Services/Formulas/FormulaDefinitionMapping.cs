using System.Text.Json;
using DOMAIN.Entities.Formulas;

#nullable enable

namespace APP.Services.Formulas;

internal static class FormulaDefinitionMapping
{
    public static FormulaRevisionDto ToDto(
        FormulaRevision revision,
        Guid questionId,
        string presentationPreset)
    {
        using var definition = JsonDocument.Parse(revision.DefinitionJson);
        using var testCases = JsonDocument.Parse(revision.TestCasesJson);
        return new FormulaRevisionDto(
            revision.Id,
            revision.FormulaDefinitionId,
            questionId,
            revision.Revision,
            revision.DefinitionHash,
            revision.FormulaLanguageVersion,
            revision.NumericPolicyVersion,
            presentationPreset,
            (int)revision.Status,
            revision.Status.ToString(),
            definition.RootElement.Clone(),
            testCases.RootElement.Clone(),
            revision.CreatedById,
            revision.ReviewedById,
            revision.ReviewedAt,
            revision.ApprovedById,
            revision.ApprovedAt,
            revision.EffectiveAt);
    }

    public static FormulaRevisionAudit Audit(
        FormulaRevision revision,
        FormulaRevisionStatus? prior,
        FormulaRevisionStatus current,
        string action,
        string reason,
        Guid actorId,
        Guid correlationId) => new()
    {
        Id = Guid.NewGuid(),
        FormulaRevisionId = revision.Id,
        PriorStatus = prior,
        NewStatus = current,
        Action = action,
        Reason = reason,
        DefinitionHash = revision.DefinitionHash,
        ActorId = actorId,
        OccurredAt = DateTime.UtcNow,
        CorrelationId = correlationId
    };
}
