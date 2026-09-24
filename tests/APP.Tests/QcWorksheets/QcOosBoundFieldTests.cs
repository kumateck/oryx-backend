using DOMAIN.Entities.QcWorksheets;
using DOMAIN.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static APP.Tests.QcWorksheets.QcWorksheetRuns;

namespace APP.Tests.QcWorksheets;

/// <summary>
/// OOS detection judges whatever a Specification characteristic binds, whatever the field's
/// type: a Select specified-organism result, a water pathogen choice, a Calculated Result. An
/// unbound field is not judged.
/// </summary>
public class QcOosBoundFieldTests
{
    private const string Organism = """["Absence of E. coli","Presence of E. coli"]""";
    private const string Water = """["Absent","Detected"]""";

    private static WorksheetField Choice(string key, string options, WorksheetFieldType type = WorksheetFieldType.Select, int order = 1) => new()
    {
        FieldKey = key, Label = key, Type = type, Mode = WorksheetFieldMode.Entry, Order = order, OptionsJson = options
    };

    private sealed record Run(QcWorksheetTestContext Harness, Guid InstanceId, User Analyst, Guid TestRequestId);

    /// <summary>A started worksheet whose Specification binds <paramref name="bindings"/> (field key → criteria).</summary>
    private static async Task<Run> Arrange(
        QcWorksheetTestContext harness, WorksheetField[] fields, params (string FieldKey, string Criteria)[] bindings)
    {
        var template = await harness.SeedTemplateWithFields(
            $"WS/OOS/{Guid.NewGuid().ToString()[..6]}", WorksheetCategory.Microbial, fields);
        var (instanceId, analyst) = await StartedWorksheet(harness, template);
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        var specification = await harness.Db.QcSpecifications.SingleAsync();
        foreach (var (fieldKey, criteria) in bindings)
            await harness.SeedCharacteristic(specification, template, fieldKey, acceptanceCriteria: criteria);

        var testRequestId = await harness.Db.QcWorksheetInstances
            .Where(item => item.Id == instanceId)
            .Select(item => item.TestRequestSubject.TestRequestId)
            .SingleAsync();

        return new Run(harness, instanceId, analyst, testRequestId);
    }

    private static async Task<List<OosCase>> Submit(Run run, params WorksheetFieldValueEntry[] entries)
    {
        var saved = await run.Harness.WorksheetInstances.SaveValues(run.InstanceId, Values(entries), run.Analyst.Id);
        Assert.True(saved.IsSuccess, saved.Error?.Description);

        var submitted = await run.Harness.WorksheetInstances.Submit(run.InstanceId, run.Analyst.Id);
        Assert.True(submitted.IsSuccess, submitted.Error?.Description);

        return await run.Harness.Db.QcOosCases.AsNoTracking()
            .Where(item => item.WorksheetInstanceId == run.InstanceId)
            .ToListAsync();
    }

    private static WorksheetFieldValueEntry Entry(string key, string value) => new() { FieldKey = key, Value = value };

    [Fact]
    public async Task A_select_presence_result_against_absence_criteria_opens_a_case_and_blocks_release()
    {
        using var harness = new QcWorksheetTestContext();
        var run = await Arrange(harness, [Choice("e_coli", Organism)], ("e_coli", "Absence of E. coli"));

        var cases = await Submit(run, Entry("e_coli", "Presence of E. coli"));

        var opened = Assert.Single(cases);
        Assert.Equal("e_coli", opened.FieldKey);
        Assert.Equal(OosCaseStatus.Open, opened.Status);
        Assert.Equal("Presence of E. coli", opened.ObservedValue);
        Assert.Equal("Absence of E. coli", opened.BreachedLimit);

        var blocked = await harness.OosCases.IsReleaseBlocked(run.TestRequestId);
        Assert.True(blocked.Value);
    }

    [Theory]
    [InlineData("Absence of E.coli")]
    [InlineData("absence of E.coli")]
    public async Task A_select_absence_result_differing_only_in_case_and_spacing_passes(string value)
    {
        using var harness = new QcWorksheetTestContext();
        var options = $$"""["{{value}}","Presence of E.coli"]""";
        var run = await Arrange(harness, [Choice("e_coli", options)], ("e_coli", "Absence of E. coli"));

        var cases = await Submit(run, Entry("e_coli", value));

        Assert.Empty(cases);
        Assert.False((await harness.OosCases.IsReleaseBlocked(run.TestRequestId)).Value);
    }

    [Fact]
    public async Task A_water_detected_choice_against_absent_opens_a_case()
    {
        using var harness = new QcWorksheetTestContext();
        var run = await Arrange(harness, [Choice("salmonella", Water)], ("salmonella", "Absent"));

        var cases = await Submit(run, Entry("salmonella", "Detected"));

        Assert.Equal("Detected", Assert.Single(cases).ObservedValue);
    }

