using System.Text.Json;
using APP.Services.QcWorksheets.WorksheetDocxImport;
using DocumentFormat.OpenXml;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.AspNetCore.Http;
using Xunit;
using static APP.Tests.QcWorksheets.WorksheetImport.TestDocx;

namespace APP.Tests.QcWorksheets.WorksheetImport;

public class CultureMediaRecognizerTests
{
    private const string Header = "MICROBIOLOGY ANALYTICAL WORKSHEET\tCULTURE MEDIUM BATCH DATA\tPage 1 of 3";

    // Synthetic run data: every one of these must be discarded.
    private static readonly string[] RunData = ["QCD/26/002/0000000042", "26/0999", "01/02/2026", "Test Issuer", "QCD/26/001/0000000041"];

    private static IEnumerable<OpenXmlElement> NewForm() =>
    [
        T(R(C("Batch No.: | QCD/26/002/0000000042", merge: VMerge.Restart), C("Culture Medium Name: | Test Agar"), C("Medium Code: QCD/RGT/TA-001")),
          R(C(merge: VMerge.Continue), C("Issue No.: 26/0999"), C("Analysis Start Date:")),
          R(C("Issue Date: 01/02/2026"), C("Issued By: Test Issuer"), C("Format No.: QCD/SOP/082/F03-02"))),
        P("["),
        P("Appearance: Cream homogenous powder"),
        P("Quantity of dehydrated powder weighed: ……………"),
        P("pH Range: 7.2 ± 0.2\t\tObserved:………"),
        P("PREVIOUSLY APPROVED BATCH"),
        P("Batch no.: QCD/26/001/0000000041"),
        P("Sterility (After 5 days of incubation): ……"),
        P("Cultural Response"),
        T(R(C("Test strain", merge: VMerge.Restart), C("Strain code", merge: VMerge.Restart), C("Incu- | bation | period", merge: VMerge.Restart),
              C("New Batch", span: 3), C("Previously Approved Batch", span: 3)),
          R(C(merge: VMerge.Continue), C(merge: VMerge.Continue), C(merge: VMerge.Continue),
              C("Plate 1"), C("Plate 2"), C("Av."), C("Plate 1"), C("Plate 2"), C("Av.")),
          R(C("Pseudo-monas aeruginosa (ATCC 9027)"), C("QCD/ | STR/P1-01"), C("18 hours"), C(), C(), C(), C(), C(), C()),
          R(C("Esche-richia coli (ATCC 8739)"), C("QCD/ | STR/E1-01"), C("72 hours"), C(), C(), C(), C(), C(), C())),
        P("Negative control: There was / was no growth of microorganisms on the plates."),
        P("Growth promoting property (Pseudomonas aeruginosa ATCC 9027): The difference between the average number of colony forming units observed on the two batches was / was not more than factor 2. Colonies did / did not produce green pigmentation."),
        P("Remark: Complies/ Does not comply"),
        Grid(["EQUIPMENT USED", "EQUIPMENT CODE"], ["Weighing Balance", "QCD/EQT/BAL/001"]),
        Grid(["", "DONE BY", "CHECKED BY"], ["SIGNATURE", "", ""]),
        P("REFERENCES"),
        P("QCD/SOP/044"),
        P("BP")
    ];

    private static WorksheetImportProposal Propose(IEnumerable<OpenXmlElement> body, string header = Header, IWorksheetImportCatalog? catalog = null)
    {
        using var stream = Create(body, header);
        return WorksheetDocxImportService.Propose("sheet.docx", stream, catalog ?? InMemoryWorksheetImportCatalog.Empty);
    }

    private static List<ProposedWorksheetField> Fields(WorksheetImportProposal proposal) =>
        proposal.Template.Sections.SelectMany(section => section.Fields).ToList();

