using System.Reflection;
using API.Controllers;
using API.Authorization;
using APP.Repository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Forms;
using SHARED;
using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public sealed class FormulaQuestionPreviewTests
{
    private const string Legacy = "{\"type\":3,\"expression\":\":old\"}";
    private const string Approved = "{\"type\":3,\"expression\":\":corrected\",\"percentageResults\":true}";

    internal static FormRepository Repository(ApplicationDbContext context) =>
        new(context, new MapperConfiguration(configuration =>
        {
            configuration.CreateMap<Question, QuestionDto>();
            configuration.CreateMap<Question, CollectionItemDto>();
            configuration.CreateMap<QuestionOption, QuestionOptionDto>();
        }).CreateMapper(), null!, null!, null!);

    [Theory]
    [InlineData(FormulaRevisionStatus.Draft)]
    [InlineData(FormulaRevisionStatus.InReview)]
    [InlineData(FormulaRevisionStatus.Retired)]
    public async Task List_uses_approved_payload_even_with_a_newer_nonapproved_revision(FormulaRevisionStatus laterStatus)
    {
        await using var context = Context();
        var question = Seed(context);
        var definition = Link(context, question);
        await Revision(context, definition, 1, FormulaRevisionStatus.Retired, Legacy);
        await Revision(context, definition, 2, FormulaRevisionStatus.Approved, Approved);
        await Revision(context, definition, 3, laterStatus, "{\"type\":3,\"expression\":\":unreleased\"}");
        await context.SaveChangesAsync();

        var result = await Repository(context).GetQuestions(new QuestionFilter { PageSize = 1 });
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalRecordCount);
        var row = Assert.Single(result.Value.Data);
        Assert.Equal(question.Id, row.Id);
        Assert.Equal(Approved, Assert.Single(row.Options).Name);
        Assert.Equal(Legacy, (await context.QuestionOptions.SingleAsync()).Name);
        Assert.False(context.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task List_keeps_legacy_formula_until_an_approved_revision_exists()
    {
        await using var context = Context();
        var question = Seed(context);
        var definition = Link(context, question);
        await Revision(context, definition, 1, FormulaRevisionStatus.InReview, Approved);
        await context.SaveChangesAsync();
        var result = await Repository(context).GetQuestions(new QuestionFilter());
        Assert.Equal(Legacy, Assert.Single(Assert.Single(result.Value.Data).Options).Name);
    }

    [Fact]
    public async Task List_can_preview_approved_formula_with_no_legacy_option()
    {
        await using var context = Context();
        var question = Seed(context, withOption: false);
        await Revision(context, Link(context, question), 1, FormulaRevisionStatus.Approved, Approved);
        await context.SaveChangesAsync();
        var result = await Repository(context).GetQuestions(new QuestionFilter());
        Assert.Equal(Approved, Assert.Single(Assert.Single(result.Value.Data).Options).Name);
        Assert.Empty(context.QuestionOptions);
    }

    [Fact]
    public void Question_list_keeps_its_specific_view_permission()
    {
        var action = typeof(FormController).GetMethod(nameof(FormController.GetQuestions))!;
        Assert.Single(action.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Contains(PermissionKeys.CanViewQuestions, Assert.Single(action.GetCustomAttributes<AnyPermissionAttribute>()).Keys);
        Assert.Empty(action.GetCustomAttributes<AllowAnonymousAttribute>());
    }

    private static Question Seed(ApplicationDbContext context, bool withOption = true)
    {
        var question = new Question { Id = Guid.NewGuid(), Label = "Assay", Type = QuestionType.Formula };
        if (withOption) question.Options.Add(new QuestionOption { Id = Guid.NewGuid(), Name = Legacy });
        context.Questions.Add(question);
        return question;
    }

    private static FormulaDefinition Link(ApplicationDbContext context, Question question)
    {
        var definition = new FormulaDefinition { Id = Guid.NewGuid(), Key = question.Id.ToString(), Name = question.Label, PresentationPreset = "Universal" };
        context.QuestionFormulaDefinitions.Add(new QuestionFormulaDefinition { Id = Guid.NewGuid(), Question = question, FormulaDefinition = definition });
        return definition;
    }

    private static async Task Revision(ApplicationDbContext context, FormulaDefinition definition, int number, FormulaRevisionStatus status, string payload)
    {
        var revision = new FormulaRevision
        {
            Id = Guid.NewGuid(), FormulaDefinition = definition, Revision = number,
            Status = FormulaRevisionStatus.Draft, AuthoringPayloadJson = payload, DefinitionHash = new string('a', 64),
            DefinitionJson = "{}", TestCasesJson = "[]", FormulaLanguageVersion = "oryx-formula-v1",
            NumericPolicyVersion = "oryx-decimal-v1-half-up", ReleaseEvidenceHash = new string('b', 64)
        };
        context.FormulaRevisions.Add(revision);
        await context.SaveChangesAsync();
        foreach (var next in new[] { FormulaRevisionStatus.InReview, FormulaRevisionStatus.Approved, FormulaRevisionStatus.Retired })
        {
            if (revision.Status == status) break;
            var prior = revision.Status;
            revision.Status = next;
            if (next == FormulaRevisionStatus.Approved)
            {
                revision.ApprovedById = Guid.NewGuid();
                revision.ApprovedAt = revision.EffectiveAt = DateTime.UtcNow.AddMinutes(-1);
            }
            if (next == FormulaRevisionStatus.Retired) revision.RetiredAt = DateTime.UtcNow;
            context.FormulaRevisionAudits.Add(new FormulaRevisionAudit
            {
                Id = Guid.NewGuid(), FormulaRevisionId = revision.Id, PriorStatus = prior, NewStatus = next,
                DefinitionHash = revision.DefinitionHash, ActorId = Guid.NewGuid(), CorrelationId = Guid.NewGuid(),
                Reason = "Prepare the governed preview fixture.", Action = next.ToString(), OccurredAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }
    }

    private static ApplicationDbContext Context() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new PreviewUser());

    private sealed class PreviewUser : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => "";
    }
}
