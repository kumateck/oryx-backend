using System.Data;
using System.Text.Json;
using System.Text.Json.Nodes;
using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
using SHARED.Services.Identity;

#nullable enable
namespace APP.Services.Formulas;

/// <summary>
/// Exceptional, permissioned repair of legacy formula configuration for a question, selected open tests, or both.
/// Preview and apply share one plan builder; apply rebuilds the plan under locks and requires the reviewed token.
/// </summary>
public sealed partial class QuestionFormulaRepairService(ApplicationDbContext db, ICurrentUserService identity, IFormulaCalculationClient calculation)
{
    private sealed record Impact(Question Question, QuestionOption Option, List<FormField> Fields,
        List<Response> Responses, List<FormResponse> Answers, List<LegacyFormulaConfigurationPin> Pins);
    private sealed record Selected(FormulaRepairTarget Target, AnalyticalTestRequest Atr, Response? Response, FormField Field);
    private sealed record Plan(Impact Impact, List<Selected> Selected, FormulaRepairPreviewDto Preview, string Evidence);
    private static Error Denied() => Error.Forbidden("FormulaRepair.Permission", "Your current permissions, department or field assignment do not allow this repair.");
    private static Error Conflict(string text) => Error.Conflict("FormulaRepair.Blocked", text);

    private Task<HashSet<string>> Grants() => CurrentPermissionSet.Load(db, identity.UserId);
    private static bool CanRead(HashSet<string> grants) => grants.Contains(PermissionKeys.CanViewQuestions) &&
        (grants.Contains(PermissionKeys.CanRepairQuestionFormula) || grants.Contains(PermissionKeys.CanRepairTestFormula));
    private static bool Covers(HashSet<string> grants, FormulaRepairCandidate candidate) =>
        (!candidate.UpdateQuestion || grants.Contains(PermissionKeys.CanRepairQuestionFormula)) &&
        (candidate.Targets.Count == 0 || grants.Contains(PermissionKeys.CanRepairTestFormula));

    private async Task<Impact?> Load(Guid questionId)
    {
        var question = await db.Questions.Include(q => q.Options).SingleOrDefaultAsync(q => q.Id == questionId && q.Type == QuestionType.Formula);
        if (question is null || question.Options.Count != 1) return null;
        var fields = await db.FormFields.IgnoreQueryFilters().Include(f => f.FormSection).Where(f => f.QuestionId == questionId).ToListAsync();
        var forms = fields.Select(f => f.FormSection.FormId).Distinct().ToList();
        var responses = await db.Responses.IgnoreQueryFilters().Where(r => forms.Contains(r.FormId)).ToListAsync();
        var ids = responses.Select(r => r.Id).ToList(); var fieldIds = fields.Select(f => f.Id).ToList();
        var answers = await db.FormResponses.IgnoreQueryFilters().Where(a => ids.Contains(a.ResponseId) && fieldIds.Contains(a.FormFieldId)).ToListAsync();
        var pins = await LegacyFormulaConfigurationReader.Latest(db, ids);
        return new(question, question.Options[0], fields, responses, answers, pins);
    }

    private async Task<List<string>> GlobalBlockers(Impact impact)
    {
        var blockers = new List<string>();
        if (await db.QuestionFormulaDefinitions.AnyAsync(q => q.QuestionId == impact.Question.Id)) blockers.Add("This question is governed. Use an approved revision and a reviewed response rebase.");
        if (impact.Responses.Any(r => r.FormRevisionId.HasValue)) blockers.Add("A governed response uses this question. Direct legacy repair cannot change its revision.");
        if (impact.Responses.Count > 2000) blockers.Add("This question needs a separately reviewed large-impact repair.");
        return blockers;
    }

    /// <summary>Repair permission never widens record access: department, test permissions and field assignment still apply.</summary>
    private async Task<bool> CanAccess(AnalyticalTestRequest atr, FormField field, HashSet<string> grants)
    {
        if (!grants.Contains(PermissionKeys.CanStartProductTest) ||
            !(grants.Contains(PermissionKeys.CanViewProductAnalyticalTestRequests) || grants.Contains(PermissionKeys.CanViewProductAnalyticalTestRequestsForAnalysing))) return false;
        var actor = await db.Users.Where(u => u.Id == identity.UserId && !u.IsDisabled)
            .Select(u => new { u.DepartmentId, Type = (DepartmentType?)u.Department.Type }).SingleOrDefaultAsync();
        if (actor?.DepartmentId is not { } department) return false;
        var productDepartment = await db.ProductionScheduleProducts.Where(p => p.Id == atr.ProductionScheduleProductId)
            .Select(p => p.Product.DepartmentId).SingleOrDefaultAsync();
        if (actor.Type != DepartmentType.NonProduction && productDepartment != department) return false;
        return await db.FormFieldAssignees.AnyAsync(a => a.FormFieldId == field.Id && a.AssigneeId == identity.UserId &&
            a.FormAssignee.FormId == field.FormSection.FormId && a.FormAssignee.BatchManufacturingRecordId == atr.BatchManufacturingRecordId &&
            a.FormAssignee.ProductionActivityStepId == atr.ProductionActivityStepId && a.FormAssignee.MaterialBatchId == null && a.FormAssignee.RoutineTrackId == null);
    }

