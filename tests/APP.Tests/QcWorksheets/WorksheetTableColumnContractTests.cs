using DOMAIN.Entities.QcWorksheets;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.QcWorksheets;

/// <summary>
/// Build brief 07, A1/A2 and A4: the Table column contract — per-column <c>options</c>, fixed
/// columns that agree on the row count, and a display-only <c>group</c> that round-trips.
/// </summary>
public class WorksheetTableColumnContractTests
{
    private static CreateWorksheetTemplateRequest TableRequest(string columnDefinitions) => new()
    {
        Code = "WS-TABLE",
        Name = "Growth promotion",
        Category = WorksheetCategory.MediaQualification,
        Sections =
        [
            new CreateWorksheetSectionRequest
            {
                Order = 1,
                Name = "Results",
                Fields =
                [
                    new CreateWorksheetFieldRequest
                    {
                        Order = 1,
                        FieldKey = "growth_promotion",
                        Label = "Growth promotion",
                        Type = WorksheetFieldType.Table,
                        Mode = WorksheetFieldMode.Entry,
                        ColumnDefinitions = columnDefinitions
                    }
                ]
            }
        ]
    };

    private static async Task<(bool Success, string? Code, string? Description, string? Stored)> Create(
        string columnDefinitions)
    {
        using var harness = new QcWorksheetTestContext();
        var userId = (await harness.SeedUser()).Id;
        var result = await harness.Templates.CreateTemplate(TableRequest(columnDefinitions), userId);
        var persisted = await harness.Db.QcWorksheetTemplates.CountAsync();

        if (!result.IsSuccess)
        {
            Assert.Equal(0, persisted);
            return (false, result.Error.Code, result.Error.Description, null);
        }

        return (true, null, null, result.Value.Sections[0].Fields[0].ColumnDefinitions);
    }

    /// <summary>
    /// The real media-qualification shape: two fixed columns (organism is the row header, strain
    /// its own fixed column), grouped plate columns, and a choice column.
    /// </summary>
    private const string MediaGrid = """
        [{"key":"organism","label":"Organism","type":"ShortText","rowHeader":true,"fixedValues":["P. aeruginosa","E. coli"]},
         {"key":"strain","label":"Strain","type":"ShortText","fixedValues":["ATCC 9027","ATCC 8739"]},
         {"key":"newBatch_plate1","label":"Plate 1","type":"ColonyCount","unit":"cfu","group":"New Batch"},
         {"key":"newBatch_plate2","label":"Plate 2","type":"ColonyCount","unit":"cfu","group":"New Batch"},
         {"key":"newBatch_colour","label":"Colour","type":"Select","group":"New Batch","options":["Yellow","Colourless"],"hint":"kept"}]
        """;

    [Fact]
    public async Task Multiple_fixed_columns_groups_and_options_round_trip_verbatim()
    {
        var result = await Create(MediaGrid);

        Assert.True(result.Success, result.Description);
        // Stored verbatim: group, options and keys the backend does not know all survive.
        Assert.Equal(MediaGrid, result.Stored);
    }

    [Fact]
    public async Task Fixed_columns_of_different_lengths_are_refused()
    {
        var result = await Create("""
            [{"key":"organism","rowHeader":true,"fixedValues":["A","B","C"]},
             {"key":"period","fixedValues":["18 hours","72 hours"]},
             {"key":"count","type":"ColonyCount"}]
            """);

        Assert.False(result.Success);
        Assert.Equal("QcWorksheetTemplate.FixedRowCountMismatch", result.Code);
        Assert.Contains("growth_promotion", result.Description);
        Assert.Contains("organism: 3", result.Description);
    }

    [Theory]
    [InlineData("""[{"key":"organism","fixedValues":[]},{"key":"count","type":"ColonyCount"}]""")]
    [InlineData("""[{"key":"organism","rowHeader":true},{"key":"count","type":"ColonyCount"}]""")]
    [InlineData("""[{"key":"organism","fixedValues":"A, B"},{"key":"count","type":"ColonyCount"}]""")]
    public async Task A_table_mixing_fixed_columns_with_open_ended_rows_is_refused(string definitions)
    {
        var result = await Create(definitions);

        Assert.False(result.Success);
        Assert.Equal("QcWorksheetTemplate.FixedRowCountMismatch", result.Code);
    }

    [Fact]
    public async Task An_open_ended_table_with_no_fixed_columns_is_accepted()
    {
        var result = await Create("""[{"key":"point","type":"ShortText"},{"key":"cfu","type":"ColonyCount"}]""");

        Assert.True(result.Success, result.Description);
    }

    [Theory]
    [InlineData("""[{"key":"colour","type":"Select"}]""")]
    [InlineData("""[{"key":"colour","type":"Select","options":["Yellow"]}]""")]
    [InlineData("""[{"key":"colour","type":6,"options":["Yellow","yellow"]}]""")]
    [InlineData("""[{"key":"colour","options":"Yellow/Colourless"}]""")]
    public async Task A_choice_column_needs_two_distinct_options(string definitions)
    {
        var result = await Create(definitions);

        Assert.False(result.Success);
        Assert.Equal("QcWorksheetTemplate.OptionsRequired", result.Code);
        Assert.Contains("growth_promotion.colour", result.Description);
    }

    [Theory]
    [InlineData("""[{"key":"cfu","type":"ColonyCount","options":["1","2"]}]""")]
    [InlineData("""[{"key":"organism","fixedValues":["A","B"],"options":["A","B"]}]""")]
    public async Task Options_on_a_non_choice_or_fixed_column_are_refused(string definitions)
    {
        var result = await Create(definitions);

        Assert.False(result.Success);
        Assert.Equal("QcWorksheetTemplate.OptionsNotAllowed", result.Code);
    }
}
