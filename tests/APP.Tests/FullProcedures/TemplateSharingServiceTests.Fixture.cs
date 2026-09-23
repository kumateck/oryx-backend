using APP.Services.FullProcedures;
using DOMAIN.Entities.FullProcedures;
using DOMAIN.Entities.Roles;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;

namespace APP.Tests.FullProcedures;

file sealed class SharingCurrentUser(Guid userId) : ICurrentUserService
{
    public Guid? UserId => userId;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public sealed partial class TemplateSharingServiceTests
{
    private sealed class Fixture : IAsyncDisposable
    {
        public ApplicationDbContext Context { get; }
        public TemplateSharingService Service { get; }
        public Guid SourceAreaId { get; } = Guid.NewGuid();
        public Guid TargetAreaId { get; } = Guid.NewGuid();
        public Guid SourceActorId { get; }
        public Guid TargetActorId { get; } = Guid.NewGuid();
        public Guid SourcePublisherRoleId { get; } = Guid.NewGuid();
        public Guid TargetAuthorRoleId { get; } = Guid.NewGuid();
        public Guid TargetPublisherRoleId { get; } = Guid.NewGuid();
        public Guid QuestionId { get; } = Guid.NewGuid();
        public Guid QuestionRevisionId { get; } = Guid.NewGuid();
        public Guid DraftQuestionId { get; } = Guid.NewGuid();
        public Guid DraftQuestionRevisionId { get; } = Guid.NewGuid();

        private Fixture(ApplicationDbContext context, Guid sourceActorId)
        {
            Context = context;
            Service = new TemplateSharingService(context);
            SourceActorId = sourceActorId;
        }

        public static async Task<Fixture> Create()
        {
            var actor = Guid.NewGuid();
            var context = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
                new SharingCurrentUser(actor));
            var fixture = new Fixture(context, actor);
            await fixture.Seed();
            return fixture;
        }

        public RequestTemplateSharingGrantRequest Request(Guid? revisionId = null) => new()
        {
            SourceAreaId = SourceAreaId, TargetAreaId = TargetAreaId,
            RequestedByAreaId = SourceAreaId, TemplateKind = TemplateRevisionKind.Question,
            DefinitionId = revisionId.HasValue ? DraftQuestionId : QuestionId,
            RevisionId = revisionId ?? QuestionRevisionId,
            Reason = "Share this exact governed question revision.",
        };

        public async Task<TemplateSharingGrantDto> RequestGrant()
        {
            var result = await Service.RequestAsync(Request(), SourceActorId,
                [SourcePublisherRoleId], Guid.NewGuid());
            if (result.IsFailure) throw new InvalidOperationException(result.Error.Code);
            return result.Value;
        }

        public async Task<TemplateSharingGrantDto> ApproveGrant(TemplateSharingGrantDto grant)
        {
            var result = await Service.ApproveAsync(grant.Id, new()
            {
                ExpectedVersion = grant.Version,
                Reason = "Accept the exact definition into the target catalog.",
            }, TargetActorId, [TargetAuthorRoleId], Guid.NewGuid());
            if (result.IsFailure) throw new InvalidOperationException(result.Error.Code);
            return result.Value;
        }

        private async Task Seed()
        {
            var sourcePublisher = Role(SourcePublisherRoleId, "Source publisher");
            var targetAuthor = Role(TargetAuthorRoleId, "Target author");
            var targetPublisher = Role(TargetPublisherRoleId, "Target publisher");
            var sourceOwner = Role(Guid.NewGuid(), "Source owner");
            var targetOwner = Role(Guid.NewGuid(), "Target owner");
            var source = Area(SourceAreaId, "Source QC", sourceOwner.Id,
                Grant(SourceAreaId, SourcePublisherRoleId, TemplateAreaAccessLevel.Publisher));
            var target = Area(TargetAreaId, "Target QC", targetOwner.Id,
                Grant(TargetAreaId, TargetAuthorRoleId, TemplateAreaAccessLevel.Author),
                Grant(TargetAreaId, TargetPublisherRoleId, TemplateAreaAccessLevel.Publisher));
            var published = Question(QuestionId, QuestionRevisionId,
                TemplateQuestionRevisionStatus.Published);
            var draft = Question(DraftQuestionId, DraftQuestionRevisionId,
                TemplateQuestionRevisionStatus.Draft);
            var section = new TemplateSection { Id = Guid.NewGuid(), TemplateAreaId = SourceAreaId,
                PurposeId = "quality-control", SubjectTypeId = "sample" };
            var sectionRevision = new TemplateSectionRevision { Id = Guid.NewGuid(),
                TemplateSectionId = section.Id, Sequence = 1,
                Status = TemplateSectionRevisionStatus.Published, Title = "Shared question use",
                ContentHash = new string('d', 64) };
            sectionRevision.Questions.Add(new TemplateSectionQuestion { Id = Guid.NewGuid(),
                TemplateSectionRevisionId = sectionRevision.Id, TemplateQuestionId = QuestionId,
                TemplateQuestionRevisionId = QuestionRevisionId, Order = 0 });
            section.Revisions.Add(sectionRevision);
            Context.AddRange(sourcePublisher, targetAuthor, targetPublisher,
                sourceOwner, targetOwner, source, target, published, draft, section);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
        }

        private TemplateQuestion Question(Guid id, Guid revisionId,
            TemplateQuestionRevisionStatus status)
        {
            var question = new TemplateQuestion { Id = id, TemplateAreaId = SourceAreaId,
                PurposeId = "quality-control", SubjectTypeId = "sample" };
            question.Revisions.Add(new TemplateQuestionRevision { Id = revisionId,
                TemplateQuestionId = id, Sequence = 1, Status = status,
                Wording = "What is the result?", InputType = "number",
                ContentHash = new string('c', 64) });
            return question;
        }

        private static TemplateArea Area(Guid id, string name, Guid ownerRoleId,
            params TemplateAreaRoleGrant[] grants) => new()
            {
                Id = id, Name = name, NormalizedName = name.ToUpperInvariant(),
                OwnerRoleId = ownerRoleId, ReviewPolicyId = "regulated-three-person",
                Version = 1, IsActive = true,
                Purposes = [new() { Id = Guid.NewGuid(), TemplateAreaId = id,
                    PurposeId = "quality-control" }],
                SubjectTypes = [new() { Id = Guid.NewGuid(), TemplateAreaId = id,
                    SubjectTypeId = "sample" }], RoleGrants = [.. grants],
            };

        private static TemplateAreaRoleGrant Grant(Guid areaId, Guid roleId,
            TemplateAreaAccessLevel access) => new()
            { Id = Guid.NewGuid(), TemplateAreaId = areaId, RoleId = roleId, AccessLevel = access };
        private static Role Role(Guid id, string name) => new() { Id = id,
            Name = name.Replace(" ", string.Empty), NormalizedName = name.Replace(" ", string.Empty)
                .ToUpperInvariant(), DisplayName = name };
        public ValueTask DisposeAsync() => Context.DisposeAsync();
    }
}
