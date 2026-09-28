using System.Text.Json;
using System.Text.Json.Serialization;
using DOMAIN.Entities.QcWorksheets;
using SHARED;

namespace APP.Services.QcWorksheets.WorksheetDocxImport.AiExtraction;

/// <summary>
/// Build brief 11: the response-side counterpart to <see cref="WorksheetExtractionSchema"/> —
/// one place that turns a provider's structured-output JSON (matching that schema) into an
/// <see cref="AiExtractionResult"/>, so the grounding check, the forced
/// <see cref="ImportConfidence.Low"/>/<see cref="WorksheetImportFlagCodes.AiExtracted"/>, and the
/// refuse-rather-than-partially-accept rule live in exactly one place for both
/// <see cref="AnthropicWorksheetExtractor"/> and <see cref="OpenAiWorksheetExtractor"/>.
/// Extracted unchanged from build brief 10's <c>AnthropicWorksheetExtractor.BuildResult</c>.
/// </summary>
public static class WorksheetExtractionResponseParser
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Deserializes <paramref name="toolInput"/> as <see cref="AiToolResponse"/> and builds the result, or refuses.</summary>
    public static Result<AiExtractionResult> Parse(
        JsonElement toolInput, string redactedText, int inputTokens, int outputTokens)
    {
        AiToolResponse parsed;
        try
        {
            parsed = toolInput.Deserialize<AiToolResponse>(JsonOptions);
        }
        catch (JsonException)
        {
            return Result.Failure<AiExtractionResult>(WorksheetImportErrors.AiResponseUngrounded);
        }

        if (parsed?.Template is null || string.IsNullOrWhiteSpace(parsed.Template.Name))
            return Result.Failure<AiExtractionResult>(WorksheetImportErrors.AiResponseUngrounded);

        return BuildResult(parsed, redactedText, inputTokens, outputTokens);
    }

    private static Result<AiExtractionResult> BuildResult(
        AiToolResponse parsed, string redactedText, int inputTokens, int outputTokens)
    {
        var template = new ProposedWorksheetTemplate
        {
            Code = parsed.Template.Code,
            Name = parsed.Template.Name,
            Department = parsed.Template.Department,
            Category = parsed.Template.Category,
            Sections = []
        };

        var sections = parsed.Template.Sections ?? [];
        foreach (var section in sections)
        {
            var proposedSection = new ProposedWorksheetSection
            {
                Order = template.Sections.Count + 1,
                Name = section.Name ?? "General",
                Fields = []
            };
            template.Sections.Add(proposedSection);

            foreach (var field in section.Fields ?? [])
            {
                if (string.IsNullOrWhiteSpace(field.SourceQuote) || !Grounded(field.SourceQuote, redactedText))
                    return Result.Failure<AiExtractionResult>(WorksheetImportErrors.AiResponseUngrounded);

                if (string.IsNullOrWhiteSpace(field.Label))
                    return Result.Failure<AiExtractionResult>(WorksheetImportErrors.AiResponseUngrounded);

                // A choice field's options must exactly match two mutually exclusive quoted
                // phrases from the document — never invented options.
                if (field.Type == WorksheetFieldType.Select)
                {
                    var options = field.Options ?? [];
                    if (options.Count != 2 || options.Any(option => !Grounded(option, redactedText)))
                        return Result.Failure<AiExtractionResult>(WorksheetImportErrors.AiResponseUngrounded);
                }

                proposedSection.Fields.Add(new ProposedWorksheetField
                {
                    Order = proposedSection.Fields.Count + 1,
                    FieldKey = field.FieldKey,
                    Label = field.Label,
                    Type = field.Type,
                    Mode = field.Mode,
                    Unit = field.Unit,
                    Analyte = field.Analyte,
                    ConstantValue = field.ConstantValue,
                    FormulaExpression = field.FormulaExpression,
                    Options = field.Options
                });
            }
        }

        var specificationProposals = new List<SpecificationCharacteristicProposal>();
        foreach (var spec in parsed.SpecificationProposals ?? [])
        {
            if (string.IsNullOrWhiteSpace(spec.SourceQuote) || !Grounded(spec.SourceQuote, redactedText))
                return Result.Failure<AiExtractionResult>(WorksheetImportErrors.AiResponseUngrounded);
            if (string.IsNullOrWhiteSpace(spec.TestName))
                return Result.Failure<AiExtractionResult>(WorksheetImportErrors.AiResponseUngrounded);

            specificationProposals.Add(new SpecificationCharacteristicProposal
            {
                TestName = spec.TestName,
                Analyte = spec.Analyte,
                AcceptanceCriteria = spec.AcceptanceCriteria,
                PrintedCriteria = spec.PrintedCriteria,
                AlertLimit = spec.AlertLimit,
                ActionLimit = spec.ActionLimit,
                Reference = spec.Reference,
                // Decision 4, enforced here regardless of anything the model reports.
                Confidence = ImportConfidence.Low
            });
        }

        var flags = new List<WorksheetImportFlag>
        {
            new()
            {
                Code = WorksheetImportFlagCodes.AiExtracted,
                Message = "This proposal was produced by the AI fallback extractor, not a deterministic recognizer. Verify every field against the source before saving."
            }
        };

        foreach (var suggestion in parsed.DictionarySuggestions ?? [])
        {
            if (string.IsNullOrWhiteSpace(suggestion)) continue;
            flags.Add(new WorksheetImportFlag
            {
                Code = WorksheetImportFlagCodes.AiDictionarySuggestion,
                Message = suggestion
            });
        }

        return Result.Success(new AiExtractionResult(template, specificationProposals, flags, inputTokens, outputTokens));
    }

    private static bool Grounded(string quote, string redactedText) =>
        !string.IsNullOrWhiteSpace(quote) && redactedText.Contains(quote, StringComparison.Ordinal);

    // --- Structured tool-call payload (our own schema, matches WorksheetExtractionSchema.Schema) ---

    public sealed class AiToolResponse
    {
        public AiTemplate Template { get; set; }
        public List<AiSpecProposal> SpecificationProposals { get; set; }
        public List<string> DictionarySuggestions { get; set; }
    }

    public sealed class AiTemplate
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
        public WorksheetCategory Category { get; set; }
        public List<AiSection> Sections { get; set; }
    }

    public sealed class AiSection
    {
        public string Name { get; set; }
        public List<AiFieldProposal> Fields { get; set; }
    }

    public sealed class AiFieldProposal
    {
        public string FieldKey { get; set; }
        public string Label { get; set; }
        public WorksheetFieldType Type { get; set; }
        public WorksheetFieldMode Mode { get; set; }
        public string Unit { get; set; }
        public string Analyte { get; set; }
        public string ConstantValue { get; set; }
        public string FormulaExpression { get; set; }
        public List<string> Options { get; set; }
        public string SourceQuote { get; set; }

        // Accepted but never trusted — Confidence is always forced to Low when the field is
        // materialized as an ImportFieldProvenance entry by the caller.
        public string Confidence { get; set; }
    }

    public sealed class AiSpecProposal
    {
        public string TestName { get; set; }
        public string Analyte { get; set; }
        public string AcceptanceCriteria { get; set; }
        public string PrintedCriteria { get; set; }
        public string AlertLimit { get; set; }
        public string ActionLimit { get; set; }
        public string Reference { get; set; }
        public string SourceQuote { get; set; }

        // Accepted but ignored — see BuildResult, which always sets Confidence = Low.
        public string Confidence { get; set; }
    }
}
