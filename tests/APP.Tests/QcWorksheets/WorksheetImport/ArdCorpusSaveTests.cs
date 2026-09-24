using System.Text.Json;
using APP.Services.QcWorksheets.WorksheetDocxImport;
using DOMAIN.Entities.QcWorksheets;
using Xunit;

namespace APP.Tests.QcWorksheets.WorksheetImport;

/// <summary>
/// "Ready to POST unchanged": every media proposal from the real corpus is pushed through the
/// real <c>CreateTemplate</c> validation (duplicate keys, constants, formulas). Runs only with
/// <c>QC_ARD_CORPUS_DIR</c> set.
/// </summary>
public class ArdCorpusSaveTests
{
    [CorpusFact]
    public async Task Every_media_proposal_saves_as_a_draft_template()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = (await harness.SeedUser()).Id;

        foreach (var file in ArdCorpus.Load().Where(file => file.Proposal.Template is not null))
        {
            var request = WorksheetImportTemplateMapper.ToCreateRequest(file.Proposal.Template);

            // Codes repeat across the old and new form of one medium; the test is about structure.
            request.Code = $"{request.Code}#{Guid.NewGuid():N}"[..Math.Min(100, request.Code.Length + 33)];

            var result = await harness.Templates.CreateTemplate(request, userId);
            Assert.True(result.IsSuccess, $"{file.RelativePath}: {result.Error?.Code} {result.Error?.Description}");
        }
    }

    [CorpusFact]
    public void Cultural_response_organisms_match_the_test_strain_list()
    {
        foreach (var file in ArdCorpus.Load().Where(file => file.Proposal.Template is not null))
        {
            var fields = file.Proposal.Template.Sections.SelectMany(section => section.Fields).ToList();
            var strains = OrganismValues(fields.SingleOrDefault(field => field.FieldKey == "test_strains"));
            var response = OrganismValues(fields.SingleOrDefault(field => field.FieldKey == "cultural_response"));

            // Compared without punctuation: the sheets themselves disagree on "subs" vs "subs."
            // between their two tables. What matters is the same organisms, in the same rows.
            Assert.NotEmpty(response);
            if (strains.Count > 0)
                Assert.True(strains.Select(ImportText.Canonical).SequenceEqual(response.Select(ImportText.Canonical)),
                    $"{file.RelativePath}: [{string.Join(", ", strains)}] vs [{string.Join(", ", response)}]");
        }
    }

    private static List<string> OrganismValues(ProposedWorksheetField? table)
    {
        if (table?.ColumnDefinitions is null)
            return [];

        var organism = JsonDocument.Parse(table.ColumnDefinitions).RootElement.EnumerateArray()
            .FirstOrDefault(column => column.GetProperty("type").GetString() == nameof(WorksheetFieldType.Organism));

        return organism.ValueKind == JsonValueKind.Object && organism.TryGetProperty("fixedValues", out var values)
            ? values.EnumerateArray().Select(value => value.GetString() ?? string.Empty).ToList()
            : [];
    }
}
