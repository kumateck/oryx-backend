using DOMAIN.Entities.StpRevisions;
using Microsoft.EntityFrameworkCore;

namespace INFRASTRUCTURE.Context;

public partial class ApplicationDbContext
{
    public DbSet<StpRevision> StpRevisions => Set<StpRevision>();
    public DbSet<StpRevisionSection> StpRevisionSections => Set<StpRevisionSection>();
    public DbSet<StpRevisionTest> StpRevisionTests => Set<StpRevisionTest>();
    public DbSet<StpRevisionTestStep> StpRevisionTestSteps => Set<StpRevisionTestStep>();
    public DbSet<StpRevisionApproval> StpRevisionApprovals => Set<StpRevisionApproval>();
    public DbSet<StpRevisionDocument> StpRevisionDocuments => Set<StpRevisionDocument>();
    public DbSet<StpTestLink> StpTestLinks => Set<StpTestLink>();

    private static void ConfigureStpRevisions(ModelBuilder builder)
    {
        var revision = builder.Entity<StpRevision>();
        revision.HasQueryFilter(item => item.DeletedAt == null);
        // Per STP number: at most one current approved revision, and one revision in preparation (Pending or Draft).
        // Named indexes: two filtered indexes over the same columns.
        revision.HasIndex(item => new { item.Kind, item.StpNumber }, "IX_StpRevisions_Current").IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL AND \"Status\" = 1");
        revision.HasIndex(item => new { item.Kind, item.StpNumber }, "IX_StpRevisions_InPreparation").IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL AND \"Status\" IN (0, 4)");
        revision.HasIndex(item => new { item.Kind, item.StpNumber, item.RevisionNumber }).HasDatabaseName("IX_StpRevisions_Number");
        revision.HasOne(item => item.RevisedFrom).WithMany().HasForeignKey(item => item.RevisedFromId).OnDelete(DeleteBehavior.Restrict);
        revision.HasOne(item => item.CreatedBy).WithMany().HasForeignKey(item => item.CreatedById).OnDelete(DeleteBehavior.Restrict);
        revision.HasMany(item => item.Sections).WithOne(item => item.StpRevision)
            .HasForeignKey(item => item.StpRevisionId).OnDelete(DeleteBehavior.Cascade);
        revision.HasMany(item => item.Tests).WithOne(item => item.StpRevision)
            .HasForeignKey(item => item.StpRevisionId).OnDelete(DeleteBehavior.Cascade);
        revision.HasMany(item => item.Approvals).WithOne(item => item.StpRevision)
            .HasForeignKey(item => item.StpRevisionId).OnDelete(DeleteBehavior.Cascade);

        var section = builder.Entity<StpRevisionSection>();
        section.HasQueryFilter(item => item.DeletedAt == null);
        section.HasIndex(item => new { item.StpRevisionId, item.Kind }).IsUnique().HasFilter("\"DeletedAt\" IS NULL");

        var test = builder.Entity<StpRevisionTest>();
        test.HasQueryFilter(item => item.DeletedAt == null);
        test.HasIndex(item => new { item.StpRevisionId, item.TestKey }).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        test.HasMany(item => item.Steps).WithOne(item => item.StpRevisionTest)
            .HasForeignKey(item => item.StpRevisionTestId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<StpRevisionTestStep>().HasQueryFilter(item => item.DeletedAt == null);

        var document = builder.Entity<StpRevisionDocument>();
        // One approved file per revision.
        document.HasIndex(item => item.StpRevisionId).IsUnique();
        document.HasOne(item => item.StpRevision).WithMany().HasForeignKey(item => item.StpRevisionId).OnDelete(DeleteBehavior.Restrict);

        var link = builder.Entity<StpTestLink>();
        link.HasQueryFilter(item => item.DeletedAt == null);
        link.HasIndex(item => new { item.MaterialSpecificationId, item.FormSectionId }, "IX_StpTestLinks_Material").IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL AND \"MaterialSpecificationId\" IS NOT NULL");
        link.HasIndex(item => new { item.ProductSpecificationId, item.FormSectionId }, "IX_StpTestLinks_Product").IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL AND \"ProductSpecificationId\" IS NOT NULL");
        link.ToTable(table => table.HasCheckConstraint("CK_StpTestLinks_OneSpecification",
            "(\"MaterialSpecificationId\" IS NULL) <> (\"ProductSpecificationId\" IS NULL)"));
        link.HasOne<DOMAIN.Entities.MaterialSpecifications.MaterialSpecification>().WithMany().HasForeignKey(item => item.MaterialSpecificationId).OnDelete(DeleteBehavior.Cascade);
        link.HasOne<DOMAIN.Entities.ProductSpecifications.ProductSpecification>().WithMany().HasForeignKey(item => item.ProductSpecificationId).OnDelete(DeleteBehavior.Cascade);
        link.HasIndex(item => new { item.Kind, item.StpNumber, item.TestKey });
        link.HasOne<DOMAIN.Entities.Forms.FormSection>().WithMany().HasForeignKey(item => item.FormSectionId).OnDelete(DeleteBehavior.Cascade);

        var stage = builder.Entity<StpRevisionApproval>();
        stage.HasOne(item => item.Approval).WithMany().HasForeignKey(item => item.ApprovalId).OnDelete(DeleteBehavior.Restrict);
    }
}
