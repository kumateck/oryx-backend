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
/// whose schema is <see cref="WorksheetExtractionSchema"/> (build brief 11: shared with
/// <see cref="OpenAiWorksheetExtractor"/>, not a hand-maintained copy), with a required
/// <c>sourceQuote</c> per field. The response is deserialized strictly by
/// <see cref="WorksheetExtractionResponseParser"/>: anything that doesn't validate, or whose
/// <c>sourceQuote</c> isn't verbatim in the redacted text, is refused — never partially accepted.
/// Confidence and the <see cref="WorksheetImportFlagCodes.AiExtracted"/> flag are forced there,
/// unconditionally, regardless of anything the model itself reports.
/// </summary>
public sealed class AnthropicWorksheetExtractor(
    HttpClient httpClient,
    AnthropicSettings settings,
    ILogger<AnthropicWorksheetExtractor> logger) : IAiWorksheetExtractor
{
    private static readonly JsonSerializerOptions JsonOptions = WorksheetExtractionResponseParser.JsonOptions;

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
                string.Equals(block.Name, WorksheetExtractionSchema.ToolName, StringComparison.Ordinal));
            if (toolUse?.Input is null)
                return Result.Failure<AiExtractionResult>(WorksheetImportErrors.AiResponseUngrounded);

            var inputTokens = envelope?.Usage?.InputTokens ?? 0;
            var outputTokens = envelope?.Usage?.OutputTokens ?? 0;
            logger.LogInformation(
                "AI worksheet extraction: {InputTokens} input / {OutputTokens} output tokens.",
                inputTokens, outputTokens);

            return WorksheetExtractionResponseParser.Parse(
                toolUse.Input.Value, document.FullText, inputTokens, outputTokens);
        }
    }

    private object BuildRequest(RedactedDocument document)
    {
        var instructions = WorksheetExtractionSchema.Instructions +
            "\n\n--- DOCUMENT (redacted) ---\n" + document.FullText;

        return new
        {
            model = settings.Model,
            max_tokens = 8000,
            tools = new[]
            {
                new
                {
                    name = WorksheetExtractionSchema.ToolName,
                    description = "Propose a worksheet template and specification characteristics grounded in the supplied document.",
                    input_schema = WorksheetExtractionSchema.Schema
                }
            },
            tool_choice = new { type = "tool", name = WorksheetExtractionSchema.ToolName },
            messages = new[]
            {
                new { role = "user", content = instructions }
            }
        };
    }

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
}
