using APP.Services.QcWorksheets;
using APP.Services.QcWorksheets.WorksheetDocxImport;
using DOMAIN.Entities.QcWorksheets;
using Xunit;
using Xunit.Abstractions;

namespace APP.Tests.QcWorksheets.WorksheetImport;

/// <summary>The six product microbiology ARDs. Runs only with <c>QC_ARD_CORPUS_DIR</c> set.</summary>
public class ArdCorpusProductTests(ITestOutputHelper output)
{
    private static List<CorpusFile> Products() =>
        ArdCorpus.Load().Where(file => file.Expected == ArdFamily.ProductMicro).ToList();

    private static List<ProposedWorksheetField> Fields(CorpusFile file) =>
        file.Proposal.Template.Sections.SelectMany(section => section.Fields).ToList();

    [CorpusFact]
    public void Every_product_sheet_becomes_a_microbial_template()
    {
        var products = Products();

        Assert.Equal(6, products.Count);
        foreach (var file in products)
        {
            Assert.NotNull(file.Proposal.Template);
            Assert.Equal(WorksheetCategory.Microbial, file.Proposal.Template.Category);
            Assert.DoesNotContain(file.Proposal.Flags, flag => flag.Code == WorksheetImportFlagCodes.RecognizerPending);
        }
    }

    [CorpusFact]
    public void No_running_header_run_data_reaches_a_constant()
    {
        foreach (var file in Products())
        {
            var header = file.Document.HeaderText;
            var starts = RunDataLabels.LabelStartRegex().Matches(header).ToList();
            var runData = starts.Select((match, index) =>
                {
                    var end = index + 1 < starts.Count ? starts[index + 1].Index : header.Length;
                    return (Label: match.Value.TrimEnd(':', ' '), Value: ImportText.Normalize(header[(match.Index + match.Length)..end]));
                })
                .Where(item => RunDataLabels.TryMatch(item.Label, out _) && item.Value.Length >= 4)
                .Select(item => item.Value)
                .ToList();

            var fields = Fields(file);
            var texts = fields.Where(field => field.Mode == WorksheetFieldMode.Constant).Select(field => field.ConstantValue!)
                .Concat(fields.Select(field => field.Label))
                .Append(file.Proposal.Template.Name).Append(file.Proposal.Template.Code).ToList();

            Assert.NotEmpty(runData);
            foreach (var value in runData)
                Assert.DoesNotContain(texts, text => text.Contains(value, StringComparison.OrdinalIgnoreCase));
        }
    }

    [CorpusFact]
    public void Every_enumeration_is_plates_then_calculated_average_then_calculated_result()
    {
        foreach (var file in Products())
        {
            var fields = Fields(file).ToDictionary(field => field.FieldKey);
            var plateTables = file.Document.Tables.Count(table => ImportText.Canonical(table.Resolved(0, 0)).StartsWith("plate1"));
            var results = fields.Values.Where(field => field.Type == WorksheetFieldType.Result).ToList();

            Assert.Equal(plateTables, results.Count);
            foreach (var result in results)
            {
                Assert.Equal(WorksheetFieldMode.Calculated, result.Mode);
                Assert.Matches(@"^cfu/(g|mL)$", result.Unit);
                var analysis = QcFormulaEvaluator.Analyze(result.FormulaExpression);
                Assert.True(analysis.IsValid, result.FormulaExpression);

                var average = fields[Assert.Single(analysis.ScalarFieldKeys)];
                Assert.Equal((WorksheetFieldType.CalculatedValue, WorksheetFieldMode.Calculated), (average.Type, average.Mode));
                var plates = QcFormulaEvaluator.Analyze(average.FormulaExpression).ScalarFieldKeys.Select(key => fields[key]).ToList();
                Assert.Equal(2, plates.Count);
                Assert.All(plates, plate => Assert.Equal((WorksheetFieldType.ColonyCount, WorksheetFieldMode.Entry), (plate.Type, plate.Mode)));
            }
        }
    }

    [CorpusFact]
    public void Every_specified_organism_result_is_a_two_option_select()
    {
        foreach (var file in Products())
        {
            var organisms = Fields(file).Where(field => field.Label == "Result and Interpretation" && field.Type != WorksheetFieldType.Instructions).ToList();

            Assert.NotEmpty(organisms);
            foreach (var result in organisms)
            {
                Assert.Equal(WorksheetFieldType.Select, result.Type);
                Assert.Equal(2, result.Options!.Count);
                Assert.StartsWith("Presence of", result.Options[0]);
                Assert.StartsWith("Absence of", result.Options[1]);
            }
        }
    }

    [CorpusFact]
    public void Printed_specifications_are_proposals_bound_to_real_fields()
    {
        foreach (var file in Products())
        {
            var keys = Fields(file).Select(field => field.FieldKey).ToHashSet();
            var proposals = file.Proposal.SpecificationProposals;

            Assert.True(proposals.Count >= 2, file.RelativePath);
            Assert.All(proposals, proposal =>
            {
                Assert.Contains(proposal.SourceFieldKey, keys);
                Assert.Equal(SpecificationStage.Finished, proposal.Stage);
                Assert.False(string.IsNullOrWhiteSpace(proposal.ProductName));
            });

            // Printed specifications are proposals, never template content.
            Assert.DoesNotContain(Fields(file), field => (field.ConstantValue ?? string.Empty).StartsWith("NMT", StringComparison.OrdinalIgnoreCase));
        }
    }

    [CorpusFact]
    public void Media_references_resolve_once_the_media_sheets_are_imported()
    {
        var corpus = ArdCorpus.Load();

        // What importing the new-form media sheets first would leave in the catalog.
        var media = corpus.Where(file => file.Proposal.Family == ArdFamily.CultureMedia
                                         && file.Proposal.FormatVersion == nameof(CultureMediaFormat.New))
            .Select(file => new CatalogTemplate(Guid.NewGuid(), file.Proposal.Template.Code, file.Proposal.Template.Name))
            .ToList();
        var catalog = new InMemoryWorksheetImportCatalog([], [], media);

        foreach (var file in corpus.Where(file => file.Expected == ArdFamily.ProductMicro))
        {
            using var stream = File.OpenRead(Path.Combine(Environment.GetEnvironmentVariable(CorpusFactAttribute.Variable)!, file.RelativePath));
            var proposal = WorksheetDocxImportService.Propose(file.RelativePath, stream, catalog);
            var references = proposal.Template.Sections.SelectMany(section => section.Fields)
                .Where(field => field.Type == WorksheetFieldType.ReferencedResult).ToList();

            output.WriteLine($"{file.RelativePath}: {references.Count} media references, "
                             + $"{references.Count(field => field.ReferencedResultSourceTemplateId is not null)} resolved");
            Assert.NotEmpty(references);
            Assert.All(references, field => Assert.NotNull(field.ReferencedResultSourceTemplateId));
            Assert.DoesNotContain(proposal.Flags, flag => flag.Code == WorksheetImportFlagCodes.MediaTemplateMissing);
        }
    }

    [CorpusFact]
    public void Without_media_templates_every_reference_is_flagged_but_still_proposed()
    {
        foreach (var file in Products())
        {
            var references = Fields(file).Count(field => field.Type == WorksheetFieldType.ReferencedResult);
            Assert.Equal(references, file.Proposal.Flags.Count(flag => flag.Code == WorksheetImportFlagCodes.MediaTemplateMissing));
        }
    }
}
