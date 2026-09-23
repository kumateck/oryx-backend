using APP.Services.FullProcedures;
using DOMAIN.Entities.FullProcedures;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;

namespace APP.Tests.FullProcedures;

file sealed class AdoptionCurrentUser(Guid userId) : ICurrentUserService
{
    public Guid? UserId => userId;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public sealed partial class TemplateAdoptionServiceTests
{
    private sealed partial class Fixture : IAsyncDisposable
    {
        public ApplicationDbContext Context { get; }
        public TemplateAdoptionService Service { get; }
        public Guid ActorId { get; }
        public Guid SourceAreaId { get; } = Guid.NewGuid();
        public Guid TargetAreaId { get; } = Guid.NewGuid();
        public Guid SourceRoleId { get; } = Guid.NewGuid();
        public Guid TargetAuthorRoleId { get; } = Guid.NewGuid();
        public Guid SourceQuestionId { get; } = Guid.NewGuid();
        public Guid SourceQuestionRevisionId { get; } = Guid.NewGuid();
        public Guid TargetQuestionId { get; } = Guid.NewGuid();
        public Guid TargetQuestionRevisionId { get; } = Guid.NewGuid();
        public Guid SourceSectionId { get; } = Guid.NewGuid();
        public Guid SourceSectionRevisionId { get; } = Guid.NewGuid();
        public Guid TargetSectionId { get; } = Guid.NewGuid();
        public Guid TargetSectionRevisionId { get; } = Guid.NewGuid();
        public Guid SourceFormId { get; } = Guid.NewGuid();
        public Guid SourceFormRevisionId { get; } = Guid.NewGuid();
        public Guid TargetFormId { get; } = Guid.NewGuid();
        public Guid TargetFormRevisionId { get; } = Guid.NewGuid();
        public Guid SourceActivityId { get; } = Guid.NewGuid();
        public Guid SourceActivityRevisionId { get; } = Guid.NewGuid();
        public Guid TargetActivityId { get; } = Guid.NewGuid();
        public Guid TargetActivityRevisionId { get; } = Guid.NewGuid();
        public Guid SourceWorkflowId { get; } = Guid.NewGuid();
        public Guid SourceWorkflowRevisionId { get; } = Guid.NewGuid();
        public Dictionary<TemplateRevisionKind, Guid> GrantIds { get; } = [];

        private Fixture(ApplicationDbContext context, Guid actorId)
        {
            Context = context;
            ActorId = actorId;
            Service = new TemplateAdoptionService(context,
                new TemplateQuestionService(context), new TemplateSectionService(context),
                new TemplateFormService(context), new TemplateActivityService(context),
                new TemplateWorkflowService(context));
        }

        public static async Task<Fixture> Create()
        {
            var actor = Guid.NewGuid();
            var context = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
                new AdoptionCurrentUser(actor));
            var fixture = new Fixture(context, actor);
            await fixture.Seed();
            return fixture;
        }

        public AdoptTemplateRevisionRequest Request(TemplateRevisionKind kind) => new()
        {
            ExpectedGrantVersion = 2,
            Reason = "Adopt the approved source into a controlled local draft.",
            DependencyMappings = kind switch
            {
                TemplateRevisionKind.Section => [Dependency(SourceQuestionId,
                    SourceQuestionRevisionId, TargetQuestionId, TargetQuestionRevisionId)],
                TemplateRevisionKind.Form => [Dependency(SourceSectionId,
                    SourceSectionRevisionId, TargetSectionId, TargetSectionRevisionId)],
                TemplateRevisionKind.Activity => [Dependency(SourceFormId,
                    SourceFormRevisionId, TargetFormId, TargetFormRevisionId)],
                TemplateRevisionKind.Workflow => [Dependency(SourceActivityId,
                    SourceActivityRevisionId, TargetActivityId, TargetActivityRevisionId)],
                _ => [],
            },
            RoleMappings = kind == TemplateRevisionKind.Activity
                ? [new() { SourceRoleId = SourceRoleId, TargetRoleId = TargetAuthorRoleId }]
                : [],
        };

        public async Task<TemplateAdoptionDto> Adopt(TemplateRevisionKind kind)
        {
            var result = await Service.AdoptAsync(GrantIds[kind], Request(kind), ActorId,
                [TargetAuthorRoleId], Guid.NewGuid());
            if (result.IsFailure) throw new InvalidOperationException(result.Error.Code);
            return result.Value;
        }

        private static TemplateAdoptionDependencyMappingRequest Dependency(
            Guid sourceId, Guid sourceRevisionId, Guid targetId, Guid targetRevisionId) => new()
            {
                SourceDefinitionId = sourceId, SourceRevisionId = sourceRevisionId,
                TargetDefinitionId = targetId, TargetRevisionId = targetRevisionId,
            };

        public ValueTask DisposeAsync() => Context.DisposeAsync();
    }
}
