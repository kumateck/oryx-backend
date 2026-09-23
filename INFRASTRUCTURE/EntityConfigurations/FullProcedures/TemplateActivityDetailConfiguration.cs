using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.EntityConfigurations.FullProcedures;

public sealed class TemplateActivityActionConfiguration
    : IEntityTypeConfiguration<TemplateActivityAction>
{
    public void Configure(EntityTypeBuilder<TemplateActivityAction> builder)
    {
        builder.ToTable("TemplateActivityActions", table =>
        {
            table.HasCheckConstraint("CK_TemplateActivityAction_Order", "\"Order\" >= 0");
            table.HasCheckConstraint("CK_TemplateActivityAction_Type", "\"ActionType\" BETWEEN 0 AND 3");
            table.HasCheckConstraint("CK_TemplateActivityAction_Approval",
                "\"ActionType\" <> 2 OR \"RequiresApproval\"");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Key).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Name).HasMaxLength(150).IsRequired();
        builder.HasIndex(item => new { item.TemplateActivityRevisionId, item.Key }).IsUnique();
        builder.HasIndex(item => new { item.TemplateActivityRevisionId, item.Order }).IsUnique();
        builder.HasOne(item => item.TemplateActivityRevision).WithMany(item => item.Actions)
            .HasForeignKey(item => item.TemplateActivityRevisionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TemplateActivityActionRoleConfiguration
    : IEntityTypeConfiguration<TemplateActivityActionRole>
{
    public void Configure(EntityTypeBuilder<TemplateActivityActionRole> builder)
    {
        builder.ToTable("TemplateActivityActionRoles", table => table.HasCheckConstraint(
            "CK_TemplateActivityActionRole_Kind", "\"RoleKind\" BETWEEN 0 AND 2"));
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new
            { item.TemplateActivityActionId, item.RoleId, item.RoleKind }).IsUnique();
        builder.HasOne(item => item.TemplateActivityAction).WithMany(item => item.Roles)
            .HasForeignKey(item => item.TemplateActivityActionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.Role).WithMany().HasForeignKey(item => item.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TemplateActivityDataBindingConfiguration
    : IEntityTypeConfiguration<TemplateActivityDataBinding>
{
    public void Configure(EntityTypeBuilder<TemplateActivityDataBinding> builder)
    {
        builder.ToTable("TemplateActivityDataBindings", table =>
        {
            table.HasCheckConstraint("CK_TemplateActivityDataBinding_Order", "\"Order\" >= 0");
            table.HasCheckConstraint("CK_TemplateActivityDataBinding_Direction", "\"Direction\" BETWEEN 0 AND 1");
            table.HasCheckConstraint("CK_TemplateActivityDataBinding_Type", "\"DataType\" BETWEEN 0 AND 6");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Key).HasMaxLength(100).IsRequired();
        builder.HasIndex(item => new { item.TemplateActivityRevisionId, item.Key }).IsUnique();
        builder.HasIndex(item => new { item.TemplateActivityRevisionId, item.Order }).IsUnique();
        builder.HasOne(item => item.TemplateActivityRevision).WithMany(item => item.DataBindings)
            .HasForeignKey(item => item.TemplateActivityRevisionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TemplateActivityCompletionRuleConfiguration
    : IEntityTypeConfiguration<TemplateActivityCompletionRule>
{
    public void Configure(EntityTypeBuilder<TemplateActivityCompletionRule> builder)
    {
        builder.ToTable("TemplateActivityCompletionRules", table =>
        {
            table.HasCheckConstraint("CK_TemplateActivityCompletionRule_Order", "\"Order\" >= 0");
            table.HasCheckConstraint("CK_TemplateActivityCompletionRule_Type", "\"RuleType\" BETWEEN 0 AND 4");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.TargetKey).HasMaxLength(100);
        builder.HasIndex(item => new { item.TemplateActivityRevisionId, item.Order }).IsUnique();
        builder.HasIndex(item => new
            { item.TemplateActivityRevisionId, item.RuleType, item.TargetKey }).IsUnique();
        builder.HasOne(item => item.TemplateActivityRevision).WithMany(item => item.CompletionRules)
            .HasForeignKey(item => item.TemplateActivityRevisionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TemplateActivityRevisionAuditConfiguration
    : IEntityTypeConfiguration<TemplateActivityRevisionAudit>
{
    public void Configure(EntityTypeBuilder<TemplateActivityRevisionAudit> builder)
    {
        builder.ToTable("TemplateActivityRevisionAudits", table =>
        {
            table.HasCheckConstraint("CK_TemplateActivityRevisionAudit_ContentHash",
                "\"ContentHash\" ~ '^[a-f0-9]{64}$'");
            table.HasCheckConstraint("CK_TemplateActivityRevisionAudit_Status",
                "\"NewStatus\" BETWEEN 0 AND 3 AND " +
                "(\"PriorStatus\" IS NULL OR \"PriorStatus\" BETWEEN 0 AND 3)");
        });
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.TemplateActivityRevisionId, item.OccurredAt });
        builder.HasIndex(item => item.CorrelationId);
        builder.Property(item => item.Action).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Reason).HasMaxLength(4000).IsRequired();
        builder.Property(item => item.ContentHash).HasMaxLength(64).IsRequired();
        builder.Property(item => item.SnapshotJson).HasColumnType("jsonb").IsRequired();
        builder.HasOne(item => item.TemplateActivityRevision).WithMany(item => item.Audits)
            .HasForeignKey(item => item.TemplateActivityRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.Actor).WithMany().HasForeignKey(item => item.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
