using DOMAIN.Entities.Products.Production;
using Microsoft.EntityFrameworkCore;

namespace INFRASTRUCTURE.Context;

public partial class ApplicationDbContext
{
    public DbSet<ProductionUnstartRepair> ProductionUnstartRepairs => Set<ProductionUnstartRepair>();

    private static void ConfigureProductionUnstart(ModelBuilder builder)
    {
        builder.Entity<ProductionActivity>().HasIndex(item => item.ProductionScheduleProductId).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        foreach (var type in new[] { typeof(ProductionActivity), typeof(ProductionActivityStep), typeof(BatchManufacturingRecord), typeof(BatchPackagingRecord) })
            builder.Entity(type).Property("DeletedAt").IsConcurrencyToken();
        var repair = builder.Entity<ProductionUnstartRepair>();
        repair.HasIndex(item => item.ActivityId).IsUnique();
        repair.HasOne<ProductionActivity>().WithMany().HasForeignKey(item => item.ActivityId).OnDelete(DeleteBehavior.Restrict);
        repair.HasOne<DOMAIN.Entities.ProductionSchedules.ProductionScheduleProduct>().WithMany().HasForeignKey(item => item.BatchId).OnDelete(DeleteBehavior.Restrict);
        repair.HasOne<DOMAIN.Entities.Users.User>().WithMany().HasForeignKey(item => item.ActorId).OnDelete(DeleteBehavior.Restrict);
    }

    private void ValidateProductionUnstartAudit()
    {
        if (ChangeTracker.Entries<ProductionUnstartRepair>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Production unstart repair evidence is append-only.");
    }
}
