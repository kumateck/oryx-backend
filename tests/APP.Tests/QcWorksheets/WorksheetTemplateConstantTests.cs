using DOMAIN.Entities.QcWorksheets;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.QcWorksheets;

public class WorksheetTemplateConstantTests
{
    [Fact]
    public async Task Constant_field_without_value_is_rejected_before_persistence()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = (await harness.SeedUser()).Id;
        var request = Request(null);

        var result = await harness.Templates.CreateTemplate(request, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("QcWorksheetTemplate.ConstantValueRequired", result.Error.Code);
        Assert.Empty(await harness.Db.QcWorksheetTemplates.ToListAsync());
    }

    [Fact]
    public async Task Constant_field_value_round_trips_through_template_detail()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = (await harness.SeedUser()).Id;
        var request = Request("Detection λ: 278 nm");

        var result = await harness.Templates.CreateTemplate(request, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal("Detection λ: 278 nm", result.Value.Sections[0].Fields[0].ConstantValue);
    }

    private static CreateWorksheetTemplateRequest Request(string constantValue) => new()
    {
        Code = "WS-CONSTANT",
        Name = "Chromatographic conditions",
        Category = WorksheetCategory.Chemical,
        Sections =
        [
            new CreateWorksheetSectionRequest
            {
                Order = 1,
                Name = "Assay",
                Fields =
                [
                    new CreateWorksheetFieldRequest
                    {
                        Order = 1,
                        FieldKey = "chromatographic_conditions",
                        Label = "Chromatographic conditions",
                        Type = WorksheetFieldType.Instructions,
                        Mode = WorksheetFieldMode.Constant,
                        ConstantValue = constantValue
                    }
                ]
            }
        ]
    };
}
