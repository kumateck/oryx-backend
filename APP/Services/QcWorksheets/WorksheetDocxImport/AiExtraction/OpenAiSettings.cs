#nullable enable

namespace APP.Services.QcWorksheets.WorksheetDocxImport.AiExtraction;

/// <summary>
/// Configuration for the OpenAI Chat Completions API call behind
/// <see cref="OpenAiWorksheetExtractor"/> (build brief 11). Unlike <see cref="AnthropicSettings"/>,
/// there is no environment/appsettings fallback for the key — OpenAI is new in this brief and
/// its key only ever comes from the DB-stored <c>AiExtractionSettings</c> row, resolved by
/// <c>IAiExtractionSettingsService.ResolveActiveAsync</c> and handed to
/// <see cref="AiWorksheetExtractorFactory"/>, which builds one of these per call.
/// </summary>
public sealed record OpenAiSettings(string ApiKey, string Model, TimeSpan Timeout)
{
    /// <summary>Fallback only for a provider row saved before a model was ever set — mirrors <see cref="AnthropicSettings.DefaultModel"/>.</summary>
    public const string DefaultModel = "gpt-5.1";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
