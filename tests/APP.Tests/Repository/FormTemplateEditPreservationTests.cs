using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Forms.Request;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

// UpdateForm replaces a template's sections and fields wholesale. The database cascades field deletes to
// saved test answers (FormResponses) and CoA items, so a template that has been used must refuse that path.
public sealed class FormTemplateEditPreservationTests
{
    private const string ConnectionVariable = "ORYX_FORM_EDIT_TEST_CONNECTION";

    private sealed class PostgresFactAttribute : FactAttribute
    {
        public PostgresFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionVariable)))
                Skip = $"Set {ConnectionVariable} to an isolated PostgreSQL test server (requires CREATE DATABASE).";
        }
    }

    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }

    private sealed record Seed(Form Form, FormSection Section, FormField Field, Question Question);

    private static IMapper Mapper() => new MapperConfiguration(cfg =>
    {
        cfg.CreateMap<CreateFormRequest, Form>();
        cfg.CreateMap<CreateFormSectionRequest, FormSection>();
        cfg.CreateMap<CreateFormFieldRequest, FormField>();
        cfg.CreateMap<CreateFormAssigneeRequest, FormAssignee>();
        cfg.CreateMap<CreateFormReviewerRequest, FormReviewer>();
    }).CreateMapper();

    private static FormRepository Repository(ApplicationDbContext db) => new(db, Mapper(), null!, null!, null!);

    private static async Task<Seed> SeedForm(ApplicationDbContext db, Actor actor)
    {
        if (!await db.Users.AnyAsync(u => u.Id == actor.UserId))
            db.Users.Add(new User { Id = actor.UserId!.Value, UserName = "form-edit-test" });
        var question = new Question { Id = Guid.NewGuid(), Label = "Assay", Type = QuestionType.ShortAnswer };
        var form = new Form { Id = Guid.NewGuid(), Name = "Assay template" };
        var section = new FormSection { Id = Guid.NewGuid(), FormId = form.Id, Name = "Results", Order = 1 };
        var field = new FormField { Id = Guid.NewGuid(), FormSectionId = section.Id, QuestionId = question.Id, Rank = 1 };
        db.AddRange(question, form, section, field);
        await db.SaveChangesAsync();
        return new Seed(form, section, field, question);
    }

    private static CreateFormRequest Edit(Seed seed, string sectionName = "Results (rev 2)") => new()
    {
        Name = seed.Form.Name,
        Sections =
        [
            new CreateFormSectionRequest
            {
                Name = sectionName,
                Order = 1,
                Fields = [new CreateFormFieldRequest { QuestionId = seed.Question.Id, Rank = 1, Required = true }],
            },
        ],
    };

    private static async Task AddAnswer(ApplicationDbContext db, Seed seed, string value)
    {
        var response = new Response { Id = Guid.NewGuid(), FormId = seed.Form.Id };
        db.Add(response);
        db.Add(new FormResponse { Id = Guid.NewGuid(), ResponseId = response.Id, FormFieldId = seed.Field.Id, Value = value });
        await db.SaveChangesAsync();
    }

    [PostgresFact]
    public async Task Editing_a_used_template_never_deletes_saved_answers()
    {
        var database = "oryx_form_edit_test_" + Guid.NewGuid().ToString("N");
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable(ConnectionVariable));
        await using var admin = new NpgsqlConnection(connection.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin))
            await create.ExecuteNonQueryAsync();
        connection.Database = database;
        try
        {
            var actor = new Actor();
            ApplicationDbContext Open() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(connection.ConnectionString).Options, actor);

            await using (var setup = Open())
                await setup.Database.EnsureCreatedAsync();

            // 1. A template with a saved answer refuses wholesale replacement and keeps the answer.
            Seed used;
            await using (var db = Open())
            {
                used = await SeedForm(db, actor);
                await AddAnswer(db, used, "99.8");
            }
            await using (var db = Open())
            {
                var result = await Repository(db).UpdateForm(Edit(used), used.Form.Id, actor.UserId!.Value);
                Assert.True(result.IsFailure, "Editing a template with saved answers must be refused.");
                Assert.Equal(ErrorType.Conflict, result.Error.Type);
                Assert.Equal(FormErrors.FieldsInUse(used.Form.Id).Code, result.Error.Code);
            }
            await using (var db = Open())
            {
                var answer = await db.FormResponses.IgnoreQueryFilters().SingleAsync(r => r.Response.FormId == used.Form.Id);
                Assert.Equal("99.8", answer.Value);
                Assert.Equal(used.Field.Id, answer.FormFieldId);
                var section = await db.FormSections.IgnoreQueryFilters().SingleAsync(s => s.FormId == used.Form.Id);
                Assert.Equal("Results", section.Name);
            }

            // 2. Soft-deleted answers are still audit history; they must survive too.
            Seed softDeleted;
            await using (var db = Open())
            {
                softDeleted = await SeedForm(db, actor);
                await AddAnswer(db, softDeleted, "withdrawn");
                var row = await db.FormResponses.SingleAsync(r => r.FormFieldId == softDeleted.Field.Id);
                row.DeletedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }
            await using (var db = Open())
            {
                var result = await Repository(db).UpdateForm(Edit(softDeleted), softDeleted.Form.Id, actor.UserId!.Value);
                Assert.True(result.IsFailure);
                Assert.Equal(ErrorType.Conflict, result.Error.Type);
            }
            await using (var db = Open())
                Assert.True(await db.FormResponses.IgnoreQueryFilters().AnyAsync(r => r.FormFieldId == softDeleted.Field.Id));

            // 3. Specification values live on the section row; replacing sections would drop them.
            Seed specified;
            await using (var db = Open())
            {
                specified = await SeedForm(db, actor);
                var section = await db.FormSections.SingleAsync(s => s.Id == specified.Section.Id);
                section.Value = "NLT 98.0%";
                await db.SaveChangesAsync();
            }
            await using (var db = Open())
            {
                var result = await Repository(db).UpdateForm(Edit(specified), specified.Form.Id, actor.UserId!.Value);
                Assert.True(result.IsFailure);
                Assert.Equal(ErrorType.Conflict, result.Error.Type);
            }
            await using (var db = Open())
                Assert.Equal("NLT 98.0%", (await db.FormSections.SingleAsync(s => s.Id == specified.Section.Id)).Value);

            // 4. An unused template can still be restructured freely.
            Seed unused;
            await using (var db = Open())
                unused = await SeedForm(db, actor);
            await using (var db = Open())
            {
                var result = await Repository(db).UpdateForm(Edit(unused), unused.Form.Id, actor.UserId!.Value);
                Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
            }
            await using (var db = Open())
            {
                var section = await db.FormSections.Include(s => s.Fields).SingleAsync(s => s.FormId == unused.Form.Id);
                Assert.Equal("Results (rev 2)", section.Name);
                Assert.True(Assert.Single(section.Fields).Required);
            }

            // 5. An invalid edit is rejected before anything is deleted.
            Seed invalid;
            await using (var db = Open())
                invalid = await SeedForm(db, actor);
            await using (var db = Open())
            {
                var request = Edit(invalid);
                request.Sections = [];
                var result = await Repository(db).UpdateForm(request, invalid.Form.Id, actor.UserId!.Value);
                Assert.True(result.IsFailure);
            }
            await using (var db = Open())
                Assert.True(await db.FormFields.AnyAsync(f => f.Id == invalid.Field.Id));
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
