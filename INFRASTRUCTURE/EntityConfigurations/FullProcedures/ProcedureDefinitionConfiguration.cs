using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.EntityConfigurations.FullProcedures;

public sealed class ProcedureDefinitionConfiguration
    : IEntityTypeConfiguration<ProcedureDefinition>
{
    public void Configure(EntityTypeBuilder<ProcedureDefinition> builder)
    {
        builder.ToTable("ProcedureDefinitions");
        builder.Property(x => x.PurposeId).HasMaxLength(120).IsRequired();
        builder.Property(x => x.SubjectTypeId).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => new { x.TemplateAreaId, x.PurposeId, x.SubjectTypeId });
        builder.HasOne(x => x.TemplateArea).WithMany().HasForeignKey(x => x.TemplateAreaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(x => !x.DeletedAt.HasValue);
    }
}

public sealed class ProcedureRevisionConfiguration
    : IEntityTypeConfiguration<ProcedureRevision>
{
    public void Configure(EntityTypeBuilder<ProcedureRevision> builder)
    {
        builder.ToTable("ProcedureRevisions", table =>
        {
            table.HasCheckConstraint("CK_ProcedureRevision_Sequence", "\"Sequence\" > 0");
            table.HasCheckConstraint("CK_ProcedureRevision_Status", "\"Status\" BETWEEN 0 AND 3");
            table.HasCheckConstraint("CK_ProcedureRevision_ContentHash",
                "\"ContentHash\" ~ '^[a-f0-9]{64}$'");
            table.HasCheckConstraint("CK_ProcedureRevision_WorkflowHash",
                "\"TemplateWorkflowContentHash\" ~ '^[a-f0-9]{64}$'");
        });
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.ParameterSchemaJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.TemplateWorkflowName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.TemplateWorkflowContentHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ContentHash).HasMaxLength(64).IsRequired().IsConcurrencyToken();
        builder.Property(x => x.Status).IsConcurrencyToken();
        builder.HasAlternateKey(x => new { x.Id, x.TemplateWorkflowRevisionId });
        builder.HasIndex(x => new { x.ProcedureDefinitionId, x.Sequence }).IsUnique();
        builder.HasIndex(x => x.ProcedureDefinitionId,
                "IX_ProcedureRevisions_OneApprovedRevision").IsUnique()
            .HasFilter("\"Status\" = 2 AND \"DeletedAt\" IS NULL");
        builder.HasIndex(x => x.ProcedureDefinitionId,
                "IX_ProcedureRevisions_OneOpenRevision").IsUnique()
            .HasFilter("\"Status\" IN (0, 1) AND \"DeletedAt\" IS NULL");
        builder.HasOne(x => x.ProcedureDefinition).WithMany(x => x.Revisions)
            .HasForeignKey(x => x.ProcedureDefinitionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.TemplateWorkflowRevision).WithMany()
            .HasForeignKey(x => new { x.TemplateWorkflowRevisionId, x.TemplateWorkflowId })
            .HasPrincipalKey(x => new { x.Id, x.TemplateWorkflowId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ReviewedBy).WithMany().HasForeignKey(x => x.ReviewedById)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ApprovedBy).WithMany().HasForeignKey(x => x.ApprovedById)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(x => !x.DeletedAt.HasValue);
    }
}
