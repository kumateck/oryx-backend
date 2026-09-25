using System.Text.RegularExpressions;
using APP.Services.QcWorksheets.WorksheetDocxImport;
using DOMAIN.Entities.QcWorksheets;
using Xunit;

namespace APP.Tests.QcWorksheets.WorksheetImport;

/// <summary>The two purified-water sheets. Runs only with <c>QC_ARD_CORPUS_DIR</c> set.</summary>
public class ArdCorpusWaterTests
{
    private static (CorpusFile Few, CorpusFile All) Water()
    {
        var water = ArdCorpus.Load().Where(file => file.Expected == ArdFamily.PurifiedWater).ToList();
        Assert.Equal(2, water.Count);
        return (water.Single(file => file.RelativePath.Contains("11")), water.Single(file => file.RelativePath.Contains("All")));
    }

    private static List<ProposedWorksheetField> Fields(CorpusFile file) =>
        file.Proposal.Template.Sections.SelectMany(section => section.Fields).ToList();

    /// <summary>What must match between the two sheets: everything but equipment identity and typography.</summary>
    private static List<string> Comparable(CorpusFile file) =>
        file.Proposal.Template.Sections.SelectMany(section => section.Fields
                .Where(field => field.Type != WorksheetFieldType.Instrument)
                .Select(field => string.Join(" | ", section.Name, field.FieldKey, field.Type, field.Mode, field.Unit,
                    field.Type == WorksheetFieldType.Reagent ? null : field.Label,
                    Regex.Replace(field.ConstantValue ?? string.Empty, @"\s+", string.Empty).ToLowerInvariant(),
                    field.FormulaExpression, field.ColumnDefinitions, string.Join("/", field.Options ?? []))))
            .ToList();

    [CorpusFact]
    public void Both_sheets_propose_the_same_one_point_template()
    {
        var (few, all) = Water();

        Assert.Equal((few.Proposal.Template.Code, few.Proposal.Template.Name), (all.Proposal.Template.Code, all.Proposal.Template.Name));
        Assert.Equal(Comparable(few), Comparable(all));

        // The one real difference: the sheets list different autoclaves (Instrument fields are
        // picked per run, so the reviewer keeps either or both).
        var fewInstruments = Fields(few).Where(field => field.Type == WorksheetFieldType.Instrument).Select(field => field.FieldKey).ToHashSet();
        var allInstruments = Fields(all).Where(field => field.Type == WorksheetFieldType.Instrument).Select(field => field.FieldKey).ToHashSet();
        Assert.Equal(["instrument_horizontal_steam_sterilizer"], fewInstruments.Except(allInstruments));
        Assert.Equal(["instrument_laboratory_vertical_autoclave"], allInstruments.Except(fewInstruments));
    }

    [CorpusFact]
    public void The_template_holds_one_subjects_results_and_no_point_rows()
    {
        foreach (var file in new[] { Water().Few, Water().All })
        {
            var fields = Fields(file).ToDictionary(field => field.FieldKey);

            Assert.DoesNotContain(fields.Values, field => field.Type == WorksheetFieldType.Table);
            Assert.Equal("{cfu_per_100ml} / 100", fields["cfu_per_ml"].FormulaExpression);
            Assert.Equal((WorksheetFieldType.Result, WorksheetFieldMode.Calculated), (fields["cfu_per_ml"].Type, fields["cfu_per_ml"].Mode));
            foreach (var pathogen in new[] { "escherichia_coli", "salmonella_spp", "pseudomonas_aeruginosa", "staphylococcus_aureus" })
            {
                Assert.Equal(WorksheetFieldType.Select, fields[$"{pathogen}_result"].Type);
                Assert.Equal(["Absent", "Detected"], fields[$"{pathogen}_result"].Options);
            }

            var texts = fields.Values.SelectMany(field => new[] { field.Label, field.ConstantValue ?? string.Empty }).ToList();
            foreach (var point in file.Proposal.SamplingPointProposals)
            {
                Assert.DoesNotContain(texts, text => Regex.IsMatch(text, $@"(?<![\w/]){Regex.Escape(point.Code)}(?!\w)"));
                Assert.DoesNotContain(texts, text => text.Contains(point.Name, StringComparison.OrdinalIgnoreCase) && point.Name.Length > 12);
            }
        }
    }

    [CorpusFact]
    public void The_point_lists_become_sampling_point_proposals_grouped_by_limit()
    {
        var (few, all) = Water();

        // "Water 11 Sampling points" lists ten points.
        Assert.Equal(10, few.Proposal.SamplingPointProposals.Count);
        Assert.Equal(29, all.Proposal.SamplingPointProposals.Count);
        Assert.All(few.Proposal.SamplingPointProposals.Concat(all.Proposal.SamplingPointProposals), point =>
        {
            Assert.Equal((PurifiedWaterRecognizer.Area, SamplingPointType.Water), (point.Area, point.Type));
            Assert.DoesNotContain("Sampling Point", point.Code);
        });

        Assert.Equal(["NMT 500cfu/mL", "NMT 80 cfu/mL"], few.Proposal.SamplingPointGroupProposals.Select(group => group.AcceptanceCriteria));
        Assert.Equal(["NMT 500cfu/mL", "NMT 200 cfu/mL", "NMT 80 cfu/mL"], all.Proposal.SamplingPointGroupProposals.Select(group => group.AcceptanceCriteria));

        string[] Ungrouped(CorpusFile file) => file.Proposal.SamplingPointProposals.Where(point => point.GroupName is null).Select(point => point.Code).ToArray();
        Assert.Equal(["NSP 5"], Ungrouped(few));
        Assert.Equal(["BSP 1", "BSP 2"], Ungrouped(all));
    }

    [CorpusFact]
    public void Printed_specifications_bind_to_the_result_fields()
    {
        foreach (var file in new[] { Water().Few, Water().All })
        {
            var keys = Fields(file).Select(field => field.FieldKey).ToHashSet();
            var proposals = file.Proposal.SpecificationProposals;

            Assert.All(proposals, proposal => Assert.Contains(proposal.SourceFieldKey, keys));
            var counts = proposals.Where(proposal => proposal.SourceFieldKey == "cfu_per_ml").ToList();
            Assert.Equal(file.Proposal.SamplingPointGroupProposals.Select(group => group.Name), counts.Select(proposal => proposal.GroupName));

            var pathogens = proposals.Where(proposal => proposal.SourceFieldKey.EndsWith("_result")).ToList();
            Assert.Equal(4, pathogens.Count);
            Assert.All(pathogens, proposal => Assert.Equal("Absent", proposal.AcceptanceCriteria));
        }
    }

    [CorpusFact]
    public void Media_cited_by_serial_number_are_all_referenced()
    {
        foreach (var file in new[] { Water().Few, Water().All })
        {
            var references = Fields(file).Where(field => field.Type == WorksheetFieldType.ReferencedResult).ToList();
            Assert.Equal(7, references.Count);
            Assert.DoesNotContain(Fields(file), field => (field.ConstantValue ?? string.Empty).Contains("QCD/MIC/BD"));
            Assert.Contains(references, field => field.FieldKey.Contains("rappaport"));
        }
    }
}