    [Fact]
    public void The_new_form_becomes_a_media_qualification_template()
    {
        var proposal = Propose(NewForm());
        var fields = Fields(proposal);

        Assert.Equal((ArdFamily.CultureMedia, "New"), (proposal.Family, proposal.FormatVersion));
        Assert.DoesNotContain(proposal.Flags, flag => flag.Code == WorksheetImportFlagCodes.SupersededFormat);
        Assert.Equal(WorksheetCategory.MediaQualification, proposal.Template.Category);
        Assert.Equal(("QCD/RGT/TA-001", "Culture Media Qualification – Test Agar"), (proposal.Template.Code, proposal.Template.Name));

        var byKey = fields.ToDictionary(field => field.FieldKey);
        Assert.Equal(WorksheetFieldType.Reagent, byKey[CultureMediaKeys.Medium].Type);
        Assert.Equal(["Complies", "Does not comply"], byKey[CultureMediaKeys.Remark].Options);
        Assert.Equal(WorksheetFieldMode.Constant, byKey["appearance"].Mode);
        Assert.Equal((WorksheetFieldMode.Entry, WorksheetFieldType.Number), (byKey["quantity_of_dehydrated_powder_weighed"].Mode, byKey["quantity_of_dehydrated_powder_weighed"].Type));
        Assert.Equal(WorksheetFieldMode.Entry, byKey["previously_approved_batch_no"].Mode);
        Assert.Equal(WorksheetFieldType.GrowthObservation, byKey["negative_control"].Type);
        Assert.Equal(2, fields.Count(field => field.Type == WorksheetFieldType.Select && field.Label.StartsWith("Growth promoting")));
        Assert.Single(fields, field => field.Type == WorksheetFieldType.Instrument);
        Assert.Equal("QCD/SOP/044; BP", byKey["references"].ConstantValue);

        // Media limits stay on the sheet: an inline, read-only acceptance range beside the
        // observed entry, and no Specification proposals at all.
        Assert.Equal((WorksheetFieldType.Result, WorksheetFieldMode.Constant, "7.2 ± 0.2 (7.0 – 7.4)"),
            (byKey["ph_range"].Type, byKey["ph_range"].Mode, byKey["ph_range"].ConstantValue));
        Assert.Equal((WorksheetFieldType.Number, WorksheetFieldMode.Entry), (byKey["ph_observed"].Type, byKey["ph_observed"].Mode));
        Assert.Empty(proposal.SpecificationProposals);
        Assert.Equal(("Test Agar", "testagar", "qcdrgtta001"), (proposal.Medium.Name, proposal.Medium.NameKey, proposal.Medium.CodeKey));
    }

    [Fact]
    public void Run_data_never_reaches_a_constant_label_or_fixed_cell()
    {
        var proposal = Propose(NewForm());
        var texts = Fields(proposal)
            .SelectMany(field => new[] { field.Label, field.ConstantValue ?? string.Empty, field.ColumnDefinitions ?? string.Empty })
            .Append(proposal.Template.Name).Append(proposal.Template.Code).ToList();

        foreach (var value in RunData)
            Assert.DoesNotContain(texts, text => text.Contains(value));
        Assert.DoesNotContain(Fields(proposal), field => field.FieldKey is "issue_no" or "issued_by" or "issue_date" or "format_no");
    }

    [Fact]
    public void Strain_rows_are_fixed_columns_and_plate_averages_are_calculated()
    {
        var response = Fields(Propose(NewForm())).Single(field => field.FieldKey == "cultural_response");
        var columns = JsonDocument.Parse(response.ColumnDefinitions!).RootElement.EnumerateArray().ToList();

        string[] Fixed(string key) => columns.Single(column => column.GetProperty("key").GetString() == key)
            .GetProperty("fixedValues").EnumerateArray().Select(value => value.GetString()!).ToArray();

        Assert.Equal(["Pseudomonas aeruginosa (ATCC 9027)", "Escherichia coli (ATCC 8739)"], Fixed("testStrain"));
        Assert.Equal(["QCD/STR/P1-01", "QCD/STR/E1-01"], Fixed("strainCode"));
        Assert.Equal(["18 hours", "72 hours"], Fixed("incubationPeriod"));
        var average = columns.Single(column => column.GetProperty("key").GetString() == "newBatch_av");
        Assert.Equal("({newBatch_plate1} + {newBatch_plate2}) / 2", average.GetProperty("formula").GetString());
        Assert.Equal("New Batch", average.GetProperty("group").GetString());
    }

    [Fact]
    public void The_old_form_is_flagged_superseded()
    {
        var proposal = Propose([
            Grid(["Lot No.: 0000000042", "Culture Medium Name: | Test Agar", "Date Received:"],
                 ["Date of Mfg.: 08/2021", "", "Date opened:"],
                 ["Date of Expiry: 07/2026", "", "Analysis Start Date:"]),
            P("Remark: Complies/ Does not comply")
        ]);

        Assert.Equal("Superseded", proposal.FormatVersion);
        Assert.Contains(proposal.Flags, flag => flag.Code == WorksheetImportFlagCodes.SupersededFormat);
        Assert.DoesNotContain(Fields(proposal), field => (field.ConstantValue ?? string.Empty).Contains("2021"));
    }