    private async Task<string?> Closed(Selected selected)
    {
        var atr = selected.Atr;
        var response = selected.Response;
        var field = selected.Field;
        if (atr.ReleasedAt.HasValue || atr.Status is not (AnalyticalTestStatus.Testing or AnalyticalTestStatus.Assigned))
            return "Only assigned or testing ATRs can be repaired.";
        if (response is null)
        {
            if (await db.Responses.IgnoreQueryFilters().AnyAsync(r =>
                r.BatchManufacturingRecordId == atr.BatchManufacturingRecordId &&
                r.ProductionActivityStepId == atr.ProductionActivityStepId))
                return "An existing response for this production step does not match the test.";
            var revision = await FormRevisionSelection.ForNewResponseAsync(db, field.FormSection.FormId);
            return revision.IsFailure || revision.Value.HasValue ? "This form requires a governed response." : null;
        }
        if (response.DeletedAt.HasValue || response.CheckedAt.HasValue || response.Approved || response.Rejected || response.FormRevisionId.HasValue)
            return "The response is closed, deleted or governed.";
        if (await db.ResponseApprovals.AnyAsync(a => a.ResponseId == response.Id) ||
            await db.ResponseFormulaSubmissionSets.AnyAsync(s => s.ResponseId == response.Id) ||
            await db.ResponseFormulaSnapshots.AnyAsync(s => s.ResponseId == response.Id))
            return "The test has approval, submission or governed snapshot evidence.";
        return null;
    }

    /// <summary>Resolves ATR, response and placement on the server; a browser-supplied response ID is never trusted.</summary>
    private async Task<Selected?> Resolve(Impact impact, FormulaRepairTarget target, HashSet<string> grants, AnalyticalTestRequest? known = null)
    {
        var field = impact.Fields.SingleOrDefault(f => f.Id == target.FormFieldId && !f.DeletedAt.HasValue && !f.FormSection.DeletedAt.HasValue);
        var atr = known ?? await db.AnalyticalTestRequests.SingleOrDefaultAsync(a => a.Id == target.AtrId);
        if (field is null || atr is null) return null;
        var formIds = await db.ProductAnalyticalRawData.Where(ard => ard.Id == atr.ChemicalArdId || ard.Id == atr.MicrobialArdId).Select(ard => ard.FormId).ToListAsync();
        if (!formIds.Contains(field.FormSection.FormId)) return null;
        var responses = impact.Responses.Where(r => r.FormId == field.FormSection.FormId && !r.DeletedAt.HasValue &&
            r.BatchManufacturingRecordId == atr.BatchManufacturingRecordId && r.ProductionActivityStepId == atr.ProductionActivityStepId).ToList();
        if (responses.Count > 1 || !await CanAccess(atr, field, grants)) return null;
        return new(target, atr, responses.SingleOrDefault(), field);
    }

    public async Task<Result<FormulaRepairContextDto>> Context(Guid questionId)
    {
        var grants = await Grants();
        if (!CanRead(grants)) return Denied();
        var impact = await Load(questionId); if (impact is null) return Conflict("One live formula question with one definition is required.");
        var tests = new List<FormulaRepairTestDto>();
        var fieldIds = impact.Fields.Where(f => !f.DeletedAt.HasValue && !f.FormSection.DeletedAt.HasValue).Select(f => f.Id).ToList();
        var assignments = db.FormFieldAssignees.Where(a => a.AssigneeId == identity.UserId && fieldIds.Contains(a.FormFieldId));
        var atrs = await db.AnalyticalTestRequests.Where(atr => assignments.Any(a =>
            a.FormAssignee.BatchManufacturingRecordId == atr.BatchManufacturingRecordId &&
            a.FormAssignee.ProductionActivityStepId == atr.ProductionActivityStepId)).ToListAsync();
        var placements = impact.Fields.Where(f => !f.DeletedAt.HasValue).ToList();
        foreach (var atr in atrs.OrderBy(a => a.ArNumber))
        foreach (var field in placements)
        {
            var resolved = await Resolve(impact, new(atr.Id, field.Id), grants, atr);
            if (resolved is not { } found) continue;
            var blocker = await Closed(found);
            var section = string.IsNullOrWhiteSpace(field.FormSection.Name) ? "Formula field" : field.FormSection.Name.Trim();
            tests.Add(new(atr.Id, found.Response?.Id, field.Id, $"{atr.ArNumber ?? "ATR without AR number"} · {section}", blocker is null, blocker));
        }
        return new FormulaRepairContextDto(questionId, impact.Question.Label, impact.Option.Name, LegacyFormulaRepairDefinition.Hash(impact.Option.Name),
            grants.Contains(PermissionKeys.CanRepairQuestionFormula), grants.Contains(PermissionKeys.CanRepairTestFormula),
            impact.Fields.Select(f => f.FormSection.FormId).Distinct().Count(), impact.Responses.Count, tests, await GlobalBlockers(impact));
    }

