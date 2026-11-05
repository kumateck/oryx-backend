using DOMAIN.Entities.Formulas;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.EntityConfigurations.Formulas;

public sealed class QuestionFormulaRepairConfiguration : IEntityTypeConfiguration<QuestionFormulaRepair>
{
    public void Configure(EntityTypeBuilder<QuestionFormulaRepair> b)
    {
        b.ToTable("QuestionFormulaRepairs"); b.HasKey(x => x.Id);
        b.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        b.Property(x => x.PreviewToken).HasMaxLength(64).IsRequired();
        b.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.EvidenceJson).HasColumnType("jsonb").IsRequired();
        b.Property(x => x.DefinitionBefore).IsRequired(); b.Property(x => x.DefinitionAfter).IsRequired();
        b.HasOne<Question>().WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class LegacyFormulaPinConfiguration : IEntityTypeConfiguration<LegacyFormulaConfigurationPin>
{
    public void Configure(EntityTypeBuilder<LegacyFormulaConfigurationPin> b)
    {
        b.ToTable("LegacyFormulaConfigurationPins"); b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.ResponseId, x.FormFieldId, x.Sequence }).IsUnique();
        b.Property(x => x.Definition).IsRequired(); b.Property(x => x.DefinitionHash).HasMaxLength(64).IsRequired();
        b.HasOne<Response>().WithMany().HasForeignKey(x => x.ResponseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<FormField>().WithMany().HasForeignKey(x => x.FormFieldId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<QuestionFormulaRepair>().WithMany().HasForeignKey(x => x.RepairId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class LegacyFormulaEvaluationConfiguration : IEntityTypeConfiguration<LegacyFormulaRepairEvaluation>
{
    public void Configure(EntityTypeBuilder<LegacyFormulaRepairEvaluation> b)
    {
        b.ToTable("LegacyFormulaRepairEvaluations"); b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.PinId, x.InputHash }).IsUnique();
        b.Property(x => x.InputHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.EngineBuildHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.ResultsJson).HasColumnType("jsonb").IsRequired();
        b.Property(x => x.EvidenceJson).HasColumnType("jsonb").IsRequired();
        b.HasOne<LegacyFormulaConfigurationPin>().WithMany().HasForeignKey(x => x.PinId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
    }
}
