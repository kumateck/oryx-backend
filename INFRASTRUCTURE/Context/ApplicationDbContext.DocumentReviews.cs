using DOMAIN.Entities.Reviews;
using Microsoft.EntityFrameworkCore;

namespace INFRASTRUCTURE.Context;

public partial class ApplicationDbContext
{
    public DbSet<ReviewConfiguration> ReviewConfigurations => Set<ReviewConfiguration>();
    public DbSet<ReviewConfigurationAssignee> ReviewConfigurationAssignees => Set<ReviewConfigurationAssignee>();
    public DbSet<DocumentReview> DocumentReviews => Set<DocumentReview>();
    public DbSet<DocumentReviewEvent> DocumentReviewEvents => Set<DocumentReviewEvent>();

    private static void ConfigureDocumentReviews(ModelBuilder builder)
    {
        var configuration = builder.Entity<ReviewConfiguration>();
        configuration.HasQueryFilter(item => item.DeletedAt == null);
        configuration.HasIndex(item => item.ItemType).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        configuration.HasMany(item => item.Assignees).WithOne(item => item.ReviewConfiguration)
            .HasForeignKey(item => item.ReviewConfigurationId).OnDelete(DeleteBehavior.Cascade);

        var assignee = builder.Entity<ReviewConfigurationAssignee>();
        assignee.HasOne(item => item.Role).WithMany().HasForeignKey(item => item.RoleId).OnDelete(DeleteBehavior.Restrict);
        assignee.HasOne(item => item.User).WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
        assignee.ToTable(table => table.HasCheckConstraint("CK_ReviewConfigurationAssignees_OneAssignee",
            "(\"RoleId\" IS NULL) <> (\"UserId\" IS NULL)"));

        var review = builder.Entity<DocumentReview>();
        review.Property(item => item.Version).IsConcurrencyToken();
        review.HasIndex(item => new { item.ModelType, item.ModelId, item.Submission }).IsUnique();
        // At most one open review per record.
        review.HasIndex(item => new { item.ModelType, item.ModelId }).IsUnique()
            .HasFilter("\"Status\" IN (0, 1)").HasDatabaseName("IX_DocumentReviews_Open");
        review.HasIndex(item => new { item.Status, item.ConfigurationType });
        review.HasOne(item => item.PreparedBy).WithMany().HasForeignKey(item => item.PreparedById).OnDelete(DeleteBehavior.Restrict);
        review.HasOne(item => item.ReviewedBy).WithMany().HasForeignKey(item => item.ReviewedById).OnDelete(DeleteBehavior.Restrict);
        review.HasMany(item => item.Events).WithOne(item => item.DocumentReview)
            .HasForeignKey(item => item.DocumentReviewId).OnDelete(DeleteBehavior.Restrict);

        var reviewEvent = builder.Entity<DocumentReviewEvent>();
        reviewEvent.HasOne(item => item.Actor).WithMany().HasForeignKey(item => item.ActorId).OnDelete(DeleteBehavior.Restrict);
    }
}
