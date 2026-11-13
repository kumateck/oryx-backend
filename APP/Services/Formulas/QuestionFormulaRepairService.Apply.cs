using System.Data;
using System.Text.Json;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Formulas;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable
namespace APP.Services.Formulas;

public sealed partial class QuestionFormulaRepairService
{
    public async Task<Result<FormulaRepairResultDto>> Apply(Guid questionId, ApplyFormulaRepairRequest request)
    {
        if (request.RequestId == Guid.Empty || request.Reason.Trim().Length is < 10 or > 1000 || request.PreviewToken.Length != 64)
            return Conflict("A reason of 10–1000 characters, a request ID and a reviewed preview are required.");
        var grants = await Grants();
        if (!CanRead(grants) || !Covers(grants, request.Candidate)) return Denied();
        var requestHash = LegacyFormulaRepairDefinition.Hash(JsonSerializer.Serialize(new { questionId, request.Candidate, Reason = request.Reason.Trim(), request.PreviewToken }));
        var replay = await Replay(request.RequestId, requestHash);
        if (replay is not null) return replay;
        // Validate with the calculation service before locking; the expression alone decides this result.
        var engine = await EngineCheck(request.Candidate.Definition);
        if (engine.IsFailure) return Result.Failure<FormulaRepairResultDto>(engine.Errors);

        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        if (transaction is not null) await Lock(questionId);
        try
        {
            // A retry that raced the original commit returns the committed repair rather than a second one.
            replay = await Replay(request.RequestId, requestHash);
            if (replay is not null) return replay;
            var planned = await Build(questionId, request.Candidate, engine.Value);
            if (planned.IsFailure) return Result.Failure<FormulaRepairResultDto>(planned.Errors);
            var plan = planned.Value;
            if (plan.Preview.Blockers.Count > 0) return Conflict(string.Join(" ", plan.Preview.Blockers));
            if (plan.Preview.PreviewToken != request.PreviewToken) return Error.Conflict("FormulaRepair.StalePreview", "The question, tests or permissions changed since the preview. Review a fresh preview.");
            var now = DateTime.UtcNow;
            var invalidated = plan.Selected.Where(s => s.Response is not null)
                .Select(s => LatestAnswer(plan.Impact, s.Response!.Id, s.Field.Id)).OfType<FormResponse>()
                .Select(a => new { answerId = a.Id, a.ResponseId, a.FormFieldId, inputHash = LegacyFormulaConfigurationReader.SafeInputHash(a.Value) }).ToList();
            var evidence = JsonSerializer.Serialize(new { preImage = JsonDocument.Parse(plan.Evidence).RootElement, targets = plan.Preview.Tests,
                plan.Preview.MissingInputs, invalidatedResults = invalidated, request.RequestId,
                definitionBeforeHash = LegacyFormulaRepairDefinition.Hash(plan.Impact.Option.Name), definitionAfterHash = LegacyFormulaRepairDefinition.Hash(request.Candidate.Definition) });
            db.QuestionFormulaRepairs.Add(new() { Id = request.RequestId, QuestionId = questionId, ActorId = identity.UserId!.Value,
                UpdatedQuestion = request.Candidate.UpdateQuestion, Reason = request.Reason.Trim(), PreviewToken = request.PreviewToken, RequestHash = requestHash,
                DefinitionBefore = plan.Impact.Option.Name, DefinitionAfter = request.Candidate.Definition, EvidenceJson = evidence, AppliedAt = now });
            foreach (var response in plan.Impact.Responses)
            foreach (var field in plan.Impact.Fields.Where(f => f.FormSection.FormId == response.FormId))
            {
                var selected = plan.Selected.Any(s => s.Response?.Id == response.Id && s.Field.Id == field.Id);
                if (!request.Candidate.UpdateQuestion && !selected) continue;
                var previous = plan.Impact.Pins.SingleOrDefault(p => p.ResponseId == response.Id && p.FormFieldId == field.Id);
                // Capture what the test used before this repair, so a shared-question change never alters its history.
                if (previous is null)
                    db.LegacyFormulaConfigurationPins.Add(Pin(response.Id, field.Id, 1, request.RequestId, CapturedDefinition(plan.Impact, response, field), false, now));
                if (selected) db.LegacyFormulaConfigurationPins.Add(Pin(response.Id, field.Id, (previous?.Sequence ?? 1) + 1,
                    request.RequestId, request.Candidate.Definition, true, now));
            }
            foreach (var targets in plan.Selected.Where(s => s.Response is null).GroupBy(s => s.Atr.Id))
            {
                var first = targets.First();
                var draft = new Response
                {
                    Id = Guid.NewGuid(), FormId = first.Field.FormSection.FormId,
                    BatchManufacturingRecordId = first.Atr.BatchManufacturingRecordId,
                    ProductionActivityStepId = first.Atr.ProductionActivityStepId,
                    CreatedById = identity.UserId,
                };
                db.Responses.Add(draft);
                var fields = request.Candidate.UpdateQuestion
                    ? plan.Impact.Fields.Where(field => field.FormSection.FormId == draft.FormId).ToList()
                    : targets.Select(target => target.Field).ToList();
                foreach (var field in fields)
                    db.LegacyFormulaConfigurationPins.Add(Pin(draft.Id, field.Id, 1, request.RequestId, plan.Impact.Option.Name, false, now));
                foreach (var target in targets)
                {
                    db.LegacyFormulaConfigurationPins.Add(Pin(draft.Id, target.Field.Id, 2, request.RequestId, request.Candidate.Definition, true, now));
                }
            }
            if (request.Candidate.UpdateQuestion)
            {
                plan.Impact.Option.Name = request.Candidate.Definition;
                plan.Impact.Option.LastUpdatedById = identity.UserId; plan.Impact.Option.UpdatedAt = now;
                plan.Impact.Question.LastUpdatedById = identity.UserId; plan.Impact.Question.UpdatedAt = now;
            }
            // Permissions are rechecked immediately before the write, inside the locked transaction.
            grants = await Grants();
            if (!CanRead(grants) || !Covers(grants, request.Candidate))
            { db.ChangeTracker.Clear(); return Denied(); }
            foreach (var target in plan.Selected)
                if (!await CanAccess(target.Atr, target.Field, grants))
                { db.ChangeTracker.Clear(); return Denied(); }
            await db.SaveChangesAsync(); if (transaction is not null) await transaction.CommitAsync();
            return await Receipt(request.RequestId, false);
        }
        catch
        {
            if (transaction is not null) await transaction.RollbackAsync();
            db.ChangeTracker.Clear();
            throw;
        }
    }

