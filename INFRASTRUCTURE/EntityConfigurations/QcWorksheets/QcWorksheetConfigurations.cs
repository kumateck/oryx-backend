using DOMAIN.Entities.QcWorksheets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.EntityConfigurations.QcWorksheets;

/// <summary>
/// Persistence mapping for the rebuilt QC module. Every table here is new; nothing in the
/// live Material/Product/Packaging QC schema is referenced or altered.
/// </summary>
public class QcApprovalConfiguration : IEntityTypeConfiguration<QcApproval>
{
    public void Configure(EntityTypeBuilder<QcApproval> builder)
    {
        builder.ToTable("QcApprovals");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Comments).HasMaxLength(1000);

        // The table spans multiple entity tables, so it is addressed by this pair rather
        // than by an FK. This index is what makes that lookup cheap.
        builder.HasIndex(item => new { item.EntityType, item.EntityId });

        // Mirrors the ResponseApproval uniqueness rule: one stage row per approver per
        // round per entity.
        builder
            .HasIndex(item => new
            {
                item.ApprovalId,
                item.EntityType,
                item.EntityId,
                item.ApprovalRound,
                item.Order,
                item.UserId,
                item.RoleId
            })
            .IsUnique();

        builder
            .HasOne(item => item.Approval)
            .WithMany()
            .HasForeignKey(item => item.ApprovalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(item => item.User)
            .WithMany()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(item => item.Role)
            .WithMany()
            .HasForeignKey(item => item.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(item => item.ApprovedBy)
            .WithMany()
            .HasForeignKey(item => item.ApprovedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class StandardTestProcedureConfiguration : IEntityTypeConfiguration<StandardTestProcedure>
{
    public void Configure(EntityTypeBuilder<StandardTestProcedure> builder)
    {
        builder.ToTable("QcStandardTestProcedures");
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);

        builder.Property(item => item.Code).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Name).HasMaxLength(500).IsRequired();
        builder.Property(item => item.Area).HasMaxLength(200);

        builder.HasIndex(item => item.Code);
        builder.HasIndex(item => item.Status);

        builder
            .HasOne(item => item.Supersedes)
            .WithMany()
            .HasForeignKey(item => item.SupersedesId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(item => item.Steps)
            .WithOne(item => item.StandardTestProcedure)
            .HasForeignKey(item => item.StandardTestProcedureId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class StpStepConfiguration : IEntityTypeConfiguration<StpStep>
{
    public void Configure(EntityTypeBuilder<StpStep> builder)
    {
        builder.ToTable("QcStpSteps");
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);

        builder.Property(item => item.Title).HasMaxLength(200);
        builder.Property(item => item.Instruction).IsRequired();

        builder.HasIndex(item => new { item.StandardTestProcedureId, item.Order });

        // The structured cross-reference. Restrict: an STP referenced by another
        // document's step must not disappear underneath it.
        builder
            .HasOne(item => item.ReferencedStp)
            .WithMany()
            .HasForeignKey(item => item.ReferencedStpId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WorksheetTemplateConfiguration : IEntityTypeConfiguration<WorksheetTemplate>
{
    public void Configure(EntityTypeBuilder<WorksheetTemplate> builder)
    {
        builder.ToTable("QcWorksheetTemplates");
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);

        builder.Property(item => item.Code).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Name).HasMaxLength(500).IsRequired();
        builder.Property(item => item.Department).HasMaxLength(200);

        builder.HasIndex(item => item.Code);
        builder.HasIndex(item => item.Status);

        builder
            .HasOne(item => item.Supersedes)
            .WithMany()
            .HasForeignKey(item => item.SupersedesId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(item => item.Stp)
            .WithMany()
            .HasForeignKey(item => item.StpId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(item => item.Sections)
            .WithOne(item => item.WorksheetTemplate)
            .HasForeignKey(item => item.WorksheetTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WorksheetSectionConfiguration : IEntityTypeConfiguration<WorksheetSection>
{
    public void Configure(EntityTypeBuilder<WorksheetSection> builder)
    {
        builder.ToTable("QcWorksheetSections");
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);

        builder.Property(item => item.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(item => new { item.WorksheetTemplateId, item.Order });

        builder
            .HasMany(item => item.Fields)
            .WithOne(item => item.WorksheetSection)
            .HasForeignKey(item => item.WorksheetSectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WorksheetFieldConfiguration : IEntityTypeConfiguration<WorksheetField>
{
    public void Configure(EntityTypeBuilder<WorksheetField> builder)
    {
        builder.ToTable("QcWorksheetFields");
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);

        builder.Property(item => item.FieldKey).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Label).HasMaxLength(500).IsRequired();
        builder.Property(item => item.Unit).HasMaxLength(50);
        builder.Property(item => item.Analyte).HasMaxLength(200);
        builder.Property(item => item.ReferencedResultSourceFieldKey).HasMaxLength(100);
        builder.Property(item => item.ReferencedResultResolutionFieldKey).HasMaxLength(100);

        // FieldKey uniqueness is worksheet-scoped, not section-scoped, so it cannot be a
        // unique index on this table (the template is one join away). It is enforced in
        // WorksheetTemplateRepository. This index serves formula resolution lookups.
        builder.HasIndex(item => new { item.WorksheetSectionId, item.FieldKey });

        builder
            .HasOne(item => item.ReferencedResultSourceTemplate)
            .WithMany()
            .HasForeignKey(item => item.ReferencedResultSourceTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(item => item.Revisions)
            .WithOne(item => item.WorksheetField)
            .HasForeignKey(item => item.WorksheetFieldId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WorksheetFieldRevisionConfiguration : IEntityTypeConfiguration<WorksheetFieldRevision>
{
    public void Configure(EntityTypeBuilder<WorksheetFieldRevision> builder)
    {
        builder.ToTable("QcWorksheetFieldRevisions");
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);

        builder.Property(item => item.FieldKey).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Label).HasMaxLength(500);
        builder.Property(item => item.Unit).HasMaxLength(50);
        builder.Property(item => item.Analyte).HasMaxLength(200);
        builder.Property(item => item.ReferencedResultSourceFieldKey).HasMaxLength(100);
        builder.Property(item => item.ReferencedResultResolutionFieldKey).HasMaxLength(100);

        builder.HasIndex(item => new { item.WorksheetFieldId, item.RevisionNumber }).IsUnique();
    }
}
