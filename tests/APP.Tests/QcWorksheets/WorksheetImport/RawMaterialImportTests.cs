using APP.Services.QcWorksheets;
using APP.Services.QcWorksheets.WorksheetDocxImport;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using DOMAIN.Entities.QcWorksheets;
using Xunit;
using static APP.Tests.QcWorksheets.WorksheetImport.TestDocx;

namespace APP.Tests.QcWorksheets.WorksheetImport;

/// <summary>
/// Brief 09 with synthetic documents shaped like the raw-material corpus (no real names or
/// numbers), so the rules are covered when <c>QC_RM_CORPUS_DIR</c> is not set.
/// </summary>
public class RawMaterialImportTests
{
    private const string WorksheetHeader =
        "QUALITY CONTROL DEPARTMENT\tRAW MATERIAL ANALYTICAL WORKSHEET\tBatch No.: XB/0001/99\tRaw Material Name: TESTOCAINE HCL\t"
        + "Quantity Received: 99Kg\tA.R. No.: QCD/RM/99/001\tMfg. Date: 01/2099\tSampled by: A. Tester\tIssued by: B. Tester\t"
        + "Supplier/Manufacturer: Sample Supplies Ltd\tSpec. No.: NQC/RM/SPC/901\tSTP No.: QC/STP/RM/901";

    private const string SpecificationHeader =
        "ENTRANCE PHARMACEUTICALS\tSPECIFICATION\tSPC No.: QCD/SPC/RM/901\tTESTOCAINE HYDROCHLORIDE\tRevision No.: 02\tEffective Date:";

    /// <summary>A cell of stacked paragraphs ("a | b") with tables nested after them.</summary>
    private static TableCell Nested(string text, params Table[] tables)
    {
        var cell = C(text);
        foreach (var table in tables)
            cell.AppendChild(table);
        return cell;
    }

