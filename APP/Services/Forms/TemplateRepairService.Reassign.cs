using System.Data;
using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.AnalyticalTestRequests;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable
namespace APP.Services.Forms;

public sealed partial class TemplateRepairService
{
    /// <summary>
    /// Transfer only the reviewed existing assignment rows, atomically. Answered fields stay with their analyst; the normal
    /// request audit records the reason and old/new owners. Shared worksheet eligibility and locks also protect repairs.
    /// </summary>
    public async Task<Result<ReassignTestFieldsResultDto>> ReassignFields(Guid assignmentId, TemplateRepairTestKind kind, ReassignTestFieldsRequest request)
    {
        var key = kind switch { TemplateRepairTestKind.Product => PermissionKeys.CanReassignProductTest,
            TemplateRepairTestKind.RawMaterial => PermissionKeys.CanReassignRawMaterialTest, _ => PermissionKeys.CanReassignPackagingMaterialTest };
        if (!(await Grants()).Contains(key)) return Denied();
        var fields = request.Fields ?? [];
        if (fields.Count is 0 or > 500 || fields.Select(f => f.AssignmentId).Distinct().Count() != fields.Count ||
            fields.Any(f => f.NewAssigneeId == Guid.Empty || f.NewAssigneeId == f.ExpectedAssigneeId) || (request.Reason ?? "").Trim().Length is < 10 or > 1000)
            return Conflict("Choose 1–500 different question assignments, a different analyst for each, and a reason of 10–1000 characters.");
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        if (transaction is not null) await Lock();
        var header = await db.FormAssignees.AsNoTracking().SingleOrDefaultAsync(a => a.Id == assignmentId);
        if (header is null) return Error.NotFound("TestReassignment.Assignment", "The test assignment was not found.");
        var template = await Load(header.FormId);
        var test = template?.Tests.SingleOrDefault(t => t.Assignee.Id == assignmentId);
        if (test is null) return Conflict("This assignment no longer belongs to an available test.");
        if (test.Kind != kind || !(await Grants()).Contains(test.AccessKey)) return Denied();
        if (test.Blocker is not null) return Conflict(test.Blocker);
        var ids = fields.Select(f => f.AssignmentId).ToList();
        var rows = await db.FormFieldAssignees.Where(a => a.FormAssigneeId == assignmentId && ids.Contains(a.Id)).ToListAsync();
        if (rows.Count != fields.Count) return Conflict("A selected assignment is no longer on this test. Reload its assignments.");
        if (rows.All(row => row.AssigneeId == fields.Single(f => f.AssignmentId == row.Id).NewAssigneeId))
            return new ReassignTestFieldsResultDto(0, true);
        if (rows.Any(row => row.AssigneeId != fields.Single(f => f.AssignmentId == row.Id).ExpectedAssigneeId))
            return Conflict("An analyst changed since this reassignment was reviewed. Reload the test.");
        var fieldIds = rows.Select(row => row.FormFieldId).ToList();
        if (test.Response is { } response && await db.FormResponses.AnyAsync(r => r.ResponseId == response.Id && fieldIds.Contains(r.FormFieldId)))
            return Conflict("Some selected questions already have saved responses. Keep those with their analyst and select unanswered questions only.");
        var targetIds = fields.Select(f => f.NewAssigneeId).Distinct().ToList();
        var activeTargets = await db.Users.Where(u => targetIds.Contains(u.Id) && !u.IsDisabled).Select(u => u.Id).ToListAsync();
        if (activeTargets.Count != targetIds.Count) return Conflict("Choose active analysts for every selected question.");
        var startKey = kind switch { TemplateRepairTestKind.Product => PermissionKeys.CanStartProductTest,
            TemplateRepairTestKind.RawMaterial => PermissionKeys.CanStartRawMaterialTest, _ => PermissionKeys.CanStartPackagingMaterialTest };
        foreach (var targetId in targetIds)
            if (!(await CurrentPermissionSet.Load(db, targetId)).Contains(startKey))
                return Conflict("Each new analyst must currently be permitted to perform this kind of test.");
        foreach (var row in rows) row.AssigneeId = fields.Single(f => f.AssignmentId == row.Id).NewAssigneeId;
        if (kind == TemplateRepairTestKind.Product)
        {
            var atr = await db.AnalyticalTestRequests.Include(a => a.Assignees).SingleAsync(a => a.Id == test.RecordId);
            var oldIds = rows.Select(r => r.Id).ToHashSet();
            var allAnalysts = test.Assignee.FieldAssignees.Where(a => !oldIds.Contains(a.Id) && a.AssigneeId.HasValue)
                .Select(a => a.AssigneeId!.Value).Concat(targetIds).ToHashSet();
            db.Set<AnalyticalTestRequestAssignee>().RemoveRange(atr.Assignees.Where(a => !allAnalysts.Contains(a.UserId)));
            foreach (var id in allAnalysts.Where(id => atr.Assignees.All(a => a.UserId != id)))
                atr.Assignees.Add(new AnalyticalTestRequestAssignee { AnalyticalTestRequestId = atr.Id, UserId = id });
        }
        if (!(await Grants()).Contains(key)) { db.ChangeTracker.Clear(); return Denied(); }
        await db.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        return new ReassignTestFieldsResultDto(rows.Count, false);
    }

}
