using DOMAIN.Entities.Formulas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.EntityConfigurations.Formulas;

public class FormulaMigrationRunConfiguration : IEntityTypeConfiguration<FormulaMigrationRun>
{
    public void Configure(EntityTypeBuilder<FormulaMigrationRun> builder)
    {
        builder.ToTable("FormulaMigrationRuns");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.ReleaseId, item.SourceFingerprint, item.Mode }).IsUnique();
        builder.Property(item => item.ReleaseId).HasMaxLength(100).IsRequired();
        builder.Property(item => item.SourceFingerprint).HasMaxLength(128).IsRequired();
        builder.Property(item => item.CorpusChecksum).HasMaxLength(64).IsRequired();
        builder.Property(item => item.CodeVersion).HasMaxLength(100).IsRequired();
        builder.Property(item => item.ApplyManifestHash).HasMaxLength(64);
        builder.Property(item => item.SignedReportHash).HasMaxLength(64);
        builder.Property(item => item.SignedReportLocation).HasMaxLength(2000);
        builder.HasOne(item => item.InitiatedBy).WithMany().HasForeignKey(item => item.InitiatedById)
            .OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_FormulaMigrationRun_Mode", "\"Mode\" BETWEEN 0 AND 1");
            table.HasCheckConstraint("CK_FormulaMigrationRun_Status", "\"Status\" BETWEEN 0 AND 3");
            table.HasCheckConstraint("CK_FormulaMigrationRun_Counts", "\"TotalCount\" >= 0 AND \"SucceededCount\" >= 0 AND \"FailedCount\" >= 0");
            table.HasCheckConstraint("CK_FormulaMigrationRun_SignedReportHash",
                "\"SignedReportHash\" IS NULL OR \"SignedReportHash\" ~ '^[a-f0-9]{64}$'");
            table.HasCheckConstraint("CK_FormulaMigrationRun_ApplyManifestHash",
                "\"ApplyManifestHash\" IS NULL OR \"ApplyManifestHash\" ~ '^[a-f0-9]{64}$'");
            table.HasCheckConstraint("CK_FormulaMigrationRun_ApplyProvenance",
                "\"Mode\" <> 1 OR (\"ApplyManifestHash\" IS NOT NULL AND \"SignedReportHash\" IS NOT NULL AND btrim(\"SignedReportLocation\") <> '')");
        });
    }
}

public class FormulaMigrationItemConfiguration : IEntityTypeConfiguration<FormulaMigrationItem>
{
    public void Configure(EntityTypeBuilder<FormulaMigrationItem> builder)
    {
        builder.ToTable("FormulaMigrationItems");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new
        {
            item.FormulaMigrationRunId,
            item.LegacyFormulaArtifactId,
            item.PlacementKey
        }).IsUnique();
        builder.Property(item => item.SourceHash).HasMaxLength(64).IsRequired();
        builder.Property(item => item.PlacementKey).HasMaxLength(120).IsRequired();
        builder.Property(item => item.Action).HasMaxLength(100).IsRequired();
        builder.Property(item => item.ApprovalReference).HasMaxLength(250);
        builder.Property(item => item.Error).HasMaxLength(4000);
        builder.Property(item => item.BeforeHash).HasMaxLength(64).IsRequired();
        builder.Property(item => item.AfterHash).HasMaxLength(64);
        builder.HasOne(item => item.FormulaMigrationRun).WithMany(item => item.Items)
            .HasForeignKey(item => item.FormulaMigrationRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.LegacyFormulaArtifact).WithMany()
            .HasForeignKey(item => item.LegacyFormulaArtifactId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_FormulaMigrationItem_Class",
                "\"MigrationClass\" IS NULL OR \"MigrationClass\" BETWEEN 0 AND 3");
            table.HasCheckConstraint("CK_FormulaMigrationItem_Status", "\"Status\" BETWEEN 0 AND 3");
            table.HasCheckConstraint("CK_FormulaMigrationItem_ApprovalScope",
                "\"ApprovalScope\" IS NULL OR \"ApprovalScope\" BETWEEN 0 AND 1");
        });
    }
}

public class FormulaReconciliationResultConfiguration
    : IEntityTypeConfiguration<FormulaReconciliationResult>
{
    public void Configure(EntityTypeBuilder<FormulaReconciliationResult> builder)
    {
        builder.ToTable("FormulaReconciliationResults");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.FormulaMigrationRunId, item.ControlName }).IsUnique();
        builder.Property(item => item.ControlName).HasMaxLength(150).IsRequired();
        builder.Property(item => item.ExpectedValue).HasMaxLength(2000).IsRequired();
        builder.Property(item => item.ActualValue).HasMaxLength(2000).IsRequired();
        builder.Property(item => item.EvidenceJson).HasColumnType("jsonb").IsRequired();
        builder.HasOne(item => item.FormulaMigrationRun).WithMany()
            .HasForeignKey(item => item.FormulaMigrationRunId).OnDelete(DeleteBehavior.Restrict);
    }
}
