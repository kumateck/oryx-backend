using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.EntityConfigurations.FullProcedures;

public sealed class TemplateAdoptionConfiguration : IEntityTypeConfiguration<TemplateAdoption>
{
    public void Configure(EntityTypeBuilder<TemplateAdoption> builder)
    {
        builder.ToTable("TemplateAdoptions", table =>
        {
            table.HasCheckConstraint("CK_TemplateAdoption_Kind", "\"TemplateKind\" BETWEEN 0 AND 4");
            table.HasCheckConstraint("CK_TemplateAdoption_GrantVersion", "\"GrantVersion\" > 0");
            table.HasCheckConstraint("CK_TemplateAdoption_SourceHash",
                "\"SourceContentHash\" ~ '^[a-f0-9]{64}$'");
            table.HasCheckConstraint("CK_TemplateAdoption_TargetHash",
                "\"TargetContentHash\" ~ '^[a-f0-9]{64}$'");
            table.HasCheckConstraint("CK_TemplateAdoption_SnapshotHash",
                "\"SnapshotHash\" ~ '^[a-f0-9]{64}$'");
        });
        builder.Property(x => x.SourceContentHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.TargetContentHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SnapshotHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.DependencyMappingsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.RoleMappingsJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => x.TemplateSharingGrantId).IsUnique();
        builder.HasIndex(x => new { x.TargetAreaId, x.TemplateKind, x.AdoptedAt });
        builder.HasIndex(x => x.CorrelationId);
        builder.HasOne(x => x.TemplateSharingGrant).WithMany()
            .HasForeignKey(x => x.TemplateSharingGrantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.TargetArea).WithMany().HasForeignKey(x => x.TargetAreaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AdoptedBy).WithMany().HasForeignKey(x => x.AdoptedById)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(x => !x.DeletedAt.HasValue);
    }
}
