using APP.Services.FullProcedures;
using DOMAIN.Entities.FullProcedures;
using DOMAIN.Entities.Roles;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;

namespace APP.Tests.FullProcedures;

file sealed class SectionCurrentUser(Guid userId) : ICurrentUserService
{
    public Guid? UserId => userId;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public sealed partial class TemplateSectionServiceTests
{
    private sealed class Fixture : IAsyncDisposable
    {
        public ApplicationDbContext Context { get; }
        public TemplateSectionService Service { get; }
        public Guid AreaId { get; } = Guid.NewGuid();
        public Guid AuthorId { get; }
        public Guid ReviewerId { get; } = Guid.NewGuid();
        public Guid PublisherId { get; } = Guid.NewGuid();
        public Guid AuthorRoleId { get; } = Guid.NewGuid();
        public Guid ReviewerRoleId { get; } = Guid.NewGuid();
        public Guid PublisherRoleId { get; } = Guid.NewGuid();
        public Guid ViewerRoleId { get; } = Guid.NewGuid();
        public Guid FirstQuestionId { get; } = Guid.NewGuid();
        public Guid FirstRevisionId { get; } = Guid.NewGuid();
        public Guid SecondQuestionId { get; } = Guid.NewGuid();
        public Guid SecondRevisionId { get; } = Guid.NewGuid();
        public Guid DraftQuestionId { get; } = Guid.NewGuid();
        public Guid DraftRevisionId { get; } = Guid.NewGuid();

        private Fixture(ApplicationDbContext context, Guid authorId)
        {
            Context = context;
            Service = new TemplateSectionService(context);
            AuthorId = authorId;
        }

        public static async Task<Fixture> Create()
        {
            var author = Guid.NewGuid();
            var context = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
                new SectionCurrentUser(author));
            var fixture = new Fixture(context, author);
            await fixture.Seed();
            return fixture;
        }

        public CreateTemplateSectionRequest SectionRequest() => new()
        {
            TemplateAreaId = AreaId, PurposeId = "quality-control",
            SubjectTypeId = "sample", Title = "Environmental checks",
            Questions =
            [
                new() { QuestionId = FirstQuestionId, RevisionId = FirstRevisionId, Order = 0 },
                new() { QuestionId = SecondQuestionId, RevisionId = SecondRevisionId, Order = 1 },
            ],
            ConditionalRules =
            [
                new()
                {
                    TargetQuestionId = SecondQuestionId,
                    DependsOnQuestionId = FirstQuestionId,
                    Operator = TemplateSectionConditionOperator.Equals, Value = "acceptable",
                },
            ],
            Reason = "Create the controlled section draft.",
        };

        public CreateTemplateSectionRevisionRequest RevisionRequest(string title)
        {
            var source = SectionRequest();
            return new CreateTemplateSectionRevisionRequest
            {
                Title = title, Questions = source.Questions,
                ConditionalRules = source.ConditionalRules,
                Reason = "Create a controlled replacement section.",
            };
        }

        public UpdateTemplateSectionRevisionRequest UpdateRequest(string hash)
        {
            var source = SectionRequest();
            return new UpdateTemplateSectionRevisionRequest
            {
                Title = source.Title, Questions = source.Questions,
                ConditionalRules = source.ConditionalRules,
                ExpectedContentHash = hash,
                Reason = "Correct the controlled section draft.",
            };
        }

        public TemplateSectionTransitionRequest Transition(string hash) => new()
        {
            ExpectedContentHash = hash,
            Reason = "Complete the controlled section transition.",
        };

        public async Task<TemplateSectionRevisionDto> CreateSection()
        {
            var result = await Service.CreateAsync(
                SectionRequest(), AuthorId, [AuthorRoleId], Guid.NewGuid());
            if (result.IsFailure) throw new InvalidOperationException(result.Error.Code);
            return result.Value;
        }

        private async Task Seed()
        {
            var owner = Role(Guid.NewGuid(), "Template owners");
            var author = Role(AuthorRoleId, "Template authors");
            var reviewer = Role(ReviewerRoleId, "Template reviewers");
            var publisher = Role(PublisherRoleId, "Template publishers");
            var viewer = Role(ViewerRoleId, "Template viewers");
            var area = new TemplateArea
            {
                Id = AreaId, Name = "Quality templates", NormalizedName = "QUALITY TEMPLATES",
                OwnerRoleId = owner.Id, ReviewPolicyId = "regulated-three-person",
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
            Context.AddRange(owner, author, reviewer, publisher, viewer, area,
                Question(FirstQuestionId, FirstRevisionId, "Record pH",
                    TemplateQuestionRevisionStatus.Published),
                Question(SecondQuestionId, SecondRevisionId, "Record temperature",
                    TemplateQuestionRevisionStatus.Published),
                Question(DraftQuestionId, DraftRevisionId, "Draft observation",
                    TemplateQuestionRevisionStatus.Draft));
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
        }

        private TemplateQuestion Question(
            Guid questionId, Guid revisionId, string wording, TemplateQuestionRevisionStatus status)
        {
            var question = new TemplateQuestion
            {
                Id = questionId, TemplateAreaId = AreaId,
                PurposeId = "quality-control", SubjectTypeId = "sample",
            };
            question.Revisions.Add(new TemplateQuestionRevision
            {
                Id = revisionId, TemplateQuestionId = questionId, Sequence = 1,
                Status = status, Wording = wording,
                AnswerType = TemplateQuestionAnswerType.ShortText,
                InputType = "text", ContentHash = new string('a', 64),
            });
            return question;
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
