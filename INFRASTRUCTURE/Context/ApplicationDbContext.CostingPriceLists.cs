using DOMAIN.Entities.Costing;
using Microsoft.EntityFrameworkCore;

namespace INFRASTRUCTURE.Context;

public partial class ApplicationDbContext
{
    public DbSet<CostingPriceList> CostingPriceLists => Set<CostingPriceList>();
    public DbSet<CostingPriceListLine> CostingPriceListLines => Set<CostingPriceListLine>();
    public DbSet<CostingPriceListApproval> CostingPriceListApprovals => Set<CostingPriceListApproval>();

    private static void ConfigureCostingPriceLists(ModelBuilder builder)
    {
        var list = builder.Entity<CostingPriceList>();
        list.HasQueryFilter(item => item.DeletedAt == null);
        list.Property(item => item.Version).IsConcurrencyToken();
        list.HasIndex(item => new { item.Division, item.RevisionNumber }).IsUnique();
        list.HasIndex(item => item.Division).IsUnique().HasFilter("\"DeletedAt\" IS NULL AND \"Status\" IN (0, 4)")
            .HasDatabaseName("IX_CostingPriceLists_OneOpenPerDivision");
        list.HasIndex(item => new { item.Division, item.Status, item.EffectiveFrom });
        list.HasOne(item => item.PreviousRevision).WithMany().HasForeignKey(item => item.PreviousRevisionId).OnDelete(DeleteBehavior.Restrict);
        list.HasOne(item => item.CreatedBy).WithMany().HasForeignKey(item => item.CreatedById).OnDelete(DeleteBehavior.Restrict);
        list.HasMany(item => item.Lines).WithOne(item => item.CostingPriceList).HasForeignKey(item => item.CostingPriceListId).OnDelete(DeleteBehavior.Cascade);
        list.HasMany(item => item.Approvals).WithOne(item => item.CostingPriceList).HasForeignKey(item => item.CostingPriceListId).OnDelete(DeleteBehavior.Cascade);
        var line = builder.Entity<CostingPriceListLine>();
        line.HasQueryFilter(item => item.DeletedAt == null);
        line.Property(item => item.Price).HasPrecision(29, 10);
        line.HasIndex(item => new { item.CostingPriceListId, item.ProductId, item.ProductPackingId }).IsUnique();
        line.HasIndex(item => item.ProductId);
        builder.Entity<CostingPriceListApproval>().HasOne(item => item.Approval).WithMany()
            .HasForeignKey(item => item.ApprovalId).OnDelete(DeleteBehavior.Restrict);
    }
}
