using DOMAIN.Entities.QcWorksheets;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static APP.Tests.QcWorksheets.QcWorksheetRuns;

namespace APP.Tests.QcWorksheets;

/// <summary>
/// Build brief 07, Phase A: per-row calculated table columns (<c>"mode": "Calculated"</c>) are
/// evaluated and persisted at submit, one value per (row, column), following the scalar pattern.
/// </summary>
public class WorksheetCalculatedColumnTests
{
    private const string Av = """{"key":"newBatch_av","label":"Av.","type":"CalculatedValue","mode":"Calculated","formula":"({newBatch_plate1} + {newBatch_plate2}) / 2","group":"New Batch"}""";

    private const string Plates = """
        {"key":"newBatch_plate1","label":"Plate 1","type":"ColonyCount","group":"New Batch"},
        {"key":"newBatch_plate2","label":"Plate 2","type":"ColonyCount","group":"New Batch"}
        """;

    private static readonly string FixedGrid =
        $$"""[{"key":"organism","rowHeader":true,"fixedValues":["P. aeruginosa","E. coli"]},{{Plates}},{{Av}}]""";

    private static readonly string OpenGrid = $"[{Plates},{Av}]";

    private static WorksheetField Table(string definitions) => new()
    {
        FieldKey = "media", Label = "Growth promotion", Type = WorksheetFieldType.Table,
        Mode = WorksheetFieldMode.Entry, Order = 1, ColumnDefinitions = definitions
    };

    private static async Task<List<WorksheetFieldValue>> Averages(QcWorksheetTestContext harness) =>
        await harness.Db.QcWorksheetFieldValues.AsNoTracking()
            .Where(value => value.ColumnKey == "newBatch_av")
            .OrderBy(value => value.RowIndex)
            .ToListAsync();

    [Fact]
    public async Task Per_row_averages_are_persisted_for_every_fixed_row_and_feed_aggregates()
    {
        using var harness = new QcWorksheetTestContext();
        var mean = new WorksheetField
        {
            FieldKey = "mean_av", Label = "Mean of averages", Type = WorksheetFieldType.CalculatedValue,
            Mode = WorksheetFieldMode.Calculated, FormulaExpression = "AVG({media.newBatch_av})", Order = 2
        };
        var template = await harness.SeedTemplateWithFields(
            "WS/M/AV", WorksheetCategory.MediaQualification, Table(FixedGrid), mean);
        var (instanceId, analyst) = await StartedWorksheet(harness, template);
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        var saved = await harness.WorksheetInstances.SaveValues(instanceId, Values(
            Cell("media", "newBatch_plate1", 0, "40"), Cell("media", "newBatch_plate2", 0, "42"),
            Cell("media", "newBatch_plate1", 1, "55"), Cell("media", "newBatch_plate2", 1, "60")), analyst.Id);
        Assert.True(saved.IsSuccess, saved.Error?.Description);

        var submitted = await harness.WorksheetInstances.Submit(instanceId, analyst.Id);

        Assert.True(submitted.IsSuccess, submitted.Error?.Description);
        var averages = await Averages(harness);
        Assert.Equal(new[] { 0, 1 }, averages.Select(value => value.RowIndex!.Value));
        Assert.Equal(new[] { "41", "57.5" }, averages.Select(value => value.Value));
        Assert.All(averages, value => Assert.Equal("media", value.FieldKey));

        var meanValue = await harness.Db.QcWorksheetFieldValues.SingleAsync(value => value.FieldKey == "mean_av");
        Assert.Equal("49.25", meanValue.Value);
    }

    [Fact]
    public async Task An_unevaluatable_row_blocks_submit_naming_the_cell_and_persists_nothing()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedTemplateWithFields("WS/M/TNTC", WorksheetCategory.MediaQualification, Table(FixedGrid));
        var (instanceId, analyst) = await StartedWorksheet(harness, template);
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        await harness.WorksheetInstances.SaveValues(instanceId, Values(
            Cell("media", "newBatch_plate1", 0, "40"), Cell("media", "newBatch_plate2", 0, "42"),
            Cell("media", "newBatch_plate1", 1, "55"), Cell("media", "newBatch_plate2", 1, "TNTC")), analyst.Id);

        var refused = await harness.WorksheetInstances.Submit(instanceId, analyst.Id);

        Assert.False(refused.IsSuccess);
        Assert.Equal("QcWorksheetInstance.CalculatedFieldUnevaluatable", refused.Error.Code);
        Assert.StartsWith("Table 'media', column 'newBatch_av', row 2: ", refused.Error.Description);
        Assert.Contains("column 'newBatch_plate2' holds 'TNTC', which is not a number.", refused.Error.Description);

