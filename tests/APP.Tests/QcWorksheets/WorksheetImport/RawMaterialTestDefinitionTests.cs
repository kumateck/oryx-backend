using APP.Services.QcWorksheets;
using APP.Services.QcWorksheets.WorksheetDocxImport;
using APP.Services.QcWorksheets.WorksheetDocxImport.TestDefinitions;
using DocumentFormat.OpenXml.Wordprocessing;
using DOMAIN.Entities.QcWorksheets;
using Xunit;
using static APP.Tests.QcWorksheets.WorksheetImport.TestDocx;

namespace APP.Tests.QcWorksheets.WorksheetImport;

/// <summary>
/// Brief 12: one synthetic test per raw-material test definition and per variant, always run.
/// Every formula is also pushed through the real worksheet calculator with sample numbers, so a
/// definition cannot ship a formula that does not parse or cannot be evaluated.
/// </summary>
public class RawMaterialTestDefinitionTests
{
    private const string Header =
        "QUALITY CONTROL DEPARTMENT\tRAW MATERIAL ANALYTICAL WORKSHEET\tBatch No.: XB/0001/99\tRaw Material Name: TESTOCAINE HCL\t"
        + "Quantity Received: 99Kg\tA.R. No.: QCD/RM/99/001\tSpec. No.: NQC/RM/SPC/901\tSTP No.: QC/STP/RM/901";

    private const string Crucible =
        "Weight of empty crucible (W1) = | Weight of crucible + sample before drying (W2) = | Weight of crucible + sample after drying (W3) =";

    private const string LossOnDrying = Crucible + " | Loss on Drying = (W2-W3) x 100 % | (W2-W1) | = ______ - ______ x 100% = | -";

    private const string Water = "wt of sample taken: (i) (ii) | Determinations: (i) (ii) Average %:";

    private static readonly Table TitrationGridTable = Grid(["", "Blank", "Sample 1", "Sample 2"], ["Wt. taken", "", "", ""],
        ["Final volume", "", "", ""], ["Initial volume", "", "", ""], ["Titre obtained", "", "", ""]);

    private static TableCell Body(string lines, params Table[] tables)
    {
        var cell = C(lines);
        foreach (var table in tables)
            cell.AppendChild((Table)table.CloneNode(true));
        return cell;
    }

    /// <summary>A worksheet of numbered tests: each a title row and one body row.</summary>
    private static WorksheetImportProposal Sheet(params (string Title, TableCell Body)[] tests)
    {
        var rows = new List<TableRow> { R(C("No"), C("TEST & OBSERVATIONS")) };
        foreach (var (test, index) in tests.Select((test, index) => (test, index)))
        {
            rows.Add(R(C($"{index + 1}."), C(test.Title)));
            rows.Add(R(C(), test.Body));
        }

        rows.Add(R(C(), C("Analysed by: checked by: | Date: Date:")));
        using var stream = Create([T(rows.ToArray())], Header);
        return WorksheetDocxImportService.Propose("901 - Testocaine.docx", stream, InMemoryWorksheetImportCatalog.Empty);
    }

    private static WorksheetImportProposal Sheet(string title, string lines, params Table[] tables) => Sheet((title, Body(lines, tables)));

    private static ProposedWorksheetField Field(WorksheetImportProposal proposal, string key) =>
        proposal.Template.Sections.SelectMany(section => section.Fields).Single(field => field.FieldKey == key);

    private static List<string> Flags(WorksheetImportProposal proposal, string code) =>
        proposal.Flags.Where(flag => flag.Code == code).Select(flag => flag.Message).ToList();

    private static Dictionary<string, double> Evaluate(
        WorksheetImportProposal proposal, Dictionary<string, double>? scalars = null, Dictionary<string, double>? cells = null)
    {
        var (values, failure) = ProposalCalculator.Evaluate(proposal.Template, scalars, cells);
        Assert.True(failure is null, failure);
        return values;
    }

    /// <summary>A definition formula is Medium confidence and flagged FormulaFromDefinition; it is never High.</summary>
    private static void AssertFromDefinition(WorksheetImportProposal proposal, string key)
    {
        Assert.Equal(ImportConfidence.Medium, proposal.FieldProvenance.Single(item => item.FieldKey == key && item.ColumnKey is null).Confidence);
        Assert.Contains(proposal.Flags, flag => flag.Code == WorksheetImportFlagCodes.FormulaFromDefinition);
    }

    // ---------------------------------------------------------------- gravimetric

    [Theory]
    [InlineData("Loss on Drying Balance ID:", "loss_on_drying")]
    [InlineData("L.O.D", "loss_on_drying")]
    [InlineData("Loss on Ignition", "loss_on_ignition")]
    public void A_loss_is_w2_minus_w3_over_the_sample_weight(string title, string definition)
    {
        var proposal = Sheet(title, LossOnDrying);
        var section = proposal.Template.Sections.Single();
        var prefix = ImportText.SnakeKey(section.Name, 30);

        Assert.Equal(definition, section.TestDefinition);
        Assert.Equal($"(({{{prefix}_w2}} - {{{prefix}_w3}}) * 100) / ({{{prefix}_w2}} - {{{prefix}_w1}})", Field(proposal, $"{prefix}_result").FormulaExpression);
        AssertFromDefinition(proposal, $"{prefix}_result");
        Assert.Contains("printed: 'Loss on Drying = (W2-W3) x 100 % / (W2-W1)'", Assert.Single(Flags(proposal, WorksheetImportFlagCodes.FormulaFromDefinition)));
        Assert.Empty(Flags(proposal, WorksheetImportFlagCodes.FormulaFromPrint));

        var values = Evaluate(proposal, new() { [$"{prefix}_w1"] = 20, [$"{prefix}_w2"] = 22, [$"{prefix}_w3"] = 21.9 });
        Assert.Equal(5, values[$"{prefix}_result"], 6);
    }

    [Theory]
    [InlineData("Sulfated Ash Instrument ID:", "sulfated_ash", "Sulfated Ash")]
    [InlineData("Residue on Ignition", "sulfated_ash", "Residue on Ignition")]
    [InlineData("Total Ash", "total_ash", "Total Ash")]
    public void A_residue_is_w3_minus_w1_over_the_sample_weight(string title, string definition, string name)
    {
        var proposal = Sheet(title, Crucible + $" | {name} = (W3-W1) x 100 % | (W2-W1)");
        var prefix = ImportText.SnakeKey(name, 30);

        Assert.Equal(definition, proposal.Template.Sections.Single().TestDefinition);
        Assert.Equal($"(({{{prefix}_w3}} - {{{prefix}_w1}}) * 100) / ({{{prefix}_w2}} - {{{prefix}_w1}})", Field(proposal, $"{prefix}_result").FormulaExpression);
        AssertFromDefinition(proposal, $"{prefix}_result");

        var values = Evaluate(proposal, new() { [$"{prefix}_w1"] = 20, [$"{prefix}_w2"] = 22, [$"{prefix}_w3"] = 20.004 });
        Assert.Equal(0.2, values[$"{prefix}_result"], 6);
    }

    [Fact]
    public void A_printed_arrangement_that_differs_from_the_definition_wins_and_is_flagged()
    {
        // The corpus prints the loss arrangement under Sulfated Ash.
        var proposal = Sheet("Sulfated Ash", Crucible + " | Sulfated Ash = (W2-W3) x100 % | (W2-W1)");

        Assert.Equal("(({sulfated_ash_w2} - {sulfated_ash_w3}) * 100) / ({sulfated_ash_w2} - {sulfated_ash_w1})", Field(proposal, "sulfated_ash_result").FormulaExpression);
        Assert.Contains("differs from the 'Sulfated Ash' definition's formula; the printed one is used", Assert.Single(Flags(proposal, WorksheetImportFlagCodes.FormulaFromPrint)));
        Assert.Empty(Flags(proposal, WorksheetImportFlagCodes.FormulaFromDefinition));
        Assert.Equal(ImportConfidence.Medium, proposal.FieldProvenance.Single(item => item.FieldKey == "sulfated_ash_result").Confidence);
    }

