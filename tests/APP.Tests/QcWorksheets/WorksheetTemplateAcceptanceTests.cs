using DOMAIN.Entities.QcWorksheets;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.QcWorksheets;

/// <summary>
/// Acceptance criterion 4 — FieldKey uniqueness is worksheet-scoped — plus the field
/// revision history and formula validation this milestone also delivers.
/// </summary>
public class WorksheetTemplateAcceptanceTests
{
    private static CreateWorksheetFieldRequest Field(
        string key,
        string label,
        WorksheetFieldType type = WorksheetFieldType.Number,
        WorksheetFieldMode mode = WorksheetFieldMode.Entry,
        int order = 1) => new()
    {
        Order = order,
        FieldKey = key,
        Label = label,
        Type = type,
        Mode = mode
    };

    /// <summary>
    /// Criterion 4 — two fields sharing a FieldKey in <i>different</i> sections of the same
    /// template are rejected, because formulas resolve keys worksheet-scoped.
    /// </summary>
    [Fact]
    public async Task Duplicate_field_key_across_different_sections_is_rejected()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = (await harness.SeedUser()).Id;

        var result = await harness.Templates.CreateTemplate(new CreateWorksheetTemplateRequest
        {
            Code = "WS-010",
            Name = "Duplicate keys",
            Category = WorksheetCategory.Chemical,
            Sections =
            [
                new CreateWorksheetSectionRequest
                {
                    Order = 1, Name = "Section A",
                    Fields = [Field("absorbance", "Absorbance A")]
                },
                new CreateWorksheetSectionRequest
                {
                    Order = 2, Name = "Section B",
                    Fields = [Field("absorbance", "Absorbance B")]
                }
            ]
        }, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("QcWorksheetTemplate.DuplicateFieldKey", result.Error.Code);
        Assert.Empty(await harness.Db.QcWorksheetTemplates.ToListAsync());
    }

    /// <summary>The same key in one section is equally rejected.</summary>
    [Fact]
    public async Task Duplicate_field_key_within_one_section_is_rejected()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = (await harness.SeedUser()).Id;

        var result = await harness.Templates.CreateTemplate(new CreateWorksheetTemplateRequest
        {
            Code = "WS-011",
            Name = "Duplicate keys",
            Category = WorksheetCategory.Chemical,
            Sections =
            [
                new CreateWorksheetSectionRequest
                {
                    Order = 1, Name = "Section A",
                    Fields = [Field("ph", "pH", order: 1), Field("ph", "pH again", order: 2)]
                }
            ]
        }, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("QcWorksheetTemplate.DuplicateFieldKey", result.Error.Code);
    }

    /// <summary>Distinct keys across sections are accepted.</summary>
    [Fact]
    public async Task Distinct_field_keys_across_sections_are_accepted()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = (await harness.SeedUser()).Id;

