using DOMAIN.Entities.QcWorksheets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace APP.Services.QcWorksheets.WorksheetDocxImport.AiExtraction;

/// <summary>
/// Builds the concrete <see cref="IAiWorksheetExtractor"/> for a resolved provider/key/model
/// (build brief 11). Kept separate from <see cref="AiWorksheetExtractorRouter"/> so the router
/// can be unit-tested with a fake factory returning fake extractor stand-ins, without needing a
/// real <see cref="IHttpClientFactory"/> — the router itself never touches HTTP concerns.
/// </summary>
public interface IAiWorksheetExtractorFactory
{
    IAiWorksheetExtractor Create(AiExtractionProvider provider, string apiKey, string model);
}

/// <summary>
/// Real implementation: pulls the named <c>HttpClient</c> ("AnthropicClient"/"OpenAiClient")
/// already registered in DI and constructs the matching extractor with the settings resolved
/// for this call — never a cached, redeploy-only settings singleton, which is the whole point of
/// this brief.
/// </summary>
public sealed class AiWorksheetExtractorFactory(
    IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory) : IAiWorksheetExtractorFactory
{
    public IAiWorksheetExtractor Create(AiExtractionProvider provider, string apiKey, string model) => provider switch
    {
        AiExtractionProvider.Anthropic => new AnthropicWorksheetExtractor(
            httpClientFactory.CreateClient("AnthropicClient"),
            new AnthropicSettings(
                apiKey, string.IsNullOrWhiteSpace(model) ? AnthropicSettings.DefaultModel : model, TimeSpan.FromSeconds(60)),
            loggerFactory.CreateLogger<AnthropicWorksheetExtractor>()),

        AiExtractionProvider.OpenAi => new OpenAiWorksheetExtractor(
            httpClientFactory.CreateClient("OpenAiClient"),
            new OpenAiSettings(
                apiKey, string.IsNullOrWhiteSpace(model) ? OpenAiSettings.DefaultModel : model, TimeSpan.FromSeconds(60)),
            loggerFactory.CreateLogger<OpenAiWorksheetExtractor>()),

        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unknown AI extraction provider.")
    };
}