    [Fact]
    public void Catalog_matches_replace_unmatched_flags()
    {
        var catalog = new InMemoryWorksheetImportCatalog(
            [new CatalogEquipment(Guid.NewGuid(), "QCD/EQT/BAL/001", "Balance")], [new CatalogReagent(Guid.NewGuid(), "Test Agar")], []);

        var proposal = Propose(NewForm(), catalog: catalog);

        Assert.DoesNotContain(proposal.Flags, flag => flag.Code is WorksheetImportFlagCodes.UnmatchedEquipment or WorksheetImportFlagCodes.UnmatchedReagent);
        Assert.NotNull(Assert.Single(proposal.ReagentMatches).ReagentId);
    }

    [Fact]
    public async Task The_mapped_proposal_is_accepted_by_the_real_create_endpoint_logic()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = (await harness.SeedUser()).Id;

        var request = WorksheetImportTemplateMapper.ToCreateRequest(Propose(NewForm()).Template);
        var remark = request.Sections.SelectMany(section => section.Fields).Single(field => field.FieldKey == CultureMediaKeys.Remark);
        Assert.Equal("[\"Complies\",\"Does not comply\"]", remark.OptionsJson);
        Assert.DoesNotContain(request.Sections.SelectMany(section => section.Fields),
            field => field.OptionsJson is not null && field.Type is not (WorksheetFieldType.Select or WorksheetFieldType.GrowthObservation));

        var result = await harness.Templates.CreateTemplate(request, userId);

        Assert.True(result.IsSuccess, result.Error?.Description);
        Assert.Equal(QcDocumentStatus.Draft, result.Value.Status);
        var saved = result.Value.Sections.SelectMany(section => section.Fields).Single(field => field.FieldKey == CultureMediaKeys.Remark);
        Assert.Equal("[\"Complies\",\"Does not comply\"]", saved.OptionsJson);
    }

    [Theory]
    [InlineData("QUALITY CONTROL MICROBIOLOGY CERTIFICATE OF ANALYSIS", WorksheetImportFlagCodes.CompletedOutputNotTemplate)]
    [InlineData("ANALYTICAL RAW DATA – MICROBIOLOGY", WorksheetImportFlagCodes.RecognizerPending)]
    [InlineData("ENVIRONMENTAL MONITORING RAW DATA", WorksheetImportFlagCodes.RecognizerPending)]
    [InlineData("MICROBIOLOGY ANALYTICAL WORKSHEET WATER", WorksheetImportFlagCodes.RecognizerPending)]
    [InlineData("A MEMO", WorksheetImportFlagCodes.UnknownFamily)]
    public void Other_families_stop_with_a_flag_and_no_template(string header, string flag)
    {
        var proposal = Propose([P("Remark: Complies/ Does not comply")], header);

        Assert.Null(proposal.Template);
        Assert.Contains(proposal.Flags, item => item.Code == flag);
        Assert.NotNull(proposal.Source);
    }

    [Fact]
    public void An_unreadable_file_is_a_flagged_proposal_not_an_exception()
    {
        using var stream = new MemoryStream("not a zip"u8.ToArray());

        var proposal = WorksheetDocxImportService.Propose("broken.docx", stream, InMemoryWorksheetImportCatalog.Empty);

        Assert.Equal(WorksheetImportFlagCodes.InvalidFile, Assert.Single(proposal.Flags).Code);
    }

    [Theory]
    [InlineData("sheet.docx", 10, null)]
    [InlineData("sheet.docm", 10, "Macro-enabled documents (.docm) are not supported.")]
    [InlineData("sheet.pdf", 10, "Only .docx files are supported.")]
    [InlineData("sheet.docx", 0, "The uploaded file is empty.")]
    [InlineData("sheet.docx", DocxUploadGuard.MaxBytes + 1, "The file exceeds the maximum allowed size of 20MB.")]
    public void Uploads_are_guarded(string name, long length, string? expected)
    {
        var file = new FormFile(Stream.Null, 0, length, "files", name);
        Assert.Equal(expected, DocxUploadGuard.Validate(file));
    }
}
