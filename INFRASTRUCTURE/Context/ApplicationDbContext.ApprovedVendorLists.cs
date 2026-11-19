using DOMAIN.Entities.ApprovedVendorLists;
using Microsoft.EntityFrameworkCore;

namespace INFRASTRUCTURE.Context;

public partial class ApplicationDbContext
{
    public DbSet<ApprovedVendorList> ApprovedVendorLists => Set<ApprovedVendorList>();
    public DbSet<ApprovedVendorListLine> ApprovedVendorListLines => Set<ApprovedVendorListLine>();
    public DbSet<ApprovedVendorListApproval> ApprovedVendorListApprovals => Set<ApprovedVendorListApproval>();

    private static void ConfigureApprovedVendorLists(ModelBuilder builder)
    {
        var list = builder.Entity<ApprovedVendorList>();
        list.HasQueryFilter(item => item.DeletedAt == null);
        // At most one list in preparation and one current approved list.
        list.HasIndex(item => item.Status).IsUnique().HasFilter("\"DeletedAt\" IS NULL AND \"Status\" IN (0, 1)")
            .HasDatabaseName("IX_ApprovedVendorLists_OpenStatus");
        list.HasOne(item => item.PreviousRevision).WithMany().HasForeignKey(item => item.PreviousRevisionId)
            .OnDelete(DeleteBehavior.Restrict);
        list.HasOne(item => item.CreatedBy).WithMany().HasForeignKey(item => item.CreatedById).OnDelete(DeleteBehavior.Restrict);
        list.HasMany(item => item.Lines).WithOne(item => item.ApprovedVendorList)
            .HasForeignKey(item => item.ApprovedVendorListId).OnDelete(DeleteBehavior.Cascade);
        list.HasMany(item => item.Approvals).WithOne(item => item.ApprovedVendorList)
            .HasForeignKey(item => item.ApprovedVendorListId).OnDelete(DeleteBehavior.Cascade);

        var line = builder.Entity<ApprovedVendorListLine>();
        line.HasQueryFilter(item => item.DeletedAt == null);
        line.Property(item => item.QuantityPerPack).HasPrecision(18, 4);
        line.HasIndex(item => new { item.ApprovedVendorListId, item.MaterialId, item.ManufacturerId, item.SupplierId })
            .IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        line.HasIndex(item => item.MaterialId);
        line.HasOne(item => item.Material).WithMany().HasForeignKey(item => item.MaterialId).OnDelete(DeleteBehavior.Restrict);
        line.HasOne(item => item.Manufacturer).WithMany().HasForeignKey(item => item.ManufacturerId).OnDelete(DeleteBehavior.Restrict);
        line.HasOne(item => item.Supplier).WithMany().HasForeignKey(item => item.SupplierId).OnDelete(DeleteBehavior.Restrict);

        var stage = builder.Entity<ApprovedVendorListApproval>();
        stage.HasOne(item => item.Approval).WithMany().HasForeignKey(item => item.ApprovalId).OnDelete(DeleteBehavior.Restrict);
    }
}
