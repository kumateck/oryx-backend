using System.Net;
using System.Text;
using System.Text.Json;
using APP.Services.Formulas;
using Xunit;

namespace APP.Tests.Repository;

public sealed class FormulaCalculationClientTests
{
    private static readonly FormulaCalculationSettings EnabledSettings = new(
        true,
        new Uri("http://formula.internal/"),
        new string('s', 32),
        TimeSpan.FromSeconds(1));

    [Fact]
    public async Task Validate_WhenRuntimeDisabled_FailsWithoutSending()
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("must not send"));
        var client = CreateClient(handler, EnabledSettings with { Enabled = false });

        var result = await client.ValidateAsync(JsonDocument.Parse("{}").RootElement);

        Assert.True(result.IsFailure);
        Assert.Equal("FormulaCalculation.Disabled", Assert.Single(result.Errors).Code);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task Validate_ForwardsRequestAndAcceptsVersionedResponse()
    {
        var handler = new StubHandler(request =>
        {
            Assert.Equal("/internal/formula/v1/validate", request.RequestUri?.AbsolutePath);
            return Response(HttpStatusCode.OK, ValidResponse);
        });
        var client = CreateClient(handler, EnabledSettings);

        var result = await client.ValidateAsync(JsonDocument.Parse("{}").RootElement);

        Assert.True(result.IsSuccess);
        Assert.Equal("COMPLETED", result.Value.StatusName);
        Assert.Equal(new string('a', 64), result.Value.EngineBuildHash);
    }

    [Fact]
    public async Task Validate_RejectsMalformedServiceResponse()
    {
        var handler = new StubHandler(_ => Response(HttpStatusCode.OK, "{}"));
        var client = CreateClient(handler, EnabledSettings);

        var result = await client.ValidateAsync(JsonDocument.Parse("{}").RootElement);

        Assert.True(result.IsFailure);
        Assert.Equal("FormulaCalculation.InvalidResponse", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Validate_MapsUnavailableWithoutLeakingBody()
    {
        var handler = new StubHandler(_ => Response(
            HttpStatusCode.Unauthorized,
            "{\"secret\":\"must-not-surface\"}"));
        var client = CreateClient(handler, EnabledSettings);

        var result = await client.ValidateAsync(JsonDocument.Parse("{}").RootElement);

        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal("FormulaCalculation.Unavailable", error.Code);
        Assert.DoesNotContain("must-not-surface", error.Description);
    }

    private static FormulaCalculationClient CreateClient(
        HttpMessageHandler handler,
        FormulaCalculationSettings settings) =>
        new(new HttpClient(handler) { BaseAddress = settings.BaseUri }, settings);

    private static HttpResponseMessage Response(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    private const string ValidResponse = """
    {
      "schemaVersion":"oryx-formula-service-response-v1",
      "operation":0,
      "requestId":"request-1",
      "status":5,
      "statusName":"COMPLETED",
      "evaluatorVersion":"oryx-formula-evaluator-spike-v1",
      "engineBuildHash":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
      "claimedDefinitionHash":null,
      "computedDefinitionHash":null,
      "claimedConfigurationHash":null,
      "computedConfigurationHash":null,
      "computedInputHash":null,
      "placementKey":null,
      "canonicalDefinition":null,
      "dependencies":[],
      "definitionIssues":[],
      "configurationIssues":[],
      "requestIssues":[],
      "evaluation":null,
      "testOutcomes":[]
    }
    """;

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount += 1;
            return Task.FromResult(respond(request));
        }
    }
}
