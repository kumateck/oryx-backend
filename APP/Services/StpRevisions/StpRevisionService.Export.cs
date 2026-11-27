using APP.Services.Authorization;
using DOMAIN.Entities.StpRevisions;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.StpRevisions;

/// <summary>A revision's Word file: the stored approved file, or a draft file generated now and marked as not approved.</summary>
public sealed record StpRevisionFile(byte[] Content, string FileName, string Sha256, bool Approved);

public sealed partial class StpRevisionService
{
    public async Task<Result<StpRevisionFile>> Export(Guid id, Guid actorId)
    {
        var revision = await context.StpRevisions.AsNoTracking()
            .Include(item => item.Sections).Include(item => item.Tests).ThenInclude(test => test.Steps)
            .AsSplitQuery().FirstOrDefaultAsync(item => item.Id == id);
        if (revision is null) return NotFound;
        var refused = await Refuse(StpRevisionTask.Export, revision.Kind, actorId);
        if (refused is not null) return refused;
        var stored = await context.StpRevisionDocuments.AsNoTracking().FirstOrDefaultAsync(item => item.StpRevisionId == id);
        if (stored is not null) return new StpRevisionFile(stored.Content, stored.FileName, stored.Sha256, true);
        // An approved revision always has its file: it is stored in the approval transaction.
        if (revision.Status is StpRevisionStatus.Approved or StpRevisionStatus.Superseded)
            return Error.Conflict("StpRevision.File", "The approved file of this revision is missing. Contact support.");
        var prepared = await StpRevisionFiles.SignatureAsync(context, revision.CreatedById, revision.CreatedAt);
        var file = StpDocxExport.Write(revision, prepared, await StpRevisionFiles.CheckedByAsync(context, id), null, draft: true);
        return new StpRevisionFile(file.Content, file.FileName, file.Sha256, false);
    }
}
