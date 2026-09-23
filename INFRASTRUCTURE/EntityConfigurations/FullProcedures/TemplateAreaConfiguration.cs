using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.EntityConfigurations.FullProcedures;

public sealed class TemplateAreaConfiguration : IEntityTypeConfiguration<TemplateArea>
{
    public void Configure(EntityTypeBuilder<TemplateArea> builder)
    {
        builder.ToTable("TemplateAreas", table =>
        {
            table.HasCheckConstraint("CK_TemplateArea_Name", "btrim(\"Name\") <> ''");
            table.HasCheckConstraint("CK_TemplateArea_Version", "\"Version\" > 0");
        });
        builder.Property(item => item.Name).HasMaxLength(100).IsRequired();
        builder.Property(item => item.NormalizedName).HasMaxLength(100).IsRequired();
        builder.Property(item => item.ReviewPolicyId).HasMaxLength(120).IsRequired();
        builder.Property(item => item.Version).IsConcurrencyToken();
        builder.HasIndex(item => item.NormalizedName).IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");
        builder.HasOne(item => item.OwnerRole).WithMany()
            .HasForeignKey(item => item.OwnerRoleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);
    }
}

public abstract class TemplateAreaBindingConfiguration<T> : IEntityTypeConfiguration<T>
    where T : class
{
    protected abstract string TableName { get; }
    protected abstract string ValueProperty { get; }

    public void Configure(EntityTypeBuilder<T> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey("Id");
        builder.Property(ValueProperty).HasMaxLength(120).IsRequired();
        builder.HasIndex("TemplateAreaId", ValueProperty).IsUnique();
        builder.HasOne<TemplateArea>("TemplateArea").WithMany(TableName switch
            {
                "TemplateAreaPurposes" => nameof(TemplateArea.Purposes),
                "TemplateAreaSubjectTypes" => nameof(TemplateArea.SubjectTypes),
                _ => nameof(TemplateArea.Capabilities),
            })
            .HasForeignKey("TemplateAreaId").OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TemplateAreaPurposeConfiguration
    : TemplateAreaBindingConfiguration<TemplateAreaPurpose>
{
    protected override string TableName => "TemplateAreaPurposes";
    protected override string ValueProperty => nameof(TemplateAreaPurpose.PurposeId);
}

public sealed class TemplateAreaSubjectTypeConfiguration
    : TemplateAreaBindingConfiguration<TemplateAreaSubjectType>
{
    protected override string TableName => "TemplateAreaSubjectTypes";
    protected override string ValueProperty => nameof(TemplateAreaSubjectType.SubjectTypeId);
}

public sealed class TemplateAreaCapabilityConfiguration
    : TemplateAreaBindingConfiguration<TemplateAreaCapability>
{
    protected override string TableName => "TemplateAreaCapabilities";
    protected override string ValueProperty => nameof(TemplateAreaCapability.CapabilityId);
}

public sealed class TemplateAreaRoleGrantConfiguration
    : IEntityTypeConfiguration<TemplateAreaRoleGrant>
{
    public void Configure(EntityTypeBuilder<TemplateAreaRoleGrant> builder)
    {
        builder.ToTable("TemplateAreaRoleGrants", table => table.HasCheckConstraint(
            "CK_TemplateAreaRoleGrant_AccessLevel", "\"AccessLevel\" BETWEEN 0 AND 4"));
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.TemplateAreaId, item.RoleId }).IsUnique();
        builder.HasOne(item => item.TemplateArea).WithMany(item => item.RoleGrants)
            .HasForeignKey(item => item.TemplateAreaId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.Role).WithMany().HasForeignKey(item => item.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TemplateAreaAuditConfiguration : IEntityTypeConfiguration<TemplateAreaAudit>
{
    public void Configure(EntityTypeBuilder<TemplateAreaAudit> builder)
    {
        builder.ToTable("TemplateAreaAudits", table =>
            table.HasCheckConstraint("CK_TemplateAreaAudit_SnapshotHash",
                "\"SnapshotHash\" ~ '^[a-f0-9]{64}$'"));
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.TemplateAreaId, item.Version }).IsUnique();
        builder.Property(item => item.Action).HasMaxLength(80).IsRequired();
        builder.Property(item => item.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(item => item.SnapshotJson).HasColumnType("jsonb").IsRequired();
        builder.Property(item => item.SnapshotHash).HasMaxLength(64).IsRequired();
        builder.HasOne(item => item.TemplateArea).WithMany(item => item.Audits)
            .HasForeignKey(item => item.TemplateAreaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.Actor).WithMany().HasForeignKey(item => item.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