    [Fact]
    public void A_required_input_the_sheet_lacks_is_added_and_flagged()
    {
        var proposal = Sheet("Total Ash", "Weight of empty crucible (W1) = | Weight of crucible + sample before drying (W2) =");

        var added = Field(proposal, "total_ash_w3");
        Assert.Equal((WorksheetFieldType.Number, WorksheetFieldMode.Entry, "g"), (added.Type, added.Mode, added.Unit));
        Assert.Contains("'Weight of crucible + sample after drying (W3)' is required by the 'Total Ash' definition",
            Assert.Single(Flags(proposal, WorksheetImportFlagCodes.DefinitionInputAdded)));
        Assert.Contains("no formula is printed", Assert.Single(Flags(proposal, WorksheetImportFlagCodes.FormulaFromDefinition)));
        Assert.Equal("(({total_ash_w3} - {total_ash_w1}) * 100) / ({total_ash_w2} - {total_ash_w1})", Field(proposal, "total_ash_result").FormulaExpression);
    }

    // ---------------------------------------------------------------- assay by titration

    private static WorksheetImportProposal Titration(string numerator, string trailing, string denominator, string equivalence = "Equivalence = 1 mL of 0.1 M NaOH is equivalent to 20.00 mg of X") =>
        Sheet(
            ("Loss on Drying", Body(LossOnDrying)),
            ("Assay – Titration Balance ID.", Body(
                $"Factor of Volumetric Solution = | {equivalence} | Content Calculations: | {numerator} | % Assay = -------------------------------- {trailing} | {denominator} | "
                + "( – ) x x x | % Assay (1) = ------------------------------- x 100 | x | ( – ) x x x | % Assay (2) = ------------------------------- x 100 | x | Average %Assay =",
                TitrationGridTable)));

    /// <summary>Titre 20 mL over the blank, factor 1, equivalence 20 mg/mL, 400 mg of sample, LOD 5 %.</summary>
    private static double TitrationResult(WorksheetImportProposal proposal, bool back = false)
    {
        const string p = "assay_titration";
        var scalars = new Dictionary<string, double>
        {
            ["loss_on_drying_w1"] = 20, ["loss_on_drying_w2"] = 22, ["loss_on_drying_w3"] = 21.9,
            [$"{p}_factor"] = 1, [$"{p}_blank_final_volume"] = back ? 20.1 : 0.1, [$"{p}_blank_initial_volume"] = 0,
            [$"{p}_dilution_factor"] = 2, [$"{p}_purity"] = 99, [$"{p}_filled_weight"] = 500, [$"{p}_claim"] = 250,
            [$"{p}_water"] = 5, [$"{p}_loi"] = 5, [$"{p}_equivalence_value"] = 20
        };
        var cells = new Dictionary<string, double>();
        for (var row = 0; row < 2; row++)
        {
            cells[$"{p}_titration.wtTaken[{row}]"] = 400;
            cells[$"{p}_titration.finalVolume[{row}]"] = back ? 0.1 : 20.1;
            cells[$"{p}_titration.initialVolume[{row}]"] = 0;
        }

        return Evaluate(proposal, scalars, cells)[$"{p}_result"];
    }

    [Theory]
    // Direct titration, no drying correction.
    [InlineData("(Sample Titre – Blank Titre) x Factor x Equiv.", "x 100", "Weight of sample", "",
        "({titreObtained} - {assay_titration_blank_titre}) * {assay_titration_factor} * 20.00 / ({wtTaken}) * 100", 100)]
    // Dried basis: LOD is this worksheet's own Loss on Drying result.
    [InlineData("(Sample Titre – Blank Titre) x Factor x Equiv. x 100", "x 100", "Weight of sample x (100 – LOD)", "dried basis (100 − LOD)",
        "({titreObtained} - {assay_titration_blank_titre}) * {assay_titration_factor} * 20.00 * 100 / ({wtTaken} * (100 - {loss_on_drying_result})) * 100", 105.263158)]
    [InlineData("(Sample Titre – Blank Titre) x Factor x Equiv. x 100 x 100", "", "Weight of sample x (100 – L.O.D)", "dried basis (100 − LOD)",
        "({titreObtained} - {assay_titration_blank_titre}) * {assay_titration_factor} * 20.00 * 100 * 100 / ({wtTaken} * (100 - {loss_on_drying_result}))", 105.263158)]
    // Water instead of LOD: the worksheet has no Water test, so an entry is added for it.
    [InlineData("(Sample Titre – Blank Titre) x Factor x Equiv. x 100", "x 100", "Weight of sample x (100 – Water)", "anhydrous basis (100 − Water)",
        "({titreObtained} - {assay_titration_blank_titre}) * {assay_titration_factor} * 20.00 * 100 / ({wtTaken} * (100 - {assay_titration_water})) * 100", 105.263158)]
    [InlineData("(Sample titre – Blank Titre) x Factor x Equiv. x 100 x 100", "", "Weight of sample x (100 – L.O.I)", "ignited basis (100 − LOI)",
        "({titreObtained} - {assay_titration_blank_titre}) * {assay_titration_factor} * 20.00 * 100 * 100 / ({wtTaken} * (100 - {assay_titration_loi}))", 105.263158)]
    // Extra factors in the numerator.
    [InlineData("(Sample Titre – Blank Titre) x Factor x Equiv. x Dilution factor x 100", "", "Weight of sample", "dilution factor",
        "({titreObtained} - {assay_titration_blank_titre}) * {assay_titration_factor} * 20.00 * {assay_titration_dilution_factor} * 100 / ({wtTaken})", 200)]
    [InlineData("(Sample Titre – Blank Titre) x Factor x Equiv. x % purity", "", "Weight of sample", "% purity",
        "({titreObtained} - {assay_titration_blank_titre}) * {assay_titration_factor} * 20.00 * {assay_titration_purity} / ({wtTaken})", 99)]
    [InlineData("(Sample Titre – Blank Titre) x Factor x Equiv. x Filled weight", "x 100", "Weight of sample x claim", "filled weight, claim",
        "({titreObtained} - {assay_titration_blank_titre}) * {assay_titration_factor} * 20.00 * {assay_titration_filled_weight} / ({wtTaken} * {assay_titration_claim}) * 100", 200)]
    // No blank in the formula.
    [InlineData("Sample Titre x Factor x Equiv. x 100", "", "Weight of sample", "no blank",
        "{titreObtained} * {assay_titration_factor} * 20.00 * 100 / ({wtTaken})", 100.5)]
    public void A_titration_assay_is_the_variant_the_sheet_prints(
        string numerator, string trailing, string denominator, string variants, string formula, double expected)
    {
        var proposal = Titration(numerator, trailing, denominator);
        var section = proposal.Template.Sections.Single(item => item.Name == "Assay – Titration");
        var table = Field(proposal, "assay_titration_titration");

        Assert.Equal("assay_titration", section.TestDefinition);
        Assert.Equal(variants, string.Join(", ", section.TestDefinitionVariants));
        Assert.Contains($"\"key\":\"assay\",\"label\":\"% Assay\",\"type\":\"Number\",\"unit\":\"%\",\"mode\":\"Calculated\",\"formula\":\"{formula}\"", table.ColumnDefinitions);
        Assert.Contains("\"fixedValues\":[\"Sample 1\",\"Sample 2\"]", table.ColumnDefinitions);
        Assert.Contains("\"key\":\"wtTaken\",\"label\":\"Wt. taken\",\"type\":\"Number\",\"unit\":\"mg\"", table.ColumnDefinitions);
        Assert.Equal("AVG({assay_titration_titration.assay})", Field(proposal, "assay_titration_result").FormulaExpression);
        Assert.Equal("{assay_titration_blank_final_volume} - {assay_titration_blank_initial_volume}", Field(proposal, "assay_titration_blank_titre").FormulaExpression);

        // The printed formula is the provenance; the constant is read from the sheet, as printed.
        AssertFromDefinition(proposal, "assay_titration_result");
        Assert.Contains(proposal.Flags, flag => flag.Code == WorksheetImportFlagCodes.FormulaFromDefinition && flag.Message.Contains($"[{numerator}] / [{denominator}]"));
        Assert.Equal((WorksheetFieldMode.Constant, "1 mL of 0.1 M NaOH is equivalent to 20.00 mg of X"),
            (Field(proposal, "assay_titration_equivalence").Mode, Field(proposal, "assay_titration_equivalence").ConstantValue));
        Assert.Empty(Flags(proposal, WorksheetImportFlagCodes.FormulaNeedsReview));
        Assert.Empty(Flags(proposal, WorksheetImportFlagCodes.DefinitionExtraLine));

        Assert.Equal(expected, TitrationResult(proposal), 5);
    }

