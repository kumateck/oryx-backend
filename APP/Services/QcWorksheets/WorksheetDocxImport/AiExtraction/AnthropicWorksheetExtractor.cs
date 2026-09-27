#nullable enable

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.Extensions.Logging;
using SHARED;

namespace APP.Services.QcWorksheets.WorksheetDocxImport.AiExtraction;

/// <summary>
/// Calls the Anthropic Messages API to extract a worksheet template from a document that no
/// deterministic recognizer matched (build brief 10). Structured output only — a tool-use call
/// whose schema mirrors <see cref="ProposedWorksheetTemplate"/> and
/// <see cref="SpecificationCharacteristicProposal"/> plus a required <c>sourceQuote</c> per
/// field. The response is deserialized strictly: anything that doesn't validate, or whose
/// <c>sourceQuote</c> isn't verbatim in the redacted text, is refused — never partially accepted.
/// Confidence and the <see cref="WorksheetImportFlagCodes.AiExtracted"/> flag are forced here,
/// unconditionally, regardless of anything the model itself reports.
/// </summary>
public sealed class AnthropicWorksheetExtractor(
    HttpClient httpClient,
    AnthropicSettings settings,
    ILogger<AnthropicWorksheetExtractor> logger) : IAiWorksheetExtractor
{
    private const string ToolName = "propose_worksheet_template";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<Result<AiExtractionResult>> ExtractAsync(
        RedactedDocument document, CancellationToken cancellationToken)
    {
        if (!settings.IsConfigured)
            return Result.Failure<AiExtractionResult>(WorksheetImportErrors.AiExtractionUnavailable);

        var requestBody = BuildRequest(document);

        HttpResponseMessage? response = null;
        Exception? transientFailure = null;
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "v1/messages")
                {
                    Content = JsonContent.Create(requestBody, options: JsonOptions)
                };
                request.Headers.Add("x-api-key", settings.ApiKey);
                request.Headers.Add("anthropic-version", "2023-06-01");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                response = await httpClient.SendAsync(request, cancellationToken);
                transientFailure = null;
                break;
            }
            catch (HttpRequestException ex)
            {
                transientFailure = ex;
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                transientFailure = ex;
            }

            // One retry on transient failure only; a 4xx or schema failure never retries.
        }

        if (transientFailure is not null || response is null)
        {
            logger.LogWarning(transientFailure, "AI worksheet extraction: request failed after retry.");
            return Result.Failure<AiExtractionResult>(WorksheetImportErrors.AiExtractionUnavailable);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                // No retry on a 4xx (bad request/auth); already gave the one transient retry above.
                logger.LogWarning(
                    "AI worksheet extraction: Anthropic API returned {Status}.", (int)response.StatusCode);
                return response.StatusCode == HttpStatusCode.TooManyRequests
                    ? Result.Failure<AiExtractionResult>(WorksheetImportErrors.AiExtractionUnavailable)
                    : Result.Failure<AiExtractionResult>(WorksheetImportErrors.AiExtractionUnavailable);
            }

            AnthropicMessageResponse? envelope;
            try
            {
                envelope = await response.Content.ReadFromJsonAsync<AnthropicMessageResponse>(
                    JsonOptions, cancellationToken);
            }
            catch (JsonException)
            {
                return Result.Failure<AiExtractionResult>(WorksheetImportErrors.AiResponseUngrounded);
            }

            var toolUse = envelope?.Content?.FirstOrDefault(block =>
                string.Equals(block.Type, "tool_use", StringComparison.Ordinal) &&
                string.Equals(block.Name, ToolName, StringComparison.Ordinal));
            if (toolUse?.Input is null)
                return Result.Failure<AiExtractionResult>(WorksheetImportErrors.AiResponseUngrounded);

            AiToolResponse? parsed;
            try
            {
                parsed = toolUse.Input.Value.Deserialize<AiToolResponse>(JsonOptions);
            }
            catch (JsonException)
            {
                return Result.Failure<AiExtractionResult>(WorksheetImportErrors.AiResponseUngrounded);
            }

            if (parsed?.Template is null || string.IsNullOrWhiteSpace(parsed.Template.Name))
                return Result.Failure<AiExtractionResult>(WorksheetImportErrors.AiResponseUngrounded);

            var inputTokens = envelope?.Usage?.InputTokens ?? 0;
            var outputTokens = envelope?.Usage?.OutputTokens ?? 0;
            logger.LogInformation(
                "AI worksheet extraction: {InputTokens} input / {OutputTokens} output tokens.",
                inputTokens, outputTokens);

            return BuildResult(parsed, document.FullText, inputTokens, outputTokens);
        }
    }

    private Result<AiExtractionResult> BuildResult(
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

    private object BuildRequest(RedactedDocument document)
    {
        var instructions =
            "You extract a QC worksheet template from an already-redacted analytical worksheet document. " +
            "Every field you propose MUST include a sourceQuote: the exact text (verbatim, character for " +
            "character) from the document below that the field was read from. Never invent a sourceQuote. " +
            "A choice field's options must be exactly two mutually exclusive phrases quoted verbatim from " +
            "the document — never invented options. If you recognize a run-data label or choice phrase not " +
            "in the standard dictionary but are confident it is one, add it to dictionarySuggestions as free " +
            "text; never apply it yourself.\n\n--- DOCUMENT (redacted) ---\n" + document.FullText;

        return new
        {
            model = settings.Model,
            max_tokens = 8000,
            tools = new[]
            {
                new
                {
                    name = ToolName,
                    description = "Propose a worksheet template and specification characteristics grounded in the supplied document.",
                    input_schema = ToolSchema
                }
            },
            tool_choice = new { type = "tool", name = ToolName },
            messages = new[]
            {
                new { role = "user", content = instructions }
            }
        };
    }

    private static readonly object ToolSchema = new
    {
        type = "object",
        required = new[] { "template" },
        properties = new
        {
            template = new
            {
                type = "object",
                required = new[] { "name", "sections" },
                properties = new
                {
                    code = new { type = "string" },
                    name = new { type = "string" },
                    department = new { type = "string" },
                    category = new { type = "string" },
                    sections = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "object",
                            required = new[] { "name", "fields" },
                            properties = new
                            {
                                name = new { type = "string" },
                                fields = new
                                {
                                    type = "array",
                                    items = new
                                    {
                                        type = "object",
                                        required = new[] { "label", "type", "sourceQuote" },
                                        properties = new
                                        {
                                            fieldKey = new { type = "string" },
                                            label = new { type = "string" },
                                            type = new { type = "string" },
                                            mode = new { type = "string" },
                                            unit = new { type = "string" },
                                            analyte = new { type = "string" },
                                            constantValue = new { type = "string" },
                                            formulaExpression = new { type = "string" },
                                            options = new { type = "array", items = new { type = "string" } },
                                            sourceQuote = new { type = "string" },
                                            confidence = new { type = "string" }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            },
            specificationProposals = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    required = new[] { "testName", "sourceQuote" },
                    properties = new
                    {
                        testName = new { type = "string" },
                        analyte = new { type = "string" },
                        acceptanceCriteria = new { type = "string" },
                        printedCriteria = new { type = "string" },
                        alertLimit = new { type = "string" },
                        actionLimit = new { type = "string" },
                        reference = new { type = "string" },
                        sourceQuote = new { type = "string" },
                        confidence = new { type = "string" }
                    }
                }
            },
            dictionarySuggestions = new { type = "array", items = new { type = "string" } }
        }
    };

    // --- Anthropic API response envelope ---

    private sealed class AnthropicMessageResponse
    {
        [JsonPropertyName("content")] public List<AnthropicContentBlock>? Content { get; set; }
        [JsonPropertyName("usage")] public AnthropicUsage? Usage { get; set; }
    }

    private sealed class AnthropicContentBlock
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("input")] public JsonElement? Input { get; set; }
    }

    private sealed class AnthropicUsage
    {
        [JsonPropertyName("input_tokens")] public int InputTokens { get; set; }
        [JsonPropertyName("output_tokens")] public int OutputTokens { get; set; }
    }

    // --- Structured tool-call payload (our own schema, matches ToolSchema above) ---

    private sealed class AiToolResponse
    {
        public AiTemplate? Template { get; set; }
        public List<AiSpecProposal>? SpecificationProposals { get; set; }
        public List<string>? DictionarySuggestions { get; set; }
    }

    private sealed class AiTemplate
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Department { get; set; }
        public WorksheetCategory Category { get; set; }
        public List<AiSection>? Sections { get; set; }
    }

    private sealed class AiSection
    {
        public string? Name { get; set; }
        public List<AiFieldProposal>? Fields { get; set; }
    }

    private sealed class AiFieldProposal
    {
        public string? FieldKey { get; set; }
        public string? Label { get; set; }
        public WorksheetFieldType Type { get; set; }
        public WorksheetFieldMode Mode { get; set; }
        public string? Unit { get; set; }
        public string? Analyte { get; set; }
        public string? ConstantValue { get; set; }
        public string? FormulaExpression { get; set; }
        public List<string>? Options { get; set; }
        public string? SourceQuote { get; set; }

        // Accepted but never trusted — Confidence is always forced to Low when the field is
        // materialized as an ImportFieldProvenance entry by the caller.
        public string? Confidence { get; set; }
    }

    private sealed class AiSpecProposal
    {
        public string? TestName { get; set; }
        public string? Analyte { get; set; }
        public string? AcceptanceCriteria { get; set; }
        public string? PrintedCriteria { get; set; }
        public string? AlertLimit { get; set; }
        public string? ActionLimit { get; set; }
        public string? Reference { get; set; }
        public string? SourceQuote { get; set; }

        // Accepted but ignored — see BuildResult, which always sets Confidence = Low.
        public string? Confidence { get; set; }
    }
}
