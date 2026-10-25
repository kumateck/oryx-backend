using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Alerts;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace APP.Services.NotificationService;

public class NotificationDeliveryDispatcher(ApplicationDbContext context, INotificationDeliveryEffects effects,
    ILogger<NotificationDeliveryDispatcher> logger)
{
    public async Task<bool> DispatchOne(DateTime now, CancellationToken cancellationToken = default, bool emailOnly = false)
    {
        var row = await context.Set<NotificationDelivery>().Include(item => item.Notification)
            .Where(item => (item.Notification.AlertType == AlertType.Email) == emailOnly && item.CompletedAt == null && item.NextAttemptAt <= now && (item.LeaseUntil == null || item.LeaseUntil <= now))
            .OrderBy(item => item.NextAttemptAt).ThenBy(item => item.Id).FirstOrDefaultAsync(cancellationToken);
        if (row == null) return false;
        // Optimistic claim fencing works across API instances without holding a transaction during I/O.
        row.LeaseId = Guid.NewGuid();
        row.LeaseUntil = now.AddMinutes(2);
        row.Attempts++;
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { context.ChangeTracker.Clear(); return true; }
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            if (row.Kind == NotificationDeliveryKind.Read)
            {
                if (row.RecipientId == null) throw new InvalidOperationException("Read event requires its recipient.");
                await effects.Read(new NotificationReadEvent { Id = row.Id, NotificationId = row.NotificationId, UserId = row.RecipientId.Value }, timeout.Token);
                row.PublishedAt = DateTime.UtcNow; row.CompletedAt = DateTime.UtcNow; row.LeaseUntil = null;
                await context.SaveChangesAsync(cancellationToken);
                return true;
            }
            var recipients = await context.Users.Where(user => row.Notification.Recipients.Contains(user.Id) && !user.IsDisabled && user.DeletedAt == null)
                .Select(user => new UserDto { Id = user.Id, Email = user.Email, FirstName = user.FirstName, LastName = user.LastName }).ToListAsync(timeout.Token);
            if (recipients.Count == 0) row.LastErrorCode = "NoActiveRecipients";
            else
            {
                if (row.PublishedAt == null)
                {
                    var payload = new NotificationDto {
                        Id = row.Notification.Id, Message = row.Notification.Message, Type = row.Notification.Type,
                        SentAt = row.Notification.SentAt, ShipmentDocumentId = row.Notification.ShipmentDocumentId, Recipients = row.Notification.AlertType == AlertType.Email ? recipients : recipients.Select(user => new UserDto { Id = user.Id }).ToList(),
                    };
                    if (row.Notification.AlertType == AlertType.InApp) await effects.Publish(payload, timeout.Token);
                    else await effects.Email(payload, timeout.Token);
                    row.PublishedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync(cancellationToken);
                }
                // Mongo audit retries separately from publishing. Duplicate audit entries carry the same id.
                foreach (var recipient in recipients) await effects.Audit(row.Id, recipient.Id, timeout.Token);
                row.LastErrorCode = null;
            }
            row.CompletedAt = DateTime.UtcNow;
            row.LeaseUntil = null;
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception error)
        {
            // No payload, recipient PII, broker address, or credential enters the log.
            logger.LogWarning("Notification {NotificationId} delivery attempt {Attempt} failed ({ErrorType})", row.Id, row.Attempts, error.GetType().Name);
            row.LastErrorCode = error.GetType().Name[..Math.Min(100, error.GetType().Name.Length)];
            row.LeaseUntil = null;
            row.NextAttemptAt = now.AddSeconds(Math.Min(300, Math.Pow(2, Math.Min(row.Attempts, 8))));
            await context.SaveChangesAsync(cancellationToken);
        }
        return true;
    }
}
