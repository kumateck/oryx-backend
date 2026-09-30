using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.EntityConfigurations.FullProcedures;

public sealed class TemplateWorkflowConfiguration : IEntityTypeConfiguration<TemplateWorkflow>
{
    public void Configure(EntityTypeBuilder<TemplateWorkflow> builder)
    {
        builder.ToTable("TemplateWorkflows");
        builder.Property(item => item.PurposeId).HasMaxLength(120).IsRequired();
        builder.Property(item => item.SubjectTypeId).HasMaxLength(120).IsRequired();
        builder.HasIndex(item => new { item.TemplateAreaId, item.PurposeId, item.SubjectTypeId });
        builder.HasOne(item => item.TemplateArea).WithMany()
            .HasForeignKey(item => item.TemplateAreaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);
    }
}

public sealed class TemplateWorkflowRevisionConfiguration
    : IEntityTypeConfiguration<TemplateWorkflowRevision>
{
    public void Configure(EntityTypeBuilder<TemplateWorkflowRevision> builder)
    {
        builder.ToTable("TemplateWorkflowRevisions", table =>
        {
            table.HasCheckConstraint("CK_TemplateWorkflowRevision_Sequence", "\"Sequence\" > 0");
            table.HasCheckConstraint("CK_TemplateWorkflowRevision_Status", "\"Status\" BETWEEN 0 AND 3");
            table.HasCheckConstraint("CK_TemplateWorkflowRevision_ContentHash",
                "\"ContentHash\" ~ '^[a-f0-9]{64}$'");
        });
        builder.Property(item => item.Name).HasMaxLength(150).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(4000).IsRequired();
        builder.Property(item => item.ContentHash).HasMaxLength(64).IsRequired()
            .IsConcurrencyToken();
        builder.Property(item => item.Status).IsConcurrencyToken();
        builder.HasAlternateKey(item => new { item.Id, item.TemplateWorkflowId });
        builder.HasIndex(item => new { item.TemplateWorkflowId, item.Sequence }).IsUnique();
        builder.HasIndex(item => item.TemplateWorkflowId,
                "IX_TemplateWorkflowRevisions_OnePublishedRevision").IsUnique()
            .HasFilter("\"Status\" = 2 AND \"DeletedAt\" IS NULL");
        builder.HasIndex(item => item.TemplateWorkflowId,
                "IX_TemplateWorkflowRevisions_OneOpenRevision").IsUnique()
            .HasFilter("\"Status\" IN (0, 1) AND \"DeletedAt\" IS NULL");
        builder.HasOne(item => item.TemplateWorkflow).WithMany(item => item.Revisions)
            .HasForeignKey(item => item.TemplateWorkflowId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.ReviewedBy).WithMany()
            .HasForeignKey(item => item.ReviewedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.PublishedBy).WithMany()
            .HasForeignKey(item => item.PublishedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);
    }
}

