using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.EntityConfigurations.FullProcedures;

public sealed class TemplateFormConfiguration : IEntityTypeConfiguration<TemplateForm>
{
    public void Configure(EntityTypeBuilder<TemplateForm> builder)
    {
        builder.ToTable("TemplateForms");
        builder.Property(item => item.PurposeId).HasMaxLength(120).IsRequired();
        builder.Property(item => item.SubjectTypeId).HasMaxLength(120).IsRequired();
        builder.HasIndex(item => new { item.TemplateAreaId, item.PurposeId, item.SubjectTypeId });
        builder.HasOne(item => item.TemplateArea).WithMany()
            .HasForeignKey(item => item.TemplateAreaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);
    }
}

public sealed class TemplateFormRevisionConfiguration
    : IEntityTypeConfiguration<TemplateFormRevision>
{
    public void Configure(EntityTypeBuilder<TemplateFormRevision> builder)
    {
        builder.ToTable("TemplateFormRevisions", table =>
        {
            table.HasCheckConstraint("CK_TemplateFormRevision_Sequence", "\"Sequence\" > 0");
            table.HasCheckConstraint("CK_TemplateFormRevision_Status", "\"Status\" BETWEEN 0 AND 3");
            table.HasCheckConstraint("CK_TemplateFormRevision_ContentHash",
                "\"ContentHash\" ~ '^[a-f0-9]{64}$'");
        });
        builder.Property(item => item.Name).HasMaxLength(150).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(4000).IsRequired();
        builder.Property(item => item.ContentHash).HasMaxLength(64).IsRequired()
            .IsConcurrencyToken();
        builder.Property(item => item.Status).IsConcurrencyToken();
        builder.HasAlternateKey(item => new { item.Id, item.TemplateFormId });
        builder.HasIndex(item => new { item.TemplateFormId, item.Sequence }).IsUnique();
        builder.HasIndex(item => item.TemplateFormId,
                "IX_TemplateFormRevisions_OnePublishedRevision").IsUnique()
            .HasFilter("\"Status\" = 2 AND \"DeletedAt\" IS NULL");
        builder.HasIndex(item => item.TemplateFormId,
                "IX_TemplateFormRevisions_OneOpenRevision").IsUnique()
            .HasFilter("\"Status\" IN (0, 1) AND \"DeletedAt\" IS NULL");
        builder.HasOne(item => item.TemplateForm).WithMany(item => item.Revisions)
            .HasForeignKey(item => item.TemplateFormId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.ReviewedBy).WithMany()
            .HasForeignKey(item => item.ReviewedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.PublishedBy).WithMany()
            .HasForeignKey(item => item.PublishedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);
    }
}

public sealed class TemplateFormSectionConfiguration
    : IEntityTypeConfiguration<TemplateFormSection>
{
    public void Configure(EntityTypeBuilder<TemplateFormSection> builder)
    {
        builder.ToTable("TemplateFormSections", table => table.HasCheckConstraint(
            "CK_TemplateFormSection_Order", "\"Order\" >= 0"));
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.TemplateFormRevisionId, item.Id });
        builder.HasIndex(item => new
            { item.TemplateFormRevisionId, item.TemplateSectionId }).IsUnique();
        builder.HasIndex(item => new { item.TemplateFormRevisionId, item.Order }).IsUnique();
        builder.HasOne(item => item.TemplateFormRevision).WithMany(item => item.Sections)
            .HasForeignKey(item => item.TemplateFormRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.TemplateSection).WithMany()
            .HasForeignKey(item => item.TemplateSectionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.TemplateSectionRevision).WithMany()
            .HasForeignKey(item => new
                { item.TemplateSectionRevisionId, item.TemplateSectionId })
            .HasPrincipalKey(item => new { item.Id, item.TemplateSectionId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TemplateFormConditionalRuleConfiguration
    : IEntityTypeConfiguration<TemplateFormConditionalRule>
{
    public void Configure(EntityTypeBuilder<TemplateFormConditionalRule> builder)
    {
        builder.ToTable("TemplateFormConditionalRules", table =>
        {
            table.HasCheckConstraint("CK_TemplateFormConditionalRule_Operator",
                "\"Operator\" BETWEEN 0 AND 2");
            table.HasCheckConstraint("CK_TemplateFormConditionalRule_Value",
                "(\"Operator\" = 2 AND \"ComparisonValue\" IS NULL) OR " +
                "(\"Operator\" IN (0, 1) AND length(trim(\"ComparisonValue\")) > 0)");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ComparisonValue).HasMaxLength(500);
        builder.HasIndex(item => new
            { item.TemplateFormRevisionId, item.TargetFormSectionId }).IsUnique();
        builder.HasOne(item => item.TemplateFormRevision).WithMany(item => item.ConditionalRules)
            .HasForeignKey(item => item.TemplateFormRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.TargetFormSection).WithMany()
            .HasForeignKey(item => new
                { item.TemplateFormRevisionId, item.TargetFormSectionId })
            .HasPrincipalKey(item => new { item.TemplateFormRevisionId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.SourceFormSection).WithMany()
            .HasForeignKey(item => new
                { item.TemplateFormRevisionId, item.SourceFormSectionId })
            .HasPrincipalKey(item => new { item.TemplateFormRevisionId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TemplateQuestionRevision>().WithMany()
            .HasForeignKey(item => new { item.SourceQuestionRevisionId, item.SourceQuestionId })
            .HasPrincipalKey(item => new { item.Id, item.TemplateQuestionId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TemplateFormRevisionAuditConfiguration
    : IEntityTypeConfiguration<TemplateFormRevisionAudit>
{
    public void Configure(EntityTypeBuilder<TemplateFormRevisionAudit> builder)
    {
        builder.ToTable("TemplateFormRevisionAudits", table =>
        {
            table.HasCheckConstraint("CK_TemplateFormRevisionAudit_ContentHash",
                "\"ContentHash\" ~ '^[a-f0-9]{64}$'");
            table.HasCheckConstraint("CK_TemplateFormRevisionAudit_Status",
                "\"NewStatus\" BETWEEN 0 AND 3 AND " +
                "(\"PriorStatus\" IS NULL OR \"PriorStatus\" BETWEEN 0 AND 3)");
        });
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.TemplateFormRevisionId, item.OccurredAt });
        builder.HasIndex(item => item.CorrelationId);
        builder.Property(item => item.Action).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Reason).HasMaxLength(4000).IsRequired();
        builder.Property(item => item.ContentHash).HasMaxLength(64).IsRequired();
        builder.Property(item => item.SnapshotJson).HasColumnType("jsonb").IsRequired();
        builder.HasOne(item => item.TemplateFormRevision).WithMany(item => item.Audits)
            .HasForeignKey(item => item.TemplateFormRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.Actor).WithMany().HasForeignKey(item => item.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
