using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.StandardTestProcedures;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class StandardTestProcedureRepository(ApplicationDbContext context, IMapper mapper) : IStandardTestProcedureRepository
{
    public async Task<Result<Guid>> CreateStandardTestProcedure(CreateStandardTestProcedureRequest request)
    {
        var existingProcedure = await context.StandardTestProcedures
            .FirstOrDefaultAsync(stp => stp.StpNumber == request.StpNumber);

        if (existingProcedure != null)
        {
            return Error.Validation("StandardTestProcedure.Exists", "Standard test procedure already exists.");
        }
        
        var standardTestProcedure = mapper.Map<StandardTestProcedure>(request);
        await context.StandardTestProcedures.AddAsync(standardTestProcedure);
        
        await context.SaveChangesAsync();
        return standardTestProcedure.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<StandardTestProcedureDto>>>> GetStandardTestProcedures(int page, int pageSize, string searchQuery)
    {
        var query = context.StandardTestProcedures
            .AsQueryable()
            .Where(stp => stp.DeletedAt == null)
            .AsSplitQuery();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, stp => stp.StpNumber);
        }
        
        return await PaginationHelper
            .GetPaginatedResultAsync(query,
                page,
                pageSize,
                mapper.Map<StandardTestProcedureDto>);
        
    }

    public async Task<Result<StandardTestProcedureDto>> GetStandardTestProcedure(Guid id)
    {
        var procedure = await context.StandardTestProcedures.FirstOrDefaultAsync(stp => stp.Id == id);
        
        return procedure is null ? 
            Error.NotFound("StandardTestProcedure.NotFound", "Standard test procedure not found") : 
            Result.Success(mapper.Map<StandardTestProcedureDto>(procedure));
    }

    public async Task<Result> UpdateStandardTestProcedure(Guid id, CreateStandardTestProcedureRequest request)
    {
        var procedure = await context.StandardTestProcedures.FirstOrDefaultAsync(stp => stp.Id == id);
        
        if (procedure is null)
        {
            return Error.NotFound("StandardTestProcedure.NotFound", "Standard test procedure not found");
        }
        
        mapper.Map(request, procedure);
        
        context.StandardTestProcedures.Update(procedure);
        await context.SaveChangesAsync();
        
        return Result.Success();
    }

    public async Task<Result> DeleteStandardTestProcedure(Guid id, Guid userId)
    {
        var procedure = await context.StandardTestProcedures
            .FirstOrDefaultAsync(stp => stp.Id == id);
        if (procedure is null)
        {
            return Error.NotFound("StandardTestProcedure.NotFound", "Standard test procedure not found");
        }
        
        procedure.DeletedAt = DateTime.UtcNow;
        procedure.LastDeletedById = userId;
        
        context.StandardTestProcedures.Update(procedure);
        await context.SaveChangesAsync();
        
        return Result.Success();
    }
}