using DOMAIN.Entities.QcWorksheets;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.QcWorksheets;

/// <summary>
/// Build brief 07, A1 and A4: Select, MultiSelect and GrowthObservation fields carry an
/// <c>OptionsJson</c> choice list, validated at template save and carried through the detail,
/// the revision history and new versions.
/// </summary>
public class WorksheetFieldOptionsTests
{
    private static CreateWorksheetFieldRequest Field(
        string key, WorksheetFieldType type, string? optionsJson = null, int order = 1) => new()
    {
        Order = order,
        FieldKey = key,
        Label = key,
        Type = type,
        Mode = WorksheetFieldMode.Entry,
        OptionsJson = optionsJson
    };

    private static CreateWorksheetTemplateRequest Request(params CreateWorksheetFieldRequest[] fields) => new()
    {
        Code = "WS-OPTIONS",
        Name = "Options",
        Category = WorksheetCategory.Microbial,
        Sections = [new CreateWorksheetSectionRequest { Order = 1, Name = "Main", Fields = fields.ToList() }]
    };

    [Theory]
    [InlineData(WorksheetFieldType.Select, null)]
    [InlineData(WorksheetFieldType.Select, "[]")]
    [InlineData(WorksheetFieldType.MultiSelect, """["Absent"]""")]
    [InlineData(WorksheetFieldType.GrowthObservation, """["Growth","growth"," "]""")]
    [InlineData(WorksheetFieldType.Select, "not json")]
    [InlineData(WorksheetFieldType.Select, """[1,2]""")]
    public async Task Choice_field_without_two_distinct_options_is_refused_before_persistence(
        WorksheetFieldType type, string? optionsJson)
    {
        using var harness = new QcWorksheetTestContext();
        var userId = (await harness.SeedUser()).Id;

        var result = await harness.Templates.CreateTemplate(Request(Field("choice", type, optionsJson)), userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("QcWorksheetTemplate.OptionsRequired", result.Error.Code);
        Assert.Contains("choice", result.Error.Description);
        Assert.Empty(await harness.Db.QcWorksheetTemplates.ToListAsync());
    }

    [Theory]
    [InlineData(WorksheetFieldType.Number)]
    [InlineData(WorksheetFieldType.Result)]
    [InlineData(WorksheetFieldType.ShortText)]
    public async Task Non_choice_field_with_options_is_refused(WorksheetFieldType type)
    {
        using var harness = new QcWorksheetTestContext();
        var userId = (await harness.SeedUser()).Id;

        var result = await harness.Templates.CreateTemplate(
            Request(Field("reading", type, """["A","B"]""")), userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("QcWorksheetTemplate.OptionsNotAllowed", result.Error.Code);
        Assert.Contains("reading", result.Error.Description);
    }

    [Fact]
    public async Task Empty_options_on_a_non_choice_field_read_as_none()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = (await harness.SeedUser()).Id;

        var result = await harness.Templates.CreateTemplate(Request(Field("ph", WorksheetFieldType.Number, "[]")), userId);

        Assert.True(result.IsSuccess, result.Error?.Description);
        Assert.Null(result.Value.Sections[0].Fields[0].OptionsJson);
    }

    [Fact]
    public async Task Options_are_stored_trimmed_and_distinct_and_round_trip_through_detail()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = (await harness.SeedUser()).Id;

        var result = await harness.Templates.CreateTemplate(
            Request(Field("pathogen", WorksheetFieldType.Select, """[" Absent ","Detected","Absent",""]""")),
            userId);

        Assert.True(result.IsSuccess, result.Error?.Description);
        var field = result.Value.Sections[0].Fields[0];
        Assert.Equal("""["Absent","Detected"]""", field.OptionsJson);
        Assert.Equal(field.OptionsJson, Assert.Single(field.Revisions).OptionsJson);
    }

    [Fact]
    public async Task Changing_options_is_snapshotted_in_the_revision_history()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = (await harness.SeedUser()).Id;

        var created = await harness.Templates.CreateTemplate(
            Request(Field("verdict", WorksheetFieldType.Select, """["Complies","Does not comply"]""")), userId);
        Assert.True(created.IsSuccess, created.Error?.Description);

        var edit = Request(Field("verdict", WorksheetFieldType.Select, """["Complies","Does not comply","Not tested"]"""));
        var edited = await harness.Templates.UpdateTemplate(
            created.Value.Id, new UpdateWorksheetTemplateRequest
            {
                Code = edit.Code, Name = edit.Name, Category = edit.Category, Sections = edit.Sections
            }, userId);

        Assert.True(edited.IsSuccess, edited.Error?.Description);
        var revisions = edited.Value.Sections[0].Fields[0].Revisions;
        Assert.Equal(2, revisions.Count);
        Assert.Equal("""["Complies","Does not comply"]""", revisions[0].OptionsJson);
        Assert.Equal("""["Complies","Does not comply","Not tested"]""", revisions[1].OptionsJson);
    }

    [Fact]
    public async Task A_new_version_carries_the_options_forward()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetTemplate);
        var userId = harness.Approver.Id;

        var v1 = await harness.Templates.CreateTemplate(
            Request(Field("growth", WorksheetFieldType.GrowthObservation, """["Growth","No Growth"]""")), userId);
        Assert.True(v1.IsSuccess, v1.Error?.Description);

        await harness.Templates.SubmitForReview(v1.Value.Id, userId);
        await harness.Templates.Approve(v1.Value.Id, new QcApprovalRequest
        {
            Password = QcWorksheetTestContext.CorrectPassword, Comments = "Approved by"
        }, userId, []);
        Assert.True((await harness.Templates.MakeEffective(v1.Value.Id, userId)).IsSuccess);

        var v2 = await harness.Templates.CreateNewVersion(v1.Value.Id, userId);

        Assert.True(v2.IsSuccess, v2.Error?.Description);
        var field = v2.Value.Sections[0].Fields[0];
        Assert.Equal("""["Growth","No Growth"]""", field.OptionsJson);
        Assert.Equal(field.OptionsJson, Assert.Single(field.Revisions).OptionsJson);
    }

    /// <summary>
    /// The new column is nullable: a template stored before options existed — even one whose
    /// Select field has none — still loads, and its fields read as having no options.
    /// </summary>
    [Fact]
    public async Task Existing_templates_without_options_still_load()
    {
        using var harness = new QcWorksheetTestContext();
        var template = await harness.SeedTemplateWithFields(
            "WS/LEGACY", WorksheetCategory.Microbial,
            new WorksheetField { FieldKey = "legacy_choice", Type = WorksheetFieldType.Select, Mode = WorksheetFieldMode.Entry },
            new WorksheetField { FieldKey = "count", Type = WorksheetFieldType.ColonyCount, Mode = WorksheetFieldMode.Entry, Order = 2 });

        var loaded = await harness.Templates.GetTemplate(template.Id);

        Assert.True(loaded.IsSuccess, loaded.Error?.Description);
        Assert.All(loaded.Value.Sections[0].Fields, field => Assert.Null(field.OptionsJson));
    }
}
