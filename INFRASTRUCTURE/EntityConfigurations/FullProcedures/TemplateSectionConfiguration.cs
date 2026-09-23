using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.EntityConfigurations.FullProcedures;

public sealed class TemplateSectionConfiguration : IEntityTypeConfiguration<TemplateSection>
{
    public void Configure(EntityTypeBuilder<TemplateSection> builder)
    {
        builder.ToTable("TemplateSections");
        builder.Property(item => item.PurposeId).HasMaxLength(120).IsRequired();
        builder.Property(item => item.SubjectTypeId).HasMaxLength(120).IsRequired();
        builder.HasIndex(item => new { item.TemplateAreaId, item.PurposeId, item.SubjectTypeId });
        builder.HasOne(item => item.TemplateArea).WithMany()
            .HasForeignKey(item => item.TemplateAreaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);
    }
}

public sealed class TemplateSectionRevisionConfiguration
    : IEntityTypeConfiguration<TemplateSectionRevision>
{
    public void Configure(EntityTypeBuilder<TemplateSectionRevision> builder)
    {
        builder.ToTable("TemplateSectionRevisions", table =>
        {
            table.HasCheckConstraint("CK_TemplateSectionRevision_Sequence", "\"Sequence\" > 0");
            table.HasCheckConstraint("CK_TemplateSectionRevision_Status", "\"Status\" BETWEEN 0 AND 3");
            table.HasCheckConstraint("CK_TemplateSectionRevision_ContentHash",
                "\"ContentHash\" ~ '^[a-f0-9]{64}$'");
        });
        builder.Property(item => item.Title).HasMaxLength(150).IsRequired();
        builder.Property(item => item.ContentHash).HasMaxLength(64).IsRequired()
            .IsConcurrencyToken();
        builder.Property(item => item.Status).IsConcurrencyToken();
        builder.HasAlternateKey(item => new { item.Id, item.TemplateSectionId });
        builder.HasIndex(item => new { item.TemplateSectionId, item.Sequence }).IsUnique();
        builder.HasIndex(item => item.TemplateSectionId,
                "IX_TemplateSectionRevisions_OnePublishedRevision").IsUnique()
            .HasFilter("\"Status\" = 2 AND \"DeletedAt\" IS NULL");
        builder.HasIndex(item => item.TemplateSectionId,
                "IX_TemplateSectionRevisions_OneOpenRevision").IsUnique()
            .HasFilter("\"Status\" IN (0, 1) AND \"DeletedAt\" IS NULL");
        builder.HasOne(item => item.TemplateSection).WithMany(item => item.Revisions)
            .HasForeignKey(item => item.TemplateSectionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.ReviewedBy).WithMany()
            .HasForeignKey(item => item.ReviewedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.PublishedBy).WithMany()
            .HasForeignKey(item => item.PublishedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);
    }
}

public sealed class TemplateSectionQuestionConfiguration
    : IEntityTypeConfiguration<TemplateSectionQuestion>
{
    public void Configure(EntityTypeBuilder<TemplateSectionQuestion> builder)
    {
        builder.ToTable("TemplateSectionQuestions", table => table.HasCheckConstraint(
            "CK_TemplateSectionQuestion_Order", "\"Order\" >= 0"));
        builder.HasKey(item => item.Id);
        builder.HasAlternateKey(item => new { item.TemplateSectionRevisionId, item.Id });
        builder.HasIndex(item => new
            { item.TemplateSectionRevisionId, item.TemplateQuestionId }).IsUnique();
        builder.HasIndex(item => new { item.TemplateSectionRevisionId, item.Order }).IsUnique();
        builder.HasOne(item => item.TemplateSectionRevision).WithMany(item => item.Questions)
            .HasForeignKey(item => item.TemplateSectionRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.TemplateQuestion).WithMany()
            .HasForeignKey(item => item.TemplateQuestionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.TemplateQuestionRevision).WithMany()
            .HasForeignKey(item => new
                { item.TemplateQuestionRevisionId, item.TemplateQuestionId })
            .HasPrincipalKey(item => new { item.Id, item.TemplateQuestionId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TemplateSectionConditionalRuleConfiguration
    : IEntityTypeConfiguration<TemplateSectionConditionalRule>
{
    public void Configure(EntityTypeBuilder<TemplateSectionConditionalRule> builder)
    {
        builder.ToTable("TemplateSectionConditionalRules", table =>
        {
            table.HasCheckConstraint("CK_TemplateSectionConditionalRule_Operator",
                "\"Operator\" BETWEEN 0 AND 2");
            table.HasCheckConstraint("CK_TemplateSectionConditionalRule_Value",
                "(\"Operator\" = 2 AND \"ComparisonValue\" IS NULL) OR " +
                "(\"Operator\" IN (0, 1) AND length(trim(\"ComparisonValue\")) > 0)");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ComparisonValue).HasMaxLength(500);
        builder.HasIndex(item => new
            { item.TemplateSectionRevisionId, item.TargetSectionQuestionId }).IsUnique();
        builder.HasOne(item => item.TemplateSectionRevision)
            .WithMany(item => item.ConditionalRules)
            .HasForeignKey(item => item.TemplateSectionRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.TargetSectionQuestion).WithMany()
            .HasForeignKey(item => new
                { item.TemplateSectionRevisionId, item.TargetSectionQuestionId })
            .HasPrincipalKey(item => new { item.TemplateSectionRevisionId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.DependsOnSectionQuestion).WithMany()
            .HasForeignKey(item => new
                { item.TemplateSectionRevisionId, item.DependsOnSectionQuestionId })
            .HasPrincipalKey(item => new { item.TemplateSectionRevisionId, item.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TemplateSectionRevisionAuditConfiguration
    : IEntityTypeConfiguration<TemplateSectionRevisionAudit>
{
    public void Configure(EntityTypeBuilder<TemplateSectionRevisionAudit> builder)
    {
        builder.ToTable("TemplateSectionRevisionAudits", table =>
        {
            table.HasCheckConstraint("CK_TemplateSectionRevisionAudit_ContentHash",
                "\"ContentHash\" ~ '^[a-f0-9]{64}$'");
            table.HasCheckConstraint("CK_TemplateSectionRevisionAudit_Status",
                "\"NewStatus\" BETWEEN 0 AND 3 AND " +
                "(\"PriorStatus\" IS NULL OR \"PriorStatus\" BETWEEN 0 AND 3)");
        });
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.TemplateSectionRevisionId, item.OccurredAt });
        builder.HasIndex(item => item.CorrelationId);
        builder.Property(item => item.Action).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Reason).HasMaxLength(4000).IsRequired();
        builder.Property(item => item.ContentHash).HasMaxLength(64).IsRequired();
        builder.Property(item => item.SnapshotJson).HasColumnType("jsonb").IsRequired();
        builder.HasOne(item => item.TemplateSectionRevision).WithMany(item => item.Audits)
            .HasForeignKey(item => item.TemplateSectionRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.Actor).WithMany().HasForeignKey(item => item.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
