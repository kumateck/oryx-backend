using System.Net;
using System.Text.Json;
using APP.Services.QcWorksheets.WorksheetDocxImport;
using APP.Services.QcWorksheets.WorksheetDocxImport.AiExtraction;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace APP.Tests.QcWorksheets.WorksheetImport;

public class OpenAiWorksheetSchemaTests
{
    [Fact]
    public void Strict_schema_requires_every_property_and_disallows_extras_at_every_depth()
    {
        CheckObject(OpenAiWorksheetSchema.Schema);

        var template = OpenAiWorksheetSchema.Schema.GetProperty("properties").GetProperty("template");
        var optionalCategory = template.GetProperty("properties").GetProperty("category");
        Assert.Equal(new[] { "string", "null" }, optionalCategory.GetProperty("type")
            .EnumerateArray().Select(value => value.GetString()));

        // Anthropic retains its existing, less restrictive tool schema.
        using var anthropicSchema = JsonDocument.Parse(JsonSerializer.Serialize(WorksheetExtractionSchema.Schema));
        Assert.False(anthropicSchema.RootElement.TryGetProperty("additionalProperties", out _));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "QcWorksheetTemplate.AiProviderRejectedRequest")]
    [InlineData(HttpStatusCode.Unauthorized, "QcWorksheetTemplate.AiProviderRejectedRequest")]
    [InlineData(HttpStatusCode.TooManyRequests, "QcWorksheetTemplate.AiProviderRateLimited")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "QcWorksheetTemplate.AiProviderUnreachable")]
    public async Task Provider_failures_are_not_reported_as_missing_keys(HttpStatusCode status, string expectedCode)
    {
        using var client = new HttpClient(new StatusHandler(status))
        {
            BaseAddress = new Uri("https://unit-test.invalid/")
        };
        var extractor = new OpenAiWorksheetExtractor(client,
            new OpenAiSettings("test-key", "gpt-5.4-mini", TimeSpan.FromSeconds(5)),
            NullLogger<OpenAiWorksheetExtractor>.Instance);

        var result = await extractor.ExtractAsync(new RedactedDocument { FullText = "pH 6.8" }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
    }

    private static void CheckObject(JsonElement node)
    {
        if (node.ValueKind != JsonValueKind.Object) return;
        if (node.TryGetProperty("properties", out var properties))
        {
            Assert.False(node.GetProperty("additionalProperties").GetBoolean());
            var keys = properties.EnumerateObject().Select(property => property.Name).ToHashSet();
            var required = node.GetProperty("required").EnumerateArray()
                .Select(item => item.GetString()!).ToHashSet();
            Assert.True(keys.SetEquals(required));
            foreach (var property in properties.EnumerateObject()) CheckObject(property.Value);
        }
        if (node.TryGetProperty("items", out var items)) CheckObject(items);
    }

    private sealed class StatusHandler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status));
    }
}
