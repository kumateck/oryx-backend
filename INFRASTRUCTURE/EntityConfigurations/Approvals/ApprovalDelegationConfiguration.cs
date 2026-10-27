using DOMAIN.Entities.Approvals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.EntityConfigurations.Approvals;

/// <summary>
/// Approval delegations: who handed their approvals to whom, and for when. Both users are kept with
/// <c>Restrict</c> so a delegation always says who it was between, even after it ends.
/// </summary>
public class ApprovalDelegationConfiguration : IEntityTypeConfiguration<ApprovalDelegation>
{
    public void Configure(EntityTypeBuilder<ApprovalDelegation> builder)
    {
        builder.ToTable("ApprovalDelegations");
        builder.HasQueryFilter(item => !item.DeletedAt.HasValue);
        builder.Property(item => item.Reason).HasMaxLength(500);
        builder.HasOne(item => item.Delegator).WithMany().HasForeignKey(item => item.DelegatorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.Delegate).WithMany().HasForeignKey(item => item.DelegateId).OnDelete(DeleteBehavior.Restrict);
        // Who may act for whom right now is asked on every approval request.
        builder.HasIndex(item => new { item.DelegateId, item.StartsAt, item.EndsAt });
        builder.HasIndex(item => new { item.DelegatorId, item.StartsAt, item.EndsAt });
    }
}
