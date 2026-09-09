using DOMAIN.Entities.Formulas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.EntityConfigurations.Formulas;

public class FormRevisionConfiguration : IEntityTypeConfiguration<FormRevision>
{
    public void Configure(EntityTypeBuilder<FormRevision> builder)
    {
        builder.ToTable("FormRevisions");
        builder.HasIndex(item => new { item.FormId, item.Sequence }).IsUnique();
        builder.HasIndex(item => item.FormId).IsUnique()
            .HasFilter("\"Status\" = 2 AND \"DeletedAt\" IS NULL");
        builder.Property(item => item.ContentHash).HasMaxLength(64).IsRequired();
        builder.HasOne(item => item.Form).WithMany().HasForeignKey(item => item.FormId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.ApprovedBy).WithMany().HasForeignKey(item => item.ApprovedById)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.ReviewedBy).WithMany().HasForeignKey(item => item.ReviewedById)
            .OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_FormRevision_Sequence", "\"Sequence\" > 0");
            table.HasCheckConstraint("CK_FormRevision_Status", "\"Status\" BETWEEN 0 AND 3");
            table.HasCheckConstraint("CK_FormRevision_ContentHash", "\"ContentHash\" ~ '^[a-f0-9]{64}$'");
        });
    }
}

public class FormRevisionAuditConfiguration : IEntityTypeConfiguration<FormRevisionAudit>
{
    public void Configure(EntityTypeBuilder<FormRevisionAudit> builder)
    {
        builder.ToTable("FormRevisionAudits");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.FormRevisionId, item.OccurredAt });
        builder.Property(item => item.Action).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Reason).HasMaxLength(4000).IsRequired();
        builder.Property(item => item.ContentHash).HasMaxLength(64).IsRequired();
        builder.HasOne(item => item.FormRevision).WithMany()
            .HasForeignKey(item => item.FormRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.Actor).WithMany()
            .HasForeignKey(item => item.ActorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class FormFieldRevisionConfiguration : IEntityTypeConfiguration<FormFieldRevision>
{
    public void Configure(EntityTypeBuilder<FormFieldRevision> builder)
    {
        builder.ToTable("FormFieldRevisions");
        builder.HasIndex(item => new { item.FormRevisionId, item.PlacementKey }).IsUnique();
        builder.Property(item => item.PlacementKey).HasMaxLength(120).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(1000000);
        builder.Property(item => item.FieldHash).HasMaxLength(64).IsRequired();
        builder.HasOne(item => item.FormRevision).WithMany(item => item.Fields)
            .HasForeignKey(item => item.FormRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.FormField).WithMany().HasForeignKey(item => item.FormFieldId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.Question).WithMany().HasForeignKey(item => item.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class FormFieldFormulaConfigurationMap : IEntityTypeConfiguration<FormFieldFormulaConfiguration>
{
    public void Configure(EntityTypeBuilder<FormFieldFormulaConfiguration> builder)
    {
        builder.ToTable("FormFieldFormulaConfigurations");
        builder.HasIndex(item => item.FormFieldRevisionId).IsUnique();
        builder.HasIndex(item => item.ConfigurationHash);
        builder.Property(item => item.BindingsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(item => item.ResultTargetsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(item => item.DisplayPolicyJson).HasColumnType("jsonb").IsRequired();
        builder.Property(item => item.MethodReference).HasMaxLength(250);
        builder.Property(item => item.ConfigurationHash).HasMaxLength(64).IsRequired();
        builder.HasOne(item => item.FormFieldRevision).WithOne(item => item.FormulaConfiguration)
            .HasForeignKey<FormFieldFormulaConfiguration>(item => item.FormFieldRevisionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.FormulaRevision).WithMany()
            .HasForeignKey(item => item.FormulaRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_FormFieldFormulaConfiguration_Hash", "\"ConfigurationHash\" ~ '^[a-f0-9]{64}$'"));
    }
}
