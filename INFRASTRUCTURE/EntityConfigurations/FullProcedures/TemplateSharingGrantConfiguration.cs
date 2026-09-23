using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.EntityConfigurations.FullProcedures;

public sealed class TemplateSharingGrantConfiguration
    : IEntityTypeConfiguration<TemplateSharingGrant>
{
    public void Configure(EntityTypeBuilder<TemplateSharingGrant> builder)
    {
        builder.ToTable("TemplateSharingGrants", table =>
        {
            table.HasCheckConstraint("CK_TemplateSharingGrant_Areas",
                "\"SourceAreaId\" <> \"TargetAreaId\" AND " +
                "\"RequestedByAreaId\" IN (\"SourceAreaId\", \"TargetAreaId\")");
            table.HasCheckConstraint("CK_TemplateSharingGrant_Kind",
                "\"TemplateKind\" BETWEEN 0 AND 4");
            table.HasCheckConstraint("CK_TemplateSharingGrant_Status",
                "\"Status\" BETWEEN 0 AND 3");
            table.HasCheckConstraint("CK_TemplateSharingGrant_Version", "\"Version\" > 0");
            table.HasCheckConstraint("CK_TemplateSharingGrant_ContentHash",
                "\"RevisionContentHash\" ~ '^[a-f0-9]{64}$'");
        });
        builder.Property(item => item.PurposeId).HasMaxLength(120).IsRequired();
        builder.Property(item => item.SubjectTypeId).HasMaxLength(120).IsRequired();
        builder.Property(item => item.RevisionContentHash).HasMaxLength(64).IsRequired();
        builder.Property(item => item.Version).IsConcurrencyToken();
        builder.HasIndex(item => new { item.SourceAreaId, item.TargetAreaId,
                item.TemplateKind, item.DefinitionId, item.RevisionId },
                "IX_TemplateSharingGrants_OneOpenGrant").IsUnique()
            .HasFilter("\"Status\" IN (0, 1) AND \"DeletedAt\" IS NULL");
        builder.HasIndex(item => new { item.TargetAreaId, item.Status });
        builder.HasOne(item => item.SourceArea).WithMany()
            .HasForeignKey(item => item.SourceAreaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.TargetArea).WithMany()
            .HasForeignKey(item => item.TargetAreaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.RequestedBy).WithMany()
            .HasForeignKey(item => item.RequestedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.DecidedBy).WithMany()
            .HasForeignKey(item => item.DecidedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.RevokedBy).WithMany()
            .HasForeignKey(item => item.RevokedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);
    }
}

public sealed class TemplateSharingGrantAuditConfiguration
    : IEntityTypeConfiguration<TemplateSharingGrantAudit>
{
    public void Configure(EntityTypeBuilder<TemplateSharingGrantAudit> builder)
    {
        builder.ToTable("TemplateSharingGrantAudits", table =>
        {
            table.HasCheckConstraint("CK_TemplateSharingGrantAudit_Status",
                "\"NewStatus\" BETWEEN 0 AND 3 AND " +
                "(\"PriorStatus\" IS NULL OR \"PriorStatus\" BETWEEN 0 AND 3)");
            table.HasCheckConstraint("CK_TemplateSharingGrantAudit_SnapshotHash",
                "\"SnapshotHash\" ~ '^[a-f0-9]{64}$'");
        });
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.TemplateSharingGrantId, item.Version }).IsUnique();
        builder.HasIndex(item => item.CorrelationId);
        builder.Property(item => item.Action).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Reason).HasMaxLength(4000).IsRequired();
        builder.Property(item => item.SnapshotJson).HasColumnType("jsonb").IsRequired();
        builder.Property(item => item.SnapshotHash).HasMaxLength(64).IsRequired();
        builder.HasOne(item => item.TemplateSharingGrant).WithMany(item => item.Audits)
            .HasForeignKey(item => item.TemplateSharingGrantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.Actor).WithMany().HasForeignKey(item => item.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
