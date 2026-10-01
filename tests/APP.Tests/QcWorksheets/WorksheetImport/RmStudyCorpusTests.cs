using System.Text.RegularExpressions;
using APP.Services.QcWorksheets;
using APP.Services.QcWorksheets.WorksheetDocxImport;
using APP.Services.QcWorksheets.WorksheetDocxImport.TestDefinitions;
using DOMAIN.Entities.QcWorksheets;
using Xunit;
using Xunit.Abstractions;

namespace APP.Tests.QcWorksheets.WorksheetImport;

/// <summary>Brief 12 on the study corpus of real raw-material worksheets. Runs only with <c>QC_RM_STUDY_DIR</c> set.</summary>
public class RmStudyCorpusTests(ITestOutputHelper output)
{
    private static IEnumerable<(StudyFile File, ProposedWorksheetSection Section, RawMaterialTestDefinition Definition)> DefinedSections() =>
        StudyCorpus.Worksheets().SelectMany(file => file.Proposal.Template.Sections
            .Where(section => section.TestDefinition is not null)
            .Select(section => (file, section, RawMaterialTestDefinitions.Find(section.TestDefinition))));

    [StudyCorpusFact]
    public void Every_file_is_read_without_a_crash()
    {
        // Loading proposes every file; an exception anywhere fails here.
        var files = StudyCorpus.Load();

        Assert.True(files.Count >= 300, $"only {files.Count} files");
        Assert.True(StudyCorpus.Worksheets().Count() >= 310, $"only {StudyCorpus.Worksheets().Count()} worksheets classified");
        Assert.All(StudyCorpus.Worksheets(), file => Assert.NotEmpty(file.Proposal.Template.Sections));
    }

    [StudyCorpusFact]
    public void Every_defined_test_has_its_inputs_and_a_formula()
    {
        var sections = DefinedSections().ToList();
        Assert.True(sections.Count >= 2000, $"only {sections.Count} sections were read with a definition");

        foreach (var (file, section, definition) in sections)
        {
            var where = $"{file.FileName} / {section.Name} ({definition.Key})";
            Assert.NotNull(definition);

            foreach (var input in definition.Inputs.Where(input => input.Required && !input.KeyFromLabel))
            {
                var present = input.Type == WorksheetFieldType.Instrument
                    ? section.Fields.Count(field => field.Type == WorksheetFieldType.Instrument)
                    : section.Fields.Count(field => Regex.IsMatch(field.FieldKey, $@"_{input.Key}(_\d+)*$"));
                Assert.True(present >= (input.Type == WorksheetFieldType.Instrument ? 1 : input.Replicates), $"{where}: input '{input.Key}' ×{present}");
            }

            var calculated = section.Fields.Where(field => field.Mode == WorksheetFieldMode.Calculated).ToList();
            if (definition.Calculations.Any(calculation => !calculation.Optional))
                Assert.True(calculated.Count > 0 || section.Fields.Any(field => field.ColumnDefinitions?.Contains("\"Calculated\"") == true), $"{where}: no calculation");
            Assert.All(calculated, field => Assert.True(QcFormulaEvaluator.Analyze(field.FormulaExpression).IsValid, $"{where}: {field.FieldKey} = '{field.FormulaExpression}'"));
            Assert.DoesNotContain(section.Fields, field => (field.FormulaExpression ?? field.ColumnDefinitions ?? string.Empty).Contains("{@"));

            // Something a Specification characteristic can bind to.
            Assert.True(RawMaterialResultField.Choose(section) is not null, $"{where}: no result field");
        }
    }

    [StudyCorpusFact]
    public void Formulas_are_left_for_review_only_on_tests_with_no_definition()
    {
        var left = 0;
        foreach (var file in StudyCorpus.Worksheets())
        {
            foreach (var section in file.Proposal.Template.Sections)
            {
                var empty = section.Fields.Count(field => field.Mode == WorksheetFieldMode.Calculated && string.IsNullOrEmpty(field.FormulaExpression));
                left += empty;
                if (section.TestDefinition is not null)
                    Assert.True(empty == 0, $"{file.FileName} / {section.Name}: a defined test with an empty formula");
            }

            var definedNames = file.Proposal.Template.Sections.Where(section => section.TestDefinition is not null).Select(section => section.Name).ToList();
            Assert.All(file.Proposal.Flags.Where(flag => flag.Code == WorksheetImportFlagCodes.FormulaNeedsReview),
                flag => Assert.DoesNotContain(definedNames, name => flag.Message.StartsWith($"{name} /")));
        }

        // 196 before brief 12.
        Assert.True(left <= 10, $"{left} formulas left for review");
    }

