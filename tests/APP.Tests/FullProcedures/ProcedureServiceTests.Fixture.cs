using APP.Services.FullProcedures;
using DOMAIN.Entities.FullProcedures;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Sites;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.FullProcedures;

file sealed class ProcedureCurrentUser(Guid userId) : ICurrentUserService
{
    public Guid? UserId => userId;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public sealed partial class ProcedureServiceTests
{
    private sealed class Fixture : IAsyncDisposable
    {
        public ApplicationDbContext Context { get; }
        public ProcedureService Service { get; }
        public Guid AreaId { get; } = Guid.NewGuid();
        public Guid AuthorId { get; } = Guid.NewGuid();
        public Guid ReviewerId { get; } = Guid.NewGuid();
        public Guid ApproverId { get; } = Guid.NewGuid();
        public Guid AuthorRoleId { get; } = Guid.NewGuid();
        public Guid ReviewerRoleId { get; } = Guid.NewGuid();
        public Guid ApproverRoleId { get; } = Guid.NewGuid();
        public Guid ProductId { get; } = Guid.NewGuid();
        public Guid SiteId { get; } = Guid.NewGuid();
        public Guid WorkflowId { get; } = Guid.NewGuid();
        public Guid WorkflowRevisionId { get; } = Guid.NewGuid();
        public Guid ActivityNodeId { get; } = Guid.NewGuid();

        private Fixture(ApplicationDbContext context)
        {
            Context = context;
            Service = new ProcedureService(context);
        }

        public static async Task<Fixture> Create()
        {
            var actor = Guid.NewGuid();
            var context = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
                new ProcedureCurrentUser(actor));
            var fixture = new Fixture(context);
            await fixture.Seed();
            return fixture;
        }

        public CreateProcedureRequest Request() => new()
        {
            Name = "Commercial tablet Procedure",
            Description = "Controlled manufacturing and packaging Procedure.",
            TemplateWorkflowId = WorkflowId,
            TemplateWorkflowRevisionId = WorkflowRevisionId,
            ParameterSchemaJson = "{\"type\":\"object\",\"maximum\":1.50}",
            Applicabilities = [new() { ProductId = ProductId, SiteId = SiteId,
                BatchType = ProcedureBatchType.Commercial }],
            StageScopes = [new() { WorkflowNodeKey = "mix", RecordScope =
                ProcedureRecordScope.Manufacturing }],
            Reason = "Create the controlled Procedure draft.",
        };

        public ProcedureTransitionRequest Transition(string hash) => new()
        {
            ExpectedContentHash = hash,
            Reason = "Complete the controlled Procedure transition.",
        };

        public async Task<ProcedureRevisionDto> CreateProcedure()
        {
            var result = await Service.CreateAsync(Request(), AuthorId, [AuthorRoleId],
                Guid.NewGuid());
            if (result.IsFailure) throw new InvalidOperationException(result.Error.Code);
            return result.Value;
        }

        public async Task Review(ProcedureRevisionDto revision)
        {
            var transition = Transition(revision.ContentHash);
            Assert.True((await Service.SubmitForReviewAsync(revision.Id, transition,
                AuthorId, [AuthorRoleId], Guid.NewGuid())).IsSuccess);
            Assert.True((await Service.RecordReviewAsync(revision.Id, transition,
                ReviewerId, [ReviewerRoleId], Guid.NewGuid())).IsSuccess);
        }

        public async Task Approve(ProcedureRevisionDto revision)
        {
            await Review(revision);
            Assert.True((await Service.ApproveAsync(revision.Id, Transition(revision.ContentHash),
                ApproverId, [ApproverRoleId], Guid.NewGuid())).IsSuccess);
        }

        private async Task Seed()
        {
            var owner = Role(Guid.NewGuid(), "Procedure owners");
            var author = Role(AuthorRoleId, "Procedure authors");
            var reviewer = Role(ReviewerRoleId, "Procedure reviewers");
            var approver = Role(ApproverRoleId, "Procedure approvers");
            var area = new TemplateArea
            {
                Id = AreaId, Name = "Production Procedures",
                NormalizedName = "PRODUCTION PROCEDURES", OwnerRoleId = owner.Id,
                ReviewPolicyId = "regulated-three-person", Version = 1, IsActive = true,
                RoleGrants =
                [
                    Grant(AuthorRoleId, TemplateAreaAccessLevel.Author),
                    Grant(ReviewerRoleId, TemplateAreaAccessLevel.Reviewer),
                    Grant(ApproverRoleId, TemplateAreaAccessLevel.Publisher),
                ],
            };
            var workflow = Workflow();
            Context.AddRange(owner, author, reviewer, approver, area,
                new Product { Id = ProductId, Name = "Dicnac 100", Code = "NT081" },
                new Site { Id = SiteId, Name = "Accra Plant" }, workflow);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
        }

        private TemplateWorkflow Workflow()
        {
            var workflow = new TemplateWorkflow { Id = WorkflowId, TemplateAreaId = AreaId,
                PurposeId = "production-procedure", SubjectTypeId = "product" };
            var revision = new TemplateWorkflowRevision { Id = WorkflowRevisionId,
                TemplateWorkflowId = WorkflowId, Sequence = 1,
                Status = TemplateWorkflowRevisionStatus.Published,
                Name = "Tablet workflow", Description = "Approved workflow.",
                ContentHash = new string('a', 64) };
            revision.Nodes.AddRange(
            [
                Node(Guid.NewGuid(), "start", "Start", 0, TemplateWorkflowNodeType.Start),
                Node(ActivityNodeId, "mix", "Mix materials", 1, TemplateWorkflowNodeType.Activity),
                Node(Guid.NewGuid(), "end", "End", 2, TemplateWorkflowNodeType.End),
            ]);
            workflow.Revisions.Add(revision);
            return workflow;
        }

        private TemplateWorkflowNode Node(Guid id, string key, string name, int order,
            TemplateWorkflowNodeType type) => new() { Id = id,
            TemplateWorkflowRevisionId = WorkflowRevisionId, Key = key, Name = name,
            Order = order, NodeType = type };
        private TemplateAreaRoleGrant Grant(Guid roleId, TemplateAreaAccessLevel access) => new()
            { Id = Guid.NewGuid(), TemplateAreaId = AreaId, RoleId = roleId, AccessLevel = access };
        private static Role Role(Guid id, string name) => new() { Id = id,
            Name = name.Replace(" ", string.Empty),
            NormalizedName = name.Replace(" ", string.Empty).ToUpperInvariant(), DisplayName = name };
        public ValueTask DisposeAsync() => Context.DisposeAsync();
    }
}
