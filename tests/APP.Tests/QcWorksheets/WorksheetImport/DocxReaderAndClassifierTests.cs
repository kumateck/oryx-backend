using APP.Services.QcWorksheets.WorksheetDocxImport;
using DOMAIN.Entities.QcWorksheets;
using Xunit;
using static APP.Tests.QcWorksheets.WorksheetImport.TestDocx;

namespace APP.Tests.QcWorksheets.WorksheetImport;

public class DocxDocumentReaderTests
{
    [Fact]
    public void GridSpan_is_expanded_and_every_filler_points_at_its_origin()
    {
        var table = Read([T(
            R(C("Strain", merge: VMerge.Restart), C("New Batch", span: 2)),
            R(C(merge: VMerge.Continue), C("Plate 1"), C("Plate 2")),
            R(C("E. coli"), C(), C()))]).Tables.Single();

        Assert.Equal(3, table.ColumnCount);
        Assert.Equal("New Batch", table.Resolved(0, 2));
        Assert.True(table.Cell(0, 2)!.IsHorizontalSpan);
        Assert.Equal("Strain", table.Resolved(1, 0));
        Assert.True(table.Cell(1, 0)!.IsContinuation);
        Assert.Equal(0, table.Cell(1, 0)!.OriginRow);
        Assert.Equal("Plate 2", table.Resolved(1, 2));
    }

    [Fact]
    public void Ragged_rows_are_padded_to_a_rectangle()
    {
        var table = Read([T(R(C("a"), C("b"), C("c")), R(C("d")))]).Tables.Single();

        Assert.Equal(3, table.Rows[1].Count);
        Assert.Equal(string.Empty, table.Resolved(1, 2));
    }

    [Fact]
    public void Noise_paragraphs_are_stripped_and_tabs_read_as_spaces()
    {
        var document = Read([P("["), P("[["), P("`"), P("pH Range: 7.2 ± 0.2\tObserved:……")]);

        var block = Assert.Single(document.Blocks);
        Assert.Equal("pH Range: 7.2 ± 0.2 Observed:……", block.Text);
    }

    [Theory]
    [InlineData("Pseudo-monas aeruginosa", "Pseudomonas aeruginosa")]
    [InlineData("Esche- richia coli", "Escherichia coli")]
    [InlineData("Incu- bation temp.", "Incubation temp.")]
    [InlineData("Soyabean-Casein Digest Agar", "Soyabean-Casein Digest Agar")]
    [InlineData("Seed-lot culture", "Seed-lot culture")]
    [InlineData("QCD/ STR/P1-01", "QCD/STR/P1-01")]
    public void Hyphenation_is_repaired_only_inside_known_words(string printed, string expected) =>
        Assert.Equal(expected, ImportText.Clean(printed));

    [Fact]
    public void A_cell_split_over_paragraphs_reads_as_one_value()
    {
        var table = Read([T(R(C("Esche- | richia coli"), C("QCD/ | STR/E1-01")))]).Tables.Single();

        Assert.Equal("Escherichia coli", table.Resolved(0, 0));
        Assert.Equal("QCD/STR/E1-01", table.Resolved(0, 1));
    }

    [Fact]
    public void Running_header_text_comes_from_every_header_part_without_page_numbers()
    {
        var document = Read([P("body")], "QUALITY CONTROL\tPage 1 of 4", "", "WATER\tPage 4 of 4");

        Assert.Equal("QUALITY CONTROL WATER", document.HeaderText);
    }

    [Fact]
    public void Page_split_tables_with_a_repeated_header_are_stitched()
    {
        DocumentFormat.OpenXml.Wordprocessing.TableRow Header1() => R(C("Room No.", merge: VMerge.Restart), C("Room Name", merge: VMerge.Restart), C("Count"));
        DocumentFormat.OpenXml.Wordprocessing.TableRow Header2() => R(C(merge: VMerge.Continue), C(merge: VMerge.Continue), C("(CFU/4Hrs)"));

        var document = Read([
            T(Header1(), Header2(), R(C("R-1"), C("Room one"), C())),
            T(Header1(), Header2(), R(C("R-2"), C("Room two"), C()), R(C("R-3"), C("Room three"), C())),
            T(Header1(), Header2(), R(C("R-4"), C("Room four"), C()))
        ]);

        var table = Assert.Single(document.Tables);
        Assert.Equal(6, table.Rows.Count);
        Assert.Equal(["R-1", "R-2", "R-3", "R-4"], Enumerable.Range(2, 4).Select(row => table.Resolved(row, 0)));
        Assert.Equal([0, 1, 2], table.SourceTables);
    }

    [Fact]
    public void Tables_separated_by_text_or_with_different_headers_are_not_stitched()
    {
        var document = Read([
            Grid(["Antimicrobial", "Zone"], ["A 10 µg", "18-24 mm"]),
            P("Staphylococcus aureus (ATCC 6538)"),
            Grid(["Antimicrobial", "Zone"], ["B 30 µg", "20-26 mm"]),
            Grid(["EQUIPMENT USED", "EQUIPMENT CODE"], ["Balance", "QCD/EQT/BAL/001"])
        ]);

        Assert.Equal(3, document.Tables.Count());
    }