    [StudyCorpusFact]
    public void Nothing_printed_is_dropped()
    {
        foreach (var file in StudyCorpus.Worksheets())
            Assert.DoesNotContain(file.Proposal.Flags, flag => flag.Code == WorksheetImportFlagCodes.UnrecognizedContent
                                                               && (flag.Message.Contains("was not turned into") || flag.Message.Contains("No numbered test rows")));
    }

    [StudyCorpusFact]
    public void A_sheet_that_numbers_nothing_still_yields_its_tests()
    {
        var sheet = StudyCorpus.Worksheets().Single(file => file.FileName.StartsWith("244"));

        Assert.Equal(
            ["description", "solubility", "identity_reaction", "identity_reaction", "appearance_of_solution", "acidity_alkalinity", null,
             "sulfates", "heavy_metals", "loss_on_drying", "assay_titration"],
            sheet.Proposal.Template.Sections.Select(section => section.TestDefinition));
        Assert.Contains(sheet.Proposal.Template.Sections[^1].Fields, field => field.Type == WorksheetFieldType.Table && field.ColumnDefinitions.Contains("\"key\":\"assay\""));
    }

    [StudyCorpusFact]
    public async Task Every_worksheet_saves_as_a_draft_template()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = (await harness.SeedUser()).Id;

        foreach (var file in StudyCorpus.Worksheets())
        {
            var request = WorksheetImportTemplateMapper.ToCreateRequest(file.Proposal.Template);
            request.Code = $"RM-{Guid.NewGuid():N}";

            // A test with no definition still leaves its formula for the reviewer (locked decision 4).
            foreach (var field in request.Sections.SelectMany(section => section.Fields)
                         .Where(field => field.Mode == WorksheetFieldMode.Calculated && string.IsNullOrEmpty(field.FormulaExpression)))
                field.FormulaExpression = "100";

            var result = await harness.Templates.CreateTemplate(request, userId);
            Assert.True(result.IsSuccess, $"{file.FileName}: {result.Error?.Code} {result.Error?.Description}");
        }
    }

    [StudyCorpusFact]
    public void Every_formula_evaluates_on_sample_numbers()
    {
        foreach (var file in StudyCorpus.Worksheets())
        {
            var template = file.Proposal.Template;
            var left = template.Sections.SelectMany(section => section.Fields)
                .Where(field => field.Mode == WorksheetFieldMode.Calculated && string.IsNullOrEmpty(field.FormulaExpression)).ToList();
            foreach (var field in left)
                field.FormulaExpression = "100";

            var (values, failure) = ProposalCalculator.Evaluate(template);

            foreach (var field in left)
                field.FormulaExpression = null;
            Assert.True(failure is null, $"{file.FileName}: {failure}");
            Assert.All(values.Values, value => Assert.True(double.IsFinite(value)));
        }
    }

    [StudyCorpusFact]
    public void Coverage_report()
    {
        var worksheets = StudyCorpus.Worksheets().ToList();
        var sections = worksheets.SelectMany(file => file.Proposal.Template.Sections).ToList();
        var flags = worksheets.SelectMany(file => file.Proposal.Flags).GroupBy(flag => flag.Code).OrderByDescending(group => group.Count());

        output.WriteLine($"files {StudyCorpus.Load().Count}, worksheets {worksheets.Count}, sections {sections.Count}, fields {sections.Sum(section => section.Fields.Count)}");
        output.WriteLine($"defined {sections.Count(section => section.TestDefinition is not null)}, undefined {sections.Count(section => section.TestDefinition is null)}");
        output.WriteLine("flags: " + string.Join(", ", flags.Select(group => $"{group.Key}×{group.Count()}")));
        output.WriteLine("definitions: " + string.Join(", ", sections.Where(section => section.TestDefinition is not null)
            .GroupBy(section => section.TestDefinition).OrderByDescending(group => group.Count()).Select(group => $"{group.Key}×{group.Count()}")));
        output.WriteLine("variants: " + string.Join(", ", sections.SelectMany(section => (section.TestDefinitionVariants ?? []).Select(variant => $"{section.TestDefinition}: {variant}"))
            .GroupBy(variant => variant).OrderByDescending(group => group.Count()).Select(group => $"{group.Key}×{group.Count()}")));
        output.WriteLine("undefined: " + string.Join("; ", sections.Where(section => section.TestDefinition is null)
            .GroupBy(section => section.Name).OrderByDescending(group => group.Count()).Take(25).Select(group => $"{group.Key}×{group.Count()}")));
    }
}
