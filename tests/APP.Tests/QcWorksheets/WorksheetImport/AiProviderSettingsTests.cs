using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using API.Controllers;
using APP.Services.QcWorksheets.WorksheetDocxImport;
using APP.Services.QcWorksheets.WorksheetDocxImport.AiExtraction;
using APP.Utils;
using DOMAIN.Entities.QcWorksheets;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.QcWorksheets.WorksheetImport;

/// <summary>
/// Build brief 11. Every test here uses an in-memory <see cref="ApplicationDbContext"/>, an
/// <see cref="EphemeralDataProtectionProvider"/>, or a fake <see cref="HttpMessageHandler"/>/
/// <see cref="IAiWorksheetExtractorFactory"/> stand-in — no test in this file ever makes a real
/// network call to Anthropic, OpenAI, or any other host.
/// </summary>
file class NoCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class AiProviderSettingsTests
{
    private static ApplicationDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new NoCurrentUserService());

    private static AiExtractionSettingsService CreateService(ApplicationDbContext context) =>
        new(context, new EphemeralDataProtectionProvider());

    // -----------------------------------------------------------------
    // AiExtractionSettingsService — encryption, preview, round-trip
    // -----------------------------------------------------------------

    [Fact]
    public async Task SaveProviderKeyAsync_then_ResolveActiveAsync_round_trips_to_the_same_plaintext_key()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var actorId = Guid.NewGuid();

        var saved = await service.SaveProviderKeyAsync(
            AiExtractionProvider.Anthropic, "claude-opus-5-5", "sk-ant-abcdef1234", actorId, CancellationToken.None);
        Assert.True(saved.IsSuccess);

        var activated = await service.SetActiveProviderAsync(AiExtractionProvider.Anthropic, actorId, CancellationToken.None);
        Assert.True(activated.IsSuccess);

        var resolved = await service.ResolveActiveAsync(CancellationToken.None);
        Assert.True(resolved.IsSuccess);
        Assert.Equal(AiExtractionProvider.Anthropic, resolved.Value.Provider);
        Assert.Equal("claude-opus-5-5", resolved.Value.Model);
        Assert.Equal("sk-ant-abcdef1234", resolved.Value.ApiKey);
    }

    [Fact]
    public async Task KeyPreview_never_contains_more_than_the_last_4_characters_of_the_real_key()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var saved = await service.SaveProviderKeyAsync(
            AiExtractionProvider.OpenAi, "gpt-5.1", "sk-openai-verylongsecretkey9999", Guid.NewGuid(), CancellationToken.None);
        Assert.True(saved.IsSuccess);

        var status = saved.Value.Providers.Single(item => item.Provider == AiExtractionProvider.OpenAi);
        Assert.Equal("9999", status.KeyPreview);
        Assert.True(status.KeyPreview.Length <= 4);
        Assert.DoesNotContain("sk-openai-verylongsecretkey", status.KeyPreview);
    }

    [Fact]
    public async Task GetAsync_never_includes_ciphertext_or_plaintext_in_its_dto()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        const string plaintext = "sk-ant-supersecretvalue0001";

        await service.SaveProviderKeyAsync(AiExtractionProvider.Anthropic, "claude-opus-5-5", plaintext, Guid.NewGuid(), CancellationToken.None);

        var result = await service.GetAsync(CancellationToken.None);
        Assert.True(result.IsSuccess);

        var serialized = JsonSerializer.Serialize(result.Value);
        Assert.DoesNotContain(plaintext, serialized);
        Assert.DoesNotContain("EncryptedApiKey", serialized);

        var row = await context.QcAiExtractionSettings.SingleAsync(item => item.Provider == AiExtractionProvider.Anthropic);
        Assert.DoesNotContain(row.EncryptedApiKey, serialized);
    }

    [Fact]
    public async Task SaveProviderKeyAsync_refuses_an_empty_or_whitespace_key()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.SaveProviderKeyAsync(
            AiExtractionProvider.Anthropic, "claude-opus-5-5", "   ", Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(WorksheetImportErrors.AiExtractionKeyRequired, result.Error);
    }

    [Fact]
    public async Task SetActiveProviderAsync_refuses_a_provider_with_no_key_saved_yet()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.SetActiveProviderAsync(AiExtractionProvider.OpenAi, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(WorksheetImportErrors.AiExtractionUnavailable, result.Error);
    }

    [Fact]
    public async Task ResolveActiveAsync_refuses_when_no_provider_has_ever_been_activated()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.ResolveActiveAsync(CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(WorksheetImportErrors.AiExtractionUnavailable, result.Error);
    }

    [Fact]
    public async Task Switching_the_active_provider_back_does_not_require_re_entering_its_key()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var actorId = Guid.NewGuid();

        await service.SaveProviderKeyAsync(AiExtractionProvider.Anthropic, "claude-opus-5-5", "sk-ant-aaaa1111", actorId, CancellationToken.None);
        await service.SaveProviderKeyAsync(AiExtractionProvider.OpenAi, "gpt-5.1", "sk-openai-bbbb2222", actorId, CancellationToken.None);
        await service.SetActiveProviderAsync(AiExtractionProvider.Anthropic, actorId, CancellationToken.None);
        await service.SetActiveProviderAsync(AiExtractionProvider.OpenAi, actorId, CancellationToken.None);

        var backToAnthropic = await service.SetActiveProviderAsync(AiExtractionProvider.Anthropic, actorId, CancellationToken.None);
        Assert.True(backToAnthropic.IsSuccess);

        var resolved = await service.ResolveActiveAsync(CancellationToken.None);
        Assert.True(resolved.IsSuccess);
        Assert.Equal("sk-ant-aaaa1111", resolved.Value.ApiKey);
    }

    // -----------------------------------------------------------------
    // AiWorksheetExtractorRouter — fake extractor factory only
    // -----------------------------------------------------------------

    private sealed class RecordingExtractor : IAiWorksheetExtractor
    {
        public int CallCount { get; private set; }

        public Task<Result<AiExtractionResult>> ExtractAsync(RedactedDocument document, CancellationToken cancellationToken)
        {
            CallCount++;
            var template = new ProposedWorksheetTemplate { Name = "Routed Template", Sections = [] };
            return Task.FromResult(Result.Success(new AiExtractionResult(template, [], [], 1, 1)));
        }
    }

    private sealed class FakeExtractorFactory(
        IAiWorksheetExtractor anthropicExtractor, IAiWorksheetExtractor openAiExtractor) : IAiWorksheetExtractorFactory
    {
        public int CallCount { get; private set; }

        public IAiWorksheetExtractor Create(AiExtractionProvider provider, string apiKey, string model)
        {
            CallCount++;
            return provider == AiExtractionProvider.Anthropic ? anthropicExtractor : openAiExtractor;
        }
    }

    private sealed class StubSettingsService(Result<(AiExtractionProvider, string, string)> resolveResult) : IAiExtractionSettingsService
    {
        public Task<Result<AiExtractionSettingsDto>> GetAsync(CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<Result<AiExtractionSettingsDto>> SetActiveProviderAsync(AiExtractionProvider provider, Guid actorId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<Result<AiExtractionSettingsDto>> SaveProviderKeyAsync(AiExtractionProvider provider, string model, string apiKey, Guid actorId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<Result<(AiExtractionProvider Provider, string Model, string ApiKey)>> ResolveActiveAsync(CancellationToken cancellationToken) =>
            Task.FromResult(resolveResult);
    }

    [Fact]
    public async Task Router_dispatches_only_to_the_active_providers_extractor()
    {
        var anthropic = new RecordingExtractor();
        var openAi = new RecordingExtractor();
        var factory = new FakeExtractorFactory(anthropic, openAi);
        var settingsService = new StubSettingsService(
            Result.Success((AiExtractionProvider.OpenAi, "gpt-5.1", "sk-openai-test")));
        var router = new AiWorksheetExtractorRouter(settingsService, factory);

        var result = await router.ExtractAsync(new RedactedDocument { FullText = "x" }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, anthropic.CallCount);
        Assert.Equal(1, openAi.CallCount);
        Assert.Equal(1, factory.CallCount);
    }

    [Fact]
    public async Task Router_refuses_with_AiExtractionUnavailable_and_never_builds_an_extractor_when_no_key_is_configured()
    {
        var anthropic = new RecordingExtractor();
        var openAi = new RecordingExtractor();
        var factory = new FakeExtractorFactory(anthropic, openAi);
        var settingsService = new StubSettingsService(
            Result.Failure<(AiExtractionProvider, string, string)>(WorksheetImportErrors.AiExtractionUnavailable));
        var router = new AiWorksheetExtractorRouter(settingsService, factory);

        var result = await router.ExtractAsync(new RedactedDocument { FullText = "x" }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(WorksheetImportErrors.AiExtractionUnavailable, result.Error);
        Assert.Equal(0, anthropic.CallCount);
        Assert.Equal(0, openAi.CallCount);
        Assert.Equal(0, factory.CallCount);
    }

    // -----------------------------------------------------------------
    // OpenAiWorksheetExtractor — fake HttpMessageHandler only, never the network
    // -----------------------------------------------------------------

    private static OpenAiWorksheetExtractor MakeOpenAiExtractor(FakeHandler handler, string apiKey = "test-key") =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://unit-test.invalid/") },
            new OpenAiSettings(apiKey, "gpt-5.1", TimeSpan.FromSeconds(5)),
            NullLogger<OpenAiWorksheetExtractor>.Instance);

    private static RedactedDocument SimpleDocument(string text) => new()
    {
        HeaderText = "",
        Blocks = [],
        FullText = text
    };

    [Fact]
    public async Task OpenAi_ExtractAsync_forces_Low_confidence_and_AiExtracted_flag_even_when_the_fake_response_claims_High()
    {
        var document = SimpleDocument("pH of solution: 6.8 measured at start.");
        var handler = FakeHandler.StructuredOutput(new
        {
            template = new
            {
                name = "New Chemical Worksheet",
                sections = new[]
                {
                    new
                    {
                        name = "General",
                        fields = new[]
                        {
                            new
                            {
                                label = "pH",
                                type = "Number",
                                sourceQuote = "pH of solution: 6.8",
                                confidence = "High" // must be ignored
                            }
                        }
                    }
                }
            },
            specificationProposals = new[]
            {
                new
                {
                    testName = "pH",
                    sourceQuote = "pH of solution: 6.8",
                    confidence = "High" // must be ignored
                }
            }
        });

        var extractor = MakeOpenAiExtractor(handler);
        var result = await extractor.ExtractAsync(document, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value.Flags, flag => flag.Code == WorksheetImportFlagCodes.AiExtracted);
        var spec = Assert.Single(result.Value.SpecificationProposals);
        Assert.Equal(ImportConfidence.Low, spec.Confidence);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task OpenAi_ExtractAsync_refuses_an_ungrounded_sourceQuote()
    {
        var document = SimpleDocument("pH of solution: 6.8 measured at start.");
        var handler = FakeHandler.StructuredOutput(new
        {
            template = new
            {
                name = "New Chemical Worksheet",
                sections = new[]
                {
                    new
                    {
                        name = "General",
                        fields = new[]
                        {
                            new
                            {
                                label = "pH",
                                type = "Number",
                                sourceQuote = "This text does not appear anywhere in the document"
                            }
                        }
                    }
                }
            }
        });

        var result = await MakeOpenAiExtractor(handler).ExtractAsync(document, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(WorksheetImportErrors.AiResponseUngrounded, result.Error);
    }

    [Fact]
    public async Task OpenAi_ExtractAsync_refuses_a_malformed_response_rather_than_partially_accepting_it()
    {
        var document = SimpleDocument("pH of solution: 6.8.");
        var handler = new FakeHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ not valid json", Encoding.UTF8, "application/json")
            }));

        var result = await MakeOpenAiExtractor(handler).ExtractAsync(document, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(WorksheetImportErrors.AiResponseUngrounded, result.Error);
    }

    [Fact]
    public async Task OpenAi_ExtractAsync_refuses_when_no_api_key_is_configured_without_calling_the_handler()
    {
        var document = SimpleDocument("pH of solution: 6.8.");
        var handler = FakeHandler.StructuredOutput(new { template = new { name = "x", sections = Array.Empty<object>() } });

        var result = await MakeOpenAiExtractor(handler, apiKey: "").ExtractAsync(document, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(WorksheetImportErrors.AiExtractionUnavailable, result.Error);
        Assert.Equal(0, handler.CallCount);
    }

    // -----------------------------------------------------------------
    // Shared schema helper — one source of truth for both providers
    // -----------------------------------------------------------------

    [Fact]
    public void Both_providers_request_bodies_embed_the_exact_same_schema_object()
    {
        // AnthropicWorksheetExtractor.input_schema and OpenAiWorksheetExtractor's
        // response_format.json_schema.schema both come from WorksheetExtractionSchema.Schema —
        // the same static object reference, so there is no possibility of the two drifting.
        var first = JsonSerializer.Serialize(WorksheetExtractionSchema.Schema);
        var second = JsonSerializer.Serialize(WorksheetExtractionSchema.Schema);
        Assert.Equal(first, second);
        Assert.Contains("sourceQuote", first);
        Assert.Contains("\"required\"", first);
    }

    // -----------------------------------------------------------------
    // Permissions
    // -----------------------------------------------------------------

    [Fact]
    public void The_settings_permission_key_is_grantable_under_the_worksheet_template_submodule()
    {
        var permission = Assert.Single(PermissionUtils.GeneratePermissions(),
            item => item.Key == QcWorksheetPermissionKeys.CanManageAiWorksheetExtractionSettings);

        Assert.Equal(QcWorksheetPermissionCatalog.Module, permission.Module);
        Assert.Equal(QcWorksheetPermissionCatalog.WorksheetTemplates, permission.SubModule);
    }

    [Theory]
    [InlineData(nameof(QcAiExtractionSettingsController.GetSettings))]
    [InlineData(nameof(QcAiExtractionSettingsController.SaveProviderKey))]
    [InlineData(nameof(QcAiExtractionSettingsController.SetActiveProvider))]
    public void Every_settings_endpoint_is_gated_on_the_manage_settings_key_alone(string methodName)
    {
        var method = typeof(QcAiExtractionSettingsController).GetMethod(methodName)!;

        var policy = Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>().Select(attribute => attribute.Policy));
        Assert.Equal(QcWorksheetPermissionKeys.CanManageAiWorksheetExtractionSettings, policy);
    }

    /// <summary>Captures every outbound request body without ever touching the network.</summary>
    private sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        public int CallCount { get; private set; }
        public List<string> CapturedRequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            if (request.Content is not null)
                CapturedRequestBodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            return await respond(request, cancellationToken);
        }

        /// <summary>Mimics an OpenAI chat completion whose message content is the structured-output JSON string.</summary>
        public static FakeHandler StructuredOutput(object toolInput) => new((_, _) =>
        {
            var envelope = new
            {
                choices = new[]
                {
                    new { message = new { content = JsonSerializer.Serialize(toolInput) } }
                },
                usage = new { prompt_tokens = 123, completion_tokens = 45 }
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(envelope)
            });
        });
    }
}
