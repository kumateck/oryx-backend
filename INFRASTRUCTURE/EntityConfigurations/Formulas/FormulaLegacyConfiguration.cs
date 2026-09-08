using DOMAIN.Entities.Formulas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.EntityConfigurations.Formulas;

public class LegacyFormulaArtifactConfiguration : IEntityTypeConfiguration<LegacyFormulaArtifact>
{
    public void Configure(EntityTypeBuilder<LegacyFormulaArtifact> builder)
    {
        builder.ToTable("LegacyFormulaArtifacts");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.DatabaseFingerprint, item.SourcePath, item.SourceHash }).IsUnique();
        builder.Property(item => item.OriginalPayload).HasColumnType("jsonb").IsRequired();
        builder.Property(item => item.SourceHash).HasMaxLength(64).IsRequired();
        builder.Property(item => item.SourcePath).HasMaxLength(500).IsRequired();
        builder.Property(item => item.DatabaseFingerprint).HasMaxLength(128).IsRequired();
        builder.Property(item => item.MigrationReleaseId).HasMaxLength(100).IsRequired();
        builder.HasOne(item => item.Question).WithMany().HasForeignKey(item => item.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.QuestionOption).WithMany()
            .HasForeignKey(item => item.QuestionOptionId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_LegacyFormulaArtifact_SourceHash", "\"SourceHash\" ~ '^[a-f0-9]{64}$'"));
    }
}

public class LegacyKeyMappingConfiguration : IEntityTypeConfiguration<LegacyKeyMapping>
{
    public void Configure(EntityTypeBuilder<LegacyKeyMapping> builder)
    {
        builder.ToTable("LegacyKeyMappings");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new
        {
            item.LegacyFormulaArtifactId,
            item.KeyKind,
            item.LegacyPath,
            item.LegacyKey
        }).IsUnique();
        builder.Property(item => item.KeyKind).HasMaxLength(50).IsRequired();
        builder.Property(item => item.LegacyPath).HasMaxLength(500).IsRequired();
        builder.Property(item => item.LegacyKey).HasMaxLength(120).IsRequired();
        builder.Property(item => item.CanonicalKey).HasMaxLength(120).IsRequired();
        builder.Property(item => item.NormalizationReason).HasMaxLength(2000).IsRequired();
        builder.Property(item => item.ApprovalReference).HasMaxLength(250).IsRequired();
        builder.HasOne(item => item.LegacyFormulaArtifact).WithMany()
            .HasForeignKey(item => item.LegacyFormulaArtifactId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.ReviewedBy).WithMany().HasForeignKey(item => item.ReviewedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
