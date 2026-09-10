using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using DOMAIN.Entities.Formulas;

#nullable enable

namespace APP.Services.Formulas;

internal static class FormulaDefinitionDraft
{
    private const int DefinitionBytes = 1_048_576;
    private const int TestCaseBytes = 2_097_152;
    private const int AuthoringPayloadBytes = 2_097_152;

    internal sealed record NormalizedDraft(
        string Definition,
        string Tests,
        string AuthoringPayload,
        string AuthoringPayloadHash);

    public static NormalizedDraft? Normalize(
        FormulaRevisionDraftRequest request)
    {
        if (!IsHash(request.DefinitionHash) ||
            request.FormulaLanguageVersion != "oryx-formula-v1" ||
            string.IsNullOrWhiteSpace(request.NumericPolicyVersion) ||
            string.IsNullOrWhiteSpace(request.PresentationPreset)) return null;
        try
        {
            var authoringPayload = request.AuthoringPayload ?? request.Definition.GetRawText();
            if (string.IsNullOrWhiteSpace(authoringPayload) ||
                Encoding.UTF8.GetByteCount(authoringPayload) > AuthoringPayloadBytes) return null;
            return new NormalizedDraft(
                FormulaCanonicalJson.Canonicalize(
                    request.Definition.GetRawText(), DefinitionBytes),
                FormulaCanonicalJson.Canonicalize(
                    request.TestCases.GetRawText(), TestCaseBytes),
                authoringPayload,
                HashAuthoringPayload(authoringPayload));
        }
        catch (Exception error) when (error is ArgumentException or JsonException)
        {
            return null;
        }
    }

    public static FormulaRevision Build(
        FormulaDefinition definition,
        NormalizedDraft normalized,
        FormulaRevisionDraftRequest request,
        Guid actorId)
    {
        var revision = new FormulaRevision
        {
            Id = Guid.NewGuid(),
            FormulaDefinitionId = definition.Id,
            FormulaDefinition = definition,
            Revision = definition.Revisions.Select(item => item.Revision)
                .DefaultIfEmpty().Max() + 1,
            Status = FormulaRevisionStatus.Draft,
            CreatedById = actorId
        };
        Apply(revision, normalized, request, actorId);
        return revision;
    }

    public static void Apply(
        FormulaRevision revision,
        NormalizedDraft normalized,
        FormulaRevisionDraftRequest request,
        Guid actorId)
    {
        revision.DefinitionJson = normalized.Definition;
        revision.TestCasesJson = normalized.Tests;
        revision.AuthoringPayloadJson = normalized.AuthoringPayload;
        revision.AuthoringPayloadHash = normalized.AuthoringPayloadHash;
        revision.DefinitionHash = request.DefinitionHash;
        revision.FormulaLanguageVersion = request.FormulaLanguageVersion;
        revision.NumericPolicyVersion = request.NumericPolicyVersion;
        revision.ReleaseEvidenceHash = FormulaCanonicalJson.HashJson(
            "oryx:formula-draft-evidence:v1", normalized.Tests, TestCaseBytes);
        revision.LastUpdatedById = actorId;
    }

    public static JsonElement ValidationRequest(FormulaRevision revision)
    {
        using var definition = JsonDocument.Parse(revision.DefinitionJson);
        using var testCases = JsonDocument.Parse(revision.TestCasesJson);
        return JsonSerializer.SerializeToElement(new
        {
            schemaVersion = "oryx-formula-service-validate-request-v1",
            requestId = Guid.NewGuid().ToString("N"),
            formulaLanguageVersion = revision.FormulaLanguageVersion,
            numericPolicyVersion = revision.NumericPolicyVersion,
            definitionHash = revision.DefinitionHash,
            definition = definition.RootElement.Clone(),
            testCases = testCases.RootElement.Clone()
        });
    }

    public static bool CanTransition(
        FormulaRevision? revision,
        FormulaRevisionStatus expected,
        FormulaRevisionTransitionRequest request) => revision is not null &&
        revision.Status == expected &&
        revision.DefinitionHash == request.ExpectedDefinitionHash &&
        request.Reason.Trim().Length is >= 10 and <= 4000;

    public static bool IsValid(
        FormulaServiceResponse response,
        FormulaRevision revision) => response.Status == 5 &&
        response.ComputedDefinitionHash == revision.DefinitionHash &&
        response.DefinitionIssues.Count == 0 && response.RequestIssues.Count == 0 &&
        HasRequiredTestCoverage(response.TestOutcomes) &&
        response.TestOutcomes.All(item => item.Passed && item.ExpectedStatus == item.ActualStatus &&
            (item.Category switch
            {
                0 or 5 => item.ActualStatus == 0 &&
                    item.ActualResults.ValueKind == JsonValueKind.Object &&
                    item.ActualResults.EnumerateObject().Any(),
                2 => item.ActualStatus == 1,
                3 => item.ActualStatus == 2,
                4 => item.ActualStatus == 3,
                _ => true
            }));

    private static bool HasRequiredTestCoverage(
        IReadOnlyList<FormulaServiceTestOutcome> outcomes)
    {
        int[] requiredCategories = [0, 1, 2, 3, 5];
        return outcomes.Select(item => item.Id).Distinct().Count() == outcomes.Count &&
            requiredCategories.All(category =>
                outcomes.Any(item => item.Category == category));
    }

    private static bool IsHash(string? value) => value?.Length == 64 &&
        value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static string HashAuthoringPayload(string payload)
    {
        var bytes = Encoding.UTF8.GetBytes(
            $"oryx:formula-authoring-payload:v1\n{payload}");
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }
}