    [Fact]
    public void A_back_titration_subtracts_the_sample_titre_from_the_blank()
    {
        var proposal = Titration("(Blank Titre – Sample Titre) x Factor x Equiv. x 100", "", "Weight of sample");

        Assert.Equal(["back titration"], proposal.Template.Sections.Single(item => item.Name == "Assay – Titration").TestDefinitionVariants);
        Assert.Contains("\"formula\":\"({assay_titration_blank_titre} - {titreObtained}) * {assay_titration_factor} * 20.00 * 100 / ({wtTaken})\"",
            Field(proposal, "assay_titration_titration").ColumnDefinitions);
        Assert.Equal(100, TitrationResult(proposal, back: true), 5);
    }

    [Fact]
    public void A_correction_for_a_test_the_worksheet_lacks_adds_an_entry_for_it()
    {
        var proposal = Titration("(Sample Titre – Blank Titre) x Factor x Equiv. x 100", "x 100", "Weight of sample x (100 – Water)");

        var water = Field(proposal, "assay_titration_water");
        Assert.Equal((WorksheetFieldType.Number, WorksheetFieldMode.Entry, "%", "Water (%)"), (water.Type, water.Mode, water.Unit, water.Label));
        Assert.Contains("the formula uses 'Water (%)', but this worksheet has no such test", Assert.Single(Flags(proposal, WorksheetImportFlagCodes.DefinitionInputAdded)));

        // With a Water test on the sheet the same formula refers to its result instead.
        var withWater = Sheet(("Water Instrument ID:", Body(Water)), ("Assay – Titration", Body(
            "Factor of Volumetric Solution = | Equivalence = 1 mL of 0.1 M NaOH is equivalent to 20.00 mg of X | Content Calculations: | "
            + "(Sample Titre – Blank Titre) x Factor x Equiv. x 100 | % Assay = ------------------------ x 100 | Weight of sample x (100- Water) | Average %Assay =",
            TitrationGridTable)));
        Assert.Contains("(100 - {water_result})", Field(withWater, "assay_titration_titration").ColumnDefinitions);
        Assert.Empty(Flags(withWater, WorksheetImportFlagCodes.DefinitionInputAdded));
    }

    [Fact]
    public void A_titration_with_no_printed_formula_and_a_blank_equivalence_uses_the_base_formula()
    {
        var proposal = Sheet("Assay – Titration", "Factor of Volumetric Solution = | Equivalence = ____________ | Content Calculations: | ( – ) x x x", TitrationGridTable);

        // The equivalence is never taken from the definition: its blank is an entry.
        Assert.Equal((WorksheetFieldType.Number, WorksheetFieldMode.Entry), (Field(proposal, "assay_titration_equivalence_value").Type, Field(proposal, "assay_titration_equivalence_value").Mode));
        Assert.Contains("\"formula\":\"({titreObtained} - {assay_titration_blank_titre}) * {assay_titration_factor} * {assay_titration_equivalence_value} * 100 / {wtTaken}\"",
            Field(proposal, "assay_titration_titration").ColumnDefinitions);
        Assert.Contains("\"key\":\"wtTaken\",\"label\":\"Wt. taken\",\"type\":\"Number\",\"unit\":\"g\"", Field(proposal, "assay_titration_titration").ColumnDefinitions);
        Assert.Contains(proposal.Flags, flag => flag.Code == WorksheetImportFlagCodes.FormulaFromDefinition && flag.Message.Contains("no formula is printed"));
        Assert.Equal(100, TitrationResult(proposal), 5);
    }

    [Fact]
    public void A_plain_assay_is_a_titration_when_it_prints_the_titration_grid()
    {
        var proposal = Sheet("Assay: Balance ID:", "Factor of 0.1M NaOH: 1mL of 0.1M NaOH ≡ 9.21 mg of X | Content Calculations: | "
                                                    + "(Sample Titre – Blank Titre) x Factor x Equiv. x 100 | % Assay = -------------------- | Weight of sample", TitrationGridTable);

        Assert.Equal("assay_titration", proposal.Template.Sections.Single().TestDefinition);
        Assert.Equal("Factor of 0.1M NaOH", Field(proposal, "assay_factor").Label);
        Assert.Equal("1mL of 0.1M NaOH ≡ 9.21 mg of X", Field(proposal, "assay_equivalence").ConstantValue);
        Assert.Contains("* {assay_factor} * 9.21 * 100 / ({wtTaken})", Field(proposal, "assay_titration").ColumnDefinitions);
    }

    // ---------------------------------------------------------------- assay by HPLC

    private static readonly Table PeakAreas = Grid(["Injection", "Standard", "Sample 1", "Sample 2"], ["1", "", "", ""], ["2", "", "", ""],
        ["3", "", "", ""], ["Average", "", "", ""], ["SD", "", "", ""], ["RSD", "", "", ""]);

    private static WorksheetImportProposal Hplc(string title, string calculation) => Sheet(
        ("Water Instrument ID:", Body(Water)),
        ("Loss on Drying", Body(LossOnDrying)),
        (title, Body("Chromatographic Conditions: Balance ID: | Detection λ: 210 nm Injection volume: 20μl Temperature: 40°C | Column: C18 250 X 4.6mm Flowrate: 1.2ml/min | "
                     + "Diluent: Mobile Phase | wt of Std taken: wt of Spl taken (i) (ii) | Dilution: | " + calculation
                     + " | x x x 100 | % Assay(i) = ---------------------------------= | X x (100 - ) | Average % assay:", PeakAreas)));

    private static double ReadingsResult(WorksheetImportProposal proposal, string prefix, string table)
    {
        var scalars = new Dictionary<string, double>
        {
            ["water_reading_1"] = 5, ["water_reading_2"] = 5, ["loss_on_drying_w1"] = 20, ["loss_on_drying_w2"] = 22, ["loss_on_drying_w3"] = 21.9,
            [$"{prefix}_std_conc"] = 1, [$"{prefix}_spl_conc_1"] = 1, [$"{prefix}_spl_conc_2"] = 1, [$"{prefix}_purity"] = 99.5,
            [$"{prefix}_dilution_factor"] = 100, [$"{prefix}_spl_weight_1"] = 0.1, [$"{prefix}_spl_weight_2"] = 0.1
        };
        var cells = new Dictionary<string, double>();
        for (var row = 0; row < 3; row++)
        {
            cells[$"{prefix}_{table}.standard[{row}]"] = cells[$"{prefix}_{table}.std[{row}]"] = 1000;
            cells[$"{prefix}_{table}.sample1[{row}]"] = 1010;
            cells[$"{prefix}_{table}.sample2[{row}]"] = 990;
        }

        return Evaluate(proposal, scalars, cells)[$"{prefix}_result"];
    }

