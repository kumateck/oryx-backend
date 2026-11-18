using System.Data;
using System.Text.Json;
using APP.Services.Formulas;
using APP.Utils;
using DOMAIN.Entities.Forms;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable
namespace APP.Services.Forms;

public sealed partial class TemplateRepairService
{
    public async Task<Result<TemplateRepairResultDto>> Apply(Guid formId, ApplyTemplateRepairRequest request)
    {
        if (request.RequestId == Guid.Empty || (request.Reason ?? "").Trim().Length is < 10 or > 1000 || (request.PreviewToken ?? "").Length != 64)
            return Conflict("A reason of 10–1000 characters, a request ID and a reviewed preview are required.");
        if (!(await Grants()).Contains(PermissionKeys.CanApplyTemplateRepair)) return Denied();
        var reason = request.Reason!.Trim();
        var requestHash = LegacyFormulaRepairDefinition.Hash(JsonSerializer.Serialize(new { formId, request.Candidate, Reason = reason, request.PreviewToken }));
        var replay = await Replay(request.RequestId, requestHash);
        if (replay is not null) return replay;

        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        if (transaction is not null) await Lock();
        try
        {
            // A retry that raced the original commit returns the committed repair rather than a second one.
            replay = await Replay(request.RequestId, requestHash);
            if (replay is not null) return replay;
            var planned = await Build(formId, request.Candidate, PermissionKeys.CanApplyTemplateRepair);
            if (planned.IsFailure) return Result.Failure<TemplateRepairResultDto>(planned.Errors);
            var plan = planned.Value;
            if (plan.Preview.Blockers.Count > 0) return Conflict(string.Join(" ", plan.Preview.Blockers));
            if (plan.Preview.PreviewToken != request.PreviewToken)
                return Error.Conflict("TemplateRepair.StalePreview", "The template, its tests or your permissions changed since the preview. Review a fresh preview.");
            var now = DateTime.UtcNow;
            var added = plan.Selected.Select(s => (s.Test, s.Field, s.Analyst, Row: new FormFieldAssignee
            {
                Id = Guid.NewGuid(), FormAssigneeId = s.Test.Assignee.Id, FormFieldId = s.Field.Id, AssigneeId = s.Analyst.UserId,
                CreatedById = identity.UserId, CreatedAt = now,
            })).ToList();
            var evidence = JsonSerializer.Serialize(new
            {
                preImage = JsonDocument.Parse(plan.Evidence).RootElement, tests = plan.Preview.Tests, request.RequestId,
                added = added.Select(a => new { formFieldAssigneeId = a.Row.Id, formAssigneeId = a.Test.Assignee.Id, formFieldId = a.Field.Id, assigneeId = a.Analyst.UserId }),
            });
            db.TemplateRepairs.Add(new TemplateRepair
            {
                Id = request.RequestId, FormId = formId, ActorId = identity.UserId!.Value, Reason = reason, PreviewToken = request.PreviewToken!,
                RequestHash = requestHash, EvidenceJson = evidence, AppliedAt = now,
            });
            foreach (var a in added)
            {
                db.FormFieldAssignees.Add(a.Row);
                db.TemplateRepairAssignments.Add(new TemplateRepairAssignment
                {
                    Id = Guid.NewGuid(), RepairId = request.RequestId, FormAssigneeId = a.Test.Assignee.Id, FormFieldAssigneeId = a.Row.Id,
                    FormFieldId = a.Field.Id, AssigneeId = a.Analyst.UserId,
                });
            }
            // Permissions are rechecked immediately before the write, inside the locked transaction.
            var grants = await Grants();
            if (!grants.Contains(PermissionKeys.CanApplyTemplateRepair) || plan.Selected.Any(s => !grants.Contains(s.Test.AccessKey)))
            { db.ChangeTracker.Clear(); return Denied(); }
            await db.SaveChangesAsync();
            if (transaction is not null) await transaction.CommitAsync();
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
    private async Task<Result<TemplateRepairResultDto>?> Replay(Guid requestId, string requestHash)
    {
        var existing = await db.TemplateRepairs.AsNoTracking().SingleOrDefaultAsync(r => r.Id == requestId);
        if (existing is null) return null;
        return existing.ActorId == identity.UserId && existing.RequestHash == requestHash
            ? await Receipt(existing.Id, true)
            : Error.Conflict("TemplateRepair.RequestReused", "This request ID belongs to another repair. Review a fresh preview.");
    }

    private async Task<TemplateRepairResultDto> Receipt(Guid id, bool replay)
    {
        var rows = await db.TemplateRepairAssignments.AsNoTracking().Where(a => a.RepairId == id).ToListAsync();
        return new(id, replay, rows.Select(a => a.FormAssigneeId).Distinct().ToList(), rows.Count);
    }

    /// <summary>
    /// Serialises repairs with assignment, reassignment, worksheet creation, submission and test status changes for the brief
    /// write. Answer saves wait at most for this short transaction; the lock timeout refuses the repair rather than holding them.
    /// </summary>
    private async Task Lock()
    {
        await db.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '5s'");
        await db.Database.ExecuteSqlRawAsync("""
            LOCK TABLE "Forms", "FormSections", "FormFields", "FormAssignees", "FormFieldAssignees", "Responses", "FormResponses", "ResponseApprovals", "Questions",
              "ResponseFormulaSubmissionSets", "ResponseFormulaSnapshots",
              "FormRevisions", "AnalyticalTestRequests", "MaterialBatches", "MaterialSamplings", "MaterialRetestRequests",
              "TemplateRepairs", "TemplateRepairAssignments"
            IN SHARE ROW EXCLUSIVE MODE;
            """);
    }

    public async Task<Result<List<TemplateRepairHistoryDto>>> History(Guid formId)
    {
        if (!(await Grants()).Contains(PermissionKeys.CanViewTemplateRepair)) return Denied();
        var rows = await db.TemplateRepairs.AsNoTracking().Where(r => r.FormId == formId).OrderByDescending(r => r.AppliedAt)
            .Select(r => new { r.Id, r.ActorId, r.AppliedAt, r.Reason }).ToListAsync();
        var ids = rows.Select(r => r.Id).ToList();
        var counts = await db.TemplateRepairAssignments.AsNoTracking().Where(a => ids.Contains(a.RepairId)).GroupBy(a => a.RepairId)
            .Select(g => new { g.Key, Tests = g.Select(a => a.FormAssigneeId).Distinct().Count(), Fields = g.Count() }).ToDictionaryAsync(g => g.Key);
        var actorIds = rows.Select(r => r.ActorId).Distinct().ToList();
        var names = await db.Users.IgnoreQueryFilters().AsNoTracking().Where(u => actorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email }).ToDictionaryAsync(u => u.Id);
        return rows.Select(r =>
        {
            var name = names.TryGetValue(r.ActorId, out var user) ? $"{user.FirstName} {user.LastName}".Trim() : "";
            if (name.Length == 0) name = user?.Email ?? r.ActorId.ToString();
            var count = counts.GetValueOrDefault(r.Id);
            return new TemplateRepairHistoryDto(r.Id, r.ActorId, name, r.AppliedAt, r.Reason, count?.Tests ?? 0, count?.Fields ?? 0);
        }).ToList();
    }
}