        var result = await harness.Templates.CreateTemplate(new CreateWorksheetTemplateRequest
        {
            Code = "WS-012",
            Name = "Valid template",
            Category = WorksheetCategory.Microbial,
            Sections =
            [
                new CreateWorksheetSectionRequest
                {
                    Order = 1, Name = "Plates",
                    Fields = [Field("plate1", "Plate 1"), Field("plate2", "Plate 2", order: 2)]
                },
                new CreateWorksheetSectionRequest
                {
                    Order = 2, Name = "Result",
                    Fields = [Field("dilution_factor", "Dilution factor")]
                }
            ]
        }, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Sections.Count);
        Assert.Equal(3, result.Value.Sections.Sum(section => section.Fields.Count));
    }

    /// <summary>
    /// A CalculatedValue formula may reference any field in the template, across sections —
    /// the CFU case from the real microbiology worksheets.
    /// </summary>
    [Fact]
    public async Task Calculated_field_may_reference_keys_from_other_sections()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = (await harness.SeedUser()).Id;

        var result = await harness.Templates.CreateTemplate(new CreateWorksheetTemplateRequest
        {
            Code = "WS-013",
            Name = "TAMC",
            Category = WorksheetCategory.Microbial,
            Sections =
            [
                new CreateWorksheetSectionRequest
                {
                    Order = 1, Name = "Plates",
                    Fields = [Field("plate1", "Plate 1"), Field("plate2", "Plate 2", order: 2)]
                },
                new CreateWorksheetSectionRequest
                {
                    Order = 2, Name = "Calculation",
                    Fields =
                    [
                        new CreateWorksheetFieldRequest
                        {
                            Order = 1, FieldKey = "average_count", Label = "Average count",
                            Type = WorksheetFieldType.CalculatedValue,
                            Mode = WorksheetFieldMode.Calculated,
                            FormulaExpression = "({plate1} + {plate2}) / 2"
                        },
                        new CreateWorksheetFieldRequest
                        {
                            Order = 2, FieldKey = "dilution_factor", Label = "Dilution factor",
                            Type = WorksheetFieldType.Number, Mode = WorksheetFieldMode.Entry
                        },
                        new CreateWorksheetFieldRequest
                        {
                            Order = 3, FieldKey = "tamc_result", Label = "TAMC",
                            Type = WorksheetFieldType.CfuCalculation,
                            Mode = WorksheetFieldMode.Calculated,
                            FormulaExpression = "{average_count} * {dilution_factor}"
                        }
                    ]
                }
            ]
        }, userId);

        Assert.True(result.IsSuccess);
    }

    /// <summary>A formula referencing an unknown field key is rejected at save time.</summary>
    [Fact]
    public async Task Formula_referencing_unknown_field_is_rejected()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = (await harness.SeedUser()).Id;

        var result = await harness.Templates.CreateTemplate(new CreateWorksheetTemplateRequest
        {
            Code = "WS-014",
            Name = "Bad formula",
            Category = WorksheetCategory.Chemical,
            Sections =
            [
                new CreateWorksheetSectionRequest
                {
                    Order = 1, Name = "Calc",
                    Fields =
                    [
                        new CreateWorksheetFieldRequest
                        {
                            Order = 1, FieldKey = "result", Label = "Result",
                            Type = WorksheetFieldType.CalculatedValue,
                            Mode = WorksheetFieldMode.Calculated,
                            FormulaExpression = "{does_not_exist} * 2"
                        }
                    ]
                }
            ]
        }, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("QcWorksheetTemplate.InvalidFormula", result.Error.Code);
        Assert.Contains("does_not_exist", result.Error.Description);
    }

    /// <summary>An aggregate over a field that is not a Table is rejected.</summary>
    [Fact]
    public async Task Aggregate_over_non_table_field_is_rejected()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = (await harness.SeedUser()).Id;

        var result = await harness.Templates.CreateTemplate(new CreateWorksheetTemplateRequest
        {
            Code = "WS-015",
            Name = "Bad aggregate",
            Category = WorksheetCategory.Chemical,
            Sections =
            [
                new CreateWorksheetSectionRequest
                {
                    Order = 1, Name = "Calc",
                    Fields =
                    [
                        Field("scalar", "A scalar"),
                        new CreateWorksheetFieldRequest
                        {
                            Order = 2, FieldKey = "result", Label = "Result",
                            Type = WorksheetFieldType.CalculatedValue,
                            Mode = WorksheetFieldMode.Calculated,
                            FormulaExpression = "AVG({scalar.column})"
                        }
                    ]
                }
            ]
        }, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("QcWorksheetTemplate.InvalidFormula", result.Error.Code);
    }

    /// <summary>
    /// A field keeps its identity and gains a revision snapshot when its configuration
    /// changes during a Draft edit cycle — and does not when nothing changed.
    /// </summary>
    [Fact]
    public async Task Field_configuration_changes_are_snapshotted_as_revisions()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = (await harness.SeedUser()).Id;

        var created = await harness.Templates.CreateTemplate(new CreateWorksheetTemplateRequest
        {
            Code = "WS-016",
            Name = "Revisions",
            Category = WorksheetCategory.Chemical,
            Sections =
            [
                new CreateWorksheetSectionRequest
                {
                    Order = 1, Name = "Main",
                    Fields = [Field("ph", "pH")]
                }
            ]
        }, userId);

        Assert.True(created.IsSuccess);
        var fieldId = created.Value.Sections[0].Fields[0].Id;
        Assert.Single(created.Value.Sections[0].Fields[0].Revisions);

        // A real configuration change adds a snapshot.
        var edited = await harness.Templates.UpdateTemplate(created.Value.Id,
            new UpdateWorksheetTemplateRequest
            {
                Code = "WS-016",
                Name = "Revisions",
                Category = WorksheetCategory.Chemical,
                Sections =
                [
                    new CreateWorksheetSectionRequest
                    {
                        Order = 1, Name = "Main",
                        Fields =
                        [
                            new CreateWorksheetFieldRequest
                            {
                                Order = 1, FieldKey = "ph", Label = "pH at 25C",
                                Type = WorksheetFieldType.Number,
                                Mode = WorksheetFieldMode.Entry,
                                Unit = "pH"
                            }
                        ]
                    }
                ]
            }, userId);

        Assert.True(edited.IsSuccess);

        var field = edited.Value.Sections[0].Fields[0];
        Assert.Equal(fieldId, field.Id); // identity preserved across the edit
        Assert.Equal(2, field.Revisions.Count);
        Assert.Equal("pH", field.Revisions[0].Label);
        Assert.Equal("pH at 25C", field.Revisions[1].Label);

        // Saving again with no change adds nothing.
        var unchanged = await harness.Templates.UpdateTemplate(created.Value.Id,
            new UpdateWorksheetTemplateRequest
            {
                Code = "WS-016",
                Name = "Revisions",
                Category = WorksheetCategory.Chemical,
                Sections =
                [
                    new CreateWorksheetSectionRequest
                    {
                        Order = 1, Name = "Main",
                        Fields =
                        [
                            new CreateWorksheetFieldRequest
                            {
                                Order = 1, FieldKey = "ph", Label = "pH at 25C",
                                Type = WorksheetFieldType.Number,
                                Mode = WorksheetFieldMode.Entry,
                                Unit = "pH"
                            }
                        ]
                    }
                ]
            }, userId);

        Assert.True(unchanged.IsSuccess);
        Assert.Equal(2, unchanged.Value.Sections[0].Fields[0].Revisions.Count);
    }

    /// <summary>The edit-triggers-versioning rule applies to templates exactly as to STPs.</summary>
    [Fact]
    public async Task Effective_template_rejects_edits_and_versions_correctly()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetTemplate);
        var userId = harness.Approver.Id;

        var request = new CreateWorksheetTemplateRequest
        {
            Code = "WS-017",
            Name = "Lifecycle",
            Category = WorksheetCategory.Chemical,
            Sections =
            [
                new CreateWorksheetSectionRequest
                {
                    Order = 1, Name = "Main", Fields = [Field("ph", "pH")]
                }
            ]
        };

        var v1 = await harness.Templates.CreateTemplate(request, userId);
        Assert.True(v1.IsSuccess);

        await harness.Templates.SubmitForReview(v1.Value.Id, userId);
        await harness.Templates.Approve(v1.Value.Id, new QcApprovalRequest
        {
            Password = QcWorksheetTestContext.CorrectPassword, Comments = "Approved by"
        }, userId, []);
        var effective = await harness.Templates.MakeEffective(v1.Value.Id, userId);
        Assert.True(effective.IsSuccess);

        var blocked = await harness.Templates.UpdateTemplate(v1.Value.Id,
            new UpdateWorksheetTemplateRequest
            {
                Code = "WS-017", Name = "Changed", Category = WorksheetCategory.Chemical,
                Sections = [new CreateWorksheetSectionRequest
                {
                    Order = 1, Name = "Main", Fields = [Field("ph", "pH")]
                }]
            }, userId);

        Assert.False(blocked.IsSuccess);
        Assert.Equal("QcWorksheet.NotEditable", blocked.Error.Code);

        var v2 = await harness.Templates.CreateNewVersion(v1.Value.Id, userId);
        Assert.True(v2.IsSuccess);
        Assert.Equal(2, v2.Value.Version);
        Assert.Equal(v1.Value.Id, v2.Value.SupersedesId);
        Assert.Single(v2.Value.Sections);

        var stillEffective = await harness.Templates.GetTemplate(v1.Value.Id);
        Assert.Equal(QcDocumentStatus.Effective, stillEffective.Value.Status);
    }

    /// <summary>A template must have at least one section.</summary>
    [Fact]
    public async Task Template_without_sections_is_rejected()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = (await harness.SeedUser()).Id;

        var result = await harness.Templates.CreateTemplate(new CreateWorksheetTemplateRequest
        {
            Code = "WS-018", Name = "Empty", Category = WorksheetCategory.Chemical, Sections = []
        }, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("QcWorksheetTemplate.NoSections", result.Error.Code);
    }
}