    [Theory]
    [InlineData("Assay – HPLC Equipment ID:", "assay_hplc", "Calculations: | : spl peak x conc std x % Purity x 100 | std peak conc spl (100 – Water)", "anhydrous basis (100 − Water)",
        "{assay_hplc_sample_1_average} * {assay_hplc_std_conc} * {assay_hplc_purity} * 100 / ({assay_hplc_standard_average} * {assay_hplc_spl_conc_1} * (100 - {water_result}))", 104.736842)]
    [InlineData("Assay: Equipment ID:", "assay", "Calculations: | : spl peak x conc std x 100 x % Purity | std peak conc spl (100 – LoD)", "dried basis (100 − LOD)",
        "{assay_sample_1_average} * {assay_std_conc} * 100 * {assay_purity} / ({assay_standard_average} * {assay_spl_conc_1} * (100 - {loss_on_drying_result}))", 104.736842)]
    [InlineData("Assay : HPLC", "assay_hplc", "Calculations:", "",
        "{assay_hplc_sample_1_average} / {assay_hplc_standard_average} * {assay_hplc_std_conc} / {assay_hplc_spl_conc_1} * {assay_hplc_purity}", 99.5)]
    public void An_hplc_assay_is_calculated_per_sample_from_the_mean_peak_areas(
        string title, string prefix, string calculation, string variants, string formula, double expected)
    {
        var proposal = Hplc(title, calculation);
        var section = proposal.Template.Sections[^1];

        Assert.Equal("assay_hplc", section.TestDefinition);
        Assert.Equal(variants, string.Join(", ", section.TestDefinitionVariants));
        Assert.Equal(formula, Field(proposal, $"{prefix}_assay_1").FormulaExpression);
        Assert.Equal($"({{{prefix}_assay_1}} + {{{prefix}_assay_2}}) / 2", Field(proposal, $"{prefix}_result").FormulaExpression);
        AssertFromDefinition(proposal, $"{prefix}_result");

        // Injection rows × Standard / Sample 1 / Sample 2, with the Average, SD and RSD rows calculated.
        Assert.Contains("\"fixedValues\":[\"1\",\"2\",\"3\"]", Field(proposal, $"{prefix}_peak_areas").ColumnDefinitions);
        Assert.Equal($"AVG({{{prefix}_peak_areas.standard}})", Field(proposal, $"{prefix}_standard_average").FormulaExpression);
        Assert.Equal($"RSD({{{prefix}_peak_areas.standard}}) * AVG({{{prefix}_peak_areas.standard}}) / 100", Field(proposal, $"{prefix}_standard_sd").FormulaExpression);
        Assert.Equal($"RSD({{{prefix}_peak_areas.sample2}})", Field(proposal, $"{prefix}_sample_2_rsd").FormulaExpression);

        // Inputs: the printed weights and dilution; % purity is an entry; the concentrations the formula needs are added.
        Assert.Equal(["wt of Std taken", "wt of Spl taken (i)", "wt of Spl taken (ii)", "Dilution"],
            new[] { "std_weight", "spl_weight_1", "spl_weight_2", "dilution" }.Select(key => Field(proposal, $"{prefix}_{key}").Label));
        Assert.Equal((WorksheetFieldMode.Entry, "%"), (Field(proposal, $"{prefix}_purity").Mode, Field(proposal, $"{prefix}_purity").Unit));
        Assert.Equal(4, Flags(proposal, WorksheetImportFlagCodes.DefinitionInputAdded).Count(message => message.Contains("concentration") || message.Contains("Purity")));

        // The chromatographic conditions are a constant, kept as printed.
        var conditions = Field(proposal, $"{prefix}_method_conditions");
        Assert.Equal(WorksheetFieldMode.Constant, conditions.Mode);
        Assert.Contains("Detection λ: 210 nm", conditions.ConstantValue);
        Assert.Empty(Flags(proposal, WorksheetImportFlagCodes.DefinitionExtraLine));

        Assert.Equal(expected, ReadingsResult(proposal, prefix, "peak_areas"), 5);
    }

    // ---------------------------------------------------------------- assay by UV

    private static readonly Table Absorbances = T(R(C("Solution"), C("Absorbance", span: 2), C("Mean Abs")),
        R(C("Sample 1"), C(), C(), C()), R(C("Sample 2"), C(), C(), C()));

    private static readonly Table AbsorbancesWithStandard = T(R(C("Solution"), C("Absorbance", span: 2), C("Mean Abs")),
        R(C("Std"), C(), C(), C()), R(C("Sample 1"), C(), C(), C()), R(C("Sample 2"), C(), C(), C()));

    [Fact]
    public void A_uv_assay_by_specific_absorbance_reads_its_constant_from_the_sheet()
    {
        var proposal = Sheet(("Loss on Drying", Body(LossOnDrying)), ("Assay – UV Balance ID:", Body(
            "wt of sample taken (i) g (ii) g | Dilution:__________________[ %w/v] | A¦=715, λ=257 nm | Calculations: | : spl abs x dil factor x 100 x 100 | "
            + "A¦ spl wt taken (100 – LoD) | x x 100 | % Assay(i) = --------------------- x 100 | X x (100 - ) | Average % assay:", Absorbances)));
        var section = proposal.Template.Sections[^1];

        Assert.Equal("assay_uv", section.TestDefinition);
        Assert.Equal(["specific absorbance A(1%, 1cm)", "dried basis (100 − LOD)"], section.TestDefinitionVariants);
        Assert.Equal("{assay_uv_sample_1_average} * {assay_uv_dilution_factor} * 100 * 100 / (715 * {assay_uv_spl_weight_1} * (100 - {loss_on_drying_result}))",
            Field(proposal, "assay_uv_assay_1").FormulaExpression);

        // A(1%, 1cm) and the wavelength are constants read from the sheet.
        Assert.Equal((WorksheetFieldMode.Constant, "715"), (Field(proposal, "assay_uv_a11").Mode, Field(proposal, "assay_uv_a11").ConstantValue));
        Assert.Equal((WorksheetFieldMode.Constant, "257", "nm"),
            (Field(proposal, "assay_uv_wavelength").Mode, Field(proposal, "assay_uv_wavelength").ConstantValue, Field(proposal, "assay_uv_wavelength").Unit));

        // Two readings per sample, one column per solution, and the mean of each.
        Assert.Contains("\"fixedValues\":[\"1\",\"2\"]", Field(proposal, "assay_uv_absorbance").ColumnDefinitions);
        Assert.Equal("AVG({assay_uv_absorbance.sample2})", Field(proposal, "assay_uv_sample_2_average").FormulaExpression);
        Assert.Contains("'Dilution factor' is required", Assert.Single(Flags(proposal, WorksheetImportFlagCodes.DefinitionInputAdded)));

        var scalars = new Dictionary<string, double>
        {
            ["loss_on_drying_w1"] = 20, ["loss_on_drying_w2"] = 22, ["loss_on_drying_w3"] = 21.9,
            ["assay_uv_dilution_factor"] = 100, ["assay_uv_spl_weight_1"] = 0.1, ["assay_uv_spl_weight_2"] = 0.1
        };
        var cells = Enumerable.Range(0, 2).SelectMany(row => new[] { $"assay_uv_absorbance.sample1[{row}]", $"assay_uv_absorbance.sample2[{row}]" }).ToDictionary(key => key, _ => 0.715);
        Assert.Equal(105.263158, Evaluate(proposal, scalars, cells)["assay_uv_result"], 5);
    }

    [Theory]
    [InlineData("Calculations: | : Spl abs x Std conc x % Purity | Std abs Spl conc | x x | % Assay(i) = ----------------- | X | Average % assay:")]
    [InlineData("Calculations:")]
    public void A_uv_assay_against_a_standard_compares_the_mean_absorbances(string calculation)
    {
        var proposal = Sheet("Assay: UV Equipment ID:", "wt of sample taken (i) g (ii) g Std wt g | Dilution:__________[ %w/v] | λ=342nm | " + calculation, AbsorbancesWithStandard);
        var section = proposal.Template.Sections.Single();

        Assert.Equal("assay_uv", section.TestDefinition);
        Assert.Contains("against a standard", section.TestDefinitionVariants);
        Assert.Equal(["Std", "Sample 1", "Sample 2"], new[] { "std", "sample1", "sample2" }.Select(key => key).Select(key =>
            System.Text.Json.JsonDocument.Parse(Field(proposal, "assay_uv_absorbance").ColumnDefinitions).RootElement.EnumerateArray()
                .Single(column => column.GetProperty("key").GetString() == key).GetProperty("label").GetString()));
        Assert.Equal("Std wt", Field(proposal, "assay_uv_std_weight").Label);
        Assert.Null(proposal.Template.Sections.Single().Fields.FirstOrDefault(field => field.FieldKey == "assay_uv_a11"));

        Assert.Equal((1.01 + 0.99) / 2 * 99.5, ReadingsResult(proposal, "assay_uv", "absorbance"), 5);
    }

