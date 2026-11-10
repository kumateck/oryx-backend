using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Forms;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Repository;

internal static class ResponseApprovalRoundManager
{
    internal static IReadOnlyList<ResponseApproval> Current(
        IEnumerable<ResponseApproval> approvals)
    {
        var list = approvals.ToList();
        if (list.Count == 0) return [];
        var round = list.Max(item => item.ApprovalRound);
        return [.. list.Where(item => item.ApprovalRound == round)];
    }

    internal static async Task<bool> StartAsync(
        ApplicationDbContext context,
        Guid responseId,
        IReadOnlyCollection<ApprovalStage> stages,
        Approval approval)
    {
        var existing = await context.ResponseApprovals
            .Where(item => item.ResponseId == responseId).ToListAsync();
        var current = Current(existing);
        if (current.Any(item => item.Status == ApprovalStatus.Pending)) return false;

        var response = await context.Responses.FirstOrDefaultAsync(item => item.Id == responseId)
            ?? throw new InvalidOperationException($"Response {responseId} was not found.");
        var submissionSetId = await context.ResponseFormulaSubmissionSets.AsNoTracking()
            .Where(item => item.ResponseId == responseId)
            .OrderByDescending(item => item.Sequence)
            .Select(item => (Guid?)item.Id)
            .FirstOrDefaultAsync();
        if (response.FormRevisionId.HasValue && !submissionSetId.HasValue)
            throw new InvalidOperationException(
                "A versioned response requires authoritative formula submission evidence.");
        var round = existing.Count == 0 ? 1 : existing.Max(item => item.ApprovalRound) + 1;
        if (!stages.Any(item => item.Required))
            throw new InvalidOperationException("A response approval workflow requires at least one required stage.");
        var now = DateTime.UtcNow;
        var rows = stages.Select(stage => new ResponseApproval
        {
            ResponseId = responseId,
            ApprovalId = approval.Id,
            ApprovalRound = round,
            Required = stage.Required,
            Order = stage.Order,
            UserId = stage.UserId,
            RoleId = stage.RoleId,
            FormulaSubmissionSetId = submissionSetId,
            CreatedAt = now,
        }).ToList();

        ActivateEligible(rows, now);

        response.Approved = false;
        response.Rejected = false;
        await context.ResponseApprovals.AddRangeAsync(rows);
        await context.SaveChangesAsync();
        return true;
    }
    internal static void ActivateEligible(IReadOnlyCollection<ResponseApproval> rows, DateTime now)
    {
        var nextRequired = rows.Where(row => row.Required && row.Status == ApprovalStatus.Pending)
            .Min(row => (int?)row.Order);
        if (!nextRequired.HasValue) return;

        foreach (var row in rows)
        {
            if (row.Status == ApprovalStatus.Pending && row.Order <= nextRequired.Value)
                row.ActivatedAt ??= now;
        }
    }
}
