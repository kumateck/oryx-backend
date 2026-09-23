using DOMAIN.Entities.FullProcedures;

namespace APP.Tests.FullProcedures;

public sealed partial class TemplateAdoptionServiceTests
{
    private sealed partial class Fixture
    {
        private static TemplateActivity Activity(Guid areaId, Guid id, Guid revisionId,
            Guid formId, Guid formRevisionId, Guid roleId, char hash)
        {
            var item = new TemplateActivity { Id = id, TemplateAreaId = areaId,
                PurposeId = "quality-control", SubjectTypeId = "sample" };
            var revision = new TemplateActivityRevision { Id = revisionId,
                TemplateActivityId = id, Sequence = 1,
                Status = TemplateActivityRevisionStatus.Published,
                Name = "Controlled activity", Instructions = "Perform the controlled work.",
                ContentHash = new string(hash, 64) };
            revision.Forms.Add(new TemplateActivityFormBinding { Id = Guid.NewGuid(),
                TemplateActivityRevisionId = revisionId, TemplateFormId = formId,
                TemplateFormRevisionId = formRevisionId, Key = "evidence", Order = 0,
                Usage = TemplateActivityFormUsage.Evidence, IsRequired = true });
            var action = new TemplateActivityAction { Id = Guid.NewGuid(),
                TemplateActivityRevisionId = revisionId, Key = "perform", Name = "Perform",
                Order = 0, ActionType = TemplateActivityActionType.Perform };
            action.Roles.Add(new TemplateActivityActionRole { Id = Guid.NewGuid(),
                TemplateActivityActionId = action.Id, RoleId = roleId,
                RoleKind = TemplateActivityActionRoleKind.Performer });
            revision.Actions.Add(action);
            revision.CompletionRules.Add(new TemplateActivityCompletionRule
            {
                Id = Guid.NewGuid(), TemplateActivityRevisionId = revisionId, Order = 0,
                RuleType = TemplateActivityCompletionRuleType.AllActionsCompleted,
            });
            revision.CompletionRules.Add(new TemplateActivityCompletionRule
            {
                Id = Guid.NewGuid(), TemplateActivityRevisionId = revisionId, Order = 1,
                RuleType = TemplateActivityCompletionRuleType.FormSubmitted,
                TargetKey = "evidence",
            });
            item.Revisions.Add(revision);
            return item;
        }

        private static TemplateWorkflow Workflow(Guid areaId, Guid id, Guid revisionId,
            Guid activityId, Guid activityRevisionId, char hash)
        {
            var item = new TemplateWorkflow { Id = id, TemplateAreaId = areaId,
                PurposeId = "quality-control", SubjectTypeId = "sample" };
            var revision = new TemplateWorkflowRevision { Id = revisionId,
                TemplateWorkflowId = id, Sequence = 1,
                Status = TemplateWorkflowRevisionStatus.Published,
                Name = "Controlled workflow", Description = "Run the controlled activity.",
                ContentHash = new string(hash, 64) };
            var start = Node(revisionId, "start", "Start", 0, TemplateWorkflowNodeType.Start);
            var activity = Node(revisionId, "activity", "Activity", 1,
                TemplateWorkflowNodeType.Activity);
            activity.TemplateActivityId = activityId;
            activity.TemplateActivityRevisionId = activityRevisionId;
            var end = Node(revisionId, "end", "End", 2, TemplateWorkflowNodeType.End);
            revision.Nodes.AddRange([start, activity, end]);
            revision.Edges.AddRange([
                Edge(revisionId, start.Id, activity.Id),
                Edge(revisionId, activity.Id, end.Id),
            ]);
            revision.Layouts.AddRange([
                Layout(revisionId, start.Id, 0, 0),
                Layout(revisionId, activity.Id, 200, 0),
                Layout(revisionId, end.Id, 400, 0),
            ]);
            item.Revisions.Add(revision);
            return item;
        }

        private TemplateSharingGrant Grant(TemplateRevisionKind kind,
            Guid definitionId, Guid revisionId, char hash) => new()
            {
                Id = Guid.NewGuid(), SourceAreaId = SourceAreaId,
                TargetAreaId = TargetAreaId, RequestedByAreaId = SourceAreaId,
                TemplateKind = kind, DefinitionId = definitionId, RevisionId = revisionId,
                RevisionContentHash = new string(hash, 64), PurposeId = "quality-control",
                SubjectTypeId = "sample", Status = TemplateSharingGrantStatus.Active,
                Version = 2, RequestedById = Guid.NewGuid(), RequestedAt = DateTime.UtcNow,
                DecidedById = Guid.NewGuid(), DecidedAt = DateTime.UtcNow,
            };

        private static TemplateWorkflowNode Node(Guid revisionId, string key, string name,
            int order, TemplateWorkflowNodeType type) => new()
            {
                Id = Guid.NewGuid(), TemplateWorkflowRevisionId = revisionId,
                Key = key, Name = name, Order = order, NodeType = type,
            };

        private static TemplateWorkflowEdge Edge(Guid revisionId, Guid source, Guid target) => new()
        {
            Id = Guid.NewGuid(), TemplateWorkflowRevisionId = revisionId,
            SourceNodeId = source, TargetNodeId = target, Order = 0,
        };

        private static TemplateWorkflowNodeLayout Layout(Guid revisionId, Guid nodeId,
            double x, double y) => new()
            {
                Id = Guid.NewGuid(), TemplateWorkflowRevisionId = revisionId,
                TemplateWorkflowNodeId = nodeId, PositionX = x, PositionY = y,
            };
    }
}
