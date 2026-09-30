using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.EntityConfigurations.FullProcedures;

public sealed class TemplateQuestionConfiguration : IEntityTypeConfiguration<TemplateQuestion>
{
    public void Configure(EntityTypeBuilder<TemplateQuestion> builder)
    {
        builder.ToTable("TemplateQuestions");
        builder.Property(item => item.PurposeId).HasMaxLength(120).IsRequired();
        builder.Property(item => item.SubjectTypeId).HasMaxLength(120).IsRequired();
        builder.HasIndex(item => new { item.TemplateAreaId, item.PurposeId, item.SubjectTypeId });
        builder.HasOne(item => item.TemplateArea).WithMany()
            .HasForeignKey(item => item.TemplateAreaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);
    }
}

public sealed class TemplateQuestionRevisionConfiguration
    : IEntityTypeConfiguration<TemplateQuestionRevision>
{
    public void Configure(EntityTypeBuilder<TemplateQuestionRevision> builder)
    {
        builder.ToTable("TemplateQuestionRevisions", table =>
        {
            table.HasCheckConstraint("CK_TemplateQuestionRevision_Sequence", "\"Sequence\" > 0");
            table.HasCheckConstraint("CK_TemplateQuestionRevision_Status", "\"Status\" BETWEEN 0 AND 3");
            table.HasCheckConstraint("CK_TemplateQuestionRevision_AnswerType", "\"AnswerType\" BETWEEN 0 AND 12");
            table.HasCheckConstraint("CK_TemplateQuestionRevision_Sensitivity", "\"Sensitivity\" BETWEEN 0 AND 2");
            table.HasCheckConstraint("CK_TemplateQuestionRevision_Range",
                "\"Minimum\" IS NULL OR \"Maximum\" IS NULL OR \"Minimum\" <= \"Maximum\"");
            table.HasCheckConstraint("CK_TemplateQuestionRevision_ContentHash",
                "\"ContentHash\" ~ '^[a-f0-9]{64}$'");
        });
        builder.Property(item => item.Wording).HasMaxLength(500).IsRequired();
        builder.Property(item => item.InputType).HasMaxLength(40).IsRequired();
        builder.Property(item => item.HelpText).HasMaxLength(1000);
        builder.Property(item => item.Minimum).HasPrecision(28, 10);
        builder.Property(item => item.Maximum).HasPrecision(28, 10);
        builder.Property(item => item.ContentHash).HasMaxLength(64).IsRequired()
            .IsConcurrencyToken();
        builder.Property(item => item.Status).IsConcurrencyToken();
        builder.HasAlternateKey(item => new { item.Id, item.TemplateQuestionId });
        builder.HasIndex(item => new { item.TemplateQuestionId, item.Sequence }).IsUnique();
        builder.HasIndex(item => item.TemplateQuestionId,
                "IX_TemplateQuestionRevisions_OnePublishedRevision").IsUnique()
            .HasFilter("\"Status\" = 2 AND \"DeletedAt\" IS NULL");
        builder.HasIndex(item => item.TemplateQuestionId,
                "IX_TemplateQuestionRevisions_OneOpenRevision").IsUnique()
            .HasFilter("\"Status\" IN (0, 1) AND \"DeletedAt\" IS NULL");
        builder.HasOne(item => item.TemplateQuestion).WithMany(item => item.Revisions)
            .HasForeignKey(item => item.TemplateQuestionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.UnitOfMeasure).WithMany()
            .HasForeignKey(item => item.UnitOfMeasureId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.ReviewedBy).WithMany()
            .HasForeignKey(item => item.ReviewedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.PublishedBy).WithMany()
            .HasForeignKey(item => item.PublishedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);
    }
}

public sealed class TemplateQuestionOptionConfiguration
    : IEntityTypeConfiguration<TemplateQuestionOption>
{
    public void Configure(EntityTypeBuilder<TemplateQuestionOption> builder)
    {
        builder.ToTable("TemplateQuestionOptions", table => table.HasCheckConstraint(
            "CK_TemplateQuestionOption_Rank", "\"Rank\" >= 0"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Value).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Label).HasMaxLength(500).IsRequired();
        builder.HasIndex(item => new { item.TemplateQuestionRevisionId, item.Value }).IsUnique();
        builder.HasIndex(item => new { item.TemplateQuestionRevisionId, item.Rank }).IsUnique();
        builder.HasOne(item => item.TemplateQuestionRevision).WithMany(item => item.Options)
            .HasForeignKey(item => item.TemplateQuestionRevisionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TemplateQuestionCalculationReferenceConfiguration
    : IEntityTypeConfiguration<TemplateQuestionCalculationReference>
{
    public void Configure(EntityTypeBuilder<TemplateQuestionCalculationReference> builder)
    {
        builder.ToTable("TemplateQuestionCalculationReferences");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new
            { item.TemplateQuestionRevisionId, item.ReferencedQuestionId }).IsUnique();
        builder.HasIndex(item => item.ReferencedQuestionRevisionId);
        builder.HasOne(item => item.TemplateQuestionRevision)
            .WithMany(item => item.CalculationReferences)
            .HasForeignKey(item => item.TemplateQuestionRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.ReferencedQuestion).WithMany()
            .HasForeignKey(item => item.ReferencedQuestionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.ReferencedQuestionRevision).WithMany()
            .HasForeignKey(item => new
                { item.ReferencedQuestionRevisionId, item.ReferencedQuestionId })
            .HasPrincipalKey(item => new { item.Id, item.TemplateQuestionId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TemplateQuestionRevisionAuditConfiguration
    : IEntityTypeConfiguration<TemplateQuestionRevisionAudit>
{
    public void Configure(EntityTypeBuilder<TemplateQuestionRevisionAudit> builder)
    {
        builder.ToTable("TemplateQuestionRevisionAudits", table =>
        {
            table.HasCheckConstraint("CK_TemplateQuestionRevisionAudit_ContentHash",
                "\"ContentHash\" ~ '^[a-f0-9]{64}$'");
            table.HasCheckConstraint("CK_TemplateQuestionRevisionAudit_Status",
                "\"NewStatus\" BETWEEN 0 AND 3 AND (\"PriorStatus\" IS NULL OR \"PriorStatus\" BETWEEN 0 AND 3)");
        });
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.TemplateQuestionRevisionId, item.OccurredAt });
        builder.HasIndex(item => item.CorrelationId);
        builder.Property(item => item.Action).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Reason).HasMaxLength(4000).IsRequired();
        builder.Property(item => item.ContentHash).HasMaxLength(64).IsRequired();
        builder.Property(item => item.SnapshotJson).HasColumnType("jsonb").IsRequired();
        builder.HasOne(item => item.TemplateQuestionRevision).WithMany(item => item.Audits)
            .HasForeignKey(item => item.TemplateQuestionRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.Actor).WithMany().HasForeignKey(item => item.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
