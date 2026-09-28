#nullable enable

using Microsoft.Extensions.Configuration;

namespace APP.Services.QcWorksheets.WorksheetDocxImport.AiExtraction;

/// <summary>
/// Configuration for the Anthropic Messages API call behind <see cref="AnthropicWorksheetExtractor"/>
/// (build brief 10). The key is environment-overridable and never checked in — <c>appsettings.json</c>
/// carries only an empty placeholder; staging/production supply <c>ANTHROPIC_API_KEY</c> (or
/// <c>Anthropic:ApiKey</c> via any other configuration provider) out of band.
/// </summary>
public sealed record AnthropicSettings(string ApiKey, string Model, TimeSpan Timeout)
{
    public const string DefaultModel = "claude-opus-5-5";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    public static AnthropicSettings Load(IConfiguration configuration)
    {
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
            apiKey = configuration["Anthropic:ApiKey"];

        var model = Environment.GetEnvironmentVariable("ANTHROPIC_MODEL");
        if (string.IsNullOrWhiteSpace(model))
            model = configuration["Anthropic:Model"];
        if (string.IsNullOrWhiteSpace(model))
            model = DefaultModel;

        return new AnthropicSettings(apiKey ?? string.Empty, model, TimeSpan.FromSeconds(60));
    }
}
