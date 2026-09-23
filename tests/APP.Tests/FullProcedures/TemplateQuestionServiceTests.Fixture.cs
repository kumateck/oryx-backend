using APP.Services.FullProcedures;
using DOMAIN.Entities.FullProcedures;
using DOMAIN.Entities.Roles;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;

namespace APP.Tests.FullProcedures;

file sealed class QuestionCurrentUser(Guid userId) : ICurrentUserService
{
    public Guid? UserId => userId;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public sealed partial class TemplateQuestionServiceTests
{
    private sealed class Fixture : IAsyncDisposable
    {
        public ApplicationDbContext Context { get; }
        public TemplateQuestionService Service { get; }
        public Guid AreaId { get; } = Guid.NewGuid();
        public Guid AuthorId { get; }
        public Guid ReviewerId { get; } = Guid.NewGuid();
        public Guid PublisherId { get; } = Guid.NewGuid();
        public Guid AuthorRoleId { get; } = Guid.NewGuid();
        public Guid ReviewerRoleId { get; } = Guid.NewGuid();
        public Guid PublisherRoleId { get; } = Guid.NewGuid();
        public Guid ViewerRoleId { get; } = Guid.NewGuid();

        private Fixture(ApplicationDbContext context, Guid authorId)
        {
            Context = context;
            Service = new TemplateQuestionService(context);
            AuthorId = authorId;
        }

        public static async Task<Fixture> Create()
        {
            var author = Guid.NewGuid();
            var context = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
                new QuestionCurrentUser(author));
            var fixture = new Fixture(context, author);
            await fixture.Seed();
            return fixture;
        }

        public CreateTemplateQuestionRequest QuestionRequest() => new()
        {
            TemplateAreaId = AreaId, PurposeId = "quality-control",
            SubjectTypeId = "sample", Wording = "Record the observed result",
            AnswerType = TemplateQuestionAnswerType.ShortText, InputType = "text",
            Required = true, Sensitivity = TemplateQuestionSensitivity.Standard,
            Reason = "Create the controlled question draft.",
        };

        public CreateTemplateQuestionRevisionRequest RevisionRequest(string wording) => new()
        {
            Wording = wording, AnswerType = TemplateQuestionAnswerType.ShortText,
            InputType = "text", Required = true,
            Sensitivity = TemplateQuestionSensitivity.Standard,
            Reason = "Create a controlled replacement revision.",
        };

        public UpdateTemplateQuestionRevisionRequest UpdateRequest(string hash) => new()
        {
            Wording = "Record the observed result", AnswerType = TemplateQuestionAnswerType.ShortText,
            InputType = "text", Required = true,
            Sensitivity = TemplateQuestionSensitivity.Standard,
            ExpectedContentHash = hash, Reason = "Correct the controlled draft content.",
        };

        public TemplateQuestionTransitionRequest Transition(string hash) => new()
        {
            ExpectedContentHash = hash,
            Reason = "Complete the controlled lifecycle transition.",
        };

        public async Task<TemplateQuestionRevisionDto> CreateQuestion()
        {
            var result = await Service.CreateAsync(
                QuestionRequest(), AuthorId, [AuthorRoleId], Guid.NewGuid());
            if (result.IsFailure) throw new InvalidOperationException(result.Error.Code);
            return result.Value;
        }

        public async Task<TemplateQuestionRevisionDto> CreateAndPublishQuestion()
        {
            var revision = await CreateQuestion();
            var transition = Transition(revision.ContentHash);
            await Service.SubmitForReviewAsync(revision.Id, transition,
                AuthorId, [AuthorRoleId], Guid.NewGuid());
            await Service.RecordReviewAsync(revision.Id, transition,
                ReviewerId, [ReviewerRoleId], Guid.NewGuid());
            var published = await Service.PublishAsync(revision.Id, transition,
                PublisherId, [PublisherRoleId], Guid.NewGuid());
            return published.Value;
        }

        private async Task Seed()
        {
            var ownerRole = Role(Guid.NewGuid(), "Template owners");
            var authorRole = Role(AuthorRoleId, "Template authors");
            var reviewerRole = Role(ReviewerRoleId, "Template reviewers");
            var publisherRole = Role(PublisherRoleId, "Template publishers");
            var viewerRole = Role(ViewerRoleId, "Template viewers");
            var area = new TemplateArea
            {
                Id = AreaId, Name = "Quality templates", NormalizedName = "QUALITY TEMPLATES",
                OwnerRoleId = ownerRole.Id, ReviewPolicyId = "regulated-three-person",
                Version = 1, IsActive = true,
                Purposes = [new TemplateAreaPurpose
                    { Id = Guid.NewGuid(), TemplateAreaId = AreaId, PurposeId = "quality-control" }],
                SubjectTypes = [new TemplateAreaSubjectType
                    { Id = Guid.NewGuid(), TemplateAreaId = AreaId, SubjectTypeId = "sample" }],
                RoleGrants =
                [
                    Grant(AuthorRoleId, TemplateAreaAccessLevel.Author),
                    Grant(ReviewerRoleId, TemplateAreaAccessLevel.Reviewer),
                    Grant(PublisherRoleId, TemplateAreaAccessLevel.Publisher),
                    Grant(ViewerRoleId, TemplateAreaAccessLevel.Viewer),
                ],
            };
            Context.AddRange(ownerRole, authorRole, reviewerRole, publisherRole, viewerRole, area);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
        }

        private TemplateAreaRoleGrant Grant(Guid roleId, TemplateAreaAccessLevel access) => new()
        {
            Id = Guid.NewGuid(), TemplateAreaId = AreaId, RoleId = roleId, AccessLevel = access,
        };

        private static Role Role(Guid id, string name) => new()
        {
            Id = id, Name = name.Replace(" ", string.Empty),
            NormalizedName = name.Replace(" ", string.Empty).ToUpperInvariant(), DisplayName = name,
        };

        public ValueTask DisposeAsync() => Context.DisposeAsync();
    }
}
