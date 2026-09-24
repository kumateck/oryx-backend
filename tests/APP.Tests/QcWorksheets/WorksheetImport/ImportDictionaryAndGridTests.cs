using APP.Services.QcWorksheets;
using APP.Services.QcWorksheets.WorksheetDocxImport;
using DOMAIN.Entities.QcWorksheets;
using Xunit;
using static APP.Tests.QcWorksheets.WorksheetImport.TestDocx;

namespace APP.Tests.QcWorksheets.WorksheetImport;

public class RunDataAndChoiceTests
{
    [Theory]
    [InlineData("Batch No.", "batch_no", RunDataDisposition.Entry)]
    [InlineData("Lot  No.", "lot_no", RunDataDisposition.Entry)]
    [InlineData("A.R. No.", "ar_no", RunDataDisposition.HeaderData)]
    [InlineData("AR Number", "ar_no", RunDataDisposition.HeaderData)]
    [InlineData("Issued by", "issued_by", RunDataDisposition.HeaderData)]
    [InlineData("Date of Expiry", "exp_date", RunDataDisposition.Entry)]
    [InlineData("Previously approved lot", "previously_approved_batch_no", RunDataDisposition.Entry)]
    [InlineData("Analysis End Date", "analysis_end_date", RunDataDisposition.Entry)]
    public void Run_data_labels_are_recognised_in_their_printed_variants(string label, string key, RunDataDisposition disposition)
    {
        Assert.True(RunDataLabels.TryMatch(label, out var match));
        Assert.Equal(key, match.Key);
        Assert.Equal(disposition, match.Disposition);
    }

    [Theory]
    [InlineData("05/05/2026", true)]
    [InlineData("08/2021", true)]
    [InlineData("QCD/26/002/0000493532", true)]
    [InlineData("26/0730", true)]
    [InlineData("Cream to yellow homogenous free flowing powder", false)]
    [InlineData("QCD/RGT/CA-001", false)]
    [InlineData("Sterilized Phosphate buffer solution pH 7.2", false)]
    public void Run_data_shaped_values_are_detected(string value, bool expected) =>
        Assert.Equal(expected, RunDataLabels.LooksLikeRunData(value));

    [Theory]
    [InlineData("Remark: Complies/ Does not comply", "Complies", "Does not comply")]
    [InlineData("Remark: Comply / Do not Comply", "Comply", "Do not comply")]
    [InlineData("Result: Absent / Detected", "Absent", "Detected")]
    [InlineData("Presence of E. coli / Absence of E. coli", "Presence of E. coli", "Absence of E. coli")]
    [InlineData("Negative control: There was / was no growth on the plates.", "Growth", "No Growth")]
    [InlineData("There was / was no clearly visible growth of the organism", "Growth", "No Growth")]
    [InlineData("Medium did / did not inhibit growth", "Did inhibit", "Did not inhibit")]
    [InlineData("… agar plates was / was not more than factor 2.", "Was more than factor 2", "Was not more than factor 2")]
    [InlineData("Colonies did / did not produce green pigmentation.", "Did produce green pigmentation", "Did not produce green pigmentation")]
    public void Every_dictionary_phrase_becomes_a_two_option_choice(string text, string first, string second)
    {
        var match = Assert.Single(ChoicePhrases.Find(text));
        Assert.Equal([first, second], match.Options);
    }

    [Theory]
    [InlineData("Ampicillin/Sulbactam A/S10/10 µg")]
    [InlineData("QCD/EQT/BAL/006")]
    [InlineData("Result (CFU/mL)")]
    [InlineData("Room/Area Name")]
    public void Other_slashes_are_not_choices(string text) => Assert.Empty(ChoicePhrases.Find(text));

    [Fact]
    public void Two_phrases_in_one_sentence_are_two_choices_in_reading_order()
    {
        var matches = ChoicePhrases.Find("… was / was not more than factor 2. Colonies did / did not produce green pigmentation.");

        Assert.Equal(["Factor 2", "Green pigmentation"], matches.Select(match => match.Topic));
    }
}

