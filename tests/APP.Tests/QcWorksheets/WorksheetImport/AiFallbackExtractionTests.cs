using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using APP.Services.QcWorksheets.WorksheetDocxImport;
using APP.Services.QcWorksheets.WorksheetDocxImport.AiExtraction;
using APP.Utils;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using SHARED;
using Xunit;

namespace APP.Tests.QcWorksheets.WorksheetImport;

/// <summary>
/// Build brief 10. Every test here uses a fake <see cref="HttpMessageHandler"/> or a stub
/// <see cref="IAiWorksheetExtractor"/> — no test in this file (or anywhere in the suite) ever
/// makes a real network call to Anthropic or any other host.
/// </summary>
public class AiFallbackExtractionTests
{
    // -----------------------------------------------------------------
    // DocumentRedactor
    // -----------------------------------------------------------------

    [Fact]
    public void Redact_replaces_a_batch_number_paired_with_its_label()
    {
        var document = TestDocx.Read([TestDocx.Grid([["Batch No.: QCD/26/002/0000493532", "Other cell"]])]);
        var redacted = DocumentRedactor.Redact(document);

        Assert.True(redacted.IsSuccess);
        Assert.DoesNotContain("QCD/26/002/0000493532", redacted.Value.FullText);
        Assert.Contains("[BATCH]", redacted.Value.FullText);
    }

    [Fact]
    public void Redact_replaces_ar_number_and_staff_name_labels()
    {
        var document = TestDocx.Read([TestDocx.Grid([
            ["A.R. No.: 26/0730", "Sampled by: Jane Doe"]
        ])]);
        var redacted = DocumentRedactor.Redact(document);

        Assert.True(redacted.IsSuccess);
        Assert.DoesNotContain("26/0730", redacted.Value.FullText);
        Assert.DoesNotContain("Jane Doe", redacted.Value.FullText);
        Assert.Contains("[AR_NUMBER]", redacted.Value.FullText);
        Assert.Contains("[NAME]", redacted.Value.FullText);
    }

    [Fact]
    public void Redact_refuses_hard_when_a_matched_label_has_no_recognizable_value()
    {
        // "Batch No.:" at the very end of the cell, nothing after it to redact.
        var document = TestDocx.Read([TestDocx.Grid([["Batch No.:"]])]);
        var redacted = DocumentRedactor.Redact(document);

        Assert.True(redacted.IsFailure);
        Assert.Equal(RedactionErrors.UnrecognizedValueShape, redacted.Error);
    }

    [Fact]
    public void Redact_leaves_non_run_data_labels_untouched()
    {
        var document = TestDocx.Read([TestDocx.Grid([["Product Name: Widget Cream"]])]);
        var redacted = DocumentRedactor.Redact(document);

        Assert.True(redacted.IsSuccess);
        Assert.Contains("Widget Cream", redacted.Value.FullText);
    }

    // -----------------------------------------------------------------
    // AnthropicWorksheetExtractor — fake HttpMessageHandler only, never the network
    // -----------------------------------------------------------------

