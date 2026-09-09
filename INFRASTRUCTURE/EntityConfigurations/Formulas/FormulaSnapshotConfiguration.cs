using DOMAIN.Entities.Formulas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.EntityConfigurations.Formulas;

public class ResponseFormulaSnapshotConfiguration : IEntityTypeConfiguration<ResponseFormulaSnapshot>
{
    public void Configure(EntityTypeBuilder<ResponseFormulaSnapshot> builder)
    {
        builder.ToTable("ResponseFormulaSnapshots");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.ResponseId, item.PlacementKey, item.Sequence }).IsUnique();
        builder.Property(item => item.PlacementKey).HasMaxLength(120).IsRequired();
        builder.Property(item => item.DefinitionHash).HasMaxLength(64).IsRequired();
        builder.Property(item => item.ConfigurationHash).HasMaxLength(64).IsRequired();
        builder.Property(item => item.ExecutableDefinitionJson).HasColumnType("jsonb").IsRequired();
        builder.Property(item => item.BindingsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(item => item.ResultTargetsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(item => item.TableShapeJson).HasColumnType("jsonb").IsRequired();
        builder.Property(item => item.CalculationPolicyJson).HasColumnType("jsonb").IsRequired();
        builder.Property(item => item.DisplayPolicyJson).HasColumnType("jsonb").IsRequired();
        builder.Property(item => item.MethodReference).HasMaxLength(250);
        builder.Property(item => item.ApprovalReference).HasMaxLength(250);
        builder.HasOne(item => item.Response).WithMany().HasForeignKey(item => item.ResponseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.FormulaRevision).WithMany()
            .HasForeignKey(item => item.FormulaRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.SupersedesSnapshot).WithMany()
            .HasForeignKey(item => item.SupersedesSnapshotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.CapturedBy).WithMany().HasForeignKey(item => item.CapturedById)
            .OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_ResponseFormulaSnapshot_Sequence", "\"Sequence\" > 0");
            table.HasCheckConstraint("CK_ResponseFormulaSnapshot_Reason", "\"Reason\" BETWEEN 0 AND 2");
            table.HasCheckConstraint("CK_ResponseFormulaSnapshot_DefinitionHash", "\"DefinitionHash\" ~ '^[a-f0-9]{64}$'");
            table.HasCheckConstraint("CK_ResponseFormulaSnapshot_ConfigurationHash", "\"ConfigurationHash\" ~ '^[a-f0-9]{64}$'");
        });
    }
}

public class FormulaExecutionConfiguration : IEntityTypeConfiguration<FormulaExecution>
{
    public void Configure(EntityTypeBuilder<FormulaExecution> builder)
    {
        builder.ToTable("FormulaExecutions");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.ResponseFormulaSnapshotId, item.IdempotencyKey }).IsUnique();
        builder.Property(item => item.IdempotencyKey).HasMaxLength(120).IsRequired();
        builder.Property(item => item.EngineVersion).HasMaxLength(64).IsRequired();
        builder.Property(item => item.EngineBuildHash).HasMaxLength(64).IsRequired();
        builder.Property(item => item.FormulaLanguageVersion).HasMaxLength(64).IsRequired();
        builder.Property(item => item.NumericPolicyVersion).HasMaxLength(64).IsRequired();
        builder.Property(item => item.InputHash).HasMaxLength(64).IsRequired();
        builder.Property(item => item.ResultHash).HasMaxLength(64);
        builder.Property(item => item.ResolvedInputsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(item => item.RawResultsJson).HasColumnType("jsonb");
        builder.Property(item => item.RoundedResultsJson).HasColumnType("jsonb");
        builder.Property(item => item.DisplayResultsJson).HasColumnType("jsonb");
        builder.Property(item => item.CalculationTraceJson).HasColumnType("jsonb").IsRequired();
        builder.HasOne(item => item.ResponseFormulaSnapshot).WithMany()
            .HasForeignKey(item => item.ResponseFormulaSnapshotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.SupersedesExecution).WithMany()
            .HasForeignKey(item => item.SupersedesExecutionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.Actor).WithMany().HasForeignKey(item => item.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_FormulaExecution_Authority", "\"Authority\" BETWEEN 0 AND 1");
            table.HasCheckConstraint("CK_FormulaExecution_Trigger", "\"Trigger\" BETWEEN 0 AND 3");
            table.HasCheckConstraint("CK_FormulaExecution_Status", "\"Status\" BETWEEN 0 AND 7");
            table.HasCheckConstraint("CK_FormulaExecution_InputHash", "\"InputHash\" ~ '^[a-f0-9]{64}$'");
            table.HasCheckConstraint("CK_FormulaExecution_ResultHash", "\"ResultHash\" IS NULL OR \"ResultHash\" ~ '^[a-f0-9]{64}$'");
            table.HasCheckConstraint("CK_FormulaExecution_TraceSize", "octet_length(\"CalculationTraceJson\"::text) <= 65536");
        });
    }
}
