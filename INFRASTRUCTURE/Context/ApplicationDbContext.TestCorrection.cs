using DOMAIN.Entities.Forms;
using Microsoft.EntityFrameworkCore;
namespace INFRASTRUCTURE.Context;
public partial class ApplicationDbContext
{
    public DbSet<TestCorrectionReturn> TestCorrectionReturns => Set<TestCorrectionReturn>();
    private void ValidateTestCorrectionHistory()
    {
        var responseIds = ChangeTracker.Entries<Response>().Where(e => e.State is EntityState.Modified or EntityState.Deleted)
            .Select(e => e.Entity.Id).Concat(ChangeTracker.Entries<FormResponse>()
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).Select(e => e.Entity.ResponseId)).Distinct().ToList();
        if (responseIds.Count > 0 && TestCorrectionReturns.Any(h => responseIds.Contains(h.SourceResponseId)))
            throw new InvalidOperationException("Returned test evidence is immutable.");
        if (ChangeTracker.Entries<PendingCertificateChange>().Any(e => e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Certificate revision history is append-only.");
        if (ChangeTracker.Entries<TestCorrectionReturn>().Any(e => e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Test correction history is append-only.");
    }
}
