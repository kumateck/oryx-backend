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
/// Calls the OpenAI Chat Completions API (build brief 11) to extract a worksheet template from a
/// document that no deterministic recognizer matched, exactly as <see cref="AnthropicWorksheetExtractor"/>
/// does for Anthropic. Structured outputs only — <c>response_format: { type: "json_schema",
/// json_schema: {...}, strict: true }</c> using the identical <see cref="WorksheetExtractionSchema"/>
/// (build brief 11: one shared schema-building helper, not a second hand-maintained copy), with a
/// required <c>sourceQuote</c> per field. The response is deserialized and grounded by the same
/// <see cref="WorksheetExtractionResponseParser"/> the Anthropic extractor uses: anything that
/// doesn't validate, or whose <c>sourceQuote</c> isn't verbatim in the redacted text, is refused —
/// never partially accepted. Confidence and the <see cref="WorksheetImportFlagCodes.AiExtracted"/>
/// flag are forced there, unconditionally, regardless of anything the model itself reports. Same
/// one-retry-on-transient/no-retry-on-4xx-or-schema-failure rule as Anthropic.
/// </summary>
public sealed class OpenAiWorksheetExtractor(
    HttpClient httpClient,
    OpenAiSettings settings,
    ILogger<OpenAiWorksheetExtractor> logger) : IAiWorksheetExtractor
{
    private const string SchemaName = "worksheet_extraction";
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
                using var request = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
                {
                    Content = JsonContent.Create(requestBody, options: JsonOptions)
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
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
            return Result.Failure<AiExtractionResult>(WorksheetImportErrors.AiProviderUnreachable);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                // No retry on a 4xx (bad request/auth); already gave the one transient retry above.
                logger.LogWarning(
                    "AI worksheet extraction: OpenAI API returned {Status}.", (int)response.StatusCode);
                return Result.Failure<AiExtractionResult>(response.StatusCode switch
                {
                    HttpStatusCode.TooManyRequests => WorksheetImportErrors.AiProviderRateLimited,
                    >= HttpStatusCode.InternalServerError => WorksheetImportErrors.AiProviderUnreachable,
                    _ => WorksheetImportErrors.AiProviderRejectedRequest
                });
            }

            OpenAiChatCompletionResponse? envelope;
            try
            {
                envelope = await response.Content.ReadFromJsonAsync<OpenAiChatCompletionResponse>(
                    JsonOptions, cancellationToken);
            }
            catch (JsonException)
            {
                return Result.Failure<AiExtractionResult>(WorksheetImportErrors.AiResponseUngrounded);
            }

            var content = envelope?.Choices?.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content))
                return Result.Failure<AiExtractionResult>(WorksheetImportErrors.AiResponseUngrounded);

            JsonElement toolInput;
            try
            {
                toolInput = JsonDocument.Parse(content).RootElement.Clone();
            }
            catch (JsonException)
            {
                return Result.Failure<AiExtractionResult>(WorksheetImportErrors.AiResponseUngrounded);
            }

            var inputTokens = envelope?.Usage?.PromptTokens ?? 0;
            var outputTokens = envelope?.Usage?.CompletionTokens ?? 0;
            logger.LogInformation(
                "AI worksheet extraction: {InputTokens} input / {OutputTokens} output tokens.",
                inputTokens, outputTokens);

            return WorksheetExtractionResponseParser.Parse(toolInput, document.FullText, inputTokens, outputTokens);
        }
    }

    private object BuildRequest(RedactedDocument document)
    {
        var instructions = WorksheetExtractionSchema.Instructions +
            "\n\n--- DOCUMENT (redacted) ---\n" + document.FullText;

        return new
        {
            model = settings.Model,
            messages = new[]
            {
                new { role = "user", content = instructions }
            },
            response_format = new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = SchemaName,
                    strict = true,
                    schema = OpenAiWorksheetSchema.Schema
                }
            }
        };
    }

    // --- OpenAI Chat Completions response envelope ---

    private sealed class OpenAiChatCompletionResponse
    {
        [JsonPropertyName("choices")] public List<OpenAiChoice>? Choices { get; set; }
        [JsonPropertyName("usage")] public OpenAiUsage? Usage { get; set; }
    }

    private sealed class OpenAiChoice
    {
        [JsonPropertyName("message")] public OpenAiMessage? Message { get; set; }
    }

    private sealed class OpenAiMessage
    {
        [JsonPropertyName("content")] public string? Content { get; set; }
    }

    private sealed class OpenAiUsage
    {
        [JsonPropertyName("prompt_tokens")] public int PromptTokens { get; set; }
        [JsonPropertyName("completion_tokens")] public int CompletionTokens { get; set; }
    }
}