    private static string CapturedDefinition(Impact impact, Response response, FormField field)
    {
        var pin = impact.Pins.SingleOrDefault(p => p.ResponseId == response.Id && p.FormFieldId == field.Id);
        if (pin is not null) return pin.Definition;
        var answer = LatestAnswer(impact, response.Id, field.Id);
        if (answer is not null)
        {
            var parsed = JsonNode.Parse(answer.Value) as JsonObject;
            if (parsed?["settings"] is { } settings)
            {
                var raw = settings is JsonValue scalar && scalar.TryGetValue<string>(out var text) ? text : settings.ToJsonString();
                _ = LegacyFormulaRepairDefinition.Payload(raw); return raw;
            }
        }
        return impact.Option.Name;
    }

    private static FormResponse? LatestAnswer(Impact impact, Guid responseId, Guid fieldId) => impact.Answers
        .Where(a => a.ResponseId == responseId && a.FormFieldId == fieldId && !a.DeletedAt.HasValue).OrderByDescending(a => a.CreatedAt).FirstOrDefault();

    /// <summary>
    /// Asks the calculation service to validate the exact compiled expression. Runs before any lock is taken so a slow
    /// service never holds up worksheet saves. Returns a blocker, or a failure when the service cannot be reached.
    /// </summary>
    private async Task<Result<string?>> EngineCheck(string definition)
    {
        (JsonElement Definition, string Hash) compiled;
        try { compiled = LegacyFormulaRepairDefinition.Compile(definition); }
        catch (Exception error) when (error is JsonException or ArgumentException or InvalidOperationException or FormatException)
        { return Result.Success<string?>(null); } // Static validation reports the configuration problem.
        var validation = await calculation.ValidateAsync(LegacyFormulaRepairDefinition.ValidationRequest(definition));
        if (validation.IsFailure) return Result.Failure<string?>(validation.Errors);
        var rejected = validation.Value.Status != 5 || validation.Value.DefinitionIssues.Count != 0 || validation.Value.RequestIssues.Count != 0 ||
            validation.Value.ComputedDefinitionHash != compiled.Hash;
        return Result.Success<string?>(rejected ? "The calculation service did not validate this exact expression. Correct the formula and preview again." : null);
    }

