using APP.Services.Reviews;
using DOMAIN.Entities.Reviews;
using DOMAIN.Entities.StpRevisions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.StpRevisions;

/// <summary>Builds a revision's Word file with its signatures, and keeps the approved one.</summary>
public static class StpRevisionFiles
{
    public static async Task<StpSignature> SignatureAsync(ApplicationDbContext context, Guid? userId, DateTime? at)
    {
        if (!userId.HasValue) return null;
        var user = await context.Users.IgnoreQueryFilters().AsNoTracking().Where(item => item.Id == userId)
            .Select(item => (item.FirstName + " " + item.LastName).Trim()).FirstOrDefaultAsync();
        var designation = await context.Employees.IgnoreQueryFilters().AsNoTracking()
            .Where(item => item.UserId == userId && item.Designation != null).Select(item => item.Designation.Name).FirstOrDefaultAsync();
        return user is null ? null : new StpSignature(user, designation, at);
    }

    /// <summary>Who confirmed the revision's review (the "Checked by" signature), when it has been confirmed.</summary>
    public static async Task<StpSignature> CheckedByAsync(ApplicationDbContext context, Guid revisionId)
    {
        var confirmed = await context.DocumentReviews.AsNoTracking()
            .Where(item => item.ModelType == ReviewDocumentTypes.StpRevision && item.ModelId == revisionId && item.Status == DocumentReviewStatus.Confirmed)
            .OrderByDescending(item => item.Submission).Select(item => new { item.ReviewedById, item.ReviewedAt }).FirstOrDefaultAsync();
        return confirmed is null ? null : await SignatureAsync(context, confirmed.ReviewedById, confirmed.ReviewedAt);
    }

    /// <summary>
    /// Generates the approved file and adds it to the context, in the approval's own transaction. The approver is the
    /// person who gave the final approval, or nobody when the revision was approved without a workflow.
    /// </summary>
    public static async Task StoreApprovedAsync(ApplicationDbContext context, StpRevision revision, Guid? approverId, DateTime approvedAt)
    {
        if (await context.StpRevisionDocuments.AnyAsync(item => item.StpRevisionId == revision.Id)) return;
        var content = await context.StpRevisions.AsNoTracking()
            .Include(item => item.Sections).Include(item => item.Tests).ThenInclude(test => test.Steps)
            .AsSplitQuery().FirstAsync(item => item.Id == revision.Id);
        var prepared = await SignatureAsync(context, revision.CreatedById, revision.CreatedAt);
        var file = StpDocxExport.Write(content, prepared, await CheckedByAsync(context, revision.Id),
            await SignatureAsync(context, approverId, approvedAt), draft: false);
        context.StpRevisionDocuments.Add(new StpRevisionDocument
        {
            Id = Guid.NewGuid(), StpRevisionId = revision.Id, FileName = file.FileName, Sha256 = file.Sha256, Content = file.Content,
            CreatedAt = approvedAt, CreatedById = approverId,
        });
    }
}
