using DOMAIN.Entities.QcWorksheets;
using DOMAIN.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.QcWorksheets;

/// <summary>
/// Build brief 07, A1 and A4: a choice value must be one of its options, checked at both
/// SaveValues and Submit, with nothing persisted on refusal.
/// </summary>
public class WorksheetOptionValueTests
{
    private const string Pathogen = """["Absent","Detected"]""";

    private static WorksheetField Field(
        string key, WorksheetFieldType type, string? optionsJson = null, int order = 1) => new()
    {
        FieldKey = key,
        Label = key,
        Type = type,
        Mode = WorksheetFieldMode.Entry,
        Order = order,
        OptionsJson = optionsJson
    };

    private static async Task<(Guid InstanceId, User Analyst)> StartedWorksheet(
        QcWorksheetTestContext harness, WorksheetTemplate template)
    {
        var analyst = await harness.SeedUser("analyst.a");
        var specification = await harness.SeedEffectiveSpecification(
            SpecificationAppliesTo.RoutineEnvironmental, (template, SpecificationAnalysisType.Microbial));

        var created = await harness.TestRequests.CreateTestRequest(
            new CreateTestRequestRequest
            {
                Type = TestRequestType.RoutineEnvironmental,
                SpecificationId = specification.Id,
                ScheduleOrigin = TestRequestScheduleOrigin.Scheduled,
                ArNumber = "ARD-OPTIONS",
                Subjects = [new CreateTestRequestSubjectRequest { SubjectRef = "PW-01", SubjectLabel = "Point 1" }]
            },
            Guid.NewGuid());

        await harness.TestRequests.RecordSample(created.Value.Id, new RecordTestRequestSampleRequest(), Guid.NewGuid());
        var instanceId = created.Value.Subjects.Single().WorksheetInstances.Single().Id;
        await harness.WorksheetInstances.Assign(
            instanceId, new AssignWorksheetInstanceRequest { AssignedToId = analyst.Id }, Guid.NewGuid());
        await harness.WorksheetInstances.Start(instanceId, analyst.Id);
        return (instanceId, analyst);
    }

    private static SaveWorksheetValuesRequest Values(params WorksheetFieldValueEntry[] entries) =>
        new() { FieldValues = entries.ToList() };

    [Fact]
    public async Task A_select_value_outside_its_options_is_refused_on_save_and_nothing_is_persisted()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedTemplateWithFields(
            "WS/W/1", WorksheetCategory.Microbial,
            Field("e_coli", WorksheetFieldType.Select, Pathogen),
            Field("count", WorksheetFieldType.ColonyCount, order: 2));
        var (instanceId, analyst) = await StartedWorksheet(harness, template);

        var refused = await harness.WorksheetInstances.SaveValues(instanceId, Values(
            new WorksheetFieldValueEntry { FieldKey = "count", Value = "3" },
            new WorksheetFieldValueEntry { FieldKey = "e_coli", Value = "Present" }), analyst.Id);

        Assert.False(refused.IsSuccess);
        Assert.Equal("QcWorksheetInstance.ValueNotAnOption", refused.Error.Code);
        Assert.Contains("e_coli", refused.Error.Description);
        Assert.Empty(await harness.Db.QcWorksheetFieldValues.ToListAsync());

        var accepted = await harness.WorksheetInstances.SaveValues(instanceId, Values(
            new WorksheetFieldValueEntry { FieldKey = "e_coli", Value = "Absent" }), analyst.Id);

