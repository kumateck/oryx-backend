using APP.Services.FullProcedures;
using DOMAIN.Entities.FullProcedures;
using DOMAIN.Entities.Roles;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;

namespace APP.Tests.FullProcedures;

file sealed class FormCurrentUser(Guid userId) : ICurrentUserService
{
    public Guid? UserId => userId;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public sealed partial class TemplateFormServiceTests
{
    private sealed class Fixture : IAsyncDisposable
    {
        public ApplicationDbContext Context { get; }
        public TemplateFormService Service { get; }
        public Guid AreaId { get; } = Guid.NewGuid();
        public Guid AuthorId { get; }
        public Guid ReviewerId { get; } = Guid.NewGuid();
        public Guid PublisherId { get; } = Guid.NewGuid();
        public Guid AuthorRoleId { get; } = Guid.NewGuid();
        public Guid ReviewerRoleId { get; } = Guid.NewGuid();
        public Guid PublisherRoleId { get; } = Guid.NewGuid();
        public Guid FirstSectionId { get; } = Guid.NewGuid();
        public Guid FirstSectionRevisionId { get; } = Guid.NewGuid();
        public Guid SecondSectionId { get; } = Guid.NewGuid();
        public Guid SecondSectionRevisionId { get; } = Guid.NewGuid();
        public Guid DraftSectionId { get; } = Guid.NewGuid();
        public Guid DraftSectionRevisionId { get; } = Guid.NewGuid();
        public Guid SourceQuestionId { get; } = Guid.NewGuid();
        public Guid SourceQuestionRevisionId { get; } = Guid.NewGuid();

        private Fixture(ApplicationDbContext context, Guid authorId)
        {
            Context = context;
            Service = new TemplateFormService(context);
            AuthorId = authorId;
        }

        public static async Task<Fixture> Create()
        {
            var author = Guid.NewGuid();
            var context = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
                new FormCurrentUser(author));
            var fixture = new Fixture(context, author);
            await fixture.Seed();
            return fixture;
        }

        public CreateTemplateFormRequest FormRequest() => new()
        {
            TemplateAreaId = AreaId, PurposeId = "quality-control",
            SubjectTypeId = "sample", Name = "Environmental analysis",
            Description = "Controlled sampling and analysis form.",
            RequiresEvidence = true, RequiresSignature = true,
            Sections =
            [
                new() { SectionId = FirstSectionId, RevisionId = FirstSectionRevisionId,
                    Order = 0, IsRequired = true },
                new() { SectionId = SecondSectionId, RevisionId = SecondSectionRevisionId,
                    Order = 1, IsRequired = false },
            ],
            ConditionalRules =
            [
                new() { TargetSectionId = SecondSectionId,
                    SourceSectionId = FirstSectionId, SourceQuestionId = SourceQuestionId,
                    SourceQuestionRevisionId = SourceQuestionRevisionId,
                    Operator = TemplateFormConditionOperator.Equals, Value = "acceptable" },
            ],
            Reason = "Create the controlled form draft.",
        };

        public CreateTemplateFormRevisionRequest RevisionRequest(string name)
        {
            var source = FormRequest();
            return new CreateTemplateFormRevisionRequest
            {
                Name = name, Description = source.Description,
                RequiresEvidence = source.RequiresEvidence,
                RequiresSignature = source.RequiresSignature,
                Sections = source.Sections, ConditionalRules = source.ConditionalRules,
                Reason = "Create the controlled replacement form.",
            };
        }

        public TemplateFormTransitionRequest Transition(string hash) => new()
        {
            ExpectedContentHash = hash,
            Reason = "Complete the controlled form transition.",
        };

        public async Task<TemplateFormRevisionDto> CreateForm()
        {
            var result = await Service.CreateAsync(FormRequest(), AuthorId,
                [AuthorRoleId], Guid.NewGuid());
            if (result.IsFailure) throw new InvalidOperationException(result.Error.Code);
            return result.Value;
        }

        private async Task Seed()
        {
            var owner = Role(Guid.NewGuid(), "Template owners");
            var author = Role(AuthorRoleId, "Template authors");
            var reviewer = Role(ReviewerRoleId, "Template reviewers");
            var publisher = Role(PublisherRoleId, "Template publishers");
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
                ],
            };
            var question = Question();
            Context.AddRange(owner, author, reviewer, publisher, area, question,
                Section(FirstSectionId, FirstSectionRevisionId, "Sample receipt",
                    TemplateSectionRevisionStatus.Published, question),
                Section(SecondSectionId, SecondSectionRevisionId, "Analysis",
                    TemplateSectionRevisionStatus.Published),
                Section(DraftSectionId, DraftSectionRevisionId, "Draft checks",
                    TemplateSectionRevisionStatus.Draft));
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
        }

        private TemplateQuestion Question()
        {
            var question = new TemplateQuestion
            {
                Id = SourceQuestionId, TemplateAreaId = AreaId,
                PurposeId = "quality-control", SubjectTypeId = "sample",
            };
            question.Revisions.Add(new TemplateQuestionRevision
            {
                Id = SourceQuestionRevisionId, TemplateQuestionId = SourceQuestionId,
                Sequence = 1, Status = TemplateQuestionRevisionStatus.Published,
                Wording = "Is the sample acceptable?",
                AnswerType = TemplateQuestionAnswerType.ShortText,
                InputType = "text", ContentHash = new string('a', 64),
            });
            return question;
        }

        private TemplateSection Section(Guid id, Guid revisionId, string title,
            TemplateSectionRevisionStatus status, TemplateQuestion? question = null)
        {
            var section = new TemplateSection
            {
                Id = id, TemplateAreaId = AreaId,
                PurposeId = "quality-control", SubjectTypeId = "sample",
            };
            var revision = new TemplateSectionRevision
            {
                Id = revisionId, TemplateSectionId = id, Sequence = 1,
                Status = status, Title = title, ContentHash = new string('b', 64),
            };
            if (question is not null)
                revision.Questions.Add(new TemplateSectionQuestion
                {
                    Id = Guid.NewGuid(), TemplateSectionRevisionId = revisionId,
                    TemplateQuestionId = question.Id,
                    TemplateQuestionRevisionId = SourceQuestionRevisionId, Order = 0,
                });
            section.Revisions.Add(revision);
            return section;
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