    private async Task<Result<Plan>> Build(Guid questionId, FormulaRepairCandidate candidate, string? engineBlocker)
    {
        var grants = await Grants();
        if (!CanRead(grants) || !Covers(grants, candidate)) return Denied();
        if (!candidate.UpdateQuestion && candidate.Targets.Count == 0 || candidate.Targets.Count > 100 || candidate.Targets.Distinct().Count() != candidate.Targets.Count)
            return Conflict("Choose the question and/or 1–100 distinct test fields.");
        var impact = await Load(questionId); if (impact is null) return Conflict("One live formula definition is required.");
        var blockers = await GlobalBlockers(impact);
        if (candidate.Definition == impact.Option.Name && candidate.UpdateQuestion && candidate.Targets.Count == 0)
            blockers.Add("The corrected configuration is identical to the current question. Change the expression or labels first.");
        var invalid = LegacyFormulaRepairDefinition.Validate(impact.Option.Name, candidate.Definition);
        if (invalid is not null) blockers.Add(invalid);
        else if (engineBlocker is not null) blockers.Add(engineBlocker);
        var selected = new List<Selected>(); var missing = new List<string>(); var labels = new List<FormulaRepairPreviewTestDto>();
        foreach (var target in candidate.Targets)
        {
            var row = await Resolve(impact, target, grants); if (row is null) return Denied();
            var closed = await Closed(row); if (closed is not null) blockers.Add(closed);
            var captured = row.Response is { } response ? CapturedDefinition(impact, response, row.Field) : impact.Option.Name;
            var compatibility = LegacyFormulaRepairDefinition.Validate(captured, candidate.Definition);
            if (compatibility is not null) blockers.Add(compatibility);
            var answer = row.Response is { } saved ? LatestAnswer(impact, saved.Id, row.Field.Id) : null;
            var complete = invalid is null && APP.Services.Forms.FormAnswerCompleteness.IsComplete(answer?.Value, QuestionType.Formula, candidate.Definition);
            var label = $"{row.Atr.ArNumber ?? "ATR without AR number"} · {row.Field.FormSection.Name?.Trim() ?? "Formula field"}";
            if (!complete) missing.Add($"{label}: complete and save all required readings, then recalculate.");
            labels.Add(new(target.AtrId, target.FormFieldId, label, answer is not null, captured != candidate.Definition, !complete));
            selected.Add(row);
        }
        if (selected.GroupBy(s => (s.Atr.BatchManufacturingRecordId, s.Atr.ProductionActivityStepId))
            .Any(group => group.Select(s => s.Atr.Id).Distinct().Count() > 1))
            blockers.Add("Choose only one ATR per production step.");
        if (candidate.UpdateQuestion)
            foreach (var response in impact.Responses)
                foreach (var field in impact.Fields.Where(f => f.FormSection.FormId == response.FormId))
                    try { _ = CapturedDefinition(impact, response, field); }
                    catch (Exception error) when (error is JsonException or ArgumentException)
                    { blockers.Add("An existing test has unreadable historical configuration. Reconcile it before repairing the shared question."); }
        // The token covers the actor, candidate and complete impact (including unanswered placements and approval evidence),
        // so any change between preview and apply needs a fresh review.
        var responseIds = impact.Responses.Select(r => r.Id).ToList();
        var evidence = JsonSerializer.Serialize(new { questionId, actorId = identity.UserId, candidate, question = new { impact.Option.Name, impact.Question.UpdatedAt, impact.Question.DeletedAt },
            fields = impact.Fields.OrderBy(f => f.Id).Select(f => new { f.Id, f.FormSectionId, f.FormSection.FormId, f.DeletedAt }),
            targets = selected.OrderBy(s => s.Target.AtrId).ThenBy(s => s.Field.Id).Select(s => new
            {
                s.Target, s.Atr.BatchManufacturingRecordId, s.Atr.ProductionActivityStepId, s.Atr.Status,
                s.Atr.ReleasedAt, s.Atr.ChemicalArdId, s.Atr.MicrobialArdId, ResponseId = s.Response?.Id
            }),
            responses = impact.Responses.OrderBy(r => r.Id).Select(r => new { r.Id, r.FormId, r.FormRevisionId, r.CheckedAt, r.Approved, r.Rejected, r.DeletedAt, r.UpdatedAt }),
            answers = impact.Answers.OrderBy(a => a.Id).Select(a => new { a.Id, a.ResponseId, a.FormFieldId, a.Value, a.DeletedAt, a.UpdatedAt }),
            pins = impact.Pins.OrderBy(p => p.Id), approvals = await db.ResponseApprovals.Where(a => responseIds.Contains(a.ResponseId)).OrderBy(a => a.Id).Select(a => new { a.Id, a.ResponseId, a.Status }).ToListAsync(),
            submissions = await db.ResponseFormulaSubmissionSets.Where(s => responseIds.Contains(s.ResponseId)).OrderBy(s => s.Id).Select(s => s.Id).ToListAsync(),
            snapshots = await db.ResponseFormulaSnapshots.Where(s => responseIds.Contains(s.ResponseId)).OrderBy(s => s.Id).Select(s => s.Id).ToListAsync() });
        var captures = candidate.UpdateQuestion
            ? impact.Responses.Sum(r => impact.Fields.Count(f => f.FormSection.FormId == r.FormId && !impact.Pins.Any(p => p.ResponseId == r.Id && p.FormFieldId == f.Id)))
                + selected.Count(s => s.Response is null)
            : selected.Count(s => !impact.Pins.Any(p => p.ResponseId == s.Response?.Id && p.FormFieldId == s.Field.Id));
        return new Plan(impact, selected, new(LegacyFormulaRepairDefinition.Hash(evidence), impact.Option.Name, candidate.Definition, captures,
            selected.Count, missing, blockers.Distinct().ToList(), labels), evidence);
    }

    public async Task<Result<FormulaRepairPreviewDto>> Preview(Guid questionId, FormulaRepairCandidate candidate)
    {
        var grants = await Grants();
        if (!CanRead(grants) || !Covers(grants, candidate)) return Denied();
        var engine = await EngineCheck(candidate.Definition);
        if (engine.IsFailure) return Result.Failure<FormulaRepairPreviewDto>(engine.Errors);
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead) : null;
        var plan = await Build(questionId, candidate, engine.Value);
        return plan.IsSuccess ? plan.Value.Preview : Result.Failure<FormulaRepairPreviewDto>(plan.Errors);
    }
}
