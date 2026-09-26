using DOMAIN.Entities.QcWorksheets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.EntityConfigurations.QcWorksheets;

/// <summary>
/// Persistence mapping for Milestone 2. Every table here is new; nothing in the live
/// Material/Product/Packaging QC schema — including <c>MaterialSpecification</c> and
/// <c>ProductSpecification</c> — is referenced or altered.
/// </summary>
public class SamplingPointGroupConfiguration : IEntityTypeConfiguration<SamplingPointGroup>
{
    public void Configure(EntityTypeBuilder<SamplingPointGroup> builder)
    {
        builder.ToTable("QcSamplingPointGroups");
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);

        builder.Property(item => item.Name).HasMaxLength(200).IsRequired();

        // Not a unique index: rows are soft-deleted (DeletedAt), so a unique constraint would
        // keep a deleted group's name reserved forever. Uniqueness among live rows is
        // enforced in SamplingPointGroupRepository, which sees the soft-delete query filter.
        builder.HasIndex(item => item.Name);
    }
}

public class SpecificationConfiguration : IEntityTypeConfiguration<Specification>
{
    public void Configure(EntityTypeBuilder<Specification> builder)
    {
        builder.ToTable("QcSpecifications");
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);

        builder.Property(item => item.Code).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Name).HasMaxLength(500).IsRequired();

        builder.HasIndex(item => item.Code);
        builder.HasIndex(item => item.Status);
        builder.HasIndex(item => new { item.AppliesTo, item.Stage });

        builder
            .HasOne(item => item.Supersedes)
            .WithMany()
            .HasForeignKey(item => item.SupersedesId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(item => item.WorksheetLinks)
            .WithOne(item => item.Specification)
            .HasForeignKey(item => item.SpecificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(item => item.Characteristics)
            .WithOne(item => item.Specification)
            .HasForeignKey(item => item.SpecificationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SpecificationWorksheetLinkConfiguration : IEntityTypeConfiguration<SpecificationWorksheetLink>
{
    public void Configure(EntityTypeBuilder<SpecificationWorksheetLink> builder)
    {
        builder.ToTable("QcSpecificationWorksheetLinks");
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);

        // "At most one Chemical and one Microbial link" is deliberately NOT a unique index on
        // (SpecificationId, AnalysisType): replacing a link during a Draft edit legitimately
        // writes the new row before the old one clears, and a soft-deleted row would keep the
        // slot occupied. Enforced in SpecificationRepository instead, per the brief.
        builder.HasIndex(item => new { item.SpecificationId, item.AnalysisType });

        // The pinned version travels with the link, mirroring TestRequest's
        // SpecificationId + SpecificationVersion pair: the id points at one immutable version
        // row, and this records which version that is without needing a join. Indexed because
        // "which specifications are pinned to this template version" is the question asked
        // when a template version is revised.
        builder.HasIndex(item => new { item.WorksheetTemplateId, item.WorksheetTemplateVersion });

        // Restrict: a worksheet template a specification depends on must not vanish
        // underneath it.
        builder
            .HasOne(item => item.WorksheetTemplate)
            .WithMany()
            .HasForeignKey(item => item.WorksheetTemplateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SpecificationCharacteristicConfiguration : IEntityTypeConfiguration<SpecificationCharacteristic>
{
    public void Configure(EntityTypeBuilder<SpecificationCharacteristic> builder)
    {
        builder.ToTable("QcSpecificationCharacteristics");
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);

        builder.Property(item => item.TestName).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Analyte).HasMaxLength(200);
        builder.Property(item => item.AcceptanceCriteria).HasMaxLength(2000).IsRequired();
        builder.Property(item => item.AlertLimit).HasMaxLength(500);
        builder.Property(item => item.ActionLimit).HasMaxLength(500);
        builder.Property(item => item.SourceFieldKey).HasMaxLength(100).IsRequired();
        builder.Property(item => item.GroupName).HasMaxLength(200);

        builder.HasIndex(item => new { item.SpecificationId, item.DisplayOrder });

        // Deliberately non-unique. One EM test gets several rows sharing this pair, separated
        // by SamplingPointGroupId and carrying different Alert/Action tiers.
        builder.HasIndex(item => new { item.SourceWorksheetTemplateId, item.SourceFieldKey });

        builder
            .HasOne(item => item.SourceWorksheetTemplate)
            .WithMany()
            .HasForeignKey(item => item.SourceWorksheetTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(item => item.SamplingPointGroup)
            .WithMany()
            .HasForeignKey(item => item.SamplingPointGroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>
/// Build brief 08. A new table only: the proposal sets reference the existing template and
/// Specification tables and alter neither.
/// </summary>
public class SpecificationProposalSetConfiguration : IEntityTypeConfiguration<SpecificationProposalSet>
{
    public void Configure(EntityTypeBuilder<SpecificationProposalSet> builder)
    {
        builder.ToTable("QcSpecProposalSets");
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);

        builder.Property(item => item.SourceFileName).HasMaxLength(500).IsRequired();
        builder.Property(item => item.ProductName).HasMaxLength(500);
        builder.Property(item => item.SpecificationCode).HasMaxLength(100);
        builder.Property(item => item.ProposalJson).HasColumnType("jsonb").IsRequired();
        builder.Property(item => item.DismissReason).HasMaxLength(2000);

        // The review page lists Pending sets by family.
        builder.HasIndex(item => new { item.Status, item.Family });

        builder
            .HasOne(item => item.WorksheetTemplate)
            .WithMany()
            .HasForeignKey(item => item.WorksheetTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(item => item.AppliedSpecification)
            .WithMany()
            .HasForeignKey(item => item.AppliedSpecificationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