    /// <summary>Request IDs are scoped to the actor and exact payload: identical retries return the committed repair.</summary>
    private async Task<Result<FormulaRepairResultDto>?> Replay(Guid requestId, string requestHash)
    {
        var existing = await db.QuestionFormulaRepairs.AsNoTracking().SingleOrDefaultAsync(r => r.Id == requestId);
        if (existing is null) return null;
        return existing.ActorId == identity.UserId && existing.RequestHash == requestHash
            ? await Receipt(existing.Id, true)
            : Error.Conflict("FormulaRepair.RequestReused", "This request ID belongs to another repair. Review a fresh preview.");
    }

    private async Task<FormulaRepairResultDto> Receipt(Guid id, bool replay)
    {
        var pins = await db.LegacyFormulaConfigurationPins.AsNoTracking().Where(p => p.RepairId == id).ToListAsync();
        var fieldIds = pins.Select(p => p.FormFieldId).Distinct().ToList();
        var questionId = await db.QuestionFormulaRepairs.Where(r => r.Id == id).Select(r => r.QuestionId).SingleAsync();
        var formIds = await db.FormFields.IgnoreQueryFilters().Where(f => fieldIds.Contains(f.Id) || fieldIds.Count == 0 && f.QuestionId == questionId)
            .Select(f => f.FormSection.FormId).Distinct().ToListAsync();
        return new(id, replay, formIds, pins.Where(p => p.RequiresRecalculation).Select(p => p.ResponseId).Distinct().ToList());
    }

    private static LegacyFormulaConfigurationPin Pin(Guid response, Guid field, int sequence, Guid repair, string definition, bool recalculate, DateTime now) => new()
    { Id = Guid.NewGuid(), ResponseId = response, FormFieldId = field, Sequence = sequence, RepairId = repair, Definition = definition,
        DefinitionHash = LegacyFormulaRepairDefinition.Hash(definition), RequiresRecalculation = recalculate, CapturedAt = now };

    /// <summary>
    /// The question lock serialises repairs with formula draft saves (which hold it shared), then table locks conflict
    /// with question edits, submission and approval. Identity tables are not locked: permissions are re-read before the write.
    /// </summary>
    private async Task Lock(Guid questionId)
    {
        await db.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '5s'");
        await LegacyFormulaConfigurationReader.LockQuestion(db, questionId, shared: false);
        await db.Database.ExecuteSqlRawAsync("""
            LOCK TABLE "Questions", "QuestionOptions", "FormFields", "FormSections", "Responses", "FormResponses",
              "Forms", "FormRevisions", "QuestionFormulaDefinitions", "FormFieldFormulaConfigurations", "ResponseFormulaSnapshots", "ResponseFormulaSubmissionSets",
              "ResponseApprovals", "AnalyticalTestRequests", "FormFieldAssignees", "FormAssignees", "QuestionFormulaRepairs",
              "LegacyFormulaConfigurationPins", "LegacyFormulaRepairEvaluations"
            IN SHARE ROW EXCLUSIVE MODE;
            """);
    }

    public async Task<Result<List<FormulaRepairHistoryDto>>> History(Guid questionId)
    {
        if (!CanRead(await Grants())) return Denied();
        var rows = await db.QuestionFormulaRepairs.AsNoTracking().Where(r => r.QuestionId == questionId).OrderByDescending(r => r.AppliedAt)
            .Select(r => new { r.Id, r.ActorId, r.AppliedAt, r.Reason, r.DefinitionBefore, r.DefinitionAfter, r.UpdatedQuestion }).ToListAsync();
        var actorIds = rows.Select(r => r.ActorId).Distinct().ToList();
        var names = await db.Users.IgnoreQueryFilters().Where(u => actorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email }).ToDictionaryAsync(u => u.Id);
        var pins = await db.LegacyFormulaConfigurationPins.AsNoTracking().Where(p => rows.Select(r => r.Id).Contains(p.RepairId))
            .GroupBy(p => p.RepairId).Select(g => new { g.Key, Repaired = g.Count(p => p.RequiresRecalculation), Captured = g.Count(p => !p.RequiresRecalculation) })
            .ToDictionaryAsync(g => g.Key);
        return rows.Select(r =>
        {
            var name = names.TryGetValue(r.ActorId, out var user) ? $"{user.FirstName} {user.LastName}".Trim() : "";
            if (name.Length == 0) name = user?.Email ?? r.ActorId.ToString();
            var counts = pins.TryGetValue(r.Id, out var count) ? count : null;
            return new FormulaRepairHistoryDto(r.Id, r.ActorId, name, r.AppliedAt, r.Reason, r.DefinitionBefore, r.DefinitionAfter,
                r.UpdatedQuestion, counts?.Repaired ?? 0, counts?.Captured ?? 0);
        }).ToList();
    }
}