    // ---------------------------------------------------------------- physical constants

    [Fact]
    public void A_relative_density_is_the_pycnometer_ratio()
    {
        var proposal = Sheet("Identity Test: Relative Density Balance ID:",
            "wt of empty pycnometer (w1): Temp: ˚C | wt of pycnometer + water (w2): | wt of pycnometer + sample (w3): | wt of water (w2 – w1): | "
            + "wt of sample (w3 – w1): | = (w3 – w1) = _________-___________ = | (w2 – w1) - | wt/ml = g/ml");
        const string p = "identity_test_relative_density";

        Assert.Equal("relative_density", proposal.Template.Sections.Single().TestDefinition);
        Assert.Equal($"({{{p}_w3}} - {{{p}_w1}}) / ({{{p}_w2}} - {{{p}_w1}})", Field(proposal, $"{p}_result").FormulaExpression);
        Assert.Equal($"{{{p}_w2}} - {{{p}_w1}}", Field(proposal, $"{p}_water_weight").FormulaExpression);
        Assert.Equal((WorksheetFieldType.Number, "°C"), (Field(proposal, $"{p}_temperature").Type, Field(proposal, $"{p}_temperature").Unit));
        Assert.Empty(Flags(proposal, WorksheetImportFlagCodes.FormulaFromPrint));
        Assert.Empty(Flags(proposal, WorksheetImportFlagCodes.DefinitionExtraLine));

        var values = Evaluate(proposal, new() { [$"{p}_w1"] = 10, [$"{p}_w2"] = 20, [$"{p}_w3"] = 22.6 });
        Assert.Equal(1.26, values[$"{p}_result"], 6);
    }

    [Theory]
    [InlineData("pH: Equipment ID:", "ph", "ph", "Weight of sample taken: ________g | Preparation: | Measurement (i): (ii) Average:")]
    [InlineData("pH", "ph", "ph", "Measurement i: ii. Average pH:")]
    [InlineData("pH", "ph", "ph", "pH: pH: Avg pH:")]
    [InlineData("Identity Test B – Refractive Index Equipment ID:", "refractive_index", "identity_test_b_refractive_ind", "Determination: (i) (ii) Average:")]
    [InlineData("Refractive Index", "refractive_index", "refractive_index", "Readings: (1) (2) mean:")]
    [InlineData("Identity Test A – Melting Point Instrument ID:", "melting_point", "identity_test_a_melting_point", "Determination (i): ˚C (ii) ˚C mean ˚C")]
    [InlineData("Melting Point", "melting_point", "melting_point", "Determination 1: 2: Average:")]
    [InlineData("Water Instrument ID:", "water", "water", Water)]
    [InlineData("Conductivity: Equipment ID:", "conductivity", "conductivity", "Weight of sample taken: ____g | Preparation: | Determination (i) (ii) Average:")]
    public void Two_readings_and_their_mean(string title, string definition, string prefix, string lines)
    {
        var proposal = Sheet(title, lines);

        Assert.Equal(definition, proposal.Template.Sections.Single().TestDefinition);
        Assert.Equal($"({{{prefix}_reading_1}} + {{{prefix}_reading_2}}) / 2", Field(proposal, $"{prefix}_result").FormulaExpression);
        Assert.Equal(WorksheetFieldType.Result, Field(proposal, $"{prefix}_result").Type);
        AssertFromDefinition(proposal, $"{prefix}_result");
        Assert.Empty(Flags(proposal, WorksheetImportFlagCodes.DefinitionInputAdded));
        Assert.Empty(Flags(proposal, WorksheetImportFlagCodes.DefinitionExtraLine));

        var values = Evaluate(proposal, new() { [$"{prefix}_reading_1"] = 6.8, [$"{prefix}_reading_2"] = 7.0 });
        Assert.Equal(6.9, values[$"{prefix}_result"], 6);
    }

    [Fact]
    public void A_sheet_with_one_reading_gets_the_second_added()
    {
        var proposal = Sheet("pH", "Preparation: | pH = ________");

        Assert.Equal("pH", Field(proposal, "ph_reading_1").Label);
        Assert.Equal("pH (ii)", Field(proposal, "ph_reading_2").Label);
        Assert.Contains("'pH' is required by the 'pH' definition", Assert.Single(Flags(proposal, WorksheetImportFlagCodes.DefinitionInputAdded)));
        Assert.Equal("({ph_reading_1} + {ph_reading_2}) / 2", Field(proposal, "ph_result").FormulaExpression);
    }

    [Theory]
    [InlineData("Conductivity of the solution (C1) | Conductivity of the water used for preparing the solution (C2) : | Conductivity = C1 – 0.35C2", "0.35", 9.3)]
    [InlineData("Weight of sample taken: ____g | Preparation: | Determination (C1): (C2) | Calculation: C1 – 0.992C2", "0.992", 8.016)]
    public void A_water_corrected_conductivity_takes_its_factor_from_the_sheet(string lines, string factor, double expected)
    {
        var proposal = Sheet("Conductivity: Equipment ID:", lines);
        var section = proposal.Template.Sections.Single();

        Assert.Equal(("conductivity", "water-corrected (C1 − k × C2)"), (section.TestDefinition, Assert.Single(section.TestDefinitionVariants)));
        Assert.Equal($"{{conductivity_c1}} - {factor} * {{conductivity_c2}}", Field(proposal, "conductivity_result").FormulaExpression);
        Assert.DoesNotContain(section.Fields, field => field.FieldKey.Contains("reading"));
        Assert.Empty(Flags(proposal, WorksheetImportFlagCodes.DefinitionExtraLine));

        Assert.Equal(expected, Evaluate(proposal, new() { ["conductivity_c1"] = 10, ["conductivity_c2"] = 2 })["conductivity_result"], 6);
    }

    [Fact]
    public void A_specific_optical_rotation_adds_the_path_length_and_concentration_it_needs()
    {
        var proposal = Sheet("Specific Optical Rotation Instrument ID:", "Weight of sample taken: ____________g | Preparation: | Calculation:");
        const string p = "specific_optical_rotation";

        Assert.Equal("optical_rotation", proposal.Template.Sections.Single().TestDefinition);
        Assert.Empty(proposal.Template.Sections.Single().TestDefinitionVariants);
        Assert.Equal($"(({{{p}_rotation_1}} + {{{p}_rotation_2}}) / 2) * 100 / ({{{p}_path_length}} * {{{p}_concentration}})", Field(proposal, $"{p}_result").FormulaExpression);
        Assert.Equal(4, Flags(proposal, WorksheetImportFlagCodes.DefinitionInputAdded).Count);
        Assert.Empty(Flags(proposal, WorksheetImportFlagCodes.FormulaNeedsReview));

        var values = Evaluate(proposal, new() { [$"{p}_rotation_1"] = 1.0, [$"{p}_rotation_2"] = 1.2, [$"{p}_path_length"] = 2, [$"{p}_concentration"] = 1 });
        Assert.Equal(55, values[$"{p}_result"], 6);
    }

    [Fact]
    public void An_optical_rotation_with_readings_only_is_the_observed_angle()
    {
        var proposal = Sheet("Optical Rotation: Equipment ID:", "Determination (i): (ii) mean:");

        Assert.Equal(["observed angle only"], proposal.Template.Sections.Single().TestDefinitionVariants);
        Assert.Equal("({optical_rotation_rotation_1} + {optical_rotation_rotation_2}) / 2", Field(proposal, "optical_rotation_result").FormulaExpression);
        Assert.Empty(Flags(proposal, WorksheetImportFlagCodes.DefinitionInputAdded));
        Assert.DoesNotContain(proposal.Template.Sections.Single().Fields, field => field.FieldKey.Contains("path_length"));
    }

