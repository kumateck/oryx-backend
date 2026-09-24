using APP.Services.QcWorksheets.WorksheetDocxImport;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using DOMAIN.Entities.QcWorksheets;
using Xunit;
using static APP.Tests.QcWorksheets.WorksheetImport.TestDocx;

namespace APP.Tests.QcWorksheets.WorksheetImport;

public class EnvironmentalMonitoringRecognizerTests
{
    private const string Header =
        "ENVIRONMENTAL MONITORING  RAW DATA - MICROBIOLOGY\tA.R. No.: QCD/MIC/EN/001\tIssue No.: 26/0001\tSampled On: 01/02/2026\tSampled by: Test Sampler";

    private static TableRow[] RoomHeader() =>
    [
        R(C("Room No. | ID No.", merge: VMerge.Restart), C("Room/Area Name", merge: VMerge.Restart), C("Airborne Viables")),
        R(C(merge: VMerge.Continue), C(merge: VMerge.Continue), C("(CFU/4Hrs)"))
    ];

    private static IEnumerable<OpenXmlElement> Worksheet() =>
    [
        Grid(["Analysis Start Date:", "Analysis End Date:"]),
        P("A. MICROBIAL ENUMERATION TESTS"),
        T(R(C("Growth Promotion and Sterility of the Culture Media", span: 2)), R(C("Medium Name"), C("Test Soya Agar")),
          R(C("Medium Batch no:"), C("QCD/26/001/0000000001")), R(C("Remark"), C("Complies / Does not comply"))),
        P("TEST"),
        P("Airborne Viables"),
        Grid(["Method", "Plate Exposure"], ["Period of Exposure", "4 Hours"], ["Cleanroom Classification", "100,000 / D"], ["Conditional State", "Operational, Dynamic"]),
        P("Results"),
        // Split at a page break, header repeated: one list.
        T([.. RoomHeader(), R(C("XX-01"), C("Corridor"), C()), R(C("XX-02"), C("Dispensing Booth"), C()), R(C("NA"), C("Sampling"), C())]),
        T([.. RoomHeader(), R(C("XX-03"), C("Wash"), C()), R(C("XX-01"), C("Corridor"), C()), R(C("XX-03"), C("Blending"), C())]),
        Grid(["AREA", "SPECIFICATION"], ["Rooms", "NMT 100 cfu/4Hrs"], ["Dispensing Booth", "NMT 5 cfu/4Hrs"], ["LAF Bench", "NMT 5 cfu/4Hrs"]),
        P("Remark: Comply / Do not Comply"),
        Grid(["EQUIPMENT USED", "EQUIPMENT CODE"], ["BOD Incubator", "QCD/EQT/BOD/001"]),
        P("Comment: The above stated rooms/areas comply / do not comply with the requirements of the above tests as per specifications."),
        Grid(["Analysed By: Checked By: Date: Date:"])
    ];

    private static IEnumerable<OpenXmlElement> Certificate() =>
    [
        T(R(C("Room No. | ID No.", merge: VMerge.Restart), C("Room/Area Name", merge: VMerge.Restart), C("Airborne Viables", span: 3)),
          R(C(merge: VMerge.Continue), C(merge: VMerge.Continue), C("Specification (CFU/4Hrs)", span: 2), C("Result", merge: VMerge.Restart)),
          R(C(merge: VMerge.Continue), C(merge: VMerge.Continue), C("Alert Limit"), C("Action Limit"), C(merge: VMerge.Continue)),
          R(C("XX-01"), C("Corridor"), C("NMT 80"), C("NMT 100"), C("7")),
          R(C("XX-02"), C("Dispensing Booth"), C("NMT 3"), C("NMT 5"), C("0")),
          R(C("XX-03"), C("Wash"), C("NMT 80", span: 2), C("NMT 100"), C("2")))
    ];

    private static WorksheetImportProposal Propose(string fileName, IEnumerable<OpenXmlElement> body, string header)
    {
        using var stream = Create(body, header);
        return WorksheetDocxImportService.Propose(fileName, stream, InMemoryWorksheetImportCatalog.Empty);
    }

    private static WorksheetImportProposal Em() => Propose("EM WORKSHEET (Test Area).docx", Worksheet(), Header);