public class ParameterTableTests
{
    [Fact]
    public void A_printed_value_is_a_constant_and_a_blank_is_an_entry()
    {
        var constant = ParameterTable.Decide("Appearance", "Cream to yellow powder");
        var blank = ParameterTable.Decide("Quantity of dehydrated powder weighed", "…………………");

        Assert.Equal((ParameterKind.Constant, "Cream to yellow powder"), (constant.Kind, constant.ConstantValue));
        Assert.Equal((ParameterKind.Entry, WorksheetFieldType.Number), (blank.Kind, blank.Type));
    }

    [Fact]
    public void A_run_data_label_never_keeps_its_printed_value()
    {
        var batch = ParameterTable.Decide("Batch No.", "QCD/26/002/0000000001");
        var issuer = ParameterTable.Decide("Issued By", "A. Person");

        Assert.Equal(ParameterKind.Entry, batch.Kind);
        Assert.Null(batch.ConstantValue);
        Assert.Equal(ParameterKind.HeaderData, issuer.Kind);
        Assert.Null(issuer.ConstantValue);
    }

    [Fact]
    public void A_run_data_shaped_value_under_an_unknown_label_is_flagged_not_kept()
    {
        var decision = ParameterTable.Decide("Received on", "12/03/2026");

        Assert.Equal(ParameterKind.Entry, decision.Kind);
        Assert.Equal(WorksheetImportFlagCodes.SuspectedRunData, decision.FlagCode);
        Assert.Equal(ImportConfidence.Low, decision.Confidence);
    }

    [Fact]
    public void Text_after_a_blank_is_its_unit_and_a_dangling_pH_is_flagged()
    {
        var passages = ParameterTable.Decide("No. of passages", "…… passages");
        var buffer = ParameterTable.Decide("Buffer used", "Sterilized Phosphate buffer pH");

        Assert.Equal(("passages", ParameterKind.Entry), (passages.Unit, passages.Kind));
        Assert.Equal(WorksheetImportFlagCodes.IncompleteValue, buffer.FlagCode);
        Assert.Equal(ImportConfidence.Low, buffer.Confidence);
    }

    [Fact]
    public void A_cell_holding_several_labels_is_split_on_known_labels()
    {
        var segments = ParameterTable.Segments("Issued by: A. Person Format No.: F-01 Date Sampled:");

        Assert.Equal([("Issued by", "A. Person"), ("Format No.", "F-01"), ("Date Sampled", "")], segments);
    }

    [Fact]
    public void A_two_column_table_is_read_row_by_row()
    {
        var table = Table(["Diluent used", "Sterilized buffer"], ["pH of Diluent", ""], ["Dilution", "1 in 10"]);

        Assert.True(ParameterTable.IsTwoColumnParameterTable(table));
        Assert.Equal([ParameterKind.Constant, ParameterKind.Entry, ParameterKind.Constant],
            ParameterTable.ReadTwoColumn(table).Select(item => item.Decision.Kind));
    }
}

public class DataGridTests
{
    private static DocxTable ResponseGrid() => Read([T(
        R(C("Test strain", merge: VMerge.Restart), C("Incu- | bation | period", merge: VMerge.Restart), C("New Batch", span: 3), C("Previously Approved Batch", span: 2)),
        R(C(merge: VMerge.Continue), C(merge: VMerge.Continue), C("Plate | 1"), C("Plate 2"), C("Av."), C("Plate 1"), C("Growth")),
        R(C("Pseudo-monas aeruginosa (ATCC 9027)"), C("18 hours"), C(), C(), C(), C(), C()),
        R(C("Esche-richia coli (ATCC 8739)"), C("72 hours"), C(), C("…"), C(), C("12"), C()),
        R(C(), C(), C(), C(), C(), C(), C()))]).Tables.Single();