    /// <summary>The engine's case: plates 30 and 60, averaged, times a dilution of 10 = 450 cfu/g.</summary>
    [Fact]
    public async Task A_calculated_result_over_its_limit_opens_a_case()
    {
        using var harness = new QcWorksheetTestContext();
        WorksheetField[] fields =
        [
            new() { FieldKey = "plate1", Type = WorksheetFieldType.ColonyCount, Mode = WorksheetFieldMode.Entry, Order = 1 },
            new() { FieldKey = "plate2", Type = WorksheetFieldType.ColonyCount, Mode = WorksheetFieldMode.Entry, Order = 2 },
            new() { FieldKey = "dilution", Type = WorksheetFieldType.Dilution, Mode = WorksheetFieldMode.Entry, Order = 3 },
            new()
            {
                FieldKey = "tamc", Type = WorksheetFieldType.Result, Mode = WorksheetFieldMode.Calculated, Order = 4,
                FormulaExpression = "({plate1} + {plate2}) / 2 * {dilution}"
            }
        ];
        var run = await Arrange(harness, fields, ("tamc", "NMT 200 cfu/g"));

        var cases = await Submit(run, Entry("plate1", "30"), Entry("plate2", "60"), Entry("dilution", "10"));

        var opened = Assert.Single(cases);
        Assert.Equal("tamc", opened.FieldKey);
        Assert.Equal("450", opened.ObservedValue);
        Assert.Equal("NMT 200 cfu/g", opened.BreachedLimit);
    }

    [Fact]
    public async Task An_unbound_select_is_not_judged()
    {
        using var harness = new QcWorksheetTestContext();
        var run = await Arrange(
            harness,
            [Choice("e_coli", Organism), Choice("salmonella", Water, order: 2)],
            ("e_coli", "Absence of E. coli"));

        var cases = await Submit(run, Entry("e_coli", "Absence of E. coli"), Entry("salmonella", "Detected"));

        Assert.Empty(cases);
    }

    /// <summary>A MultiSelect complies only when every chosen value does; the worst choice stands.</summary>
    [Theory]
    [InlineData("""["Absent"]""", false)]
    [InlineData("""["Absent","Detected"]""", true)]
    public async Task A_multiselect_is_judged_choice_by_choice(string value, bool expectCase)
    {
        using var harness = new QcWorksheetTestContext();
        var run = await Arrange(
            harness, [Choice("pathogens", Water, WorksheetFieldType.MultiSelect)], ("pathogens", "Absent"));

        var cases = await Submit(run, Entry("pathogens", value));

        Assert.Equal(expectCase, cases.Count == 1);
    }

    /// <summary>
    /// Read as one raw string, <c>["5","50"]</c> would be judged by its first number (5) and pass;
    /// judged choice by choice, the 50 breaches.
    /// </summary>
    [Fact]
    public async Task A_multiselect_breach_in_any_choice_is_not_hidden_by_an_earlier_compliant_one()
    {
        using var harness = new QcWorksheetTestContext();
        var run = await Arrange(
            harness, [Choice("counts", """["5","50"]""", WorksheetFieldType.MultiSelect)], ("counts", "NMT 10"));

        var cases = await Submit(run, Entry("counts", """["5","50"]"""));

        Assert.Contains("50", Assert.Single(cases).ObservedValue);
    }

    [Theory]
    [InlineData("({plate1} + ", "is invalid")]
    [InlineData(null, "a formula is required")]
    [InlineData("{unknown} * 10", "unknown")]
    public async Task A_calculated_result_with_a_bad_formula_is_refused_at_template_save(string? formula, string expected)
    {
        using var harness = new QcWorksheetTestContext();
        var userId = (await harness.SeedUser()).Id;

        var result = await harness.Templates.CreateTemplate(new CreateWorksheetTemplateRequest
        {
            Code = "WS-TAMC", Name = "TAMC", Category = WorksheetCategory.Microbial,
            Sections =
            [
                new CreateWorksheetSectionRequest
                {
                    Order = 1, Name = "Count",
                    Fields =
                    [
                        new CreateWorksheetFieldRequest
                        {
                            Order = 1, FieldKey = "plate1", Label = "Plate 1",
                            Type = WorksheetFieldType.ColonyCount, Mode = WorksheetFieldMode.Entry
                        },
                        new CreateWorksheetFieldRequest
                        {
                            Order = 2, FieldKey = "tamc", Label = "TAMC", Type = WorksheetFieldType.Result,
                            Mode = WorksheetFieldMode.Calculated, FormulaExpression = formula
                        }
                    ]
                }
            ]
        }, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("QcWorksheetTemplate.InvalidFormula", result.Error.Code);
        Assert.Contains("tamc", result.Error.Description);
        Assert.Contains(expected, result.Error.Description);
        Assert.Empty(await harness.Db.QcWorksheetTemplates.ToListAsync());
    }
}