    /// <summary>A paragraph whose second run is superscript: "10" + "3" → "10^3".</summary>
    private static Paragraph Raised(string text, string raised, string after = "") =>
        new(new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }),
            new Run(new RunProperties(new VerticalTextAlignment { Val = VerticalPositionValues.Superscript }), new Text(raised)),
            new Run(new Text(after) { Space = SpaceProcessingModeValues.Preserve }));

    private static IEnumerable<OpenXmlElement> WorksheetBody() =>
    [
        T(R(C("No"), C("TEST & OBSERVATIONS")),
          R(C("1."), C("Description / Appearance")), R(C(), C()),
          R(C("2."), C("Sulfated Ash Instrument ID:")),
          R(C(), C("Weight of empty crucible (W1) = | Weight of crucible + sample before drying (W2) = | "
                   + "Weight of crucible + sample after drying (W3) = | Sulfated Ash = (W2-W3) x100 % | (W2-W1) | = ______ - ______ x 100% =")),
          R(C("3."), C("pH: Equipment ID:")),
          R(C(), C("Weight of sample taken: ________g | Preparation: | Determination (i): (ii) mean:")),
          R(C("4."), C("Assay – Titration Balance ID.")),
          R(C(), Nested("Factor of Volumetric Solution = | Equivalence = 1 mL of 0.1 M NaOH is equivalent to 10.00 mg of X | Content Calculations: | ( – ) x x x",
              Grid(["", "Blank", "Sample 1", "Sample 2"], ["Wt. taken", "", "", ""], ["Final volume", "", "", ""],
                   ["Initial volume", "", "", ""], ["Titre obtained", "", "", ""]))),
          R(C("5."), C("Loss on Drying Balance ID:")),
          R(C(), C("Weight of empty crucible (W1) = | Weight of crucible + sample before drying (W2) = | "
                   + "Weight of crucible + sample after drying (W3) = | L.O.D = (W2-W3) x 100 % | (W2-W1)")),
          R(C(), C("Analysed by: checked by: | Date: Date:")))
    ];

    private static IEnumerable<OpenXmlElement> SpecificationBody()
    {
        var microbial = C("Not more than 10");
        microbial.RemoveAllChildren<Paragraph>();
        microbial.Append(Raised("Not more than 10", "3", " CFU/g"), P("Absent"));

        var impurity = new TableCell(new TableCellProperties());
        impurity.Append(Raised("Impurity A", "a"));
        return
        [
            T(R(C("Test"), C("Specification"), C("Reference")),
              R(C("Description"), C("White powder"), C("BP 2025")),
              R(C("Identification Tests: | IR | Chlorides"), C("Concordant with the standard | Gives the reaction of chlorides"), C("BP 2025")),
              R(C("Appearance of Solution"), C("Clear and colourless"), C("BP 2025")),
              R(C("Related Substances"), Nested("", Grid(["Impurities", "Limits"], ["Unspecified Impurities", "NMT 0.10%"])), C("BP 2025")),
              R(C("Organic Impurities: | Procedure 1"), Nested("Table 1:", T(R(impurity, C("0.30%")))), C("USP")),
              R(C("Sulphated Ash"), C("Maximum 0.1%"), C("BP 2025")),
              R(C("pH"), C("4.0 – 6.0"), C("BP 2025")),
              R(C("Assay:"), C("99.0 – 101.0%"), C("BP 2025")),
              R(C("Microbial Contamination | TAMC | E. coli"), microbial, C("BP 2025")),
              R(C("Heavy Metals | Lead | Arsenic"), C("NMT 10 ppm"), C("BP 2025")))
        ];
    }

    private static WorksheetImportProposal Propose(string fileName, IEnumerable<OpenXmlElement> body, string header, IWorksheetImportCatalog? catalog = null)
    {
        using var stream = Create(body, header);
        return WorksheetDocxImportService.Propose(fileName, stream, catalog ?? InMemoryWorksheetImportCatalog.Empty);
    }

    private static WorksheetImportProposal Worksheet() => Propose("901 - Testocaine.docx", WorksheetBody(), WorksheetHeader);
    private static WorksheetImportProposal Specification(IWorksheetImportCatalog? catalog = null) =>
        Propose("901 Testocaine spec.docx", SpecificationBody(), SpecificationHeader, catalog);

    private static List<ProposedWorksheetField> Fields(WorksheetImportProposal proposal) =>
        proposal.Template.Sections.SelectMany(section => section.Fields).ToList();

    [Fact]
    public void Both_raw_material_documents_are_classified_and_the_capsule_header_too()
    {
        Assert.Equal(ArdFamily.RawMaterialChemical, Worksheet().Family);
        Assert.Equal(ArdFamily.RawMaterialSpecification, Specification().Family);

        var capsule = Read([Grid(["No.", "Test", "Specification", "Reference"])],
            "QUALITY CONTROL DEPARTMENT\tRAW MATERIAL SPECIFICATION\tSpecification No.: QCD/SPC/RM/902\tSHELLS\tRevision No.:03");
        Assert.Equal(ArdFamily.RawMaterialSpecification, ArdFamilyClassifier.Classify(capsule).Family);

        // A finished-product specification is not a raw-material one.
        var product = Read([P("x")], "SPECIFICATION\tSPC No.: QCD/SPC/FP/001\tSYRUP\tRevision No.: 01");
        Assert.NotEqual(ArdFamily.RawMaterialSpecification, ArdFamilyClassifier.Classify(product).Family);
    }

    [Fact]
    public void A_worksheet_becomes_one_section_per_numbered_test()
    {
        var proposal = Worksheet();
        var template = proposal.Template;

        Assert.Equal(("RM-901", WorksheetCategory.Chemical, "901"), (template.Code, template.Category, proposal.RawMaterial.PairingKey));
        Assert.Equal("Raw Material Analytical Worksheet – TESTOCAINE HCL", template.Name);
        Assert.Equal(["Description / Appearance", "Sulfated Ash", "pH", "Assay – Titration", "Loss on Drying"], template.Sections.Select(section => section.Name));
        Assert.Equal(["sulfated_ash_instrument_id", "ph_equipment_id", "assay_titration_balance_id", "loss_on_drying_balance_id"],
            Fields(proposal).Where(field => field.Type == WorksheetFieldType.Instrument).Select(field => field.FieldKey));
        Assert.Equal("description_appearance_result", Assert.Single(template.Sections[0].Fields).FieldKey);

        var ph = template.Sections[2].Fields.ToDictionary(field => field.FieldKey);
        Assert.Equal((WorksheetFieldType.Number, "g"), (ph["ph_weight_of_sample_taken"].Type, ph["ph_weight_of_sample_taken"].Unit));
        Assert.Equal(WorksheetFieldType.LongText, ph["ph_preparation"].Type);
        Assert.Equal("({ph_determination_i} + {ph_determination_ii}) / 2", ph["ph_determination_mean"].FormulaExpression);

        // Sign-off and header run data never become fields or constants.
        var texts = Fields(proposal).SelectMany(field => new[] { field.Label, field.ConstantValue ?? string.Empty }).ToList();
        foreach (var value in new[] { "XB/0001/99", "99Kg", "QCD/RM/99/001", "A. Tester", "B. Tester", "Sample Supplies", "Analysed" })
            Assert.DoesNotContain(texts, text => text.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void The_crucible_formula_is_read_from_the_print()
    {
        var proposal = Worksheet();
        var ash = proposal.Template.Sections.Single(section => section.Name == "Sulfated Ash").Fields;

        Assert.Equal(["sulfated_ash_w1", "sulfated_ash_w2", "sulfated_ash_w3"],
            ash.Where(field => field.Type == WorksheetFieldType.Number).Select(field => field.FieldKey));
        var result = ash.Single(field => field.Type == WorksheetFieldType.Result);
        Assert.Equal((WorksheetFieldMode.Calculated, "%"), (result.Mode, result.Unit));
        Assert.Equal("(({sulfated_ash_w2} - {sulfated_ash_w3}) * 100) / ({sulfated_ash_w2} - {sulfated_ash_w1})", result.FormulaExpression);
        Assert.True(QcFormulaEvaluator.Analyze(result.FormulaExpression).IsValid);
        Assert.Equal(ImportConfidence.Medium, proposal.FieldProvenance.Single(item => item.FieldKey == result.FieldKey).Confidence);
        Assert.Equal(2, proposal.Flags.Count(flag => flag.Code == WorksheetImportFlagCodes.FormulaFromPrint));
    }

    [Fact]
    public void A_titration_is_a_fixed_row_table_and_its_assay_formula_is_never_guessed()
    {
        var proposal = Worksheet();
        var fields = proposal.Template.Sections.Single(section => section.Name == "Assay – Titration").Fields;

        Assert.Equal((WorksheetFieldType.Number, WorksheetFieldMode.Entry),
            (fields.Single(field => field.Label == "Factor of Volumetric Solution").Type, fields.Single(field => field.Label == "Factor of Volumetric Solution").Mode));
        var equivalence = fields.Single(field => field.Label == "Equivalence");
        Assert.Equal((WorksheetFieldMode.Constant, "1 mL of 0.1 M NaOH is equivalent to 10.00 mg of X"), (equivalence.Mode, equivalence.ConstantValue));

        var table = fields.Single(field => field.Type == WorksheetFieldType.Table);
        Assert.Contains("\"fixedValues\":[\"Blank\",\"Sample 1\",\"Sample 2\"]", table.ColumnDefinitions);
        Assert.Contains("\"key\":\"titreObtained\",\"label\":\"Titre obtained\",\"type\":\"Number\",\"unit\":\"mL\",\"mode\":\"Calculated\",\"formula\":\"{finalVolume} - {initialVolume}\"",
            table.ColumnDefinitions);

        var assay = fields.Single(field => field.Label == "% Assay");
        Assert.Equal((WorksheetFieldType.Result, WorksheetFieldMode.Calculated, (string?)null), (assay.Type, assay.Mode, assay.FormulaExpression));
        Assert.Contains(proposal.Flags, flag => flag.Code == WorksheetImportFlagCodes.FormulaNeedsReview && flag.Message.Contains("% Assay"));
    }

    [Fact]
    public void A_specification_document_proposes_characteristics_and_no_template()
    {
        var proposal = Specification();
        var rows = proposal.SpecificationProposals;

        Assert.Null(proposal.Template);
        Assert.Equal(("QCD/SPC/RM/901", "TESTOCAINE HYDROCHLORIDE", "02", "901"),
            (proposal.RawMaterial.SpecificationCode, proposal.RawMaterial.MaterialName, proposal.RawMaterial.Revision, proposal.RawMaterial.PairingKey));

        Assert.Contains(rows, row => (row.TestName, row.Analyte, row.AcceptanceCriteria, row.ActionLimit, row.Reference)
                                     == ("Identification Tests", "Chlorides", "Gives the reaction of chlorides", "Gives the reaction of chlorides", "BP 2025"));
        Assert.Contains(rows, row => (row.TestName, row.Analyte, row.AcceptanceCriteria) == ("Related Substances", "Unspecified Impurities", "NMT 0.10%"));
        Assert.Contains(rows, row => (row.Analyte, row.AcceptanceCriteria, row.Reference) == ("Impurity A (Procedure 1)", "0.30%", "USP"));
        Assert.Equal("Assay", rows.Single(row => row.AcceptanceCriteria == "99.0 – 101.0%").TestName);

        var microbial = rows.Where(row => row.GroupName == "MICROBIAL").ToList();
        Assert.Equal([("TAMC", "Not more than 10^3 CFU/g"), ("E. coli", "Absent")], microbial.Select(row => (row.Analyte!, row.AcceptanceCriteria)));
        Assert.All(rows.Except(microbial), row => Assert.Equal("CHEMICAL", row.GroupName));

        // Two sub-tests over one criteria line: kept whole and flagged.
        var metals = rows.Single(row => row.TestName == "Heavy Metals");
        Assert.Equal((null, "NMT 10 ppm", ImportConfidence.Low), (metals.Analyte, metals.AcceptanceCriteria, metals.Confidence));
        Assert.Contains(proposal.Flags, flag => flag.Code == WorksheetImportFlagCodes.UnrecognizedContent && flag.Message.Contains("Heavy Metals"));
    }

    [Fact]
    public void The_numbered_layout_captions_its_microbial_rows()
    {
        using var stream = Create(
        [
            T(R(C("No."), C("Test"), C("Specification"), C("Reference")),
              R(C("1."), C("Cap Colour"), C("Maroon"), C("In-House")),
              R(C("Microbial Test", span: 4)),
              R(C("2."), C("E. coli"), C("Absent in 1.0g"), C("In-House")))
        ], "RAW MATERIAL SPECIFICATION\tSpecification No.: QCD/SPC/RM/902\tSHELLS\tRevision No.:03");
        var proposal = WorksheetDocxImportService.Propose("902 Shells spec.docx", stream, InMemoryWorksheetImportCatalog.Empty);

        Assert.Equal([("Cap Colour", null, "CHEMICAL"), ("Microbial Test", "E. coli", "MICROBIAL")],
            proposal.SpecificationProposals.Select(row => (row.TestName, (string?)row.Analyte, row.GroupName!)));
        Assert.Equal("03", proposal.RawMaterial.Revision);
    }

    [Fact]
    public void A_pair_in_one_upload_binds_by_test_name_and_adds_sections_for_missing_tests()
    {
        var worksheet = Worksheet();
        var specification = Specification();
        WorksheetDocxImportService.ApplyBatchRules([worksheet, specification], InMemoryWorksheetImportCatalog.Empty);

        string? Bound(string test, string? analyte = null) =>
            specification.SpecificationProposals.First(row => row.TestName == test && row.Analyte == analyte).SourceFieldKey;

        Assert.Equal((RawMaterialPairingStatus.InUpload, "901 - Testocaine.docx"), (specification.RawMaterial.Pairing!.Value, specification.RawMaterial.PairedFileName));
        Assert.Equal("description_appearance_result", Bound("Description"));
        Assert.Equal("sulfated_ash_result", Bound("Sulphated Ash"));
        Assert.Equal("ph_determination_mean", Bound("pH"));
        Assert.Equal("assay_titration_result", Bound("Assay"));

        // Appearance of Solution, Identification (IR, Chlorides), Related/Organic Impurities,
        // Microbial Contamination and Heavy Metals have no section: each gets Result(s) + attachment.
        var added = worksheet.Template.Sections.Skip(5).ToList();
        Assert.Equal(["Identification Tests", "Appearance of Solution", "Related Substances", "Organic Impurities", "Microbial Contamination", "Heavy Metals"],
            added.Select(section => section.Name));
        var microbial = added.Single(section => section.Name == "Microbial Contamination");
        Assert.Equal([("microbial_contamination_tamc", WorksheetFieldType.LongText), ("microbial_contamination_e_coli", WorksheetFieldType.LongText),
            ("microbial_contamination_attach_print_out", WorksheetFieldType.FileUpload)], microbial.Fields.Select(field => (field.FieldKey, field.Type)));
        Assert.Equal("microbial_contamination_e_coli", Bound("Microbial Contamination", "E. coli"));
        Assert.Equal(6, worksheet.Flags.Count(flag => flag.Code == WorksheetImportFlagCodes.AddedForSpecification));
        Assert.Equal(6, specification.Flags.Count(flag => flag.Code == WorksheetImportFlagCodes.AddedForSpecification));

        var keys = Fields(worksheet).Select(field => field.FieldKey).ToList();
        Assert.Equal(keys.Count, keys.Distinct().Count());
        Assert.All(specification.SpecificationProposals, row => Assert.Contains(row.SourceFieldKey, keys));
    }

    [Fact]
    public void A_saved_template_is_bound_but_never_modified()
    {
        var saved = new CatalogTemplate(Guid.NewGuid(), "RM-901", "saved", ["sulfated_ash_result"], [new CatalogSection("Sulfated Ash", "sulfated_ash_result")]);
        var catalog = new InMemoryWorksheetImportCatalog([], [], [], null, [saved]);
        var specification = Specification(catalog);
        WorksheetDocxImportService.ApplyBatchRules([specification], catalog);

        Assert.Equal((RawMaterialPairingStatus.ExistingTemplate, saved.Id), (specification.RawMaterial.Pairing!.Value, specification.RawMaterial.PairedTemplateId!.Value));
        Assert.Equal("sulfated_ash_result", specification.SpecificationProposals.Single(row => row.TestName == "Sulphated Ash").SourceFieldKey);
        Assert.All(specification.SpecificationProposals, row => Assert.Equal(saved.Id, row.SourceWorksheetTemplateId));
        Assert.Equal(specification.SpecificationProposals.Count - 1, specification.Flags.Count(flag => flag.Code == WorksheetImportFlagCodes.FieldNotOnTemplate));
        Assert.DoesNotContain(specification.Flags, flag => flag.Code == WorksheetImportFlagCodes.AddedForSpecification);
    }

    [Fact]
    public void A_specification_with_no_worksheet_anywhere_is_flagged()
    {
        var specification = Specification();
        WorksheetDocxImportService.ApplyBatchRules([specification], InMemoryWorksheetImportCatalog.Empty);

        Assert.Equal(RawMaterialPairingStatus.Missing, specification.RawMaterial.Pairing);
        Assert.Single(specification.Flags, flag => flag.Code == WorksheetImportFlagCodes.WorksheetNotFound);
        Assert.All(specification.SpecificationProposals, row => Assert.Null(row.SourceFieldKey));
    }

    [Theory]
    [InlineData("Sulphated Ash", null, "Sulfated Ash", true)]
    [InlineData("Loss on drying", null, "L.O.D", true)]
    [InlineData("Residue on Ignition", null, "Sulfated Ash", true)]
    [InlineData("Identification Tests", "IR", "Identity Test: IR", true)]
    [InlineData("Description", null, "Description / Appearance", true)]
    [InlineData("Specific Optical Rotation", null, "Optical Rotation", true)]
    [InlineData("Appearance of Solution", null, "Description / Appearance", false)]
    [InlineData("Related Substances", "A, B, C", "Identity Test A – Colour Test", false)]
    public void Test_names_match_with_synonyms_and_broader_sections(string test, string? analyte, string section, bool matches)
    {
        var index = RawMaterialTestNames.Match(test, analyte, ["Water", section], new HashSet<int>());
        Assert.Equal(matches ? 1 : -1, index);
    }
}
