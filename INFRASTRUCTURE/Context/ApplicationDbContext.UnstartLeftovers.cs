using DOMAIN.Entities.Requisitions;
using Microsoft.EntityFrameworkCore;

namespace INFRASTRUCTURE.Context;

public partial class ApplicationDbContext
{
    public DbSet<UnstartLeftoverRepair> UnstartLeftoverRepairs => Set<UnstartLeftoverRepair>();

    private static void ConfigureUnstartLeftovers(ModelBuilder builder)
    {
        var repair = builder.Entity<UnstartLeftoverRepair>();
        repair.HasOne<DOMAIN.Entities.Users.User>().WithMany().HasForeignKey(item => item.ActorId).OnDelete(DeleteBehavior.Restrict);
        repair.HasOne<DOMAIN.Entities.Departments.Department>().WithMany().HasForeignKey(item => item.DepartmentId).OnDelete(DeleteBehavior.Restrict);
    }

    private void ValidateUnstartLeftoversAudit()
    {
        if (ChangeTracker.Entries<UnstartLeftoverRepair>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Unstart leftover repair evidence is append-only.");
    }
}
