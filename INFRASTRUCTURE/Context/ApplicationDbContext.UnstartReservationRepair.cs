using DOMAIN.Entities.Products.Production;
using Microsoft.EntityFrameworkCore;

namespace INFRASTRUCTURE.Context;

public partial class ApplicationDbContext
{
    public DbSet<UnstartReservationRepair> UnstartReservationRepairs => Set<UnstartReservationRepair>();

    private static void ConfigureUnstartReservationRepair(ModelBuilder builder)
    {
        var repair = builder.Entity<UnstartReservationRepair>();
        repair.HasOne<DOMAIN.Entities.Users.User>().WithMany().HasForeignKey(item => item.ActorId).OnDelete(DeleteBehavior.Restrict);
        repair.HasOne<DOMAIN.Entities.Departments.Department>().WithMany().HasForeignKey(item => item.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        repair.HasOne<DOMAIN.Entities.Materials.Material>().WithMany().HasForeignKey(item => item.MaterialId).OnDelete(DeleteBehavior.Restrict);
    }

    private void ValidateUnstartReservationRepairAudit()
    {
        if (ChangeTracker.Entries<UnstartReservationRepair>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Unstart reservation release history is append-only.");
    }
}