public sealed class TemplateWorkflowNodeConfiguration
    : IEntityTypeConfiguration<TemplateWorkflowNode>
{
    public void Configure(EntityTypeBuilder<TemplateWorkflowNode> builder)
    {
        builder.ToTable("TemplateWorkflowNodes", table =>
        {
            table.HasCheckConstraint("CK_TemplateWorkflowNode_Order", "\"Order\" >= 0");
            table.HasCheckConstraint("CK_TemplateWorkflowNode_Type", "\"NodeType\" BETWEEN 0 AND 10");
            table.HasCheckConstraint("CK_TemplateWorkflowNode_WaitKind",
                "\"WaitKind\" IS NULL OR \"WaitKind\" BETWEEN 0 AND 2");
            table.HasCheckConstraint("CK_TemplateWorkflowNode_ReworkAttempts",
                "\"ReworkMaxAttempts\" IS NULL OR \"ReworkMaxAttempts\" BETWEEN 1 AND 10");
        });
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.TemplateWorkflowRevisionId, item.Id });
        builder.Property(item => item.Key).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Name).HasMaxLength(150).IsRequired();
        builder.Property(item => item.JoinGroupKey).HasMaxLength(100);
        builder.Property(item => item.HoldGroupKey).HasMaxLength(100);
        builder.Property(item => item.WaitConfiguration).HasMaxLength(2000);
        builder.HasIndex(item => new { item.TemplateWorkflowRevisionId, item.Key }).IsUnique();
        builder.HasIndex(item => new { item.TemplateWorkflowRevisionId, item.Order }).IsUnique();
        builder.HasOne(item => item.TemplateWorkflowRevision).WithMany(item => item.Nodes)
            .HasForeignKey(item => item.TemplateWorkflowRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.TemplateActivityRevision).WithMany()
            .HasForeignKey(item => new { item.TemplateActivityRevisionId, item.TemplateActivityId })
            .HasPrincipalKey(item => new { item.Id, item.TemplateActivityId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.ReworkTargetNode).WithMany()
            .HasForeignKey(item => new { item.TemplateWorkflowRevisionId, item.ReworkTargetNodeId })
            .HasPrincipalKey(item => new { item.TemplateWorkflowRevisionId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TemplateWorkflowEdgeConfiguration
    : IEntityTypeConfiguration<TemplateWorkflowEdge>
{
    public void Configure(EntityTypeBuilder<TemplateWorkflowEdge> builder)
    {
        builder.ToTable("TemplateWorkflowEdges", table =>
            table.HasCheckConstraint("CK_TemplateWorkflowEdge_Order", "\"Order\" >= 0"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.BranchKey).HasMaxLength(100);
        builder.Property(item => item.BranchExpression).HasMaxLength(500);
        builder.HasIndex(item => new
            { item.TemplateWorkflowRevisionId, item.SourceNodeId, item.TargetNodeId }).IsUnique();
        builder.HasOne(item => item.TemplateWorkflowRevision).WithMany(item => item.Edges)
            .HasForeignKey(item => item.TemplateWorkflowRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.SourceNode).WithMany(item => item.OutgoingEdges)
            .HasForeignKey(item => new { item.TemplateWorkflowRevisionId, item.SourceNodeId })
            .HasPrincipalKey(item => new { item.TemplateWorkflowRevisionId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.TargetNode).WithMany(item => item.IncomingEdges)
            .HasForeignKey(item => new { item.TemplateWorkflowRevisionId, item.TargetNodeId })
            .HasPrincipalKey(item => new { item.TemplateWorkflowRevisionId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TemplateWorkflowNodeLayoutConfiguration
    : IEntityTypeConfiguration<TemplateWorkflowNodeLayout>
{
    public void Configure(EntityTypeBuilder<TemplateWorkflowNodeLayout> builder)
    {
        builder.ToTable("TemplateWorkflowNodeLayouts");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new
            { item.TemplateWorkflowRevisionId, item.TemplateWorkflowNodeId }).IsUnique();
        builder.HasOne(item => item.TemplateWorkflowRevision).WithMany(item => item.Layouts)
            .HasForeignKey(item => item.TemplateWorkflowRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.TemplateWorkflowNode).WithMany()
            .HasForeignKey(item => new { item.TemplateWorkflowRevisionId, item.TemplateWorkflowNodeId })
            .HasPrincipalKey(item => new { item.TemplateWorkflowRevisionId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TemplateWorkflowRevisionAuditConfiguration
    : IEntityTypeConfiguration<TemplateWorkflowRevisionAudit>
{
    public void Configure(EntityTypeBuilder<TemplateWorkflowRevisionAudit> builder)
    {
        builder.ToTable("TemplateWorkflowRevisionAudits", table =>
        {
            table.HasCheckConstraint("CK_TemplateWorkflowRevisionAudit_ContentHash",
                "\"ContentHash\" ~ '^[a-f0-9]{64}$'");
            table.HasCheckConstraint("CK_TemplateWorkflowRevisionAudit_Status",
                "\"NewStatus\" BETWEEN 0 AND 3 AND " +
                "(\"PriorStatus\" IS NULL OR \"PriorStatus\" BETWEEN 0 AND 3)");
        });
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.TemplateWorkflowRevisionId, item.OccurredAt });
        builder.HasIndex(item => item.CorrelationId);
        builder.Property(item => item.Action).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Reason).HasMaxLength(4000).IsRequired();
        builder.Property(item => item.ContentHash).HasMaxLength(64).IsRequired();
        builder.Property(item => item.SnapshotJson).HasColumnType("jsonb").IsRequired();
        builder.HasOne(item => item.TemplateWorkflowRevision).WithMany(item => item.Audits)
            .HasForeignKey(item => item.TemplateWorkflowRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.Actor).WithMany().HasForeignKey(item => item.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
