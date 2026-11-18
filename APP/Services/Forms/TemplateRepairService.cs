using System.Data;
using System.Text.Json;
using APP.Services.Authorization;
using APP.Services.Formulas;
using APP.Utils;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
using SHARED.Services.Identity;

#nullable enable
namespace APP.Services.Forms;

/// <summary>
/// Repairs open test worksheets after their template gained fields. A test's analysts see and save only the fields they were
/// given when the test was assigned (<see cref="FormFieldAssignee"/>), so a field added to the template later is on the
/// worksheet's form but nobody can see or fill it, and a required one blocks submission. The repair gives each such field,
/// on selected open tests, to an analyst already on that test. Saved answers are never changed; checked, approved, rejected,
/// submitted and governed worksheets are refused. Preview and apply share one plan builder; apply rebuilds the plan under
/// locks and requires the reviewed preview token.
/// </summary>
public sealed partial class TemplateRepairService(ApplicationDbContext db, ICurrentUserService identity)
{
    private sealed record Analyst(Guid UserId, string Name, bool Active);
    private sealed record Test(FormAssignee Assignee, TemplateRepairTestKind Kind, bool Retest, Guid RecordId, string Label, string RecordStatus,
        bool RecordOpen, Response? Response, int Answered, string AnswerHash, string? Blocker, List<Analyst> Analysts)
    {
        public string AccessKey => Kind switch
        {
            TemplateRepairTestKind.Product => PermissionKeys.CanReassignProductTest,
            TemplateRepairTestKind.RawMaterial => PermissionKeys.CanReassignRawMaterialTest,
            _ => PermissionKeys.CanReassignPackagingMaterialTest,
        };
        public bool Holds(Guid fieldId) => Assignee.FieldAssignees.Any(a => a.FormFieldId == fieldId);
    }
    private sealed record Template(Form Form, List<FormField> Fields, List<Test> Tests);
    private sealed record Plan(Template Template, List<(Test Test, FormField Field, Analyst Analyst)> Selected, TemplateRepairPreviewDto Preview, string Evidence);

    private static Error Denied() => Error.Forbidden("TemplateRepair.Permission", "Your current permissions do not allow this template repair.");
    private static Error Conflict(string text) => Error.Conflict("TemplateRepair.Blocked", text);
    private Task<HashSet<string>> Grants() => CurrentPermissionSet.Load(db, identity.UserId);

    private static string AccessProblem(Test test) =>
        $"You need {test.AccessKey} to give this test's fields to its analysts.";

