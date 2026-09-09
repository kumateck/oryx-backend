using System.Text.Json;
using APP.Services.Formulas;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public sealed class FormulaCrossQuestionSourceResolverTests
{
    [Fact]
    public async Task Resolves_scalar_by_stable_placement_key()
    {
        var seeded = await SeedAsync("\"12.50\"");
        using var source = JsonDocument.Parse(Source(4,
            "placement/source-reading/value"));

        var result = await FormulaCrossQuestionSourceResolver.ResolveAsync(
            seeded.Context, seeded.Snapshot, source.RootElement, default);

        Assert.Equal("12.50", result);
    }

    [Fact]
    public async Task Resolves_rounded_cross_question_table_statistic()
    {
        var seeded = await SeedAsync("{\"tableData\":[[\"1.25\"],[\"2.50\"],[\"3.75\"]]}");
        using var source = JsonDocument.Parse(Source(6,
            "placement/source-reading/table/column/0", 0, 2));

        var result = await FormulaCrossQuestionSourceResolver.ResolveAsync(
            seeded.Context, seeded.Snapshot, source.RootElement, default);

        Assert.Equal("2.5", result);
    }

    [Fact]
    public async Task Rejects_self_or_unknown_placement_references()
    {
        var seeded = await SeedAsync("\"12.50\"");
        using var self = JsonDocument.Parse(Source(4, "placement/formula/value"));
        using var unknown = JsonDocument.Parse(Source(4, "placement/other/value"));

        Assert.Null(await FormulaCrossQuestionSourceResolver.ResolveAsync(
            seeded.Context, seeded.Snapshot, self.RootElement, default));
        Assert.Null(await FormulaCrossQuestionSourceResolver.ResolveAsync(
            seeded.Context, seeded.Snapshot, unknown.RootElement, default));
    }

    [Fact]
    public async Task Rejects_cross_question_value_beyond_approved_input_scale()
    {
        var seeded = await SeedAsync("\"12.501\"");
        using var source = JsonDocument.Parse(Source(4,
            "placement/source-reading/value", digits: 2));

        Assert.Null(await FormulaCrossQuestionSourceResolver.ResolveAsync(
            seeded.Context, seeded.Snapshot, source.RootElement, default));
    }

    [Fact]
    public async Task Rejects_malformed_cross_question_table_cell()
    {
        var seeded = await SeedAsync("{\"tableData\":[[\"1.25\"],[false]]}");
        using var source = JsonDocument.Parse(Source(6,
            "placement/source-reading/table/column/0", 0, 2));

        Assert.Null(await FormulaCrossQuestionSourceResolver.ResolveAsync(
            seeded.Context, seeded.Snapshot, source.RootElement, default));
    }

    private static string Source(int type, string reference, int? statistic = null,
        int? digits = null) => JsonSerializer.Serialize(new
    {
        sourceType = type, reference, dataType = 0, valueShape = 0, scope = 1,
        unit = (string?)null, statistic, inputDecimalPlaces = digits,
        tableCalculationDecimalPlaces = digits
    }, new JsonSerializerOptions { DefaultIgnoreCondition =
        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });

    private static async Task<Seeded> SeedAsync(string sourceValue)
    {
        var actor = Guid.NewGuid();
        var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            new TestUser(actor));
        var form = new Form { Id = Guid.NewGuid(), Name = "Cross source" };
        var section = new FormSection
        {
            Id = Guid.NewGuid(), FormId = form.Id, Form = form,
            Name = "Results", Description = ""
        };
        var sourceQuestion = Question("Source", QuestionType.ShortAnswer);
        var formulaQuestion = Question("Formula", QuestionType.Formula);
        var sourceField = Field(section, sourceQuestion);
        var formulaField = Field(section, formulaQuestion);
        var revision = new FormRevision
        {
            Id = Guid.NewGuid(), FormId = form.Id, Form = form, Sequence = 1,
            Status = FormRevisionStatus.Draft, ContentHash = new string('a', 64)
        };
        revision.Fields.Add(FieldRevision(revision, sourceField, "source-reading"));
        revision.Fields.Add(FieldRevision(revision, formulaField, "formula"));
        var response = new Response
        {
            Id = Guid.NewGuid(), FormId = form.Id, FormRevisionId = revision.Id,
            CreatedById = actor
        };
        context.AddRange(form, section, sourceQuestion, formulaQuestion, sourceField,
            formulaField, revision, response, new FormResponse
            {
                Id = Guid.NewGuid(), ResponseId = response.Id,
                FormFieldId = sourceField.Id, Value = sourceValue
            });
        await context.SaveChangesAsync();
        return new Seeded(context, new ResponseFormulaSnapshot
        {
            Id = Guid.NewGuid(), ResponseId = response.Id, Response = response,
            PlacementKey = "formula"
        });
    }

    private static Question Question(string label, QuestionType type) => new()
    {
        Id = Guid.NewGuid(), Label = label, Type = type,
        Validation = QuestionValidationType.None
    };

    private static FormField Field(FormSection section, Question question) => new()
    {
        Id = Guid.NewGuid(), FormSectionId = section.Id, FormSection = section,
        QuestionId = question.Id, Question = question
    };

    private static FormFieldRevision FieldRevision(FormRevision revision,
        FormField field, string placementKey) => new()
    {
        Id = Guid.NewGuid(), FormRevisionId = revision.Id, FormRevision = revision,
        PlacementKey = placementKey, FormFieldId = field.Id, QuestionId = field.QuestionId,
        FieldHash = new string('b', 64)
    };

    private sealed record Seeded(ApplicationDbContext Context,
        ResponseFormulaSnapshot Snapshot);

    private sealed class TestUser(Guid id) : ICurrentUserService
    {
        public Guid? UserId => id;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }
}
