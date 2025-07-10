using System.Globalization;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.CompanyWorkingDays;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SHARED;

namespace APP.Repository;

public class CompanyWorkingDaysRepository(ApplicationDbContext context, IMapper mapper, ILogger<CompanyWorkingDays> logger) : ICompanyWorkingDaysRepository
{
    public async Task<Result> CreateCompanyWorkingDays(List<CompanyWorkingDaysRequest> companyWorkingDays)
    {
        if (companyWorkingDays == null || companyWorkingDays.Count == 0)
        {
            return Error.Validation("CompanyWorkingDays.Invalid", "At least one working day must be selected.");
        }
        

        var existingDays = await context.CompanyWorkingDays.ToListAsync();

        if (existingDays.Count == 0)
        {
            var newEntities = mapper.Map<List<CompanyWorkingDays>>(companyWorkingDays);
            await context.CompanyWorkingDays.AddRangeAsync(newEntities);
        }
        else
        {
            foreach (var request in companyWorkingDays)
            {
                if (!IsValidStartTime(request.StartTime) || !IsValidStartTime(request.EndTime))
                {
                    return Error.Validation("Invalid.Time", "Invalid start or end time.");
                }
                
                var existing = existingDays.FirstOrDefault(e => e.Day == request.Day);
                if (existing != null)
                {
                    mapper.Map(request, existing);
                }
                else
                {
                    // Add new if not present
                    var newEntity = mapper.Map<CompanyWorkingDays>(request);
                    await context.CompanyWorkingDays.AddAsync(newEntity);
                }
            }
        }

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;
            
            if (message.Contains("String") && message.Contains("truncated", StringComparison.OrdinalIgnoreCase))
            {
                return Error.Validation("CompanyWorkingDays.TimeTooLong", "StartTime or EndTime exceeds the allowed length.");
            }
            
            logger.LogError(ex, "Error saving CompanyWorkingDays");

            return Error.Failure("CompanyWorkingDays.DatabaseError", "An error occurred while saving working days.");
        }

        return Result.Success();
    }
    
    private static bool IsValidStartTime(string input)
    {
        return DateTime.TryParseExact(
            input,
            "h:mm tt", // supports 12-hour format with AM/PM
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);
    }

    public async Task<Result<Paginateable<IEnumerable<CompanyWorkingDaysDto>>>> GetCompanyWorkingDays(int page, int pageSize, string searchQuery)
    {
        var query = context.CompanyWorkingDays.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            if (Enum.TryParse<DayOfWeek>(searchQuery, out var day))
                query = query.Where(d => d.Day == day);
        }
        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<CompanyWorkingDaysDto>);
    }

    public async Task<Result<CompanyWorkingDaysDto>> GetCompanyWorkingDay(Guid id)
    {
        var workingDay = await context.CompanyWorkingDays.FirstOrDefaultAsync(d => d.Id == id);
        if (workingDay is null)
        {
            return Error.NotFound("CompanyWorkingDays.NotFound", "Company working days not found");
        }
        var companyWorkingDaysDto = mapper.Map<CompanyWorkingDaysDto>(workingDay);
        return Result.Success(companyWorkingDaysDto);
    }

}