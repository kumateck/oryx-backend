using APP.Services.QcWorksheets;
using APP.Services.StpRevisions;
using DOMAIN.Entities.StpRevisions;
using Xunit;

namespace APP.Tests.StpRevisions;

/// <summary>Import → export → import gives the same content, for every client STP.</summary>
public class StpDocxRoundTripTests
{
    internal static StpRevision FromProposal(StpImportProposalDto proposal) => new()
    {
        Id = Guid.NewGuid(), Kind = proposal.Kind, StpNumber = proposal.StpNumber.Value,
        RevisionNumber = int.Parse(proposal.RevisionNumber.Value ?? "0"),
        SupersedesRevision = int.TryParse(proposal.SupersedesRevision.Value, out var before) ? before : null,
        Title = proposal.Title.Value, Area = proposal.Area.Value, Reference = proposal.Reference.Value,
        EffectiveDate = new DateTime(2026, 12, 1), ReviewDate = new DateTime(2028, 12, 1),
        Sections = proposal.Sections.Where(item => !string.IsNullOrWhiteSpace(item.Text))
            .Select(item => new StpRevisionSection { Kind = item.Kind, Order = (int)item.Kind, Text = item.Text }).ToList(),
        Tests = proposal.Tests.Select((test, index) => new StpRevisionTest
        {
            TestKey = test.TestKey, Number = test.Number, Title = test.Title, Order = index + 1,
            Steps = test.Steps.Select((step, position) => new StpRevisionTestStep
            {
                Order = position + 1, Kind = step.Kind, Number = step.Number, Heading = step.Heading, Text = step.Text, ReferenceCode = step.ReferenceCode,
            }).ToList(),
        }).ToList(),
    };

    private static string Content(StpImportProposalDto proposal) => string.Join("\n",
        new[] { proposal.StpNumber.Value, proposal.Title.Value, proposal.RevisionNumber.Value, proposal.SupersedesRevision.Value, proposal.Area.Value, proposal.Reference.Value }
            .Concat(proposal.Sections.Where(item => item.Text is not null).Select(item => $"[{item.Kind}] {item.Text}"))
            .Concat(proposal.Tests.SelectMany(test => new[] { $"T {test.Number} {test.Title} {test.TestKey}" }
                .Concat(test.Steps.Select(step => $"  {step.Kind} {step.Number} | {step.Heading} | {step.Text} | {step.ReferenceCode}")))));

    [Theory]
    [MemberData(nameof(StpDocxImportTests.Files), MemberType = typeof(StpDocxImportTests))]
    public void An_exported_stp_imports_back_to_the_same_content(string file, string code)
    {
        var original = StpDocxImportTests.Propose(file);
        var revision = FromProposal(original);
        var signed = new StpSignature("Ama Owusu", "QC Officer", new DateTime(2026, 11, 27));
        var (content, sha, name) = StpDocxExport.Write(revision, signed, signed, signed, draft: false);
        Assert.Matches("^[0-9a-f]{64}$", sha);
        Assert.EndsWith(".docx", name);
        using var stream = new MemoryStream(content);
        var again = new StpDocxImportService().Propose(stream, name, original.Kind);
        Assert.True(again.Succeeded, again.FailureReason);
        Assert.Equal(code, again.StpNumber.Value);
        Assert.Equal("01/12/2026", again.EffectiveDate.Value);
        Assert.Equal(Content(original), Content(again));
        // Nothing the exporter wrote needs checking, apart from reagent lines the reader always splits at dashes.
        Assert.All(again.Tests, test => Assert.Equal(StpImportConfidence.High, test.Confidence));
    }

    [Fact]
    public void A_draft_export_says_so_in_its_header_and_file_name()
    {
        var revision = FromProposal(StpDocxImportTests.Propose("009 Xzole susp.docx"));
        var (content, _, name) = StpDocxExport.Write(revision, null, null, null, draft: true);
        Assert.Contains("DRAFT", name);
        using var stream = new MemoryStream(content);
        var document = APP.Services.QcWorksheets.WorksheetDocxImport.DocxDocumentReader.Read(stream);
        Assert.Contains("DRAFT, NOT APPROVED", document.HeaderText);
    }
}