    [Fact]
    public void The_template_is_one_rooms_count_with_its_method_constants()
    {
        var proposal = Em();
        var fields = proposal.Template.Sections.SelectMany(section => section.Fields).ToDictionary(field => field.FieldKey);

        Assert.Equal((ArdFamily.EnvironmentalMonitoring, "EM-TEST-AREA"), (proposal.Family, proposal.Template.Code));
        Assert.Equal((WorksheetFieldType.ColonyCount, WorksheetFieldMode.Entry, "CFU/4Hrs"),
            (fields["airborne_viables"].Type, fields["airborne_viables"].Mode, fields["airborne_viables"].Unit));
        Assert.Equal("100,000 / D", fields["cleanroom_classification"].ConstantValue);
        Assert.Equal("Operational, Dynamic", fields["conditional_state"].ConstantValue);
        Assert.Equal(WorksheetFieldMode.Entry, fields["analysis_start_date"].Mode);
        Assert.Equal(WorksheetFieldType.ReferencedResult, fields["media_qualification_test_soya_agar"].Type);
        Assert.Equal(["Complies", "Does not comply"], fields["remark"].Options);
        Assert.Equal(["Complies", "Does not comply"], fields["comment"].Options);
        Assert.DoesNotContain(fields.Values, field => field.Type == WorksheetFieldType.Table);

        var texts = fields.Values.SelectMany(field => new[] { field.Label, field.ConstantValue ?? string.Empty }).ToList();
        foreach (var runData in new[] { "XX-0", "Corridor", "QCD/MIC/EN", "Test Sampler", "26/0001" })
            Assert.DoesNotContain(texts, text => text.Contains(runData));
    }

    [Fact]
    public void Rooms_become_points_in_their_tiers()
    {
        var proposal = Em();

        Assert.Equal(["XX-01", "XX-02", "NA", "XX-03"], proposal.SamplingPointProposals.Select(point => point.Code));
        Assert.All(proposal.SamplingPointProposals, point =>
            Assert.Equal(("Test Area", SamplingPointType.Environmental), (point.Area, point.Type)));
        Assert.Equal(["Rooms", "Dispensing Booth", "Rooms", "Rooms"], proposal.SamplingPointProposals.Select(point => point.GroupName));

        Assert.Contains(proposal.Flags, flag => flag.Message.Contains("has no room code"));
        Assert.Contains(proposal.Flags, flag => flag.Message.Contains("'XX-01' (Corridor) is listed twice"));
        Assert.Contains(proposal.Flags, flag => flag.Message.Contains("printed for two different rooms ('Wash' and 'Blending')"));
        Assert.Contains(proposal.Flags, flag => flag.Message.Contains("'LAF Bench'") && flag.Message.Contains("matches none"));
        Assert.Contains(proposal.Flags, flag => flag.Message.Contains("'Test Area' was taken from the file name"));

        var specs = proposal.SpecificationProposals;
        Assert.Equal(["Rooms", "Dispensing Booth", "LAF Bench"], specs.Select(spec => spec.GroupName));
        Assert.All(specs, spec => Assert.Equal((spec.AcceptanceCriteria, "airborne_viables"), (spec.ActionLimit, spec.SourceFieldKey)));
        Assert.All(specs, spec => Assert.Null(spec.AlertLimit));
    }

    [Fact]
    public void A_certificate_in_the_same_upload_suggests_alert_limits()
    {
        var worksheet = Em();
        var certificate = Propose("Some COA.docx", Certificate(), "QUALITY CONTROL MICROBIOLOGY CERTIFICATE OF ANALYSIS");

        WorksheetDocxImportService.ApplyBatchRules([worksheet, certificate], InMemoryWorksheetImportCatalog.Empty);

        var rooms = worksheet.SpecificationProposals.Single(spec => spec.GroupName == "Rooms");
        var booth = worksheet.SpecificationProposals.Single(spec => spec.GroupName == "Dispensing Booth");
        Assert.Equal(("NMT 80 cfu/4Hrs", "from COA 'Some COA.docx' (2 of 3 points)"), (rooms.AlertLimit, rooms.AlertLimitSource));
        Assert.Equal("NMT 3 cfu/4Hrs", booth.AlertLimit);
        Assert.Null(certificate.Template);
    }

    [Theory]
    [InlineData("ENVIRONMENTAL MONITORING WORKSHEET (Beta Oral Solids).docx", "Beta Oral Solids")]
    [InlineData("EM (Non-Beta Main Warehouse) copy.docx", "Non-Beta Main Warehouse")]
    [InlineData("EM worksheet.docx", null)]
    public void The_area_comes_from_the_file_name(string fileName, string? area) =>
        Assert.Equal(area, EnvironmentalMonitoringRecognizer.AreaFromFileName(fileName));
}
