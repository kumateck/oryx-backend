using APP.IRepository;
using DOMAIN.Entities.Notifications;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace APP.Services.NotificationService;
public class AlertRequestDispatcher(ApplicationDbContext context, IAlertRepository alerts, ILogger<AlertRequestDispatcher> logger)
{
    public async Task<bool> DispatchOne(DateTime now, CancellationToken cancellationToken)
    {
        var row = await context.Set<AlertDispatchRequest>()
            .Where(item => item.CompletedAt == null && item.NextAttemptAt <= now && (item.LeaseUntil == null || item.LeaseUntil <= now))
            .OrderBy(item => item.NextAttemptAt).ThenBy(item => item.Id).FirstOrDefaultAsync(cancellationToken);
        if (row == null) return false;
        row.LeaseId = Guid.NewGuid(); row.LeaseUntil = now.AddMinutes(2); row.Attempts++;
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { context.ChangeTracker.Clear(); return true; }
        try
        {
            // Recipient resolution and notification/outbox creation are one PostgreSQL transaction.
            await using var transaction = context.Database.IsRelational() ? await context.Database.BeginTransactionAsync(cancellationToken) : null;
            var assigned = await context.Users.Where(user => row.AssignedUserIds.Contains(user.Id) && !user.IsDisabled && user.DeletedAt == null).ToListAsync(cancellationToken);
            var configurations = await context.Alerts.Where(item => item.NotificationType == row.Type && !item.IsDisabled).ToListAsync(cancellationToken);
            if (configurations.Count == 0 && !await context.Alerts.AnyAsync(item => item.NotificationType == row.Type && item.IsDisabled, cancellationToken))
                throw new InvalidOperationException("Alert configuration missing");
            if (configurations.Count > 0)
            {
                await alerts.ProcessAlert(row.Message, row.Type, row.DepartmentId, assigned, row.ShipmentDocumentId, row.RelatedEntityType, row.RelatedEntityId, row.RelatedEntityCode, row.RelatedEntityName, row.RelatedEntityStatus, row.RelatedEntityPriority);
                if (configurations.Any(item => item.AlertTypes.Count > 0) && !context.ChangeTracker.Entries<Notification>().Any())
                    throw new InvalidOperationException("Alert has no eligible recipients");
            }
            row.CompletedAt = DateTime.UtcNow; row.LeaseUntil = null; row.LastErrorCode = configurations.Count == 0 ? "DisabledByConfiguration" : null;
            await context.SaveChangesAsync(cancellationToken);
            if (transaction != null) await transaction.CommitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception error)
        {
            // Rollback may have left added notifications in the tracker. Never save those while retrying.
            context.ChangeTracker.Clear();
            var retry = await context.Set<AlertDispatchRequest>().SingleAsync(item => item.Id == row.Id, cancellationToken);
            if (retry.LeaseId != row.LeaseId) return true;
            retry.LeaseUntil = null; retry.NextAttemptAt = now.AddSeconds(Math.Min(300, Math.Pow(2, Math.Min(retry.Attempts, 8))));
            retry.LastErrorCode = error.GetType().Name[..Math.Min(100, error.GetType().Name.Length)];
            await context.SaveChangesAsync(cancellationToken);
            logger.LogWarning("Alert request {Id} attempt {Attempt} failed ({ErrorType})", retry.Id, retry.Attempts, retry.LastErrorCode);
        }
        return true;
    }
}
