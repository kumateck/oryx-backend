using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.EntityConfigurations.Forms;

public sealed class TemplateRepairConfiguration : IEntityTypeConfiguration<TemplateRepair>
{
    public void Configure(EntityTypeBuilder<TemplateRepair> b)
    {
        b.ToTable("TemplateRepairs"); b.HasKey(x => x.Id);
        b.HasIndex(x => x.FormId);
        b.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        b.Property(x => x.PreviewToken).HasMaxLength(64).IsRequired();
        b.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.EvidenceJson).HasColumnType("jsonb").IsRequired();
        b.HasOne<Form>().WithMany().HasForeignKey(x => x.FormId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TemplateRepairAssignmentConfiguration : IEntityTypeConfiguration<TemplateRepairAssignment>
{
    public void Configure(EntityTypeBuilder<TemplateRepairAssignment> b)
    {
        b.ToTable("TemplateRepairAssignments"); b.HasKey(x => x.Id);
        b.HasIndex(x => x.RepairId);
        b.HasIndex(x => new { x.FormAssigneeId, x.FormFieldId });
        b.HasOne<TemplateRepair>().WithMany().HasForeignKey(x => x.RepairId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<FormAssignee>().WithMany().HasForeignKey(x => x.FormAssigneeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<FormField>().WithMany().HasForeignKey(x => x.FormFieldId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.AssigneeId).OnDelete(DeleteBehavior.Restrict);
    }
}