    [Fact]
    public void A_dried_basis_optical_rotation_is_read_from_the_printed_calculation()
    {
        var proposal = Sheet(("Loss on Drying", Body(LossOnDrying)), ("Specific Optical Rotation",
            Body("Weight of sample taken: ____g | Preparation: | Determination (i): (ii) mean: | Calculation: α x 100 x 100 | L x conc x (100- LOD)")));
        const string p = "specific_optical_rotation";

        Assert.Equal(["dried basis (100 − LOD)"], proposal.Template.Sections[^1].TestDefinitionVariants);
        Assert.Equal($"(({{{p}_rotation_1}} + {{{p}_rotation_2}}) / 2) * 100 * 100 / ({{{p}_path_length}} * {{{p}_concentration}} * (100 - {{loss_on_drying_result}}))",
            Field(proposal, $"{p}_result").FormulaExpression);

        var values = Evaluate(proposal, new()
        {
            ["loss_on_drying_w1"] = 20, ["loss_on_drying_w2"] = 22, ["loss_on_drying_w3"] = 21.9,
            [$"{p}_rotation_1"] = 1.0, [$"{p}_rotation_2"] = 1.2, [$"{p}_path_length"] = 2, [$"{p}_concentration"] = 1
        });
        Assert.Equal(55 * 100 / 95.0, values[$"{p}_result"], 6);
    }