    private static AnthropicWorksheetExtractor MakeExtractor(FakeHandler handler, string apiKey = "test-key") =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://unit-test.invalid/") },
            new AnthropicSettings(apiKey, "claude-opus-5-5", TimeSpan.FromSeconds(5)),
            NullLogger<AnthropicWorksheetExtractor>.Instance);

    private static RedactedDocument SimpleDocument(string text) => new()
    {
        HeaderText = "",
        Blocks = [],
        FullText = text
    };

    [Fact]
    public async Task ExtractAsync_forces_Low_confidence_and_AiExtracted_flag_even_when_the_fake_response_claims_High()
    {
        var document = SimpleDocument("pH of solution: 6.8 measured at start.");
        var handler = FakeHandler.ToolUse(new
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

        var extractor = MakeExtractor(handler);
        var result = await extractor.ExtractAsync(document, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value.Flags, flag => flag.Code == WorksheetImportFlagCodes.AiExtracted);
        var spec = Assert.Single(result.Value.SpecificationProposals);
        Assert.Equal(ImportConfidence.Low, spec.Confidence);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task ExtractAsync_refuses_an_ungrounded_sourceQuote()
    {
        var document = SimpleDocument("pH of solution: 6.8 measured at start.");
        var handler = FakeHandler.ToolUse(new
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

        var result = await MakeExtractor(handler).ExtractAsync(document, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(WorksheetImportErrors.AiResponseUngrounded, result.Error);
    }

    [Fact]
    public async Task ExtractAsync_refuses_a_malformed_response_rather_than_partially_accepting_it()
    {
        var document = SimpleDocument("pH of solution: 6.8.");
        var handler = new FakeHandler((request, cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ not valid json", Encoding.UTF8, "application/json")
            }));

        var result = await MakeExtractor(handler).ExtractAsync(document, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(WorksheetImportErrors.AiResponseUngrounded, result.Error);
    }

    [Fact]
    public async Task ExtractAsync_refuses_when_no_api_key_is_configured_without_calling_the_handler()
    {
        var document = SimpleDocument("pH of solution: 6.8.");
        var handler = FakeHandler.ToolUse(new { template = new { name = "x", sections = Array.Empty<object>() } });

        var result = await MakeExtractor(handler, apiKey: "").ExtractAsync(document, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(WorksheetImportErrors.AiExtractionUnavailable, result.Error);
        Assert.Equal(0, handler.CallCount);
    }

    // -----------------------------------------------------------------
    // WorksheetDocxImportService — permission gate and built-family exclusion
    // -----------------------------------------------------------------

    private sealed class StubCatalogLoader : IWorksheetImportCatalogLoader
    {
        public Task<IWorksheetImportCatalog> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IWorksheetImportCatalog>(InMemoryWorksheetImportCatalog.Empty);
    }

    private sealed class RecordingExtractor : IAiWorksheetExtractor
    {
        public int CallCount { get; private set; }

        public Task<Result<AiExtractionResult>> ExtractAsync(RedactedDocument document, CancellationToken cancellationToken)
        {
            CallCount++;
            var template = new ProposedWorksheetTemplate
            {
                Name = "AI Proposed Template",
                Sections = [new ProposedWorksheetSection { Name = "General", Fields = [] }]
            };
            return Task.FromResult(Result.Success(new AiExtractionResult(
                template, [], [new WorksheetImportFlag { Code = WorksheetImportFlagCodes.AiExtracted, Message = "x" }], 10, 10)));
        }
    }

    private static IFormFile ToFormFile(MemoryStream stream, string fileName)
    {
        stream.Position = 0;
        return new FormFile(stream, 0, stream.Length, "file", fileName);
    }

    [Fact]
    public async Task Without_the_permission_an_unrecognized_ard_still_gets_the_plain_refusal_and_never_calls_the_extractor()
    {
        // Header text with no recognized family marker and not "STANDARD TEST PROCEDURE" — the
        // "otherwise" case brief 10 targets.
        var stream = TestDocx.Create([TestDocx.P("Some genuinely new layout with no known markers.")],
            "ANALYTICAL WORKSHEET (chemical)");
        var extractor = new RecordingExtractor();
        var service = new WorksheetDocxImportService(new StubCatalogLoader(), extractor);

        var proposals = await service.ProposeAsync([ToFormFile(stream, "unknown.docx")], allowAiExtraction: false);

        var proposal = Assert.Single(proposals);
        Assert.Equal(ArdFamily.Unknown, proposal.Family);
        Assert.Contains(proposal.Flags, flag => flag.Code == WorksheetImportFlagCodes.UnknownFamily);
        Assert.DoesNotContain(proposal.Flags, flag => flag.Code == WorksheetImportFlagCodes.AiExtracted);
        Assert.Equal(0, extractor.CallCount);
    }

    [Fact]
    public async Task With_the_permission_an_unrecognized_ard_is_sent_to_the_extractor_and_gets_AiExtracted()
    {
        var stream = TestDocx.Create([TestDocx.P("Some genuinely new layout with no known markers.")],
            "ANALYTICAL WORKSHEET (chemical)");
        var extractor = new RecordingExtractor();
        var service = new WorksheetDocxImportService(new StubCatalogLoader(), extractor);

        var proposals = await service.ProposeAsync([ToFormFile(stream, "unknown.docx")], allowAiExtraction: true);

        var proposal = Assert.Single(proposals);
        Assert.Equal(1, extractor.CallCount);
        Assert.Contains(proposal.Flags, flag => flag.Code == WorksheetImportFlagCodes.AiExtracted);
        Assert.Equal("AI Proposed Template", proposal.Template?.Name);
    }

    [Fact]
    public async Task A_standard_test_procedure_keeps_refusing_exactly_as_before_even_with_the_permission()
    {
        var stream = TestDocx.Create([TestDocx.P("Body text.")], "STANDARD TEST PROCEDURE");
        var extractor = new RecordingExtractor();
        var service = new WorksheetDocxImportService(new StubCatalogLoader(), extractor);

        var proposals = await service.ProposeAsync([ToFormFile(stream, "stp.docx")], allowAiExtraction: true);

        var proposal = Assert.Single(proposals);
        Assert.Contains(proposal.Flags, flag => flag.Code == WorksheetImportFlagCodes.UnknownFamily &&
                                                  flag.Message.Contains("Standard Test Procedure"));
        Assert.Equal(0, extractor.CallCount);
    }

    [Fact]
    public async Task A_file_matching_a_built_family_never_reaches_the_extractor_even_with_the_permission()
    {
        // Culture media marker: a built family with its own recognizer.
        var stream = TestDocx.Create([TestDocx.Grid([["Culture Medium Name: Tryptone Soya Agar"]])]);
        var extractor = new RecordingExtractor();
        var service = new WorksheetDocxImportService(new StubCatalogLoader(), extractor);

        var proposals = await service.ProposeAsync([ToFormFile(stream, "media.docx")], allowAiExtraction: true);

        var proposal = Assert.Single(proposals);
        Assert.Equal(ArdFamily.CultureMedia, proposal.Family);
        Assert.Equal(0, extractor.CallCount);
    }

    [Fact]
    public void The_ai_permission_key_is_grantable_under_the_worksheet_template_submodule()
    {
        var permission = Assert.Single(PermissionUtils.GeneratePermissions(),
            item => item.Key == QcWorksheetPermissionKeys.CanUseAiWorksheetExtraction);

        Assert.Equal(QcWorksheetPermissionCatalog.Module, permission.Module);
        Assert.Equal(QcWorksheetPermissionCatalog.WorksheetTemplates, permission.SubModule);
    }

    // -----------------------------------------------------------------
    // Real-corpus redaction check — never sent anywhere, just asserted against locally.
    // -----------------------------------------------------------------

    [CorpusFact]
    public void Redacting_every_ARD_corpus_file_removes_its_batch_and_AR_numbers_and_staff_names()
    {
        foreach (var file in ArdCorpus.Load())
        {
            var redacted = DocumentRedactor.Redact(file.Document);
            if (redacted.IsFailure) continue; // a hard refusal never sends anything either

            foreach (var block in file.Document.Blocks)
            {
                var cellsAndText = block.Table is null
                    ? [block.Text]
                    : Enumerable.Range(0, block.Table.Rows.Count).SelectMany(row => block.Table.RowTexts(row));

                foreach (var text in cellsAndText)
                {
                    if (string.IsNullOrWhiteSpace(text)) continue;
                    if (!RunDataLabels.LabelStartRegex().IsMatch(text)) continue;

                    // Any run-data value present in the source next to a recognized label must
                    // not survive into the redacted text.
                    var matches = RunDataLabels.LabelStartRegex().Matches(text);
                    for (var i = 0; i < matches.Count; i++)
                    {
                        var label = matches[i].Groups[1].Value.Trim();
                        if (!RunDataLabels.TryMatch(label, out _)) continue;
                        var start = matches[i].Index + matches[i].Length;
                        var end = i + 1 < matches.Count ? matches[i + 1].Index : text.Length;
                        var value = text[start..end].Trim();
                        if (value.Length < 4) continue; // too short to risk a false positive on placeholders
                        Assert.DoesNotContain(value, redacted.Value.FullText, StringComparison.Ordinal);
                    }
                }
            }
        }
    }

    [RmCorpusFact]
    public void Redacting_every_RM_corpus_file_removes_its_batch_and_AR_numbers_and_staff_names()
    {
        foreach (var file in RmCorpus.Load())
        {
            var redacted = DocumentRedactor.Redact(file.Document);
            if (redacted.IsFailure) continue;

            foreach (var block in file.Document.Blocks)
            {
                var cellsAndText = block.Table is null
                    ? [block.Text]
                    : Enumerable.Range(0, block.Table.Rows.Count).SelectMany(row => block.Table.RowTexts(row));

                foreach (var text in cellsAndText)
                {
                    if (string.IsNullOrWhiteSpace(text)) continue;
                    var matches = RunDataLabels.LabelStartRegex().Matches(text);
                    for (var i = 0; i < matches.Count; i++)
                    {
                        var label = matches[i].Groups[1].Value.Trim();
                        if (!RunDataLabels.TryMatch(label, out _)) continue;
                        var start = matches[i].Index + matches[i].Length;
                        var end = i + 1 < matches.Count ? matches[i + 1].Index : text.Length;
                        var value = text[start..end].Trim();
                        if (value.Length < 4) continue;
                        Assert.DoesNotContain(value, redacted.Value.FullText, StringComparison.Ordinal);
                    }
                }
            }
        }
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

        public static FakeHandler ToolUse(object toolInput) => new((_, _) =>
        {
            var envelope = new
            {
                content = new object[]
                {
                    new { type = "tool_use", name = "propose_worksheet_template", input = toolInput }
                },
                usage = new { input_tokens = 123, output_tokens = 45 }
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(envelope)
            });
        });
    }
}
