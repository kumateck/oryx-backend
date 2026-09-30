using APP.Services.FullProcedures;
using DOMAIN.Entities.FullProcedures;
using DOMAIN.Entities.Roles;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;

namespace APP.Tests.FullProcedures;

file sealed class ActivityCurrentUser(Guid userId) : ICurrentUserService
{
    public Guid? UserId => userId;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public sealed partial class TemplateActivityServiceTests
{
    private sealed class Fixture : IAsyncDisposable
    {
        public ApplicationDbContext Context { get; }
        public TemplateActivityService Service { get; }
        public Guid AreaId { get; } = Guid.NewGuid();
        public Guid AuthorId { get; }
        public Guid ReviewerId { get; } = Guid.NewGuid();
        public Guid PublisherId { get; } = Guid.NewGuid();
        public Guid AuthorRoleId { get; } = Guid.NewGuid();
        public Guid ReviewerRoleId { get; } = Guid.NewGuid();
        public Guid PublisherRoleId { get; } = Guid.NewGuid();
        public Guid FormId { get; } = Guid.NewGuid();
        public Guid FormRevisionId { get; } = Guid.NewGuid();
        public Guid DraftFormId { get; } = Guid.NewGuid();
        public Guid DraftFormRevisionId { get; } = Guid.NewGuid();

        private Fixture(ApplicationDbContext context, Guid authorId)
        {
            Context = context;
            Service = new TemplateActivityService(context);
            AuthorId = authorId;
        }

        public static async Task<Fixture> Create()
        {
            var author = Guid.NewGuid();
            var context = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
                new ActivityCurrentUser(author));
            var fixture = new Fixture(context, author);
            await fixture.Seed();
            return fixture;
        }

        public CreateTemplateActivityRequest ActivityRequest() => new()
        {
            TemplateAreaId = AreaId, PurposeId = "quality-control",
            SubjectTypeId = "sample", Name = "Sample analysis",
            Instructions = "Receive the sample, capture evidence, and post the receipt.",
            Forms = [new() { FormId = FormId, RevisionId = FormRevisionId,
                Key = "analysis-form", Order = 0, Usage = TemplateActivityFormUsage.Input,
                IsRequired = true }],
            Actions =
            [
                new() { Key = "capture-result", Name = "Capture result", Order = 0,
                    ActionType = TemplateActivityActionType.CaptureEvidence,
                    RequiresIndependentChecker = true,
                    PerformerRoleIds = [AuthorRoleId], CheckerRoleIds = [ReviewerRoleId] },
                new() { Key = "post-receipt", Name = "Post receipt", Order = 1,
                    ActionType = TemplateActivityActionType.PostTransaction,
                    RequiresApproval = true, PerformerRoleIds = [AuthorRoleId],
                    ApproverRoleIds = [PublisherRoleId] },
            ],
            Resources = [new() { CapabilityId = "quality-gate", Order = 0, IsRequired = true }],
            DataBindings =
            [
                new() { Key = "sample-id", Order = 0,
                    Direction = TemplateActivityDataDirection.Input,
                    DataType = TemplateActivityDataType.EntityReference, IsRequired = true },
                new() { Key = "receipt-id", Order = 1,
                    Direction = TemplateActivityDataDirection.Output,
                    DataType = TemplateActivityDataType.EntityReference, IsRequired = true },
            ],
            CompletionRules =
            [
                new() { Order = 0, RuleType = TemplateActivityCompletionRuleType.AllActionsCompleted },
                new() { Order = 1, RuleType = TemplateActivityCompletionRuleType.FormSubmitted,
                    TargetKey = "analysis-form" },
                new() { Order = 2, RuleType = TemplateActivityCompletionRuleType.EvidenceCaptured,
                    TargetKey = "capture-result" },
                new() { Order = 3, RuleType = TemplateActivityCompletionRuleType.ApprovalGranted,
                    TargetKey = "post-receipt" },
                new() { Order = 4, RuleType = TemplateActivityCompletionRuleType.DomainReceiptRecorded,
                    TargetKey = "post-receipt" },
            ],
            Reason = "Create the controlled activity draft.",
        };

        public TemplateActivityTransitionRequest Transition(string hash) => new()
        {
            ExpectedContentHash = hash,
            Reason = "Complete the controlled activity transition.",
        };

        public async Task<TemplateActivityRevisionDto> CreateActivity()
        {
            var result = await Service.CreateAsync(ActivityRequest(), AuthorId,
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
                Capabilities = new[] { "capture-response", "approval", "material-action", "quality-gate" }
                    .Select(value => new TemplateAreaCapability { Id = Guid.NewGuid(),
                        TemplateAreaId = AreaId, CapabilityId = value }).ToList(),
                RoleGrants =
                [
                    Grant(AuthorRoleId, TemplateAreaAccessLevel.Author),
                    Grant(ReviewerRoleId, TemplateAreaAccessLevel.Reviewer),
                    Grant(PublisherRoleId, TemplateAreaAccessLevel.Publisher),
                ],
            };
            Context.AddRange(owner, author, reviewer, publisher, area,
                Form(FormId, FormRevisionId, TemplateFormRevisionStatus.Published),
                Form(DraftFormId, DraftFormRevisionId, TemplateFormRevisionStatus.Draft));
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
        }

        private TemplateForm Form(Guid id, Guid revisionId, TemplateFormRevisionStatus status)
        {
            var form = new TemplateForm { Id = id, TemplateAreaId = AreaId,
                PurposeId = "quality-control", SubjectTypeId = "sample" };
            form.Revisions.Add(new TemplateFormRevision { Id = revisionId,
                TemplateFormId = id, Sequence = 1, Status = status, Name = "Analysis form",
                Description = "Analysis", ContentHash = new string('c', 64) });
            return form;
        }

        private TemplateAreaRoleGrant Grant(Guid roleId, TemplateAreaAccessLevel access) => new()
            { Id = Guid.NewGuid(), TemplateAreaId = AreaId, RoleId = roleId, AccessLevel = access };
        private static Role Role(Guid id, string name) => new() { Id = id,
            Name = name.Replace(" ", string.Empty),
            NormalizedName = name.Replace(" ", string.Empty).ToUpperInvariant(), DisplayName = name };
        public ValueTask DisposeAsync() => Context.DisposeAsync();
    }
}
