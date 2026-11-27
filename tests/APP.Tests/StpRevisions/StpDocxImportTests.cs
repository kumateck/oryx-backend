using APP.Services.QcWorksheets;
using APP.Tests.QcWorksheets.WorksheetImport;
using DOMAIN.Entities.StpRevisions;
using Xunit;
using static APP.Tests.QcWorksheets.WorksheetImport.TestDocx;

namespace APP.Tests.StpRevisions;

/// <summary>
/// The STP import against the client's own STPs (Fixtures: footers and document properties blanked, so no staff names).
/// </summary>
public class StpDocxImportTests
{
    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "StpRevisions", "Fixtures", name);

    internal static StpImportProposalDto Propose(string name, StpRevisionKind kind = StpRevisionKind.Product)
    {
        using var stream = File.OpenRead(Fixture(name));
        return new StpDocxImportService().Propose(stream, name, kind);
    }

    public static TheoryData<string, string> Files => new()
    {
        { "009 Xzole susp.docx", "QCD/STP/FP/009" },
        { "001 Paracetamol Tabs.docx", "QCD/STP/FP/001" },
        { "006 Tobufen tablets.docx", "QCD/STP/FP/006" },
        { "001 - Ascorbic Acid.docx", "QCD/STP/RM/001" },
        { "004 - Amoxicillin Trihydrate.docx", "QCD/STP/RM/004" },
        { "007 - Artenimol Dihydroartemisinin.docx", "QCD/STP/RM/007" },
        { "012 - Ciprofloxacin HCl.docx", "QCD/STP/RM/012" },
        { "015 - Clindamycin Hydrochloride.docx", "QCD/STP/RM/015" },
        { "020 - Ibuprofen.docx", "QCD/STP/RM/020" },
    };

    [Theory]
    [MemberData(nameof(Files))]
    public void Every_client_stp_reads_its_header_sections_and_tests(string file, string code)
    {
        var proposal = Propose(file);
        Assert.True(proposal.Succeeded, proposal.FailureReason);
        Assert.Equal(code, proposal.StpNumber.Value);
        Assert.Equal(StpImportConfidence.High, proposal.StpNumber.Confidence);
        Assert.False(string.IsNullOrWhiteSpace(proposal.Title.Value));
        Assert.Matches(@"^\d+$", proposal.RevisionNumber.Value);
        Assert.Equal("Quality Control", proposal.Area.Value);
        foreach (var kind in new[] { StpSectionKind.Purpose, StpSectionKind.Scope, StpSectionKind.Responsibility, StpSectionKind.Accountability })
        {
            var section = Assert.Single(proposal.Sections, item => item.Kind == kind);
            Assert.Equal(StpImportConfidence.High, section.Confidence);
            Assert.False(string.IsNullOrWhiteSpace(section.Text));
        }
        var reagents = Assert.Single(proposal.Sections, item => item.Kind == StpSectionKind.Reagents);
        Assert.True(reagents.Text!.Split('\n').Length >= 4);
        Assert.True(proposal.Tests.Count >= 5);
        Assert.All(proposal.Tests, test =>
        {
            Assert.Matches(@"^5\.\d{1,2}$", test.Number);
            Assert.False(string.IsNullOrWhiteSpace(test.Title));
            Assert.NotEmpty(test.Steps);
            Assert.Matches("^[a-z0-9]+(-[a-z0-9]+)*$", test.TestKey);
        });
        Assert.Equal(proposal.Tests.Count, proposal.Tests.Select(test => test.TestKey).Distinct().Count());
        // Nothing is lost: no superscript markers left in concentration units.
        Assert.DoesNotContain(proposal.Tests.SelectMany(test => test.Steps), step => step.Text?.Contains("^w/v") == true);
    }

    [Fact]
    public void Xzole_reads_reagents_conditions_and_numbered_steps()
    {
        var proposal = Propose("009 Xzole susp.docx");
        Assert.Equal("X’Zole & METRONIDAZOLE SUSPENSION", proposal.Title.Value);
        Assert.Equal("02", proposal.RevisionNumber.Value);
        Assert.Equal("01", proposal.SupersedesRevision.Value);
        Assert.Equal("BP", proposal.Reference.Value);
        Assert.Null(proposal.EffectiveDate.Value);

        var reagents = Assert.Single(proposal.Sections, item => item.Kind == StpSectionKind.Reagents);
        Assert.Equal(["Dimethylformamide", "12.5%w/v solution of Ammonium acetate", "2M Acetic acid", "Methanol (HPLC Grade)", "6M Ammonia", "Acetone"],
            reagents.Text!.Split('\n'));
        Assert.Equal(StpImportConfidence.Medium, reagents.Confidence);
        var notes = Assert.Single(proposal.Sections, item => item.Kind == StpSectionKind.ProcedureNotes);
        Assert.Contains("QCD/SOP/003", notes.Text);

        Assert.Equal(["Description", "Identification", "Metronidazole", "pH", "Assay"], proposal.Tests.Select(test => test.Title));
        Assert.Equal(["description", "identification", "metronidazole", "ph", "assay"], proposal.Tests.Select(test => test.TestKey));
        var identification = proposal.Tests[1];
        Assert.Equal(["5.2.1", "5.2.2", "5.2.3", "5.2.4", "5.2.5"], identification.Steps.Select(step => step.Number));
        Assert.All(identification.Steps, step => Assert.Equal(StpStepKind.Step, step.Kind));

        var assay = proposal.Tests[4];
        Assert.Equal(StpStepKind.Conditions, assay.Steps[0].Kind);
        Assert.Equal("Mobile Phase", assay.Steps[0].Heading);
        Assert.Equal(StpStepKind.Conditions, assay.Steps[1].Kind);
        Assert.Equal("Chromatographic Conditions", assay.Steps[1].Heading);
        Assert.Contains("Flowrate: 1.0ml/min", assay.Steps[1].Text);
        Assert.Contains("Column:", assay.Steps[1].Text!.Split('\n')[1]);
        Assert.Equal(["5.5.1", "5.5.2", "5.5.3"], assay.Steps.Skip(2).Select(step => step.Number));
    }

    [Fact]
    public void Paracetamol_keeps_buffer_preparation_as_notes_and_dissolution_set_up_as_conditions()
    {
        var proposal = Propose("001 Paracetamol Tabs.docx");
        var notes = Assert.Single(proposal.Sections, item => item.Kind == StpSectionKind.ProcedureNotes);
        Assert.StartsWith("Preparation of Phosphate buffer 5.8:", notes.Text);
        var dissolution = Assert.Single(proposal.Tests, test => test.Title == "Dissolution");
        Assert.Equal(StpStepKind.Conditions, dissolution.Steps[0].Kind);
        Assert.Contains("Paddle", dissolution.Steps[0].Text);
        Assert.Equal(StpImportConfidence.Medium, dissolution.Steps[0].Confidence);
    }

    [Fact]
    public void Amoxicillin_buffer_line_ends_the_reagents_and_its_gradient_table_is_flagged()
    {
        var proposal = Propose("004 - Amoxicillin Trihydrate.docx");
        var reagents = Assert.Single(proposal.Sections, item => item.Kind == StpSectionKind.Reagents);
        Assert.DoesNotContain("Buffer solution", reagents.Text);
        Assert.Contains("Buffer solution pH 5.0", Assert.Single(proposal.Sections, item => item.Kind == StpSectionKind.ProcedureNotes).Text);
        var gradient = proposal.Tests.SelectMany(test => test.Steps).Single(step => step.Heading == "Gradient Elution Table");
        Assert.Equal(StpImportConfidence.Medium, gradient.Confidence);
        Assert.Contains("Time (min)", gradient.Text);
    }

    [Fact]
    public void Ibuprofen_reads_a_step_number_missing_its_point_and_flags_it()
    {
        var proposal = Propose("020 - Ibuprofen.docx", StpRevisionKind.RawMaterial);
        var assay = Assert.Single(proposal.Tests, test => test.Title == "Assay");
        Assert.Equal(["5.7.1", "5.7.2", "5.7.3", "5.7.4"], assay.Steps.Where(step => step.Kind == StpStepKind.Step).Select(step => step.Number));
        var fused = assay.Steps.Single(step => step.Number == "5.7.3");
        Assert.Equal(StpImportConfidence.Medium, fused.Confidence);
        Assert.Contains("5.73", fused.Flag);
        // The unnumbered description before 5.1 is kept for the person to place.
        Assert.Contains("White or almost white", proposal.Sections.Single(item => item.Kind == StpSectionKind.ProcedureNotes).Text);
        Assert.Contains(proposal.Tests.SelectMany(test => test.Steps), step => step.ReferenceCode == "QCD/SOP/063");
    }

    [Fact]
    public void Numbering_gaps_repeats_and_strays_are_flagged()
    {
        Assert.Contains("Test numbers skip from 5.7 to 5.9.", Propose("012 - Ciprofloxacin HCl.docx").Warnings);
        var clindamycin = Propose("015 - Clindamycin Hydrochloride.docx").Tests.Single(test => test.Number == "5.3");
        Assert.Contains(clindamycin.Steps, step => step.Flag?.Contains("repeats") == true);
        var artenimol = Propose("007 - Artenimol Dihydroartemisinin.docx").Tests.Single(test => test.Number == "5.4");
        Assert.Contains(artenimol.Steps, step => step.Number == "5.5.6" && step.Confidence == StpImportConfidence.Medium);
    }

    [Fact]
    public void A_worksheet_is_not_read_as_an_stp()
    {
        // The analytical worksheets cite an STP number in their header but have no 1.0 to 5.0 sections.
        using var stream = Create([T(R(C("1."), C("Description / Appearance")), R(C("2."), C("Solubility | Water: Ethanol (96%):")))],
            "RAW MATERIAL ANALYTICAL WORKSHEET Batch No.: X1 STP No.: QC/STP/RM/012");
        var proposal = new StpDocxImportService().Propose(stream, "worksheet.docx", StpRevisionKind.RawMaterial);
        Assert.False(proposal.Succeeded);
        Assert.Contains("worksheet", proposal.FailureReason);
    }

    [Fact]
    public void Inline_headings_and_numbered_paragraphs_outside_a_table_are_read()
    {
        using var stream = Create([
            P("1.0Purpose : To analyse the product."), P("2.0Scope: All batches."), P("3.0Responsibility: Analyst"),
            P("4.0Accountability: QC Manager"), P("5.0Procedure:"), P("Reagents"), P("Methanol - Water"),
            P("5.1 Assay:"), P("5.1.1 Weigh 1 g."), P("Refer to QCD/SOP/053"),
        ], "STANDARD TEST PROCEDURE STP No.: QCD/STP/FP/123 SAMPLE TABLETS Revision No.: 00 Supersedes: Area: Quality Control Reference: BP");
        var proposal = new StpDocxImportService().Propose(stream, "inline.docx", StpRevisionKind.Product);
        Assert.True(proposal.Succeeded, proposal.FailureReason);
        Assert.Equal("SAMPLE TABLETS", proposal.Title.Value);
        Assert.Equal("To analyse the product.", proposal.Sections.Single(item => item.Kind == StpSectionKind.Purpose).Text);
        Assert.Equal("Methanol\nWater", proposal.Sections.Single(item => item.Kind == StpSectionKind.Reagents).Text);
        var test = Assert.Single(proposal.Tests);
        Assert.Equal(("5.1", "Assay"), (test.Number, test.Title));
        Assert.Equal("5.1.1", test.Steps[0].Number);
        Assert.Equal("QCD/SOP/053", test.Steps[1].ReferenceCode);
    }

    [Fact]
    public void A_file_that_is_not_a_docx_is_refused_with_a_reason()
    {
        using var stream = new MemoryStream("not a word file"u8.ToArray());
        var proposal = new StpDocxImportService().Propose(stream, "bad.docx", StpRevisionKind.Product);
        Assert.False(proposal.Succeeded);
        Assert.NotNull(proposal.FailureReason);
    }
}
