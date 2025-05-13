using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.ActivityLogs;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using SortDirection = SHARED.SortDirection;

namespace APP.Repository;

public class ActivityLogRepository(MongoDbContext context, IMapper mapper, ApplicationDbContext dbContext) : IActivityLogRepository
{
    private readonly IMongoCollection<ActivityLog> _activityLogs = context.ActivityLogs;

    public async Task RecordActivityAsync(CreateActivityLog model)
    {
        if (model.UserId.HasValue)
        {
            model.User = mapper.Map<BsonUserDto>(await dbContext.Users.FirstOrDefaultAsync(u => u.Id == model.UserId.Value));
        }
        var activityLog = mapper.Map<ActivityLog>(model);
        await _activityLogs.InsertOneAsync(activityLog);
    }
    
    public void RecordActivity(CreateActivityLog model)
    {
        if (model.UserId.HasValue)
        {
            model.User = mapper.Map<BsonUserDto>(dbContext.Users.FirstOrDefault(u => u.Id == model.UserId.Value));
        }
        var activityLog = mapper.Map<ActivityLog>(model);
        _activityLogs.InsertOne(activityLog);
    }

    public async Task<Paginateable<IEnumerable<ActivityLogDto>>> GetActivityLogs(ActivityLogFilter filter)
    {
        var filterDefinition = Builders<ActivityLog>.Filter.Empty;
        
        filter.StartDate ??= DateTime.UtcNow.Date;
        filter.EndDate ??= DateTime.UtcNow.AddDays(1).Date;

        // Apply date range filters
        if (filter.StartDate.HasValue)
            filterDefinition &= Builders<ActivityLog>.Filter.Gte(log => log.CreatedAt, filter.StartDate.Value);
    
        if (filter.EndDate.HasValue)
            filterDefinition &= Builders<ActivityLog>.Filter.Lte(log => log.CreatedAt, filter.EndDate.Value);

        // Get total count before pagination
        var totalRecords = await _activityLogs.CountDocumentsAsync(filterDefinition);
        var totalPages = (int)Math.Ceiling(totalRecords / (double)filter.PageSize);

        // Apply sorting
        var sortDefinition = filter.SortDirection == SortDirection.Ascending
            ? Builders<ActivityLog>.Sort.Ascending(filter.SortLabel ?? "CreatedAt")
            : Builders<ActivityLog>.Sort.Descending(filter.SortLabel ?? "CreatedAt");

        // Apply pagination
        var logs = await _activityLogs
            .Find(filterDefinition)
            .Sort(sortDefinition)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Limit(filter.PageSize)
            .ToListAsync();

        var logDtos = mapper.Map<IEnumerable<ActivityLogDto>>(logs);

        // Calculate start and stop page indices
        var halfPagesToShow = filter.PageSize / 2;
        var startPageIndex = Math.Max(1, filter.Page - halfPagesToShow);
        var stopPageIndex = Math.Min(totalPages, filter.Page + halfPagesToShow);

        return new Paginateable<IEnumerable<ActivityLogDto>>
        {
            Data = logDtos,
            PageIndex = filter.Page,
            PageCount = totalPages,
            TotalRecordCount = (int)totalRecords,
            NumberOfPagesToShow = filter.PageSize,
            StartPageIndex = startPageIndex,
            StopPageIndex = stopPageIndex
        };
    }
}