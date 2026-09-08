using DOMAIN.Entities.Formulas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.EntityConfigurations.Formulas;

public class ResponseFormulaSubmissionSetConfiguration
    : IEntityTypeConfiguration<ResponseFormulaSubmissionSet>
{
    public void Configure(EntityTypeBuilder<ResponseFormulaSubmissionSet> builder)
    {
        builder.ToTable("ResponseFormulaSubmissionSets");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.ResponseId, item.Sequence }).IsUnique();
        builder.HasIndex(item => new { item.ResponseId, item.SetHash }).IsUnique();
        builder.Property(item => item.SetHash).HasMaxLength(64).IsRequired();
        builder.Property(item => item.InputAggregateHash).HasMaxLength(64).IsRequired();
        builder.Property(item => item.ConfigurationAggregateHash).HasMaxLength(64).IsRequired();
        builder.HasOne(item => item.Response).WithMany().HasForeignKey(item => item.ResponseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.SubmittedBy).WithMany().HasForeignKey(item => item.SubmittedById)
            .OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_ResponseFormulaSubmissionSet_Sequence", "\"Sequence\" > 0"));
    }
}

public class ResponseFormulaSubmissionExecutionConfiguration
    : IEntityTypeConfiguration<ResponseFormulaSubmissionExecution>
{
    public void Configure(EntityTypeBuilder<ResponseFormulaSubmissionExecution> builder)
    {
        builder.ToTable("ResponseFormulaSubmissionExecutions");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new
        {
            item.ResponseFormulaSubmissionSetId,
            item.PlacementKey
        }).IsUnique();
        builder.HasIndex(item => new
        {
            item.ResponseFormulaSubmissionSetId,
            item.Ordinal
        }).IsUnique();
        builder.Property(item => item.PlacementKey).HasMaxLength(120).IsRequired();
        builder.HasOne(item => item.ResponseFormulaSubmissionSet)
            .WithMany(item => item.Executions)
            .HasForeignKey(item => item.ResponseFormulaSubmissionSetId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.FormulaExecution).WithMany()
            .HasForeignKey(item => item.FormulaExecutionId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_ResponseFormulaSubmissionExecution_Ordinal", "\"Ordinal\" >= 0"));
    }
}
