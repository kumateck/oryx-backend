using APP.IRepository;
using APP.Repository;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Forms.Request;
using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class FormDraftAttachmentTests
{
    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => "NonProduction";
    }
    private sealed class Files(bool fail = false, ApplicationDbContext? context = null) : IFileRepository
    {
        public List<Guid> ModelIds { get; } = [];
        public async Task<Result> SaveBlobItem(string type, Guid id, string reference, IFormFile file, Guid? user)
        {
            ModelIds.Add(id);
            if (fail) return Result.Failure(Error.Failure("File.Upload", "Storage unavailable"));
            if (context is not null) await context.SaveChangesAsync();
            return Result.Success();
        }
        public Task<Result> SaveBlobItem(string type, Guid id, List<IFormFile> files, Guid? user) => throw new NotSupportedException();
        public Task<Result> DeleteAttachment(Guid id, Guid user) => throw new NotSupportedException();
        public Task<Result> DeleteAttachment(Guid id, string reference, Guid user) => throw new NotSupportedException();
    }

    [Theory]
    [InlineData(QuestionType.FileUpload)]
    [InlineData(QuestionType.Signature)]
    public async Task First_draft_upload_uses_the_persisted_field_id_and_reuses_it_for_more_files(QuestionType type)
    {
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor());
        var form = new Form { Id = Guid.NewGuid(), Name = "Test" };
        var section = new FormSection { Id = Guid.NewGuid(), FormId = form.Id, Name = "Evidence" };
        var question = new Question { Id = Guid.NewGuid(), Type = type, Label = "Evidence file" };
        var field = new FormField { Id = Guid.NewGuid(), FormSectionId = section.Id, QuestionId = question.Id };
        context.AddRange(form, section, question, field); await context.SaveChangesAsync();
        var files = new Files(context: context);
        var repo = new FormRepository(context, null!, files, null!, null!);
        var request = new SaveResponseDraftRequest { FormId = form.Id, FormFieldId = field.Id,
            Value = "data:image/png;base64,QQ==" };
        var first = await repo.SaveFormResponseDraft(request, Guid.NewGuid());
        Assert.True(first.IsSuccess, first.IsFailure ? first.Error.Description : null);
        var answer = await context.FormResponses.SingleAsync();
        Assert.NotEqual(Guid.Empty, answer.Id);
        Assert.Equal(answer.Id, Assert.Single(files.ModelIds));
        request.ResponseId = first.Value;
        var second = await repo.SaveFormResponseDraft(request, Guid.NewGuid());
        Assert.True(second.IsSuccess);
        Assert.Equal(2, files.ModelIds.Count);
        Assert.All(files.ModelIds, id => Assert.Equal(answer.Id, id));
        Assert.Equal(1, await context.FormResponses.CountAsync());
    }

    [Fact]
    public async Task Failed_storage_does_not_report_a_successful_draft()
    {
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor());
        var response = new Response { Id = Guid.NewGuid() };
        var field = new FormField { Id = Guid.NewGuid() };
        var files = new Files(true);
        var result = await APP.Services.Forms.FormDraftAttachments.Save(response, field, null,
            "data:application/pdf;base64,QQ==", Guid.NewGuid(), files, context);
        Assert.True(result.IsFailure);
        Assert.Equal("File.Upload", result.Error.Code);
        Assert.Equal(EntityState.Added, context.Entry(Assert.Single(response.FormResponses)).State);
    }
}
