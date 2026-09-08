using System.Text.Json;
using APP.Services.Formulas;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public sealed partial class FormulaResponseRuntimeServiceTests
{
    private static ApplicationDbContext CreateContext(Guid actorId) => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new RuntimeTestUser(actorId));

    private static async Task<(Guid ResponseId, string PlacementKey)> SeedAsync(
        ApplicationDbContext context, Guid actorId)
    {
        const string placementKey = "assay";
        var form = new Form { Id = Guid.NewGuid(), Name = "Assay" };
        var section = new FormSection
        {
            Id = Guid.NewGuid(), FormId = form.Id, Name = "Results", Description = ""
        };
        var question = new Question
        {
            Id = Guid.NewGuid(), Label = "Assay", Type = QuestionType.Formula,
            Validation = QuestionValidationType.None
        };
        var field = new FormField
        {
            Id = Guid.NewGuid(), FormSectionId = section.Id, QuestionId = question.Id
        };
        context.AddRange(form, section, question, field);
        var formula = Formula(actorId);
        context.AddRange(formula.Definition, formula.Revision);
        await context.SaveChangesAsync();
        formula.Revision.Status = FormulaRevisionStatus.InReview;
        context.FormulaRevisionAudits.Add(Audit(formula.Revision,
            FormulaRevisionStatus.Draft, FormulaRevisionStatus.InReview, actorId));
        await context.SaveChangesAsync();
        formula.Revision.Status = FormulaRevisionStatus.Approved;
        formula.Revision.ApprovedById = actorId;
        formula.Revision.ApprovedAt = DateTime.UtcNow;
        formula.Revision.EffectiveAt = DateTime.UtcNow;
        context.FormulaRevisionAudits.Add(Audit(formula.Revision,
            FormulaRevisionStatus.InReview, FormulaRevisionStatus.Approved, actorId));
        await context.SaveChangesAsync();

        var formRevision = new FormRevision
        {
            Id = Guid.NewGuid(), FormId = form.Id, Sequence = 1,
            Status = FormRevisionStatus.Draft, ContentHash = new string('c', 64)
        };
        var fieldRevision = new FormFieldRevision
        {
            Id = Guid.NewGuid(), FormRevisionId = formRevision.Id,
            PlacementKey = placementKey, FormFieldId = field.Id, QuestionId = question.Id,
            FieldHash = new string('d', 64)
        };
        fieldRevision.FormulaConfiguration = Configuration(
            fieldRevision.Id, formula.Revision);
        formRevision.Fields.Add(fieldRevision);
        context.FormRevisions.Add(formRevision);
        await context.SaveChangesAsync();
        formRevision.Status = FormRevisionStatus.InReview;
        context.FormRevisionAudits.Add(FormAudit(formRevision,
            FormRevisionStatus.Draft, FormRevisionStatus.InReview, actorId));
        await context.SaveChangesAsync();
        formRevision.Status = FormRevisionStatus.Approved;
        formRevision.ApprovedById = actorId;
        formRevision.ApprovedAt = DateTime.UtcNow;
        context.FormRevisionAudits.Add(FormAudit(formRevision,
            FormRevisionStatus.InReview, FormRevisionStatus.Approved, actorId));
        await context.SaveChangesAsync();

        var response = new Response
        {
            Id = Guid.NewGuid(), FormId = form.Id, FormRevisionId = formRevision.Id,
            CreatedById = actorId
        };
        context.Responses.Add(response);
        context.FormResponses.Add(new FormResponse
        {
            Id = Guid.NewGuid(), ResponseId = response.Id, FormFieldId = field.Id,
            FormFieldRevisionId = fieldRevision.Id, Value = "{\"a\":1,\"b\":2}"
        });
        await context.SaveChangesAsync();
        return (response.Id, placementKey);
    }

    private static (FormulaDefinition Definition, FormulaRevision Revision) Formula(Guid actorId)
    {
        var definition = new FormulaDefinition
        {
            Id = Guid.NewGuid(), Key = $"test-{Guid.NewGuid():N}", Name = "Assay",
            PresentationPreset = "Simple", CreatedById = actorId
        };
        var revision = new FormulaRevision
        {
            Id = Guid.NewGuid(), FormulaDefinitionId = definition.Id,
            FormulaDefinition = definition, Revision = 1, Status = FormulaRevisionStatus.Draft,
            DefinitionJson = "{\"formulaLanguageVersion\":\"oryx-formula-v1\",\"numericPolicyVersion\":\"oryx-decimal-v1-approved\",\"results\":[],\"variables\":[]}",
            TestCasesJson = "[]", DefinitionHash = new string('a', 64),
            ReleaseEvidenceHash = new string('b', 64),
            FormulaLanguageVersion = "oryx-formula-v1",
            NumericPolicyVersion = "oryx-decimal-v1-approved", CreatedById = actorId
        };
        return (definition, revision);
    }

    private static ResponseFormulaSnapshot CopySnapshot(
        ResponseFormulaSnapshot source, Guid actorId,
        string? placementKey = null, bool initial = false) => new()
    {
        Id = Guid.NewGuid(), ResponseId = source.ResponseId, Response = source.Response,
        PlacementKey = placementKey ?? source.PlacementKey,
        Sequence = initial ? 1 : source.Sequence + 1,
        FormulaRevisionId = source.FormulaRevisionId,
        FormulaRevision = source.FormulaRevision,
        DefinitionHash = source.DefinitionHash,
        ConfigurationHash = source.ConfigurationHash,
        ExecutableDefinitionJson = source.ExecutableDefinitionJson,
        BindingsJson = source.BindingsJson,
        ResultTargetsJson = source.ResultTargetsJson,
        TableShapeJson = source.TableShapeJson,
        CalculationPolicyJson = source.CalculationPolicyJson,
        DisplayPolicyJson = source.DisplayPolicyJson,
        MethodReference = source.MethodReference,
        SupersedesSnapshotId = initial ? null : source.Id,
        SupersedesSnapshot = initial ? null! : source,
        Reason = initial ? FormulaSnapshotReason.Initial : FormulaSnapshotReason.ApprovedRebase,
        ApprovalReference = initial ? "test:unexpected" : "test:approved-rebase",
        CapturedById = actorId, CapturedAt = DateTime.UtcNow
    };

    private static FormFieldFormulaConfiguration Configuration(
        Guid fieldRevisionId, FormulaRevision revision) => new()
    {
        Id = Guid.NewGuid(), FormFieldRevisionId = fieldRevisionId,
        FormulaRevisionId = revision.Id, FormulaRevision = revision,
        BindingsJson = "[{\"variableKey\":\"a\",\"source\":{\"sourceType\":0,\"reference\":\"local:a\"}},{\"variableKey\":\"b\",\"source\":{\"sourceType\":0,\"reference\":\"local:b\"}}]",
        ResultTargetsJson = "[{\"resultKey\":\"result\",\"targetKey\":\"assay:result\",\"dataType\":0,\"valueShape\":0,\"unit\":null}]",
        DisplayPolicyJson = "{}", ConfigurationHash = new string('e', 64)
    };

    private static FormulaRevisionAudit Audit(
        FormulaRevision revision, FormulaRevisionStatus prior,
        FormulaRevisionStatus current, Guid actorId) => new()
    {
        Id = Guid.NewGuid(), FormulaRevisionId = revision.Id, PriorStatus = prior,
        NewStatus = current, Action = "Test", Reason = "Controlled test transition.",
        DefinitionHash = revision.DefinitionHash, ActorId = actorId,
        OccurredAt = DateTime.UtcNow, CorrelationId = Guid.NewGuid()
    };

    private static FormRevisionAudit FormAudit(
        FormRevision revision, FormRevisionStatus prior,
        FormRevisionStatus current, Guid actorId) => new()
    {
        Id = Guid.NewGuid(), FormRevisionId = revision.Id, PriorStatus = prior,
        NewStatus = current, Action = "Test", Reason = "Controlled test transition.",
        ContentHash = revision.ContentHash, ActorId = actorId,
        OccurredAt = DateTime.UtcNow, CorrelationId = Guid.NewGuid()
    };
}
