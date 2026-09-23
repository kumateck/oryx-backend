using APP.Services.FullProcedures;
using DOMAIN.Entities.FullProcedures;
using DOMAIN.Entities.Roles;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;

namespace APP.Tests.FullProcedures;

file sealed class WorkflowCurrentUser(Guid userId) : ICurrentUserService
{
    public Guid? UserId => userId;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public sealed partial class TemplateWorkflowServiceTests
{
    private sealed class Fixture : IAsyncDisposable
    {
        public ApplicationDbContext Context { get; }
        public TemplateWorkflowService Service { get; }
        public Guid AreaId { get; } = Guid.NewGuid();
        public Guid AuthorId { get; }
        public Guid ReviewerId { get; } = Guid.NewGuid();
        public Guid PublisherId { get; } = Guid.NewGuid();
        public Guid AuthorRoleId { get; } = Guid.NewGuid();
        public Guid ReviewerRoleId { get; } = Guid.NewGuid();
        public Guid PublisherRoleId { get; } = Guid.NewGuid();
        public Guid ActivityId { get; } = Guid.NewGuid();
        public Guid ActivityRevisionId { get; } = Guid.NewGuid();
        public Guid DraftActivityId { get; } = Guid.NewGuid();
        public Guid DraftActivityRevisionId { get; } = Guid.NewGuid();

        private Fixture(ApplicationDbContext context, Guid authorId)
        {
            Context = context;
            Service = new TemplateWorkflowService(context);
            AuthorId = authorId;
        }

        public static async Task<Fixture> Create()
        {
            var author = Guid.NewGuid();
            var context = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
                new WorkflowCurrentUser(author));
            var fixture = new Fixture(context, author);
            await fixture.Seed();
            return fixture;
        }

        public CreateTemplateWorkflowRequest WorkflowRequest() => new()
        {
            TemplateAreaId = AreaId, PurposeId = "quality-control",
            SubjectTypeId = "sample", Name = "Sample release workflow",
            Description = "Receive the sample, run the activity, and close the workflow.",
            Nodes =
            [
                new() { Key = "start", Name = "Start", Order = 0,
                    NodeType = TemplateWorkflowNodeType.Start },
                new() { Key = "activity", Name = "Run analysis", Order = 1,
                    NodeType = TemplateWorkflowNodeType.Activity,
                    TemplateActivityId = ActivityId, TemplateActivityRevisionId = ActivityRevisionId },
                new() { Key = "end", Name = "End", Order = 2,
                    NodeType = TemplateWorkflowNodeType.End },
            ],
            Edges =
            [
                new() { SourceKey = "start", TargetKey = "activity", Order = 0 },
                new() { SourceKey = "activity", TargetKey = "end", Order = 0 },
            ],
            Reason = "Create the controlled workflow draft.",
        };

        public TemplateWorkflowTransitionRequest Transition(string hash) => new()
        {
            ExpectedContentHash = hash,
            Reason = "Complete the controlled workflow transition.",
        };

        public async Task<TemplateWorkflowRevisionDto> CreateWorkflow()
        {
            var result = await Service.CreateAsync(WorkflowRequest(), AuthorId,
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
                Purposes = [new TemplateAreaPurpose { Id = Guid.NewGuid(),
                    TemplateAreaId = AreaId, PurposeId = "quality-control" }],
                SubjectTypes = [new TemplateAreaSubjectType { Id = Guid.NewGuid(),
                    TemplateAreaId = AreaId, SubjectTypeId = "sample" }],
                RoleGrants =
                [
                    Grant(AuthorRoleId, TemplateAreaAccessLevel.Author),
                    Grant(ReviewerRoleId, TemplateAreaAccessLevel.Reviewer),
                    Grant(PublisherRoleId, TemplateAreaAccessLevel.Publisher),
                ],
            };
            Context.AddRange(owner, author, reviewer, publisher, area,
                Activity(ActivityId, ActivityRevisionId, TemplateActivityRevisionStatus.Published),
                Activity(DraftActivityId, DraftActivityRevisionId, TemplateActivityRevisionStatus.Draft));
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
        }

        private TemplateActivity Activity(Guid id, Guid revisionId,
            TemplateActivityRevisionStatus status)
        {
            var activity = new TemplateActivity { Id = id, TemplateAreaId = AreaId,
                PurposeId = "quality-control", SubjectTypeId = "sample" };
            activity.Revisions.Add(new TemplateActivityRevision { Id = revisionId,
                TemplateActivityId = id, Sequence = 1, Status = status, Name = "Run analysis",
                Instructions = "Perform the controlled analysis.",
                ContentHash = new string('a', 64) });
            return activity;
        }

        private TemplateAreaRoleGrant Grant(Guid roleId, TemplateAreaAccessLevel access) => new()
            { Id = Guid.NewGuid(), TemplateAreaId = AreaId, RoleId = roleId, AccessLevel = access };
        private static Role Role(Guid id, string name) => new() { Id = id,
            Name = name.Replace(" ", string.Empty),
            NormalizedName = name.Replace(" ", string.Empty).ToUpperInvariant(), DisplayName = name };
        public ValueTask DisposeAsync() => Context.DisposeAsync();
    }
}
