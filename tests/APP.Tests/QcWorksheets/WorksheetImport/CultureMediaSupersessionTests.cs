using APP.Services.QcWorksheets.WorksheetDocxImport;
using DocumentFormat.OpenXml;
using DOMAIN.Entities.QcWorksheets;
using Xunit;
using static APP.Tests.QcWorksheets.WorksheetImport.TestDocx;

namespace APP.Tests.QcWorksheets.WorksheetImport;

/// <summary>The newer media form wins, but an older form with no newer version stays importable.</summary>
public class CultureMediaSupersessionTests
{
    private const string Header = "MICROBIOLOGY ANALYTICAL WORKSHEET\tCULTURE MEDIUM BATCH DATA";

    private static WorksheetImportProposal Propose(string file, IEnumerable<OpenXmlElement> body)
    {
        using var stream = Create(body, Header);
        return WorksheetDocxImportService.Propose(file, stream, InMemoryWorksheetImportCatalog.Empty);
    }

    private static WorksheetImportProposal OldForm(string medium) => Propose($"old {medium}.docx", [
        Grid(["Lot No.: 0000000042", $"Culture Medium Name: | {medium}", "Date Received:"],
             ["Date of Mfg.: 08/2021", "", "Date opened:"])]);

    private static WorksheetImportProposal NewForm(string medium, string code) => Propose($"new {medium}.docx", [
        Grid(["Batch No.: QCD/26/002/0000000042", $"Culture Medium Name: | {medium}", $"Medium Code: {code}"],
             ["Issue No.: 26/0001", "Issued By: Test Issuer", "Analysis Start Date:"])]);

    private static string[] SupersessionFlags(WorksheetImportProposal proposal) =>
        proposal.Flags.Select(flag => flag.Code)
            .Where(code => code is WorksheetImportFlagCodes.SupersededFormat or WorksheetImportFlagCodes.SupersededFormatBlocked)
            .ToArray();

    [Fact]
    public void An_older_form_alone_keeps_only_the_warning()
    {
        var old = OldForm("Enrichment Broth");

        CultureMediaSupersession.Apply([old], InMemoryWorksheetImportCatalog.Empty);

        Assert.Equal([WorksheetImportFlagCodes.SupersededFormat], SupersessionFlags(old));
    }

    [Fact]
    public void A_newer_twin_in_the_same_upload_blocks_the_older_form()
    {
        // Same medium, printed with different capitalisation on the two forms.
        var old = OldForm("Plate count Agar");
        var twin = NewForm("Plate Count Agar", "QCD/RGT/PCA-001");

        CultureMediaSupersession.Apply([old, twin], InMemoryWorksheetImportCatalog.Empty);

        Assert.Equal([WorksheetImportFlagCodes.SupersededFormatBlocked], SupersessionFlags(old));
        Assert.Contains("new Plate Count Agar.docx", old.Flags[0].Message);
        Assert.Empty(SupersessionFlags(twin));
    }

    [Fact]
    public void A_saved_newer_template_blocks_the_older_form()
    {
        var old = OldForm("Test Agar");
        var catalog = new InMemoryWorksheetImportCatalog([], [],
            [new CatalogTemplate(Guid.NewGuid(), "QCD/RGT/TA-001", "Culture Media Qualification – Test Agar")]);

        CultureMediaSupersession.Apply([old], catalog);

        Assert.Equal([WorksheetImportFlagCodes.SupersededFormatBlocked], SupersessionFlags(old));
        Assert.Contains("QCD/RGT/TA-001", old.Flags[0].Message);
    }

    [Fact]
    public void A_different_medium_is_not_a_twin()
    {
        var old = OldForm("MacConkey Broth");

        CultureMediaSupersession.Apply([old, NewForm("MacConkey Agar", "QCD/RGT/MA-001")],
            new InMemoryWorksheetImportCatalog([], [], [new CatalogTemplate(Guid.NewGuid(), "X", "Culture Media Qualification – MacConkey Agar")]));

        Assert.Equal([WorksheetImportFlagCodes.SupersededFormat], SupersessionFlags(old));
    }

    [Fact]
    public void Identities_with_conflicting_codes_are_different_media()
    {
        var left = CultureMediaSupersession.Identify("Test Agar", "QCD/RGT/TA-001");

        Assert.True(CultureMediaSupersession.SameMedium(left, CultureMediaSupersession.Identify("TEST  agar", null)));
        Assert.True(CultureMediaSupersession.SameMedium(left, CultureMediaSupersession.Identify("Test Agar", "qcd/rgt/ta-001")));
        Assert.False(CultureMediaSupersession.SameMedium(left, CultureMediaSupersession.Identify("Test Agar", "QCD/RGT/TA-002")));
        Assert.Null(CultureMediaSupersession.Identify("", "QCD/RGT/TA-001"));
    }

    [Fact]
    public void A_saved_template_is_found_by_medium_code_before_name()
    {
        var byCode = new CatalogTemplate(Guid.NewGuid(), "QCD/RGT/TA-001", "Renamed qualification sheet");
        var catalog = new InMemoryWorksheetImportCatalog([], [],
            [new CatalogTemplate(Guid.NewGuid(), "OTHER", "Culture Media Qualification – Test Agar"), byCode]);

        Assert.Same(byCode, catalog.FindMediaTemplate("Test Agar", "QCD/RGT/TA-001"));
        Assert.Equal("OTHER", catalog.FindMediaTemplate("Test Agar")?.Code);
        Assert.Null(catalog.FindMediaTemplate("Agar"));
    }
}
