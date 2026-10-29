using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.ErrorLogs;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ErrorLogRepository(ApplicationDbContext context) : IErrorLogRepository
{
    private const int MaxPageSize = 200;
    private const int SummaryLimit = 100;

    public async Task<Paginateable<IEnumerable<ErrorLogListDto>>> GetErrorLogs(ErrorLogFilter filter)
    {
        var pageSize = Math.Clamp(filter.PageSize, 1, MaxPageSize);
        var page = Math.Max(1, filter.Page);
        var query = Filter(filter);

        var total = await query.CountAsync();
        var ordered = filter.SortDirection == SortDirection.Ascending
            ? query.OrderBy(e => e.OccurredAt)
            : query.OrderByDescending(e => e.OccurredAt);

        var data = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new ErrorLogListDto
            {
                Id = e.Id,
                OccurredAt = e.OccurredAt,
                Level = e.Level,
                Source = e.Source,
                Category = e.Category,
                Message = e.Message,
                ExceptionType = e.ExceptionType,
                Fingerprint = e.Fingerprint,
                TraceId = e.TraceId,
                StatusCode = e.StatusCode,
                HttpMethod = e.HttpMethod,
                Path = e.Path,
                UserId = e.UserId,
                UserEmail = e.UserEmail,
                UserName = context.Users.Where(u => u.Id == e.UserId).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault(),
            })
            .ToListAsync();

        var pageCount = (int)Math.Ceiling(total / (double)pageSize);
        return new Paginateable<IEnumerable<ErrorLogListDto>>
        {
            Data = data,
            PageIndex = page,
            PageCount = pageCount,
            TotalRecordCount = total,
            NumberOfPagesToShow = pageSize,
            StartPageIndex = Math.Max(1, page - pageSize / 2),
            StopPageIndex = Math.Min(pageCount, page + pageSize / 2),
        };
    }

    public async Task<Result<ErrorLogDto>> GetErrorLog(Guid id)
    {
        var entry = await context.ErrorLogs.AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new ErrorLogDto
            {
                Id = e.Id,
                OccurredAt = e.OccurredAt,
                Level = e.Level,
                Source = e.Source,
                Category = e.Category,
                Message = e.Message,
                ExceptionType = e.ExceptionType,
                ExceptionDetails = e.ExceptionDetails,
                Fingerprint = e.Fingerprint,
                TraceId = e.TraceId,
                StatusCode = e.StatusCode,
                HttpMethod = e.HttpMethod,
                Path = e.Path,
                RouteTemplate = e.RouteTemplate,
                QueryString = e.QueryString,
                RequestBody = e.RequestBody,
                RequestHeaders = e.RequestHeaders,
                UserId = e.UserId,
                UserEmail = e.UserEmail,
                UserName = context.Users.Where(u => u.Id == e.UserId).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault(),
                IpAddress = e.IpAddress,
                ElapsedMs = e.ElapsedMs,
                Release = e.Release,
                Instance = e.Instance,
                MachineName = e.MachineName,
            })
            .FirstOrDefaultAsync();

        return entry is null
            ? Error.NotFound("ErrorLog.NotFound", "No error log entry exists with this id.")
            : entry;
    }

    public async Task<List<ErrorLogSummaryDto>> GetErrorSummary(ErrorLogFilter filter)
    {
        var query = Filter(filter);

        var groups = await query
            .GroupBy(e => e.Fingerprint)
            .Select(g => new { Fingerprint = g.Key, Count = g.Count(), FirstSeen = g.Min(e => e.OccurredAt), LastSeen = g.Max(e => e.OccurredAt) })
            .OrderByDescending(g => g.Count)
            .ThenByDescending(g => g.LastSeen)
            .Take(SummaryLimit)
            .ToListAsync();

        var fingerprints = groups.Select(g => g.Fingerprint).ToList();
        var lastSeen = groups.Select(g => g.LastSeen).ToList();
        var latest = (await query
                .Where(e => fingerprints.Contains(e.Fingerprint) && lastSeen.Contains(e.OccurredAt))
                .Select(e => new { e.Id, e.Fingerprint, e.OccurredAt, e.ExceptionType, e.Message, e.Category, e.Path })
                .ToListAsync())
            .GroupBy(e => e.Fingerprint)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.OccurredAt).First());

        return groups.Select(g =>
        {
            latest.TryGetValue(g.Fingerprint, out var sample);
            return new ErrorLogSummaryDto
            {
                Fingerprint = g.Fingerprint,
                Count = g.Count,
                FirstSeen = g.FirstSeen,
                LastSeen = g.LastSeen,
                LatestId = sample?.Id ?? Guid.Empty,
                ExceptionType = sample?.ExceptionType,
                Message = sample?.Message,
                Category = sample?.Category,
                Path = sample?.Path,
            };
        }).ToList();
    }

    private IQueryable<ErrorLog> Filter(ErrorLogFilter filter)
    {
        var query = context.ErrorLogs.AsNoTracking();
        var traceId = NormalizeTraceId(filter.TraceId);

        // A reference lookup should find the entry whenever it happened; otherwise default to the last 7 days.
        if (traceId is null)
        {
            var end = filter.EndDate ?? DateTime.UtcNow.AddMinutes(1);
            var start = filter.StartDate ?? end.AddDays(-7);
            query = query.Where(e => e.OccurredAt >= start && e.OccurredAt <= end);
        }
        else
        {
            query = query.Where(e => e.TraceId == traceId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Fingerprint))
            query = query.Where(e => e.Fingerprint == filter.Fingerprint.Trim());
        if (filter.UserId.HasValue)
            query = query.Where(e => e.UserId == filter.UserId);
        if (filter.StatusCode.HasValue)
            query = query.Where(e => e.StatusCode == filter.StatusCode);
        if (!string.IsNullOrWhiteSpace(filter.Source))
            query = query.Where(e => e.Source == filter.Source.Trim());

        if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
        {
            var term = filter.SearchQuery.Trim().ToLower();
            query = query.Where(e =>
                (e.Message != null && e.Message.ToLower().Contains(term)) ||
                (e.ExceptionType != null && e.ExceptionType.ToLower().Contains(term)) ||
                (e.Path != null && e.Path.ToLower().Contains(term)) ||
                (e.Category != null && e.Category.ToLower().Contains(term)));
        }

        return query;
    }

    /// <summary>
    /// Accepts the bare 32-character trace id or a full W3C traceparent
    /// ("00-&lt;trace id&gt;-&lt;span id&gt;-01"), which is what ASP.NET writes into Result-based error bodies.
    /// </summary>
    public static string NormalizeTraceId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        var parts = trimmed.Split('-');
        return parts.Length == 4 && parts[1].Length == 32 ? parts[1] : trimmed;
    }
}