    [Theory]
    [InlineData("PREVIOUSLY APPROVED BATCH", null, true)]
    [InlineData("REFERENCES", null, true)]
    [InlineData("QCD/SOP/044", null, false)]
    [InlineData("BP", null, false)]
    [InlineData("Cultural Response", null, false)]
    [InlineData("Cultural Response", "Heading2", true)]
    public void Headings_are_styled_or_all_capital_words(string text, string? style, bool expected) =>
        Assert.Equal(expected, DocxDocumentReader.IsHeading(text, style));
}

public class ArdFamilyClassifierTests
{
    [Theory]
    [InlineData("QUALITY CONTROL MICROBIOLOGYCERTIFICATE OF ANALYSISAR. No.: X", ArdFamily.CompletedCertificate)]
    [InlineData("ENVIRONMENTAL MONITORING  RAW DATA - MICROBIOLOGY", ArdFamily.EnvironmentalMonitoring)]
    [InlineData("ANALYTICAL RAW DATA – MICROBIOLOGY\tBatch No.:", ArdFamily.ProductMicro)]
    [InlineData("ANALYTICAL RAW DATA - MICROBIOLOGY", ArdFamily.ProductMicro)]
    [InlineData("QUALITY CONTROL DEPARTMENT\tMICROBIOLOGY ANALYTICAL WORKSHEET\tWATER", ArdFamily.PurifiedWater)]
    [InlineData("SOMETHING ELSE", ArdFamily.Unknown)]
    public void Header_markers_decide_the_family(string header, ArdFamily expected) =>
        Assert.Equal(expected, ArdFamilyClassifier.Classify(Read([P("Body text")], header)).Family);

    [Fact]
    public void A_media_sheet_is_culture_media_even_though_it_mentions_water()
    {
        var document = Read(
            [Grid(["Batch No.:", "Culture Medium Name: | Test Agar"]), P("Volume of distilled water added: ……")],
            "MICROBIOLOGY ANALYTICAL WORKSHEET\tCULTURE MEDIUM BATCH DATA");

        Assert.Equal(ArdFamily.CultureMedia, ArdFamilyClassifier.Classify(document).Family);
    }

    [Fact]
    public void A_certificate_wins_over_every_other_marker()
    {
        var document = Read([P("CERTIFICATE OF ANALYSIS")], "ENVIRONMENTAL MONITORING RAW DATA");

        Assert.Equal(ArdFamily.CompletedCertificate, ArdFamilyClassifier.Classify(document).Family);
    }

    [Fact]
    public void Water_needs_the_sheet_subject_not_a_passing_mention()
    {
        var document = Read([P("Rinse with purified water.")], "MICROBIOLOGY ANALYTICAL WORKSHEET");

        Assert.Equal(ArdFamily.Unknown, ArdFamilyClassifier.Classify(document).Family);
    }

    [Fact]
    public void A_raw_material_chemical_worksheet_is_not_water()
    {
        var document = Read(
            [Grid(["TEST & OBSERVATIONS", "Solubility | Water: | Ethanol (96%):"])],
            "QUALITY CONTROL DEPARTMENT\tRAW MATERIAL ANALYTICAL WORKSHEET\tBatch No.:");

        var classification = ArdFamilyClassifier.Classify(document);

        Assert.Equal(ArdFamily.RawMaterialChemical, classification.Family);
        Assert.Equal("RAW MATERIAL ANALYTICAL WORKSHEET", classification.Evidence);
    }

    [Fact]
    public void A_finished_product_chemical_worksheet_stays_unknown()
    {
        var document = Read(
            [Grid(["TEST & OBSERVATIONS", "Assay | Weight of sample:"])],
            "QUALITY CONTROL DEPARTMENT\tFINISHED PRODUCT ANALYTICAL WORKSHEET\tBatch No.:");

        var classification = ArdFamilyClassifier.Classify(document);

        Assert.Equal(ArdFamily.Unknown, classification.Family);
        Assert.Equal("ANALYTICAL WORKSHEET (chemical)", classification.Evidence);
    }

    [Fact]
    public void A_finished_product_headed_analytical_raw_data_stays_unknown()
    {
        // The product-chemical pattern confirmed against a real Lufart ARD: headed just
        // "ANALYTICAL RAW DATA" (no "MICROBIOLOGY" suffix, no "WORKSHEET"), pages of
        // Description/Identification/Dissolution/Assay — never "analyticalworksheet", so the
        // plain fallback below missed it until this case was added.
        var document = Read(
            [Grid(["No.", "Result / Observation", "Specification"])],
            "ANALYTICAL RAW DATA\tBatch No.:\tProduct Name:");

        var classification = ArdFamilyClassifier.Classify(document);

        Assert.Equal(ArdFamily.Unknown, classification.Family);
        Assert.Equal("ANALYTICAL WORKSHEET (chemical)", classification.Evidence);
    }

    [Fact]
    public void A_standard_test_procedure_is_named_as_such()
    {
        var document = Read([P("Purpose")], "STANDARD TEST PROCEDURE\tSTP No.: QCD/STP/FP/009");

        Assert.Equal("STANDARD TEST PROCEDURE", ArdFamilyClassifier.Classify(document).Evidence);
    }
}
