using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.EntityConfigurations.FullProcedures;

public sealed class ProcedureApplicabilityConfiguration
    : IEntityTypeConfiguration<ProcedureApplicability>
{
    public void Configure(EntityTypeBuilder<ProcedureApplicability> builder)
    {
        builder.ToTable("ProcedureApplicabilities", table =>
            table.HasCheckConstraint("CK_ProcedureApplicability_BatchType",
                "\"BatchType\" BETWEEN 0 AND 3"));
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new
            { x.ProcedureRevisionId, x.ProductId, x.SiteId, x.BatchType }).IsUnique();
        builder.HasOne(x => x.ProcedureRevision).WithMany(x => x.Applicabilities)
            .HasForeignKey(x => x.ProcedureRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Site).WithMany().HasForeignKey(x => x.SiteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProcedureStageScopeConfiguration
    : IEntityTypeConfiguration<ProcedureStageScope>
{
    public void Configure(EntityTypeBuilder<ProcedureStageScope> builder)
    {
        builder.ToTable("ProcedureStageScopes", table =>
            table.HasCheckConstraint("CK_ProcedureStageScope_RecordScope",
                "\"RecordScope\" BETWEEN 0 AND 3"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.WorkflowNodeKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.WorkflowNodeName).HasMaxLength(150).IsRequired();
        builder.HasIndex(x => new
            { x.ProcedureRevisionId, x.TemplateWorkflowNodeId }).IsUnique();
        builder.HasOne(x => x.ProcedureRevision).WithMany(x => x.StageScopes)
            .HasForeignKey(x => new { x.ProcedureRevisionId, x.TemplateWorkflowRevisionId })
            .HasPrincipalKey(x => new { x.Id, x.TemplateWorkflowRevisionId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.TemplateWorkflowNode).WithMany()
            .HasForeignKey(x => new { x.TemplateWorkflowRevisionId, x.TemplateWorkflowNodeId })
            .HasPrincipalKey(x => new { x.TemplateWorkflowRevisionId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProcedureRevisionAuditConfiguration
    : IEntityTypeConfiguration<ProcedureRevisionAudit>
{
    public void Configure(EntityTypeBuilder<ProcedureRevisionAudit> builder)
    {
        builder.ToTable("ProcedureRevisionAudits", table =>
        {
            table.HasCheckConstraint("CK_ProcedureRevisionAudit_ContentHash",
                "\"ContentHash\" ~ '^[a-f0-9]{64}$'");
            table.HasCheckConstraint("CK_ProcedureRevisionAudit_Status",
                "\"NewStatus\" BETWEEN 0 AND 3 AND " +
                "(\"PriorStatus\" IS NULL OR \"PriorStatus\" BETWEEN 0 AND 3)");
        });
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ProcedureRevisionId, x.OccurredAt });
        builder.HasIndex(x => x.CorrelationId);
        builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.ContentHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SnapshotJson).HasColumnType("jsonb").IsRequired();
        builder.HasOne(x => x.ProcedureRevision).WithMany(x => x.Audits)
            .HasForeignKey(x => x.ProcedureRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Actor).WithMany().HasForeignKey(x => x.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
