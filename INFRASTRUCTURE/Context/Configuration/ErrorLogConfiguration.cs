using DOMAIN.Entities.ErrorLogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace INFRASTRUCTURE.Context;

public class ErrorLogConfiguration : IEntityTypeConfiguration<ErrorLog>
{
    public void Configure(EntityTypeBuilder<ErrorLog> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Level).HasMaxLength(16);
        builder.Property(e => e.Source).HasMaxLength(16);
        builder.Property(e => e.Category).HasMaxLength(256);
        builder.Property(e => e.ExceptionType).HasMaxLength(512);
        builder.Property(e => e.Fingerprint).HasMaxLength(32);
        builder.Property(e => e.TraceId).HasMaxLength(64);
        builder.Property(e => e.HttpMethod).HasMaxLength(16);
        builder.Property(e => e.Path).HasMaxLength(2048);
        builder.Property(e => e.RouteTemplate).HasMaxLength(512);
        builder.Property(e => e.UserEmail).HasMaxLength(256);
        builder.Property(e => e.IpAddress).HasMaxLength(64);
        builder.Property(e => e.Release).HasMaxLength(64);
        builder.Property(e => e.Instance).HasMaxLength(128);
        builder.Property(e => e.MachineName).HasMaxLength(128);

        builder.HasIndex(e => e.OccurredAt);
        builder.HasIndex(e => new { e.Fingerprint, e.OccurredAt });
        builder.HasIndex(e => e.TraceId);
        // Small partial index: only entries still waiting to be sent to the central monitor.
        builder.HasIndex(e => e.OccurredAt, "IX_ErrorLogs_Unforwarded").HasFilter("\"ForwardedAt\" IS NULL");
    }
}