    [Fact]
    public void Two_header_rows_become_groups_and_printed_columns_become_fixed_values()
    {
        var grid = DataGrid.Read(ResponseGrid());

        Assert.Equal(2, grid.HeaderRows);
        Assert.Equal(2, grid.DataRows.Count);
        var strain = grid.Columns[0];
        Assert.Equal((WorksheetFieldType.Organism, true), (strain.Type, strain.RowHeader));
        Assert.Equal(["Pseudomonas aeruginosa (ATCC 9027)", "Escherichia coli (ATCC 8739)"], strain.FixedValues);
        Assert.Equal(["18 hours", "72 hours"], grid.Columns[1].FixedValues);
        Assert.Equal(WorksheetFieldType.IncubationPeriod, grid.Columns[1].Type);
        Assert.Equal(("newBatch_plate1", "New Batch"), (grid.Columns[2].Key, grid.Columns[2].Group));
        Assert.Null(grid.Columns[2].FixedValues);
    }

    [Fact]
    public void A_column_filled_in_only_some_rows_is_an_entry_column_flagged_mixed()
    {
        var mixed = DataGrid.Read(ResponseGrid()).Columns.Single(column => column.Key == "previouslyApprovedBatch_plate1");

        Assert.Null(mixed.FixedValues);
        Assert.Equal((WorksheetImportFlagCodes.MixedColumn, ImportConfidence.Low), (mixed.FlagCode, mixed.Confidence));
    }

    [Fact]
    public void A_unit_only_sub_header_is_the_unit_not_a_group()
    {
        var table = Read([T(
            R(C("Room No. | ID No.", merge: VMerge.Restart), C("Airborne Viables")),
            R(C(merge: VMerge.Continue), C("(CFU/4Hrs)")),
            R(C("R-1"), C()))]).Tables.Single();

        var count = DataGrid.Read(table).Columns[1];
        Assert.Equal(("Airborne Viables", null, "CFU/4Hrs"), (count.Label, count.Group, count.Unit));
    }

    [Fact]
    public void Plate_average_becomes_a_calculated_column_with_a_valid_formula()
    {
        var grid = DataGrid.Read(ResponseGrid());
        var averages = PlateAverage.Apply(grid.Columns);

        var average = Assert.Single(averages);
        Assert.Equal(WorksheetFieldMode.Calculated, average.Mode);
        Assert.Equal("({newBatch_plate1} + {newBatch_plate2}) / 2", average.Formula);
        Assert.True(QcFormulaEvaluator.Analyze(average.Formula).IsValid);

        var json = ColumnDefinitionsJson.Write(grid.Columns);
        Assert.Contains("\"group\":\"New Batch\"", json);
        Assert.Contains("\"fixedValues\":[\"18 hours\",\"72 hours\"]", json);
        Assert.Contains("\"mode\":\"Calculated\"", json);
    }

    [Fact]
    public void Formula_library_expressions_parse_and_evaluate()
    {
        Assert.True(QcFormulaEvaluator.Analyze(FormulaLibrary.CfuFromAverage("average_count", "dilution_factor")).IsValid);
        Assert.True(QcFormulaEvaluator.Analyze(FormulaLibrary.ColumnAverage("results", "count")).IsValid);

        var resolver = new Scalars(new() { ["plate_1"] = 30, ["plate_2"] = 50, ["average"] = 40, ["factor"] = 10 });
        Assert.True(QcFormulaEvaluator.TryEvaluate(FormulaLibrary.Average("plate_1", "plate_2"), resolver, out var mean, out _));
        Assert.True(QcFormulaEvaluator.TryEvaluate(FormulaLibrary.CfuFromAverage("average", "factor"), resolver, out var cfu, out _));
        Assert.Equal((40d, 400d), (mean, cfu));
    }

    private sealed class Scalars(Dictionary<string, double> values) : IQcFormulaValueResolver
    {
        public bool TryGetScalar(string fieldKey, out double value) => values.TryGetValue(fieldKey, out value);

        public bool TryGetColumn(string tableFieldKey, string columnKey, out IReadOnlyList<double> column)
        {
            column = [];
            return false;
        }
    }
}
