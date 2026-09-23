using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.EntityConfigurations.FullProcedures;

public sealed class TemplateActivityConfiguration : IEntityTypeConfiguration<TemplateActivity>
{
    public void Configure(EntityTypeBuilder<TemplateActivity> builder)
    {
        builder.ToTable("TemplateActivities");
        builder.Property(item => item.PurposeId).HasMaxLength(120).IsRequired();
        builder.Property(item => item.SubjectTypeId).HasMaxLength(120).IsRequired();
        builder.HasIndex(item => new { item.TemplateAreaId, item.PurposeId, item.SubjectTypeId });
        builder.HasOne(item => item.TemplateArea).WithMany()
            .HasForeignKey(item => item.TemplateAreaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);
    }
}

public sealed class TemplateActivityRevisionConfiguration
    : IEntityTypeConfiguration<TemplateActivityRevision>
{
    public void Configure(EntityTypeBuilder<TemplateActivityRevision> builder)
    {
        builder.ToTable("TemplateActivityRevisions", table =>
        {
            table.HasCheckConstraint("CK_TemplateActivityRevision_Sequence", "\"Sequence\" > 0");
            table.HasCheckConstraint("CK_TemplateActivityRevision_Status", "\"Status\" BETWEEN 0 AND 3");
            table.HasCheckConstraint("CK_TemplateActivityRevision_ContentHash",
                "\"ContentHash\" ~ '^[a-f0-9]{64}$'");
        });
        builder.Property(item => item.Name).HasMaxLength(150).IsRequired();
        builder.Property(item => item.Instructions).HasMaxLength(4000).IsRequired();
        builder.Property(item => item.ContentHash).HasMaxLength(64).IsRequired()
            .IsConcurrencyToken();
        builder.Property(item => item.Status).IsConcurrencyToken();
        builder.HasAlternateKey(item => new { item.Id, item.TemplateActivityId });
        builder.HasIndex(item => new { item.TemplateActivityId, item.Sequence }).IsUnique();
        builder.HasIndex(item => item.TemplateActivityId,
                "IX_TemplateActivityRevisions_OnePublishedRevision").IsUnique()
            .HasFilter("\"Status\" = 2 AND \"DeletedAt\" IS NULL");
        builder.HasIndex(item => item.TemplateActivityId,
                "IX_TemplateActivityRevisions_OneOpenRevision").IsUnique()
            .HasFilter("\"Status\" IN (0, 1) AND \"DeletedAt\" IS NULL");
        builder.HasOne(item => item.TemplateActivity).WithMany(item => item.Revisions)
            .HasForeignKey(item => item.TemplateActivityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.ReviewedBy).WithMany()
            .HasForeignKey(item => item.ReviewedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.PublishedBy).WithMany()
            .HasForeignKey(item => item.PublishedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);
    }
}

public sealed class TemplateActivityFormBindingConfiguration
    : IEntityTypeConfiguration<TemplateActivityFormBinding>
{
    public void Configure(EntityTypeBuilder<TemplateActivityFormBinding> builder)
    {
        builder.ToTable("TemplateActivityFormBindings", table =>
        {
            table.HasCheckConstraint("CK_TemplateActivityFormBinding_Order", "\"Order\" >= 0");
            table.HasCheckConstraint("CK_TemplateActivityFormBinding_Usage", "\"Usage\" BETWEEN 0 AND 2");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Key).HasMaxLength(100).IsRequired();
        builder.HasIndex(item => new { item.TemplateActivityRevisionId, item.Key }).IsUnique();
        builder.HasIndex(item => new { item.TemplateActivityRevisionId, item.Order }).IsUnique();
        builder.HasIndex(item => new
            { item.TemplateActivityRevisionId, item.TemplateFormId }).IsUnique();
        builder.HasOne(item => item.TemplateActivityRevision).WithMany(item => item.Forms)
            .HasForeignKey(item => item.TemplateActivityRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.TemplateFormRevision).WithMany()
            .HasForeignKey(item => new { item.TemplateFormRevisionId, item.TemplateFormId })
            .HasPrincipalKey(item => new { item.Id, item.TemplateFormId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TemplateActivityResourceConfiguration
    : IEntityTypeConfiguration<TemplateActivityResourceRequirement>
{
    public void Configure(EntityTypeBuilder<TemplateActivityResourceRequirement> builder)
    {
        builder.ToTable("TemplateActivityResources", table => table.HasCheckConstraint(
            "CK_TemplateActivityResource_Order", "\"Order\" >= 0"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.CapabilityId).HasMaxLength(120).IsRequired();
        builder.HasIndex(item => new
            { item.TemplateActivityRevisionId, item.CapabilityId }).IsUnique();
        builder.HasIndex(item => new { item.TemplateActivityRevisionId, item.Order }).IsUnique();
        builder.HasOne(item => item.TemplateActivityRevision).WithMany(item => item.Resources)
            .HasForeignKey(item => item.TemplateActivityRevisionId).OnDelete(DeleteBehavior.Restrict);
    }
}