        Assert.Empty(await Averages(harness));
        var instance = await harness.Db.QcWorksheetInstances.AsNoTracking().SingleAsync(item => item.Id == instanceId);
        Assert.Equal(WorksheetInstanceStatus.InProgress, instance.Status);
        Assert.Null(instance.SubmittedAt);
        Assert.False(await harness.Db.QcApprovals.AnyAsync(item => item.EntityId == instanceId));
    }

    [Fact]
    public async Task A_fixed_row_left_blank_blocks_submit_naming_the_missing_column()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedTemplateWithFields("WS/M/GAP", WorksheetCategory.MediaQualification, Table(FixedGrid));
        var (instanceId, analyst) = await StartedWorksheet(harness, template);
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        await harness.WorksheetInstances.SaveValues(instanceId, Values(
            Cell("media", "newBatch_plate1", 0, "40"), Cell("media", "newBatch_plate2", 0, "42")), analyst.Id);

        var refused = await harness.WorksheetInstances.Submit(instanceId, analyst.Id);

        Assert.False(refused.IsSuccess);
        Assert.StartsWith("Table 'media', column 'newBatch_av', row 2: ", refused.Error.Description);
        Assert.Contains("column 'newBatch_plate1' has no value in this row.", refused.Error.Description);
    }

    [Fact]
    public async Task A_write_to_a_calculated_column_is_refused_and_nothing_is_persisted()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedTemplateWithFields("WS/M/W", WorksheetCategory.MediaQualification, Table(FixedGrid));
        var (instanceId, analyst) = await StartedWorksheet(harness, template);

        var refused = await harness.WorksheetInstances.SaveValues(instanceId, Values(
            Cell("media", "newBatch_plate1", 0, "40"), Cell("media", "newBatch_av", 0, "41")), analyst.Id);

        Assert.False(refused.IsSuccess);
        Assert.Equal("QcWorksheetInstance.CalculatedFieldNotEnterable", refused.Error.Code);
        Assert.StartsWith("Table 'media', column 'newBatch_av', row 1: ", refused.Error.Description);
        Assert.Empty(await harness.Db.QcWorksheetFieldValues.ToListAsync());
    }

    /// <summary>
    /// An open-ended table computes each row that has entries, and a re-submission recomputes
    /// from scratch: a changed row is recalculated and a cleared row leaves no stale result.
    /// </summary>
    [Fact]
    public async Task Open_ended_rows_are_computed_and_recomputed_from_scratch_on_resubmission()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedTemplateWithFields("WS/M/OPEN", WorksheetCategory.MediaQualification, Table(OpenGrid));
        var (instanceId, analyst) = await StartedWorksheet(harness, template);
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        await harness.WorksheetInstances.SaveValues(instanceId, Values(
            Cell("media", "newBatch_plate1", 0, "10"), Cell("media", "newBatch_plate2", 0, "20"),
            Cell("media", "newBatch_plate1", 3, "7"), Cell("media", "newBatch_plate2", 3, "9")), analyst.Id);
        Assert.True((await harness.WorksheetInstances.Submit(instanceId, analyst.Id)).IsSuccess);
        Assert.Equal(new[] { "15", "8" }, (await Averages(harness)).Select(value => value.Value));

        var returned = await harness.WorksheetInstances.ReturnForCorrection(
            instanceId, new ReturnWorksheetForCorrectionRequest { Reason = "Recount row 1" }, harness.Approver.Id);
        Assert.True(returned.IsSuccess, returned.Error?.Description);

        await harness.WorksheetInstances.SaveValues(instanceId, Values(
            Cell("media", "newBatch_plate1", 0, "30"),
            Cell("media", "newBatch_plate1", 3, ""), Cell("media", "newBatch_plate2", 3, "")), analyst.Id);
        var resubmitted = await harness.WorksheetInstances.Submit(instanceId, analyst.Id);

        Assert.True(resubmitted.IsSuccess, resubmitted.Error?.Description);
        var averages = await Averages(harness);
        Assert.Equal(0, Assert.Single(averages).RowIndex);
        Assert.Equal("25", averages[0].Value);
    }

    [Theory]
    [InlineData("""{"key":"av","mode":"Calculated","formula":"({newBatch_plate1} + {newBatch_plate3}) / 2"}""", "newBatch_plate3")]
    [InlineData("""{"key":"av","mode":"Calculated","formula":"({newBatch_plate1} + "}""", "media.av")]
    [InlineData("""{"key":"av","mode":"Calculated"}""", "a formula is required")]
    [InlineData("""{"key":"av","type":"CalculatedValue","formula":"{newBatch_plate1} * 2"}""", "must be \"mode\": \"Calculated\"")]
    [InlineData("""{"key":"av","mode":"Calculated","formula":"{av} + 1"}""", "its own column")]
    public async Task Template_save_refuses_an_invalid_column_formula(string column, string expected)
    {
        using var harness = new QcWorksheetTestContext();
        var userId = (await harness.SeedUser()).Id;
        var request = new CreateWorksheetTemplateRequest
        {
            Code = "WS-CALC-COL", Name = "Calculated column", Category = WorksheetCategory.MediaQualification,
            Sections =
            [
                new CreateWorksheetSectionRequest
                {
                    Order = 1, Name = "Results",
                    Fields =
                    [
                        new CreateWorksheetFieldRequest
                        {
                            Order = 1, FieldKey = "media", Label = "Media", Type = WorksheetFieldType.Table,
                            Mode = WorksheetFieldMode.Entry, ColumnDefinitions = $"[{Plates},{column}]"
                        }
                    ]
                }
            ]
        };

        var refused = await harness.Templates.CreateTemplate(request, userId);

        Assert.False(refused.IsSuccess);
        Assert.Equal("QcWorksheetTemplate.InvalidFormula", refused.Error.Code);
        Assert.Contains("media.av", refused.Error.Description);
        Assert.Contains(expected, refused.Error.Description);
        Assert.Empty(await harness.Db.QcWorksheetTemplates.ToListAsync());

        request.Sections[0].Fields[0].ColumnDefinitions = FixedGrid;
        var accepted = await harness.Templates.CreateTemplate(request, userId);
        Assert.True(accepted.IsSuccess, accepted.Error?.Description);
    }
}
