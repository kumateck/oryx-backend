using DOMAIN.Entities.Products.Production;
using Microsoft.EntityFrameworkCore;

namespace INFRASTRUCTURE.Context;

public partial class ApplicationDbContext
{
    public DbSet<IssuedMaterialRepair> IssuedMaterialRepairs => Set<IssuedMaterialRepair>();
    public DbSet<IssuedMaterialRepairLine> IssuedMaterialRepairLines => Set<IssuedMaterialRepairLine>();
    public DbSet<IssuedMaterialRepairAllocation> IssuedMaterialRepairAllocations => Set<IssuedMaterialRepairAllocation>();

    private static void ConfigureIssuedMaterialRepair(ModelBuilder builder)
    {
        builder.Entity<ProductionActivity>().Property(item => item.UpdatedAt).IsConcurrencyToken();
        var repair = builder.Entity<IssuedMaterialRepair>();
        repair.HasIndex(item => new { item.ProductionScheduleProductId, item.Kind, item.AppliedAt });
        repair.HasOne<DOMAIN.Entities.ProductionSchedules.ProductionScheduleProduct>().WithMany()
            .HasForeignKey(item => item.ProductionScheduleProductId).OnDelete(DeleteBehavior.Restrict);
        repair.HasOne<DOMAIN.Entities.Requisitions.Requisition>().WithMany()
            .HasForeignKey(item => item.RequisitionId).OnDelete(DeleteBehavior.Restrict);
        repair.HasOne<DOMAIN.Entities.Users.User>().WithMany().HasForeignKey(item => item.ActorId).OnDelete(DeleteBehavior.Restrict);
        repair.HasMany(item => item.Lines).WithOne().HasForeignKey(item => item.IssuedMaterialRepairId).OnDelete(DeleteBehavior.Restrict);
        repair.HasMany(item => item.Allocations).WithOne().HasForeignKey(item => item.IssuedMaterialRepairId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<IssuedMaterialRepairLine>().HasIndex(item => new { item.IssuedMaterialRepairId, item.MaterialId }).IsUnique();
    }

    private void ValidateIssuedMaterialRepairAudit()
    {
        if (ChangeTracker.Entries().Any(entry =>
            entry.Entity is IssuedMaterialRepair or IssuedMaterialRepairLine or IssuedMaterialRepairAllocation or ProductionScheduleRepair
            && entry.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Issued material repair evidence is append-only.");
    }
}
