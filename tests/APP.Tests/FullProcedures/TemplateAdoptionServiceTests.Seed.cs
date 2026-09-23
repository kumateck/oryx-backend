using DOMAIN.Entities.FullProcedures;
using DOMAIN.Entities.Roles;

namespace APP.Tests.FullProcedures;

public sealed partial class TemplateAdoptionServiceTests
{
    private sealed partial class Fixture
    {
        private async Task Seed()
        {
            var sourceOwner = Role(Guid.NewGuid(), "Source owner");
            var targetOwner = Role(Guid.NewGuid(), "Target owner");
            var sourceRole = Role(SourceRoleId, "Source performer");
            var targetAuthor = Role(TargetAuthorRoleId, "Target author");
            var sourceArea = Area(SourceAreaId, "Source area", sourceOwner.Id,
                new TemplateAreaRoleGrant { Id = Guid.NewGuid(), TemplateAreaId = SourceAreaId,
                    RoleId = SourceRoleId, AccessLevel = TemplateAreaAccessLevel.Publisher });
            var targetArea = Area(TargetAreaId, "Target area", targetOwner.Id,
                new TemplateAreaRoleGrant { Id = Guid.NewGuid(), TemplateAreaId = TargetAreaId,
                    RoleId = TargetAuthorRoleId, AccessLevel = TemplateAreaAccessLevel.Author });
            var sourceQuestion = Question(SourceAreaId, SourceQuestionId,
                SourceQuestionRevisionId, 'a');
            var targetQuestion = Question(TargetAreaId, TargetQuestionId,
                TargetQuestionRevisionId, '1');
            var sourceSection = Section(SourceAreaId, SourceSectionId,
                SourceSectionRevisionId, SourceQuestionId, SourceQuestionRevisionId, 'b');
            var targetSection = Section(TargetAreaId, TargetSectionId,
                TargetSectionRevisionId, TargetQuestionId, TargetQuestionRevisionId, '2');
            var sourceForm = Form(SourceAreaId, SourceFormId, SourceFormRevisionId,
                SourceSectionId, SourceSectionRevisionId, 'c');
            var targetForm = Form(TargetAreaId, TargetFormId, TargetFormRevisionId,
                TargetSectionId, TargetSectionRevisionId, '3');
            var sourceActivity = Activity(SourceAreaId, SourceActivityId,
                SourceActivityRevisionId, SourceFormId, SourceFormRevisionId,
                SourceRoleId, 'd');
            var targetActivity = Activity(TargetAreaId, TargetActivityId,
                TargetActivityRevisionId, TargetFormId, TargetFormRevisionId,
                TargetAuthorRoleId, '4');
            var sourceWorkflow = Workflow(SourceAreaId, SourceWorkflowId,
                SourceWorkflowRevisionId, SourceActivityId, SourceActivityRevisionId, 'e');
            Context.AddRange(sourceOwner, targetOwner, sourceRole, targetAuthor,
                sourceArea, targetArea, sourceQuestion, targetQuestion,
                sourceSection, targetSection, sourceForm, targetForm,
                sourceActivity, targetActivity, sourceWorkflow);
            foreach (var item in new[]
            {
                (TemplateRevisionKind.Question, SourceQuestionId, SourceQuestionRevisionId, 'a'),
                (TemplateRevisionKind.Section, SourceSectionId, SourceSectionRevisionId, 'b'),
                (TemplateRevisionKind.Form, SourceFormId, SourceFormRevisionId, 'c'),
                (TemplateRevisionKind.Activity, SourceActivityId, SourceActivityRevisionId, 'd'),
                (TemplateRevisionKind.Workflow, SourceWorkflowId, SourceWorkflowRevisionId, 'e'),
            })
            {
                var grant = Grant(item.Item1, item.Item2, item.Item3, item.Item4);
                GrantIds[item.Item1] = grant.Id;
                Context.Add(grant);
            }
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
        }

        private static TemplateArea Area(Guid id, string name, Guid ownerRoleId,
            TemplateAreaRoleGrant grant) => new()
            {
                Id = id, Name = name, NormalizedName = name.ToUpperInvariant(),
                OwnerRoleId = ownerRoleId, ReviewPolicyId = "regulated-three-person",
                IsActive = true, Version = 1, RoleGrants = [grant],
                Purposes = [new() { Id = Guid.NewGuid(), TemplateAreaId = id,
                    PurposeId = "quality-control" }],
                SubjectTypes = [new() { Id = Guid.NewGuid(), TemplateAreaId = id,
                    SubjectTypeId = "sample" }],
            };

        private static TemplateQuestion Question(Guid areaId, Guid id, Guid revisionId, char hash)
        {
            var item = new TemplateQuestion { Id = id, TemplateAreaId = areaId,
                PurposeId = "quality-control", SubjectTypeId = "sample" };
            item.Revisions.Add(new TemplateQuestionRevision { Id = revisionId,
                TemplateQuestionId = id, Sequence = 1,
                Status = TemplateQuestionRevisionStatus.Published,
                Wording = "Record the controlled result.",
                AnswerType = TemplateQuestionAnswerType.ShortText, InputType = "text",
                ContentHash = new string(hash, 64) });
            return item;
        }

        private static TemplateSection Section(Guid areaId, Guid id, Guid revisionId,
            Guid questionId, Guid questionRevisionId, char hash)
        {
            var item = new TemplateSection { Id = id, TemplateAreaId = areaId,
                PurposeId = "quality-control", SubjectTypeId = "sample" };
            var revision = new TemplateSectionRevision { Id = revisionId,
                TemplateSectionId = id, Sequence = 1,
                Status = TemplateSectionRevisionStatus.Published,
                Title = "Controlled section", ContentHash = new string(hash, 64) };
            revision.Questions.Add(new TemplateSectionQuestion { Id = Guid.NewGuid(),
                TemplateSectionRevisionId = revisionId, TemplateQuestionId = questionId,
                TemplateQuestionRevisionId = questionRevisionId, Order = 0 });
            item.Revisions.Add(revision);
            return item;
        }

        private static TemplateForm Form(Guid areaId, Guid id, Guid revisionId,
            Guid sectionId, Guid sectionRevisionId, char hash)
        {
            var item = new TemplateForm { Id = id, TemplateAreaId = areaId,
                PurposeId = "quality-control", SubjectTypeId = "sample" };
            var revision = new TemplateFormRevision { Id = revisionId,
                TemplateFormId = id, Sequence = 1, Status = TemplateFormRevisionStatus.Published,
                Name = "Controlled form", Description = "Capture controlled evidence.",
                RequiresEvidence = true, ContentHash = new string(hash, 64) };
            revision.Sections.Add(new TemplateFormSection { Id = Guid.NewGuid(),
                TemplateFormRevisionId = revisionId, TemplateSectionId = sectionId,
                TemplateSectionRevisionId = sectionRevisionId, Order = 0, IsRequired = true });
            item.Revisions.Add(revision);
            return item;
        }

        private static Role Role(Guid id, string name) => new()
        {
            Id = id, Name = name.Replace(" ", string.Empty), DisplayName = name,
            NormalizedName = name.Replace(" ", string.Empty).ToUpperInvariant(),
        };
    }
}
