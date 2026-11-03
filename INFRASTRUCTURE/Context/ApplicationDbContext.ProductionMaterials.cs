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

    private static void ConfigureProductionMaterials(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductionMaterialConfiguration>()
            .HasIndex(item => new { item.ProductionScheduleProductId, item.Kind }).IsUnique();
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
