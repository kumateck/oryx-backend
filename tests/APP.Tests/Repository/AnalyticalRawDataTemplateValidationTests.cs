using AutoMapper;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.ProductStandardTestProcedures;
using Microsoft.Extensions.DependencyInjection;
using APP.Repository;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.MaterialARD;
using DOMAIN.Entities.ProductAnalyticalRawData;
using DOMAIN.Entities.QualityRoutines;
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
        var result = FormValidator.ValidateForUse(CreateTemplateWithoutQuestions(), AnalysisType.Chemical);

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
                Question = new Question { Id = Guid.NewGuid(), Label = "Weight per mL", Type = QuestionType.Formula },
            }
        );

        var result = FormValidator.ValidateForUse(template, AnalysisType.Chemical);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Ard_only_requires_questions_in_shared_and_selected_analysis_sections()
    {
        var template = CreateTemplateWithoutQuestions();
        template.Sections[0].Name = "<p><span>Weight per mL</span></p>";
        template.Sections[0].AnalysisType = AnalysisType.Microbial;
        template.Sections.Add(new FormSection
        {
            Order = 1, AnalysisType = AnalysisType.Chemical, Name = "Weight per mL",
            Fields = [new FormField { Question = new Question { Label = "Weight per mL", Type = QuestionType.Formula } }]
        });

        Assert.True(FormValidator.ValidateForUse(template, AnalysisType.Chemical).IsSuccess);
        var error = Assert.Single(FormValidator.ValidateForUse(template, AnalysisType.Microbial).Errors);
        Assert.Equal("Form.Question", error.Code);
        Assert.Contains("Section 1", error.Description);
        Assert.Contains("Weight per mL", error.Description);
        Assert.DoesNotContain("<span>", error.Description);
    }

    [Fact]
    public void A_removed_question_does_not_make_a_shared_test_ready()
    {
        var template = CreateTemplateWithoutQuestions();
        template.Sections[0].Fields.Add(new FormField
        {
            Question = new Question { Label = "Retired", DeletedAt = DateTime.UtcNow }
        });
        Assert.Equal("Form.Question", Assert.Single(FormValidator.ValidateForUse(template, AnalysisType.Chemical).Errors).Code);
    }

    [Fact]
    public async Task Bulk_product_ard_is_created_from_the_saved_formula_in_the_selected_analysis()
    {
        await using var context = CreateContext();
        var template = CreateTemplateWithoutQuestions();
        template.Sections[0].AnalysisType = AnalysisType.Microbial;
        template.Sections.Add(new FormSection
        {
            Name = "Weight per mL", AnalysisType = AnalysisType.Chemical, Description = "Configured specification",
            Fields = [new FormField { Question = new Question { Label = "Weight per mL", Type = QuestionType.Formula } }]
        });
        var stp = new ProductStandardTestProcedure
        {
            Product = new Product { Name = "Synthetic product", Code = "ARD-FIXTURE" },
            StpNumber = "STP-ARD-FIXTURE"
        };
        context.AddRange(template, stp);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(_ => { }, typeof(APP.Mapper.OryxMapper));
        using var provider = services.BuildServiceProvider();
        var repository = new ProductAnalyticalRawDataRepository(context, provider.GetRequiredService<IMapper>());
        var result = await repository.CreateAnalyticalRawData(new CreateProductAnalyticalRawDataRequest
        {
            FormId = template.Id, StpId = stp.Id, Stage = TestStage.Bulk,
            AnalysisType = AnalysisType.Chemical, SpecNumber = "SPEC-ARD-FIXTURE"
        });

        Assert.True(result.IsSuccess, result.Error.Description);
        var saved = await context.ProductAnalyticalRawData.Include(row => row.CoaItems).SingleAsync();
        Assert.Equal(template.Id, saved.FormId);
        Assert.Equal("Weight per mL", Assert.Single(saved.CoaItems).DisplayLabel);
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