        Assert.True(accepted.IsSuccess, accepted.Error?.Description);
        var field = accepted.Value.Sections.SelectMany(section => section.Fields).Single(item => item.FieldKey == "e_coli");
        Assert.Equal(Pathogen, field.OptionsJson);
    }

    [Fact]
    public async Task Each_multiselect_choice_must_be_an_option()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedTemplateWithFields(
            "WS/W/2", WorksheetCategory.Microbial,
            Field("organisms", WorksheetFieldType.MultiSelect, """["E. coli","Salmonella, spp.","S. aureus"]"""));
        var (instanceId, analyst) = await StartedWorksheet(harness, template);

        var refused = await harness.WorksheetInstances.SaveValues(instanceId, Values(
            new WorksheetFieldValueEntry { FieldKey = "organisms", Value = """["E. coli","Listeria"]""" }), analyst.Id);

        Assert.False(refused.IsSuccess);
        Assert.Equal("QcWorksheetInstance.ValueNotAnOption", refused.Error.Code);
        Assert.Contains("Listeria", refused.Error.Description);
        Assert.Empty(await harness.Db.QcWorksheetFieldValues.ToListAsync());

        // An option containing a comma survives, which is why the encoding is a JSON array.
        var accepted = await harness.WorksheetInstances.SaveValues(instanceId, Values(
            new WorksheetFieldValueEntry { FieldKey = "organisms", Value = """["E. coli","Salmonella, spp."]""" }),
            analyst.Id);

        Assert.True(accepted.IsSuccess, accepted.Error?.Description);
    }

    [Fact]
    public async Task A_table_cell_in_a_choice_column_must_be_one_of_the_column_options()
    {
        using var harness = new QcWorksheetTestContext();
        var table = Field("media", WorksheetFieldType.Table);
        table.ColumnDefinitions = """
            [{"key":"organism","rowHeader":true,"fixedValues":["P. aeruginosa","E. coli"]},
             {"key":"colour","type":"Select","group":"New Batch","options":["Yellow","Colourless"]},
             {"key":"plate1","type":"ColonyCount","group":"New Batch"}]
            """;
        var template = await harness.SeedTemplateWithFields("WS/M/1", WorksheetCategory.MediaQualification, table);
        var (instanceId, analyst) = await StartedWorksheet(harness, template);

        var refused = await harness.WorksheetInstances.SaveValues(instanceId, Values(
            new WorksheetFieldValueEntry { FieldKey = "media", ColumnKey = "plate1", RowIndex = 1, Value = "41" },
            new WorksheetFieldValueEntry { FieldKey = "media", ColumnKey = "colour", RowIndex = 1, Value = "Pink" }),
            analyst.Id);

        Assert.False(refused.IsSuccess);
        Assert.Equal("QcWorksheetInstance.ValueNotAnOption", refused.Error.Code);
        Assert.Contains("media", refused.Error.Description);
        Assert.Contains("colour", refused.Error.Description);
        Assert.Empty(await harness.Db.QcWorksheetFieldValues.ToListAsync());

        var accepted = await harness.WorksheetInstances.SaveValues(instanceId, Values(
            new WorksheetFieldValueEntry { FieldKey = "media", ColumnKey = "colour", RowIndex = 1, Value = "Yellow" }),
            analyst.Id);

        Assert.True(accepted.IsSuccess, accepted.Error?.Description);
    }

    /// <summary>
    /// Submit re-checks everything recorded. The stored value is written directly, standing in
    /// for any value that reached the table without passing SaveValues' check.
    /// </summary>
    [Fact]
    public async Task A_stored_value_outside_its_options_is_refused_on_submit_and_nothing_is_persisted()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedTemplateWithFields(
            "WS/W/3", WorksheetCategory.Microbial,
            Field("e_coli", WorksheetFieldType.Select, Pathogen),
            new WorksheetField
            {
                FieldKey = "double_count", Type = WorksheetFieldType.CalculatedValue,
                Mode = WorksheetFieldMode.Calculated, FormulaExpression = "{count} * 2", Order = 2
            },
            Field("count", WorksheetFieldType.ColonyCount, order: 3));
        var (instanceId, analyst) = await StartedWorksheet(harness, template);
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        await harness.WorksheetInstances.SaveValues(instanceId, Values(
            new WorksheetFieldValueEntry { FieldKey = "count", Value = "4" }), analyst.Id);
        harness.Db.QcWorksheetFieldValues.Add(new WorksheetFieldValue
        {
            Id = Guid.NewGuid(), WorksheetInstanceId = instanceId, FieldKey = "e_coli", Value = "Present",
            EnteredById = analyst.Id, EnteredAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
        });
        await harness.Db.SaveChangesAsync();

        var refused = await harness.WorksheetInstances.Submit(instanceId, analyst.Id);

        Assert.False(refused.IsSuccess);
        Assert.Equal("QcWorksheetInstance.ValueNotAnOption", refused.Error.Code);
        Assert.Contains("e_coli", refused.Error.Description);

        var instance = await harness.Db.QcWorksheetInstances.AsNoTracking().SingleAsync(item => item.Id == instanceId);
        Assert.Equal(WorksheetInstanceStatus.InProgress, instance.Status);
        Assert.Null(instance.SubmittedAt);
        Assert.False(await harness.Db.QcWorksheetFieldValues.AnyAsync(value => value.FieldKey == "double_count"));
        Assert.False(await harness.Db.QcApprovals.AnyAsync(item => item.EntityId == instanceId));

        var value = await harness.Db.QcWorksheetFieldValues.SingleAsync(item => item.FieldKey == "e_coli");
        value.Value = "Detected";
        await harness.Db.SaveChangesAsync();

        var submitted = await harness.WorksheetInstances.Submit(instanceId, analyst.Id);
        Assert.True(submitted.IsSuccess, submitted.Error?.Description);
    }

    /// <summary>
    /// A pinned template authored before options existed can never gain them, so its choice
    /// fields are not membership-checked and in-flight worksheets keep working.
    /// </summary>
    [Fact]
    public async Task A_legacy_choice_field_without_options_accepts_any_value()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedTemplateWithFields(
            "WS/W/4", WorksheetCategory.Microbial, Field("growth", WorksheetFieldType.GrowthObservation));
        var (instanceId, analyst) = await StartedWorksheet(harness, template);
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);

        var saved = await harness.WorksheetInstances.SaveValues(instanceId, Values(
            new WorksheetFieldValueEntry { FieldKey = "growth", Value = "TNTC" }), analyst.Id);
        Assert.True(saved.IsSuccess, saved.Error?.Description);

        var submitted = await harness.WorksheetInstances.Submit(instanceId, analyst.Id);
        Assert.True(submitted.IsSuccess, submitted.Error?.Description);
    }
}
