using DOMAIN.Entities.Formulas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.EntityConfigurations.Formulas;

public class FormulaDefinitionConfiguration : IEntityTypeConfiguration<FormulaDefinition>
{
    public void Configure(EntityTypeBuilder<FormulaDefinition> builder)
    {
        builder.ToTable("FormulaDefinitions");
        builder.HasIndex(item => item.Key).IsUnique();
        builder.Property(item => item.Key).HasMaxLength(120).IsRequired();
        builder.Property(item => item.Name).HasMaxLength(250).IsRequired();
        builder.Property(item => item.PresentationPreset).HasMaxLength(100);
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_FormulaDefinition_Key", "btrim(\"Key\") <> ''"));
    }
}

public class QuestionFormulaDefinitionConfiguration : IEntityTypeConfiguration<QuestionFormulaDefinition>
{
    public void Configure(EntityTypeBuilder<QuestionFormulaDefinition> builder)
    {
        builder.ToTable("QuestionFormulaDefinitions");
        builder.HasIndex(item => new { item.QuestionId, item.FormulaDefinitionId }).IsUnique();
        builder.HasOne(item => item.Question).WithMany().HasForeignKey(item => item.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.FormulaDefinition).WithMany()
            .HasForeignKey(item => item.FormulaDefinitionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class FormulaRevisionConfiguration : IEntityTypeConfiguration<FormulaRevision>
{
    public void Configure(EntityTypeBuilder<FormulaRevision> builder)
    {
        builder.ToTable("FormulaRevisions");
        builder.HasIndex(item => new { item.FormulaDefinitionId, item.Revision }).IsUnique();
        builder.HasIndex(item => item.FormulaDefinitionId).IsUnique()
            .HasFilter("\"Status\" = 2 AND \"DeletedAt\" IS NULL");
        builder.HasIndex(item => new
        {
            item.DefinitionHash,
            item.FormulaLanguageVersion,
            item.NumericPolicyVersion
        });
        builder.Property(item => item.DefinitionJson).HasColumnType("jsonb").IsRequired();
        builder.Property(item => item.TestCasesJson).HasColumnType("jsonb").IsRequired();
        builder.Property(item => item.DefinitionHash).HasMaxLength(64).IsRequired();
        builder.Property(item => item.ReleaseEvidenceHash).HasMaxLength(64).IsRequired();
        builder.Property(item => item.FormulaLanguageVersion).HasMaxLength(64).IsRequired();
        builder.Property(item => item.NumericPolicyVersion).HasMaxLength(64).IsRequired();
        builder.HasOne(item => item.FormulaDefinition).WithMany(item => item.Revisions)
            .HasForeignKey(item => item.FormulaDefinitionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.ReviewedBy).WithMany().HasForeignKey(item => item.ReviewedById)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.ApprovedBy).WithMany().HasForeignKey(item => item.ApprovedById)
            .OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_FormulaRevision_Revision", "\"Revision\" > 0");
            table.HasCheckConstraint("CK_FormulaRevision_Status", "\"Status\" BETWEEN 0 AND 3");
            table.HasCheckConstraint("CK_FormulaRevision_DefinitionHash", "\"DefinitionHash\" ~ '^[a-f0-9]{64}$'");
            table.HasCheckConstraint("CK_FormulaRevision_EvidenceHash", "\"ReleaseEvidenceHash\" ~ '^[a-f0-9]{64}$'");
        });
    }
}

public class FormulaRevisionAuditConfiguration : IEntityTypeConfiguration<FormulaRevisionAudit>
{
    public void Configure(EntityTypeBuilder<FormulaRevisionAudit> builder)
    {
        builder.ToTable("FormulaRevisionAudits");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.FormulaRevisionId, item.OccurredAt });
        builder.Property(item => item.Action).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Reason).HasMaxLength(4000).IsRequired();
        builder.Property(item => item.DefinitionHash).HasMaxLength(64).IsRequired();
        builder.HasOne(item => item.FormulaRevision).WithMany()
            .HasForeignKey(item => item.FormulaRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.Actor).WithMany().HasForeignKey(item => item.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
