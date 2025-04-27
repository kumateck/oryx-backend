using System.Data.Entity;
using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.ExitPassRequests;
using INFRASTRUCTURE.Context;
using SHARED;

namespace APP.Repository;

public class ExitPassRequestRepository(ApplicationDbContext context, IMapper mapper) : IExitPassRequestRepository
{
    public async Task<Result<Guid>> CreateExitPassRequest(CreateExitPassRequest request, Guid userId)
    {
        var existingPass = await context.ExitPassRequests
            .FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId
                                      && e.DeletedAt == null && e.TimeIn == request.TimeIn
                                      && e.TimeOut == request.TimeOut);

        if (existingPass is not null)
        {
            return Error.Validation("ExitPassRequest.Exists", "Exit pass request already exists.");
        }

        if (request.TimeIn > request.TimeOut)
        {
            return Error.Validation("ExitPassRequest.InvalidTime", "Time in must be before time out.");
        }
        
        var exitPass = mapper.Map<ExitPassRequest>(request);
        exitPass.CreatedById = userId;
        exitPass.CreatedAt = DateTime.UtcNow;
        
        await context.ExitPassRequests.AddAsync(exitPass);
        await context.SaveChangesAsync();
        
        return exitPass.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<ExitPassRequestDto>>>> GetExitPassRequests(int page, int pageSize, string searchQuery)
    {
        var query = context.ExitPassRequests.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery);
        }
        
        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<ExitPassRequestDto>
        );
    }

    public async Task<Result<ExitPassRequestDto>> GetExitPassRequest(Guid id)
    {
        var exitPass = await context.ExitPassRequests.FirstOrDefaultAsync(e => e.Id == id);
        if (exitPass is null)
        {
            return Error.NotFound("ExitPassRequest.NotFound", "Exit pass request is not found");
        }
        var exitPassDto = mapper.Map<ExitPassRequestDto>(exitPass);
        return Result.Success(exitPassDto);
    }

    public async Task<Result> UpdateExitPassRequest(Guid id, CreateExitPassRequest request, Guid userId)
    {
        var exitPass = await context.ExitPassRequests.FirstOrDefaultAsync(e => e.Id == id);
        if (exitPass is null)
        {
            return Error.NotFound("ExitPassRequest.NotFound", "Exit pass request is not found");
        }
        mapper.Map(request, exitPass);
        
        exitPass.LastUpdatedById = userId;
        exitPass.UpdatedAt = DateTime.UtcNow;
        
        context.ExitPassRequests.Update(exitPass);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteExitPassRequest(Guid id, Guid userId)
    {
        var exitPass = await context.ExitPassRequests.FirstOrDefaultAsync(e => e.Id == id);
        if (exitPass is null)
        {
            return Error.NotFound("ExitPassRequest.NotFound", "Exit pass request is not found");
        }
        
        exitPass.LastDeletedById = userId;
        exitPass.DeletedAt = DateTime.UtcNow;
        
        context.ExitPassRequests.Update(exitPass);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}