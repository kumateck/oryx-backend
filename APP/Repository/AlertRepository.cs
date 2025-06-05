using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Alerts;
using DOMAIN.Entities.Notifications;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using AlertType = DOMAIN.Entities.Alerts.AlertType;

namespace APP.Repository;

public class AlertRepository(ApplicationDbContext context, IMapper mapper) : IAlertRepository
{
    public async Task CreateAlert(CreateAlertRequest request)
    {
        var alert = mapper.Map<Alert>(request);
        await context.Alerts.AddAsync(alert);
        await context.SaveChangesAsync();
    }
    
    public async Task<AlertDto> GetAlert(Guid alertId)
    {
        return mapper.Map<AlertDto>(await context.Alerts
            .FirstOrDefaultAsync(item => item.Id == alertId));
    }

    public async Task<Paginateable<IEnumerable<AlertDto>>> GetAlerts(int page, int pageSize, string searchQuery, bool withDisabled = false)
    {
        var query = context.Alerts
            .OrderByDescending(a => a.CreatedAt)
            .AsQueryable();

        if (!withDisabled)
        {
            query = query.Where(item => !item.IsDisabled);
        }
            
        if (!string.IsNullOrEmpty(searchQuery))
        {
           
            query = query.WhereSearch(searchQuery, q => q.Title, q => q.ModelType);
        }
        
        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<AlertDto>);
    }
    
    public async Task UpdateAlert(CreateAlertRequest request, Guid userId, Guid approvalId)
    {
        var alert = await context.Alerts.FirstOrDefaultAsync(approval => approval.Id == approvalId);
        if (alert != null)
        {
            alert.Title = request.Title;
            alert.ModelType = request.ModelType;
            alert.AlertType = request.AlertType;
            alert.TimeFrame = request.TimeFrame;
            context.Alerts.Update(alert);
            await context.SaveChangesAsync();
        }
    }
    
    public async Task ToggleDisable(Guid id)
    {
        var alert = await context.Alerts.FirstOrDefaultAsync(item => item.Id == id);
        if (alert != null)
        {
            alert.IsDisabled = !alert.IsDisabled;
            context.Alerts.Update(alert);
            await context.SaveChangesAsync();
        }
    }

    public async Task SendAlert(NotificationDto notification, NotificationType type, Guid userId)
    {
        
    }
    
}