    [Theory]
    [InlineData("Acid Value Balance ID:", "acid_value", "Acid Value", "5.610")]
    [InlineData("Saponification Value", "saponification_value", "Saponification value", "28.05")]
    [InlineData("Iodine Value Balance ID:", "iodine_value", "Iodine Value", "1.269")]
    public void A_titre_value_takes_its_numeric_factor_from_the_sheet(string title, string definition, string name, string factor)
    {
        var proposal = Sheet(title, $"Weight of sample: g Titre obtained: | Titre x {factor} | {name} = ------------------------------- | Weight of sample | x | {name} = --------------------------------- =");
        var prefix = definition;

        Assert.Equal(definition, proposal.Template.Sections.Single().TestDefinition);
        Assert.Equal($"{{{prefix}_titre}} * {factor} / ({{{prefix}_weight}})", Field(proposal, $"{prefix}_result").FormulaExpression);
        Assert.Contains($"printed: '{name} = [Titre x {factor}] / [Weight of sample]'", Assert.Single(Flags(proposal, WorksheetImportFlagCodes.FormulaFromDefinition)));
        Assert.Empty(Flags(proposal, WorksheetImportFlagCodes.DefinitionExtraLine));
        Assert.Empty(Flags(proposal, WorksheetImportFlagCodes.DefinitionInputAdded));

        var value = double.Parse(factor, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(2, Evaluate(proposal, new() { [$"{prefix}_titre"] = 2, [$"{prefix}_weight"] = value })[$"{prefix}_result"], 6);
    }

    [Fact]
    public void A_titre_value_over_a_blank_adds_the_blank_titre()
    {
        var proposal = Sheet("Iodine Value", "Weight of sample: Titre obtained: | 1.269 x (n2 – n1) | Iodine Value = ------------------------------- | Weight of sample");

        Assert.Equal(["blank titre (n2 − n1)"], proposal.Template.Sections.Single().TestDefinitionVariants);
        Assert.Equal("1.269 * ({iodine_value_blank_titre} - {iodine_value_titre}) / ({iodine_value_weight})", Field(proposal, "iodine_value_result").FormulaExpression);
        Assert.Contains("'Blank titre (n2)' is required", Assert.Single(Flags(proposal, WorksheetImportFlagCodes.DefinitionInputAdded)));
    }

    // ---------------------------------------------------------------- observation and limit tests

    [Theory]
    [InlineData("Description / Appearance", "description")]
    [InlineData("Odour", "odour")]
    [InlineData("Taste", "taste")]
    [InlineData("Cap Colour", "cap_colour")]
    [InlineData("Body Colour", "body_colour")]
    [InlineData("Printing Details", "printing_details")]
    public void An_observed_test_is_one_observation_in_its_empty_cell(string title, string definition)
    {
        var proposal = Sheet(title, "");
        var section = proposal.Template.Sections.Single();
        var field = Assert.Single(section.Fields);

        Assert.Equal(definition, section.TestDefinition);
        Assert.Equal(($"{ImportText.SnakeKey(title, 30)}_result", "Observation", WorksheetFieldType.LongText, WorksheetFieldMode.Entry), (field.FieldKey, field.Label, field.Type, field.Mode));
        Assert.Equal(ImportConfidence.High, proposal.FieldProvenance.Single().Confidence);
        Assert.Empty(proposal.Flags.Where(flag => flag.Code is WorksheetImportFlagCodes.DefinitionInputAdded or WorksheetImportFlagCodes.DefinitionExtraLine));
        Assert.Equal(field.FieldKey, RawMaterialResultField.Choose(section));
    }

    [Fact]
    public void Solubility_is_one_observation_per_printed_solvent()
    {
        var proposal = Sheet("Solubility", "Water: Ethanol (96%): | Toluene: 100g/L of NaOH: | Methylene Chloride: ____________");

        Assert.Equal("solubility", proposal.Template.Sections.Single().TestDefinition);
        Assert.Equal([("solubility_water", "Water"), ("solubility_ethanol_96", "Ethanol (96%)"), ("solubility_toluene", "Toluene"),
                ("solubility_f_100g_l_of_naoh", "100g/L of NaOH"), ("solubility_methylene_chloride", "Methylene Chloride"), ("solubility_result", "Result")],
            proposal.Template.Sections.Single().Fields.Select(field => (field.FieldKey, field.Label)));
        Assert.All(proposal.Template.Sections.Single().Fields.SkipLast(1), field => Assert.Equal(WorksheetFieldType.ShortText, field.Type));
        Assert.Empty(proposal.Flags);
    }

    [Theory]
    [InlineData("Appearance of Solution (Solution S)", "appearance_of_solution")]
    [InlineData("Colour of Solution", "appearance_of_solution")]
    [InlineData("Acidity or Alkalinity", "acidity_alkalinity")]
    [InlineData("Alkalinity:", "acidity_alkalinity")]
    [InlineData("Reducing Sugars", "reducing_sugars")]
    [InlineData("Identity Test B – Colour Test", "identity_reaction")]
    [InlineData("Identity Test: Reaction of Chlorides", "identity_reaction")]
    [InlineData("Identity Test A", "identity_reaction")]
    public void A_prepared_test_records_weight_preparation_and_observation(string title, string definition)
    {
        var proposal = Sheet(title, "Weight of sample taken: ____________g | Preparation | Observation:");
        var section = proposal.Template.Sections.Single();

        Assert.Equal(definition, section.TestDefinition);
        Assert.Equal([(WorksheetFieldType.Number, "g"), (WorksheetFieldType.LongText, null), (WorksheetFieldType.LongText, null)],
            section.Fields.Select(field => (field.Type, (string?)field.Unit)));
        Assert.Equal(["weight", "preparation", "observation"], section.Fields.Select(field => field.FieldKey[(field.FieldKey.LastIndexOf('_') + 1)..]));
        Assert.Empty(proposal.Flags);
        Assert.EndsWith("_observation", RawMaterialResultField.Choose(section));
    }

    [Fact]
    public void A_prepared_test_without_a_preparation_line_gets_one_added()
    {
        var proposal = Sheet("Appearance of Solution", "Weight of sample taken: ____________g | Observation:");

        Assert.Equal(["appearance_of_solution_weight", "appearance_of_solution_observation", "appearance_of_solution_preparation"],
            proposal.Template.Sections.Single().Fields.Select(field => field.FieldKey));
        Assert.Contains("'Preparation' is required by the 'Appearance of Solution' definition", Assert.Single(Flags(proposal, WorksheetImportFlagCodes.DefinitionInputAdded)));
        Assert.Equal(ImportConfidence.Medium, proposal.FieldProvenance.Single(item => item.FieldKey == "appearance_of_solution_preparation").Confidence);
    }

    [Theory]
    [InlineData("Heavy Metals", "heavy_metals")]
    [InlineData("Chlorides:", "chlorides")]
    [InlineData("Sulphates", "sulfates")]
    [InlineData("Iron", "iron")]
    public void A_limit_test_compares_test_and_reference_solutions_and_infers_compliance(string title, string definition)
    {
        var proposal = Sheet(title, "Test solution: | Reference solution: | Inference:");
        var section = proposal.Template.Sections.Single();
        var prefix = ImportText.SnakeKey(title, 30);

        Assert.Equal(definition, section.TestDefinition);
        Assert.Equal([$"{prefix}_test_solution", $"{prefix}_reference_solution", $"{prefix}_inference", $"{prefix}_observation"], section.Fields.Select(field => field.FieldKey));
        var inference = Field(proposal, $"{prefix}_inference");
        Assert.Equal((WorksheetFieldType.Select, WorksheetFieldMode.Entry), (inference.Type, inference.Mode));
        Assert.Equal(["Complies", "Does not comply"], inference.Options);
        Assert.Contains("'Observation' is required", Assert.Single(Flags(proposal, WorksheetImportFlagCodes.DefinitionInputAdded)));
    }

    [Fact]
    public void A_limit_test_printed_as_preparation_and_observation_gets_its_inference_added()
    {
        var proposal = Sheet("Sulfates", "Preparation: | Reference Solution: | Observation:");

        Assert.Equal(["sulfates_test_solution", "sulfates_reference_solution", "sulfates_observation", "sulfates_inference"],
            proposal.Template.Sections.Single().Fields.Select(field => field.FieldKey));
        Assert.Equal("Preparation", Field(proposal, "sulfates_test_solution").Label);
        Assert.Equal("sulfates_inference", RawMaterialResultField.Choose(proposal.Template.Sections.Single()));
    }

    [Fact]
    public void An_identity_by_ir_records_the_weighings_the_spectrum_and_an_observation()
    {
        var proposal = Sheet("Identity Test: IR Equipment ID:", "Balance ID: | Weight of KBr: __________ g Weight of sample: _________ g | Attach Print Out");
        var section = proposal.Template.Sections.Single();

        Assert.Equal("identity_ir", section.TestDefinition);
        Assert.Equal([("identity_test_ir_equipment_id", WorksheetFieldType.Instrument), ("identity_test_ir_balance_id", WorksheetFieldType.Instrument),
                ("identity_test_ir_kbr_weight", WorksheetFieldType.Number), ("identity_test_ir_sample_weight", WorksheetFieldType.Number),
                ("identity_test_ir_spectrum", WorksheetFieldType.FileUpload), ("identity_test_ir_observation", WorksheetFieldType.LongText)],
            section.Fields.Select(field => (field.FieldKey, field.Type)));
        Assert.Contains("'Observation' is required by the 'Identity by IR' definition", Assert.Single(Flags(proposal, WorksheetImportFlagCodes.DefinitionInputAdded)));
        Assert.Equal("identity_test_ir_observation", RawMaterialResultField.Choose(section));
    }

    // ---------------------------------------------------------------- capsule shells

    [Fact]
    public void A_disintegration_time_is_a_number_of_minutes_a_specification_can_bind_to()
    {
        var proposal = Sheet("Disintegration Time", "");
        var field = Assert.Single(proposal.Template.Sections.Single().Fields);

        Assert.Equal(("disintegration_time_result", WorksheetFieldType.Number, "min"), (field.FieldKey, field.Type, field.Unit));
        Assert.Equal("disintegration_time_result", RawMaterialResultField.Choose(proposal.Template.Sections.Single()));
        Assert.Empty(proposal.Flags);
    }

    [Fact]
    public void Printing_details_keep_their_cap_and_body_lines()
    {
        var proposal = Sheet("Printing Details", "CAP: | BODY:");

        Assert.Equal(["printing_details_cap", "printing_details_body", "printing_details_result"], proposal.Template.Sections.Single().Fields.Select(field => field.FieldKey));
    }

    [Fact]
    public void An_average_weight_is_twenty_weighings_with_their_total_and_average()
    {
        var proposal = Sheet("Average Weight Balance ID:",
            "01) 02) 03) 04) | 05) 06) 07) 08) | 09) 10) 11) 12) | 13) 14) 15) 16) | 17) 18) 19) 20) | Weight of 20 Shells : : | Average Weight of Shell :");
        var section = proposal.Template.Sections.Single();

        Assert.Equal("average_weight", section.TestDefinition);
        Assert.Contains("\"fixedValues\":[\"01\",\"02\"", Field(proposal, "average_weight_individual_weights").ColumnDefinitions);
        Assert.Contains("\"20\"],\"rowHeader\":true", Field(proposal, "average_weight_individual_weights").ColumnDefinitions);
        Assert.Equal(("Weight of 20 Shells", "SUM({average_weight_individual_weights.weight})"),
            (Field(proposal, "average_weight_total").Label, Field(proposal, "average_weight_total").FormulaExpression));
        Assert.Equal(("Average Weight of Shell", "AVG({average_weight_individual_weights.weight})", WorksheetFieldType.Result),
            (Field(proposal, "average_weight_result").Label, Field(proposal, "average_weight_result").FormulaExpression, Field(proposal, "average_weight_result").Type));
        Assert.Empty(Flags(proposal, WorksheetImportFlagCodes.DefinitionInputAdded));

        var cells = Enumerable.Range(0, 20).ToDictionary(row => $"average_weight_individual_weights.weight[{row}]", row => 95.0 + row % 2 * 10);
        var values = Evaluate(proposal, cells: cells);
        Assert.Equal((2000, 100), (values["average_weight_total"], values["average_weight_result"]));
    }

    // ---------------------------------------------------------------- recognizer changes

    [Fact]
    public void A_stacked_fraction_is_joined_into_one_printed_formula()
    {
        string[] lines =
        [
            "Factor of Volumetric Solution =", "Content Calculations:", "(Sample Titre – Blank Titre) x Factor x Equiv. x 100",
            "% Assay = ---------------------------------------------------------------------- x 100", "Weight of sample x (100 – LOD)",
            "( – ) x x x", "% Assay (1) = ------------------------------------------------------------------------------ x 100", "x",
            "( – ) x x x", "% Assay (2) = ------------------------------------------------------------------------- x 100", "X", "Average %Assay ="
        ];

        var result = StackedFractions.Read(lines);

        var fraction = Assert.Single(result.Fractions);
        Assert.Equal("% Assay = [(Sample Titre – Blank Titre) x Factor x Equiv. x 100] / [Weight of sample x (100 – LOD)] x 100", fraction.Text);
        // The blank worked sums under it belong to the calculation too; the labels around it do not.
        Assert.Equal(Enumerable.Range(2, 9), result.Consumed.Order());

        // HPLC / UV sheets draw the rule as a border: the line after "Calculations:" is the numerator.
        var bordered = StackedFractions.Read(["Dilution:", "Calculations:", ": spl peak x conc std x % Purity x 100", "std peak conc spl (100 – Water)", "x x x 100"]);
        Assert.Equal("[spl peak x conc std x % Purity x 100] / [std peak conc spl (100 – Water)]", Assert.Single(bordered.Fractions).Text);
    }

    [Fact]
    public void A_printed_term_the_definition_does_not_know_is_never_guessed_around()
    {
        var proposal = Titration("(Sample Titre – Blank Titre) x Factor x Equiv. x Mystery", "x 100", "Weight of sample");

        // The base formula is used, and the provenance says the print could not be read.
        Assert.Contains("* 20.00 * 100 / {wtTaken}\"", Field(proposal, "assay_titration_titration").ColumnDefinitions);
        Assert.Contains(proposal.Flags, flag => flag.Code == WorksheetImportFlagCodes.FormulaFromDefinition && flag.Message.Contains("which the definition could not read"));
        Assert.Equal(ImportConfidence.Low, proposal.FieldProvenance.Single(item => item.FieldKey == "assay_titration_result").Confidence);
        Assert.Null(PrintedTerms.Scan("Factor x Mystery", RawMaterialTestDefinitions.Find("assay_titration").Terms, []));
    }

    [Fact]
    public void A_sheet_that_numbers_nothing_opens_a_test_at_each_title_row()
    {
        using var stream = Create(
        [
            T(R(C("No"), C("TEST & OBSERVATIONS")),
              R(C(), C("Description / Appearance")), R(C(), C()),
              R(C(), C("Solubility")), R(C(), C("Water: Glycerol: Ethanol (96%):")),
              R(C(), C("Alkalinity:")), R(C(), C("Preparation: | Observation:")),
              R(C(), C("Iodates:")), R(C(), C("Preparation: | Observation:")),
              R(C(), C("Assay – Titration")), R(C(), Body("Balance ID.: | Factor of Volumetric Solution = | Content Calculations:", TitrationGridTable)),
              R(C(), C("Analysed by: checked by:")))
        ], Header);
        var proposal = WorksheetDocxImportService.Propose("902 - Unnumbered.docx", stream, InMemoryWorksheetImportCatalog.Empty);

        Assert.Equal([("Description / Appearance", "description"), ("Solubility", "solubility"), ("Alkalinity", "acidity_alkalinity"), ("Iodates", null), ("Assay – Titration", "assay_titration")],
            proposal.Template.Sections.Select(section => (section.Name, (string?)section.TestDefinition)));
        Assert.Equal(3, proposal.Template.Sections[1].Fields.Count(field => field.Type == WorksheetFieldType.ShortText));
        Assert.Empty(Flags(proposal, WorksheetImportFlagCodes.UnrecognizedContent));
    }

    [Fact]
    public void An_unnumbered_defined_test_after_a_numbered_one_opens_its_own_section()
    {
        using var stream = Create(
        [
            T(R(C("No"), C("TEST & OBSERVATIONS")),
              R(C("1."), C("Solubility")), R(C(), C("Water:")), R(C(), C("Acetone:")),
              R(C("2."), C("Chlorides")), R(C(), C("Test solution: | Reference solution: | Inference:")),
              R(C(), C("Assay – Titration")), R(C(), Body("Factor of Volumetric Solution = | Content Calculations:", TitrationGridTable)))
        ], Header);
        var proposal = WorksheetDocxImportService.Propose("903 - Mixed.docx", stream, InMemoryWorksheetImportCatalog.Empty);

        // "Water:" under Solubility stays a solvent; "Assay – Titration" is a test of its own.
        Assert.Equal(["Solubility", "Chlorides", "Assay – Titration"], proposal.Template.Sections.Select(section => section.Name));
        Assert.Equal(["solubility_water", "solubility_acetone", "solubility_result"], proposal.Template.Sections[0].Fields.Select(field => field.FieldKey));
    }

    [Fact]
    public void A_line_the_definition_does_not_know_is_kept_and_flagged()
    {
        var proposal = Sheet("pH", "Solution S was used | Volume of solution S taken ____ ml | Buffer lot: | Measurement (i): (ii) Average:", Grid(["Run", "Remark"], ["1", ""], ["2", ""]));
        var section = proposal.Template.Sections.Single();

        // A printed note, an unknown blank and a nested table: all kept, each flagged.
        var note = Field(proposal, "ph_solution_s_was_used_note");
        Assert.Equal((WorksheetFieldType.Instructions, WorksheetFieldMode.Constant, "Solution S was used"), (note.Type, note.Mode, note.ConstantValue));
        Assert.Equal((WorksheetFieldType.Number, "mL"), (Field(proposal, "ph_volume").Type, Field(proposal, "ph_volume").Unit));
        Assert.Contains(section.Fields, field => field.Label == "Buffer lot");
        Assert.Contains(section.Fields, field => field.Type == WorksheetFieldType.Table);
        Assert.Equal(3, Flags(proposal, WorksheetImportFlagCodes.DefinitionExtraLine).Count);
        Assert.Empty(Flags(proposal, WorksheetImportFlagCodes.UnrecognizedContent));
    }

    [Fact]
    public void A_limit_printed_in_the_test_name_is_a_constant()
    {
        var proposal = Sheet("pH [4.5 – 6.0]", "Measurement (i): (ii) Average:");
        var section = proposal.Template.Sections.Single();

        Assert.Equal(("pH", "ph"), (section.Name, section.TestDefinition));
        Assert.Equal((WorksheetFieldMode.Constant, "4.5 – 6.0", "Limit"), (Field(proposal, "ph_limit").Mode, Field(proposal, "ph_limit").ConstantValue, Field(proposal, "ph_limit").Label));
    }

    [Fact]
    public void A_test_with_no_definition_keeps_the_layout_reading_and_drops_nothing()
    {
        var proposal = Sheet("Barium", "Weight of sample | Preparation | Use solution S | Observation | Calculation:", Grid(["Run", "Remark"], ["1", ""], ["2", ""]));
        var section = proposal.Template.Sections.Single();

        Assert.Null(section.TestDefinition);
        Assert.Equal([("barium_weight_of_sample", WorksheetFieldType.Number), ("barium_preparation", WorksheetFieldType.LongText),
                ("barium_use_solution_s_note", WorksheetFieldType.Instructions), ("barium_observation", WorksheetFieldType.LongText),
                ("barium_table", WorksheetFieldType.Table), ("barium_result", WorksheetFieldType.Result)],
            section.Fields.Select(field => (field.FieldKey, field.Type)));

        // Locked decision 4: no definition, so the formula is still left for the reviewer.
        Assert.Null(Field(proposal, "barium_result").FormulaExpression);
        Assert.Single(Flags(proposal, WorksheetImportFlagCodes.FormulaNeedsReview));
        Assert.Empty(Flags(proposal, WorksheetImportFlagCodes.UnrecognizedContent));
        Assert.Empty(proposal.Flags.Where(flag => flag.Code is WorksheetImportFlagCodes.DefinitionExtraLine or WorksheetImportFlagCodes.DefinitionInputAdded));
    }

    [Fact]
    public void Every_definition_is_reachable_by_a_name_and_states_a_result()
    {
        Assert.Equal(RawMaterialTestDefinitions.All.Count, RawMaterialTestDefinitions.All.Select(definition => definition.Key).Distinct().Count());
        Assert.All(RawMaterialTestDefinitions.All, definition =>
        {
            Assert.NotEmpty(definition.NamePatterns);
            Assert.False(string.IsNullOrWhiteSpace(definition.Title));
            if (definition.ResultKey is not null)
                Assert.True(definition.Calculations.Any(calculation => calculation.Key == definition.ResultKey)
                            || definition.Inputs.Any(input => input.Key == definition.ResultKey), definition.Key);
        });

        // "Assay – Titration" wins over a plain "Assay"; a physical test named under "Identity Test" wins over the identity catch-all.
        Assert.Equal("assay_titration", RawMaterialTestDefinitions.ByName("ASSAY – NON AQUEOUS TITRATION (Potentiometrically)").Key);
        Assert.Equal("melting_point", RawMaterialTestDefinitions.ByName("Identity Test A– Melting point").Key);
        Assert.Equal("identity_ir", RawMaterialTestDefinitions.ByName("IDENTITY TEST C – FTIR").Key);
        Assert.Equal("identity_reaction", RawMaterialTestDefinitions.ByName("Identity Test: Sulfates Test").Key);
        Assert.Equal("sulfates", RawMaterialTestDefinitions.ByName("Sulphates").Key);
        Assert.Equal("relative_density", RawMaterialTestDefinitions.ByName("Specific Gravity").Key);
        Assert.Null(RawMaterialTestDefinitions.ByName("Water Soluble Substances"));
        Assert.Null(RawMaterialTestDefinitions.ByName("Assay"));
    }
}
