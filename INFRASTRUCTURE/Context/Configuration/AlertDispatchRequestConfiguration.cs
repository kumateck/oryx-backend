using DOMAIN.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.NotificationConfiguration;
public sealed class AlertDispatchRequestConfiguration : IEntityTypeConfiguration<AlertDispatchRequest>
{
    public void Configure(EntityTypeBuilder<AlertDispatchRequest> builder)
    {
        builder.ToTable("AlertDispatchRequests");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.SourceKey).HasMaxLength(255);
        builder.HasIndex(item => item.SourceKey).IsUnique();
        builder.Property(item => item.Message).HasMaxLength(100000);
        builder.Property(item => item.LastErrorCode).HasMaxLength(100);
        builder.Property(item => item.LeaseId).IsConcurrencyToken();
        builder.HasIndex(item => new { item.CompletedAt, item.NextAttemptAt });
    }
}
