using APP.Repository;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.MaterialARD;
using DOMAIN.Entities.ProductAnalyticalRawData;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class ArdTemplateCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class AnalyticalRawDataTemplateValidationTests
{
    private static ApplicationDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new ArdTemplateCurrentUserService()
        );

    private static Form CreateTemplateWithoutQuestions() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Collaborative template",
            Sections =
            [
                new FormSection
                {
                    Id = Guid.NewGuid(),
                    Name = "Assay",
                    Fields = [],
                },
            ],
        };

    [Fact]
    public void Draft_validation_allows_a_test_without_questions()
    {
        var result = FormValidator.Validate(CreateTemplateWithoutQuestions());

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Ard_use_validation_rejects_a_test_without_questions()
    {
        var result = FormValidator.ValidateForUse(CreateTemplateWithoutQuestions());

        Assert.True(result.IsFailure);
        Assert.Equal("Form.Question", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public void Ard_use_validation_accepts_a_question_in_every_test()
    {
        var template = CreateTemplateWithoutQuestions();
        template.Sections[0].Fields.Add(
            new FormField
            {
                Id = Guid.NewGuid(),
                QuestionId = Guid.NewGuid(),
            }
        );

        var result = FormValidator.ValidateForUse(template);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Product_ard_creation_rejects_an_incomplete_template()
    {
        await using var context = CreateContext();
        var template = CreateTemplateWithoutQuestions();
        context.Forms.Add(template);
        await context.SaveChangesAsync();
        var repository = new ProductAnalyticalRawDataRepository(context, null!);

        var result = await repository.CreateAnalyticalRawData(
            new CreateProductAnalyticalRawDataRequest
            {
                FormId = template.Id,
                StpId = Guid.NewGuid(),
            }
        );

        Assert.True(result.IsFailure);
        Assert.Equal("Form.Question", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Material_ard_creation_rejects_an_incomplete_template()
    {
        await using var context = CreateContext();
        var template = CreateTemplateWithoutQuestions();
        context.Forms.Add(template);
        await context.SaveChangesAsync();
        var repository = new MaterialAnalyticalRawDataRepository(context, null!);

        var result = await repository.CreateAnalyticalRawData(
            new CreateMaterialAnalyticalRawDataRequest
            {
                FormId = template.Id,
                StpId = Guid.NewGuid(),
                MaterialId = Guid.NewGuid(),
            }
        );

        Assert.True(result.IsFailure);
        Assert.Equal("Form.Question", Assert.Single(result.Errors).Code);
    }
}
