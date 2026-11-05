using DOMAIN.Entities.Materials.StockMigrations;
using DOMAIN.Entities.Products.Production;
using Microsoft.EntityFrameworkCore;

namespace INFRASTRUCTURE.Context;

public partial class ApplicationDbContext
{
    public DbSet<ReservationRepair> ReservationRepairs => Set<ReservationRepair>();

    private static void ConfigureReservationRepair(ModelBuilder builder)
    {
        var repair = builder.Entity<ReservationRepair>();
        repair.HasIndex(item => item.PreviewToken).IsUnique();
        repair.HasOne<DOMAIN.Entities.ProductionSchedules.ProductionScheduleProduct>().WithMany()
            .HasForeignKey(item => item.BatchId).OnDelete(DeleteBehavior.Restrict);
        repair.HasOne<DOMAIN.Entities.Users.User>().WithMany()
            .HasForeignKey(item => item.ActorId).OnDelete(DeleteBehavior.Restrict);
        repair.HasOne<StockMigration>().WithMany()
            .HasForeignKey(item => item.MigrationId).OnDelete(DeleteBehavior.Restrict);
    }

    private void ValidateReservationRepairAudit()
    {
        if (ChangeTracker.Entries<ReservationRepair>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Reservation repair evidence is append-only.");
    }
}
