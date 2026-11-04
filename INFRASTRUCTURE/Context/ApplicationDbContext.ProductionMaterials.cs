using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace INFRASTRUCTURE.Context;

public partial class ApplicationDbContext
{
    public DbSet<ProductionMaterialConfiguration> ProductionMaterialConfigurations => Set<ProductionMaterialConfiguration>();
    public DbSet<ProductionMaterialRepair> ProductionMaterialRepairs => Set<ProductionMaterialRepair>();
    public DbSet<ProductionScheduleRepair> ProductionScheduleRepairs => Set<ProductionScheduleRepair>();

    private static void ConfigureProductionMaterials(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductionScheduleRepair>().HasOne<ProductionSchedule>().WithMany()
            .HasForeignKey(item => item.ScheduleId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProductionScheduleRepair>().HasOne<DOMAIN.Entities.Products.Product>().WithMany()
            .HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProductionScheduleRepair>().HasOne<User>().WithMany()
            .HasForeignKey(item => item.ActorId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProductionScheduleRepair>().HasIndex(item => new { item.ScheduleId, item.ProductId, item.AppliedAt });
        modelBuilder.Entity<ProductionMaterialConfiguration>()
            .HasIndex(item => new { item.ProductionScheduleProductId, item.Kind }).IsUnique();
        modelBuilder.Entity<ProductionMaterialConfiguration>().HasQueryFilter(item => item.DeletedAt == null);
        modelBuilder.Entity<ProductionMaterialConfiguration>().HasOne<ProductionScheduleProduct>().WithMany()
            .HasForeignKey(item => item.ProductionScheduleProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProductionMaterialRepair>().HasOne<ProductionScheduleProduct>().WithMany()
            .HasForeignKey(item => item.ProductionScheduleProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProductionMaterialRepair>().HasOne<User>().WithMany()
            .HasForeignKey(item => item.ActorId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<MaterialBatchReservedQuantity>()
            .HasQueryFilter(item => item.DeletedAt == null && item.ReleasedAt == null);
    }
}