    /// <summary>The template, its live fields in order, and every test assigned from it (routine worksheets are not tests here).</summary>
    private async Task<Template?> Load(Guid formId)
    {
        var form = await db.Forms.AsNoTracking().SingleOrDefaultAsync(f => f.Id == formId);
        if (form is null) return null;
        var fields = await db.FormFields.AsNoTracking().Include(f => f.FormSection).Include(f => f.Question)
            .Where(f => f.FormSection.FormId == formId && f.DeletedAt == null && f.FormSection.DeletedAt == null && f.Question.DeletedAt == null)
            .ToListAsync();
        fields = fields.OrderBy(f => f.FormSection.Order).ThenBy(f => f.FormSection.CreatedAt).ThenBy(f => f.Rank).ThenBy(f => f.Id).ToList();
        var assignees = await db.FormAssignees.AsNoTracking().Include(a => a.FieldAssignees)
            .Where(a => a.FormId == formId && a.RoutineTrackId == null && (a.BatchManufacturingRecordId != null || a.MaterialBatchId != null))
            .ToListAsync();
        // A form with an approved revision pins every new worksheet to it; such worksheets change through a new revision.
        var governed = await FormRevisionSelection.EffectiveIdAsync(db, formId) is not null ||
            await FormRevisionSelection.RequiresApprovedRevisionAsync(db, formId);

        var bmrIds = assignees.Where(a => a.BatchManufacturingRecordId.HasValue).Select(a => a.BatchManufacturingRecordId!.Value).Distinct().ToList();
        var atrs = await db.AnalyticalTestRequests.AsNoTracking().Where(a => bmrIds.Contains(a.BatchManufacturingRecordId))
            .Select(a => new { a.Id, a.BatchManufacturingRecordId, a.ProductionActivityStepId, a.Stage, a.Status, a.ReleasedAt, a.ArNumber,
                a.CreatedAt, a.ProductionScheduleProductId })
            .ToListAsync();
        // Labels only. Products are filtered by department, and analysts test other departments' products, so the names are
        // read without that filter rather than through a join that would drop the request.
        var bmrNumbers = await db.BatchManufacturingRecords.IgnoreQueryFilters().AsNoTracking().Where(b => bmrIds.Contains(b.Id))
            .Select(b => new { b.Id, b.BatchNumber }).ToDictionaryAsync(b => b.Id, b => b.BatchNumber);
        var scheduleIds = atrs.Select(a => a.ProductionScheduleProductId).Distinct().ToList();
        var productNames = await db.ProductionScheduleProducts.IgnoreQueryFilters().AsNoTracking().Where(p => scheduleIds.Contains(p.Id))
            .Join(db.Products.IgnoreQueryFilters(), p => p.ProductId, product => product.Id, (p, product) => new { p.Id, product.Name })
            .ToDictionaryAsync(p => p.Id, p => p.Name);
        var batchIds = assignees.Where(a => a.MaterialBatchId.HasValue).Select(a => a.MaterialBatchId!.Value).Distinct().ToList();
        var batches = await db.MaterialBatches.AsNoTracking().Where(b => batchIds.Contains(b.Id))
            .Select(b => new { b.Id, b.BatchNumber, b.Status, b.MaterialId }).ToDictionaryAsync(b => b.Id);
        var materialIds = batches.Values.Select(b => b.MaterialId).Distinct().ToList();
        var materials = await db.Materials.IgnoreQueryFilters().AsNoTracking().Where(m => materialIds.Contains(m.Id))
            .Select(m => new { m.Id, m.Name, m.Kind }).ToDictionaryAsync(m => m.Id);
        var samples = await db.MaterialSamplings.AsNoTracking().Where(s => batchIds.Contains(s.MaterialBatchId))
            .Select(s => new { s.Id, s.MaterialBatchId, s.MaterialRetestRequestId, s.ArNumber, s.ChemicalArdId, s.SampleDate, s.CreatedAt }).ToDictionaryAsync(s => s.Id);
        var retestIds = samples.Values.Where(s => s.MaterialRetestRequestId.HasValue).Select(s => s.MaterialRetestRequestId!.Value).Distinct().ToList();
        var retests = await db.MaterialRetestRequests.AsNoTracking().Where(r => retestIds.Contains(r.Id))
            .Select(r => new { r.Id, r.Status, r.ArNumber }).ToDictionaryAsync(r => r.Id);
        var responses = await db.Responses.AsNoTracking().Where(r => r.FormId == formId && r.RoutineTrackId == null).ToListAsync();
        var responseIds = responses.Select(r => r.Id).ToList();
        var answers = await db.FormResponses.AsNoTracking().Where(a => responseIds.Contains(a.ResponseId))
            .Select(a => new { a.Id, a.ResponseId, a.FormFieldId, a.Value, a.Complies, a.UpdatedAt }).ToListAsync();
        var answered = answers.GroupBy(a => a.ResponseId).ToDictionary(g => g.Key, g => g.Select(a => a.FormFieldId).Distinct().Count());
        var answerHashes = answers.GroupBy(a => a.ResponseId).ToDictionary(g => g.Key,
            g => LegacyFormulaRepairDefinition.Hash(JsonSerializer.Serialize(g.OrderBy(a => a.Id))));
        var evidenced = (await db.ResponseApprovals.AsNoTracking().Where(a => responseIds.Contains(a.ResponseId)).Select(a => a.ResponseId).ToListAsync())
            .Concat(await db.ResponseFormulaSubmissionSets.AsNoTracking().Where(s => responseIds.Contains(s.ResponseId)).Select(s => s.ResponseId).ToListAsync())
            .Concat(await db.ResponseFormulaSnapshots.AsNoTracking().Where(s => responseIds.Contains(s.ResponseId)).Select(s => s.ResponseId).ToListAsync())
            .ToHashSet();
        var userIds = assignees.SelectMany(a => a.FieldAssignees).Where(a => a.AssigneeId.HasValue).Select(a => a.AssigneeId!.Value).Distinct().ToList();
        var users = await db.Users.IgnoreQueryFilters().AsNoTracking().Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email, u.IsDisabled, u.DeletedAt }).ToDictionaryAsync(u => u.Id);

        var tests = new List<Test>();
        foreach (var assignee in assignees)
        {
            var analysts = assignee.FieldAssignees.Where(a => a.AssigneeId.HasValue).Select(a => a.AssigneeId!.Value).Distinct().Select(id =>
            {
                var user = users.GetValueOrDefault(id);
                var name = user is null ? "" : $"{user.FirstName} {user.LastName}".Trim();
                return new Analyst(id, name.Length > 0 ? name : user?.Email ?? id.ToString(), user is { IsDisabled: false, DeletedAt: null });
            }).OrderBy(a => a.Name).ToList();
            var matching = responses.Where(r => r.MaterialBatchId == assignee.MaterialBatchId && r.MaterialSamplingId == assignee.MaterialSamplingId &&
                r.BatchManufacturingRecordId == assignee.BatchManufacturingRecordId && r.ProductionActivityStepId == assignee.ProductionActivityStepId).ToList();
            var latest = matching.OrderByDescending(r => r.ReworkSequence).ThenByDescending(r => r.CreatedAt).FirstOrDefault();
            string? blocker = null;
            TemplateRepairTestKind kind; bool retest = false; Guid recordId; string label; string status; bool open;
            if (assignee.BatchManufacturingRecordId is { } bmr)
            {
                var atr = atrs.Where(a => a.BatchManufacturingRecordId == bmr && a.ProductionActivityStepId == assignee.ProductionActivityStepId &&
                        (!assignee.Stage.HasValue || a.Stage == assignee.Stage))
                    .OrderByDescending(a => a.CreatedAt).FirstOrDefault();
                if (atr is null) continue; // An assignment whose request is gone is not an open test.
                kind = TemplateRepairTestKind.Product; recordId = atr.Id; status = atr.Status.ToString();
                label = $"{atr.ArNumber ?? "ATR without AR number"} · {productNames.GetValueOrDefault(atr.ProductionScheduleProductId) ?? "Product"} · batch {bmrNumbers.GetValueOrDefault(bmr) ?? "—"}";
                open = atr.ReleasedAt is null && atr.Status is AnalyticalTestStatus.Assigned or AnalyticalTestStatus.Testing;
                if (!open) blocker = "Only assigned or testing requests can be repaired.";
            }
            else
            {
                if (!batches.TryGetValue(assignee.MaterialBatchId!.Value, out var batch)) continue;
                var material = materials.GetValueOrDefault(batch.MaterialId);
                if (material is null) continue;
                kind = material.Kind == MaterialKind.Package ? TemplateRepairTestKind.PackagingMaterial : TemplateRepairTestKind.RawMaterial;
                var sample = assignee.MaterialSamplingId is { } sampleId ? samples.GetValueOrDefault(sampleId) : null;
                var retestRow = sample?.MaterialRetestRequestId is { } rid ? retests.GetValueOrDefault(rid) : null;
                label = $"{material.Name ?? "Material"} · batch {batch.BatchNumber ?? "—"}";
                if (retestRow is not null)
                {
                    retest = true; recordId = retestRow.Id; status = retestRow.Status.ToString();
                    var ar = string.IsNullOrWhiteSpace(retestRow.ArNumber) ? sample?.ArNumber : retestRow.ArNumber;
                    label = string.IsNullOrWhiteSpace(ar) ? $"Retest · {label}" : $"Retest {ar} · {label}";
                    open = retestRow.Status is MaterialRetestStatus.Assigned or MaterialRetestStatus.Testing;
                    if (!open) blocker = "Only assigned or testing retests can be repaired.";
                }
                else
                {
                    recordId = batch.Id; status = batch.Status.ToString();
                    open = batch.Status is BatchStatus.TestAssigned or BatchStatus.Testing;
                    if (!open) blocker = "Only material tests that are assigned or testing can be repaired.";
                }
                // Answers bind to the batch's newest sample; an assignment for an earlier sample is no longer the test.
                // The binding rule is MaterialSampleBinding's: the newest sample, when it has a chemical ARD.
                var newest = samples.Values.Where(s => s.MaterialBatchId == batch.Id).OrderByDescending(s => s.SampleDate).ThenByDescending(s => s.CreatedAt).FirstOrDefault();
                var bound = newest?.ChemicalArdId is not null ? newest.Id : (Guid?)null;
                if (open && bound != assignee.MaterialSamplingId)
                { open = false; blocker = "This assignment is for an earlier sample of the batch."; }
            }
            if (blocker is null && governed) blocker = "This template has an approved form revision. Governed worksheets change through a new form revision.";
            if (blocker is null && matching.Count(r => r.ReworkSequence == latest!.ReworkSequence) > 1)
                blocker = "This test has more than one worksheet response. Reconcile them before repairing.";
            if (blocker is null && latest is not null && (latest.CheckedAt.HasValue || latest.Approved || latest.Rejected || latest.FormRevisionId.HasValue))
                blocker = "The worksheet is checked, approved, rejected or governed.";
            if (blocker is null && latest is not null && evidenced.Contains(latest.Id))
                blocker = "The worksheet has approval, submission or governed formula evidence.";
            if (blocker is null && analysts.All(a => !a.Active))
                blocker = "No active analyst is on this test. Reassign it first.";
            tests.Add(new(assignee, kind, retest, recordId, label, status, open, latest,
                latest is null ? 0 : answered.GetValueOrDefault(latest.Id), latest is null ? "" : answerHashes.GetValueOrDefault(latest.Id) ?? "", blocker, analysts));
        }
        return new(form, fields, tests);
    }

    /// <summary>The active analyst holding most of the field's section on this test, else most of the test; ties by name.</summary>
    private static Guid? Suggest(Test test, FormField field, List<FormField> fields)
    {
        var section = fields.Where(f => f.FormSectionId == field.FormSectionId).Select(f => f.Id).ToHashSet();
        var counts = test.Analysts.Where(a => a.Active).Select(a => new
        {
            a.UserId, a.Name,
            InSection = test.Assignee.FieldAssignees.Count(fa => fa.AssigneeId == a.UserId && section.Contains(fa.FormFieldId)),
            All = test.Assignee.FieldAssignees.Count(fa => fa.AssigneeId == a.UserId),
        });
        return counts.OrderByDescending(c => c.InSection).ThenByDescending(c => c.All).ThenBy(c => c.Name).Select(c => (Guid?)c.UserId).FirstOrDefault();
    }

    /// <summary>
    /// The repair workspace for a template. Only open tests that miss fields are listed, and only those the caller may reassign.
    /// With <paramref name="formAssigneeId"/> (a test opened from its worksheet), that test is always listed, with the reason
    /// when it cannot be repaired, so it is never silently missing.
    /// </summary>
    public async Task<Result<TemplateRepairContextDto>> Context(Guid formId, Guid? formAssigneeId = null)
    {
        var grants = await Grants();
        if (!grants.Contains(PermissionKeys.CanViewTemplateRepair)) return Denied();
        var template = await Load(formId);
        if (template is null) return Error.NotFound("TemplateRepair.Template", "The template was not found.");
        var rows = new List<TemplateRepairTestDto>(); var hidden = 0;
        foreach (var test in template.Tests.OrderBy(t => t.Label))
        {
            var opened = test.Assignee.Id == formAssigneeId;
            var missing = template.Fields.Where(f => !test.Holds(f.Id)).ToList();
            if (!opened && (!test.RecordOpen || missing.Count == 0)) continue;
            var access = grants.Contains(test.AccessKey);
            if (!access && !opened) { hidden++; continue; }
            var blocker = access ? test.Blocker : AccessProblem(test);
            if (blocker is null && missing.Count == 0) blocker = "Every field of the template is already given to an analyst on this test.";
            rows.Add(new(test.Assignee.Id, test.Kind, test.Retest, test.RecordId, test.Label, test.Response?.Id, test.Answered, blocker is null, blocker,
                test.Analysts.Select(a => new TemplateRepairAnalystDto(a.UserId, a.Name,
                    test.Assignee.FieldAssignees.Count(fa => fa.AssigneeId == a.UserId))).ToList(),
                missing.Select(f => new TemplateRepairMissingFieldDto(f.Id, Suggest(test, f, template.Fields))).ToList()));
        }
        return new TemplateRepairContextDto(formId, template.Form.Name, grants.Contains(PermissionKeys.CanApplyTemplateRepair),
            template.Fields.Select(f => new TemplateRepairFieldDto(f.Id, f.FormSectionId, PlainText(f.FormSection.Name), PlainText(f.Question.Label), f.Required)).ToList(),
            rows, hidden);
    }

    private static string PlainText(string? html) => string.IsNullOrWhiteSpace(html) ? "" :
        System.Text.RegularExpressions.Regex.Replace(System.Net.WebUtility.HtmlDecode(
            System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", " ")), @"\s+", " ").Trim();

    /// <summary>Builds the reviewed plan. <paramref name="key"/> is the action's own key: view for a preview, apply for an apply.</summary>
    private async Task<Result<Plan>> Build(Guid formId, TemplateRepairCandidate candidate, string key)
    {
        var grants = await Grants();
        if (!grants.Contains(key)) return Denied();
        var inputs = candidate.Assignments ?? [];
        if (inputs.Count is 0 or > 500 || inputs.Select(i => (i.FormAssigneeId, i.FormFieldId)).Distinct().Count() != inputs.Count)
            return Conflict("Choose 1–500 distinct test fields to give to analysts.");
        var template = await Load(formId);
        if (template is null) return Error.NotFound("TemplateRepair.Template", "The template was not found.");
        var blockers = new List<string>();
        var selected = new List<(Test Test, FormField Field, Analyst Analyst)>();
        foreach (var input in inputs)
        {
            var test = template.Tests.SingleOrDefault(t => t.Assignee.Id == input.FormAssigneeId);
            if (test is null) return Conflict("A selected test is not an assigned test of this template.");
            if (!grants.Contains(test.AccessKey)) return Denied();
            if (test.Blocker is not null) blockers.Add($"{test.Label}: {test.Blocker}");
            var field = template.Fields.SingleOrDefault(f => f.Id == input.FormFieldId);
            if (field is null) { blockers.Add($"{test.Label}: a selected field is no longer on the template."); continue; }
            if (test.Holds(field.Id)) blockers.Add($"{test.Label}: {PlainText(field.Question.Label)} is already given to an analyst. Reload the repair.");
            var analyst = test.Analysts.SingleOrDefault(a => a.UserId == input.AssigneeId);
            if (analyst is not { Active: true }) { blockers.Add($"{test.Label}: give {PlainText(field.Question.Label)} to an active analyst already on this test."); continue; }
            selected.Add((test, field, analyst));
        }
        var tests = selected.GroupBy(s => s.Test.Assignee.Id).Select(g => new TemplateRepairPreviewTestDto(g.Key, g.First().Test.Label, g.First().Test.Answered,
            g.Select(s => new TemplateRepairPreviewFieldDto(s.Field.Id, PlainText(s.Field.FormSection.Name), PlainText(s.Field.Question.Label),
                s.Analyst.UserId, s.Analyst.Name)).ToList())).OrderBy(t => t.Label).ToList();
        // The token covers the actor, the candidate and the state of every selected test and the template's fields, so any
        // change between preview and apply (a save, a submission, a reassignment, a template edit) needs a fresh review.
        var touched = selected.Select(s => s.Test).DistinctBy(t => t.Assignee.Id).OrderBy(t => t.Assignee.Id).ToList();
        var evidence = JsonSerializer.Serialize(new
        {
            formId, actorId = identity.UserId, candidate = inputs.OrderBy(i => i.FormAssigneeId).ThenBy(i => i.FormFieldId),
            fields = template.Fields.OrderBy(f => f.Id).Select(f => new { f.Id, f.FormSectionId, f.QuestionId, f.Required, f.UpdatedAt, questionUpdatedAt = f.Question.UpdatedAt, f.Question.Label, sectionName = f.FormSection.Name, sectionUpdatedAt = f.FormSection.UpdatedAt }),
            tests = touched.Select(t => new
            {
                formAssigneeId = t.Assignee.Id, t.Kind, t.Retest, t.RecordId, t.RecordStatus, t.Blocker,
                fieldAssignees = t.Assignee.FieldAssignees.OrderBy(a => a.Id).Select(a => new { a.Id, a.FormFieldId, a.AssigneeId, a.UpdatedAt }),
                response = t.Response is { } r ? new { r.Id, r.CheckedAt, r.Approved, r.Rejected, r.FormRevisionId, r.ReworkSequence, r.UpdatedAt } : null,
                answeredFields = t.Answered, answerHash = t.AnswerHash,
            }),
        });
        var preview = new TemplateRepairPreviewDto(LegacyFormulaRepairDefinition.Hash(evidence), selected.Count, tests, blockers.Distinct().ToList());
        return new Plan(template, selected, preview, evidence);
    }

    public async Task<Result<TemplateRepairPreviewDto>> Preview(Guid formId, TemplateRepairCandidate candidate)
    {
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead) : null;
        var plan = await Build(formId, candidate, PermissionKeys.CanViewTemplateRepair);
        return plan.IsSuccess ? plan.Value.Preview : Result.Failure<TemplateRepairPreviewDto>(plan.Errors);
    }
}
