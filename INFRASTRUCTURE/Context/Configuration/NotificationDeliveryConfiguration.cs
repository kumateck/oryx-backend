using DOMAIN.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.NotificationConfiguration;

public sealed class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.ToTable("NotificationDeliveries");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.LeaseId).IsConcurrencyToken();
        builder.Property(item => item.LastErrorCode).HasMaxLength(100);
        builder.HasIndex(item => new { item.CompletedAt, item.NextAttemptAt });
        builder.HasOne(item => item.Notification).WithMany().HasForeignKey(item => item.NotificationId).OnDelete(DeleteBehavior.Restrict);
    }
}
