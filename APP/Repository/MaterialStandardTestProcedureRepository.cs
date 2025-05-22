using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.MaterialStandardTestProcedures;
using DOMAIN.Entities.StandardTestProcedures;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class MaterialStandardTestProcedureRepository(ApplicationDbContext context, IMapper mapper) : IMaterialStandardTestProcedureRepository
{
    public async Task<Result<Guid>> CreateMaterialStandardTestProcedure(CreateMaterialStandardTestProcedureRequest request)
    {
        var existingProcedure = await context.MaterialStandardTestProcedures
            .FirstOrDefaultAsync(stp => stp.StpNumber == request.StpNumber);

        if (existingProcedure != null)
        {
            return Error.Validation("MaterialStandardTestProcedure.Exists", "Standard test procedure already exists.");
        }
        
        var MaterialStandardTestProcedure = mapper.Map<MaterialStandardTestProcedure>(request);
        await context.MaterialStandardTestProcedures.AddAsync(MaterialStandardTestProcedure);
        
        await context.SaveChangesAsync();
        return MaterialStandardTestProcedure.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<MaterialStandardTestProcedureDto>>>> GetMaterialStandardTestProcedures(int page, int pageSize, string searchQuery)
    {
        var query = context.MaterialStandardTestProcedures
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
                mapper.Map<MaterialStandardTestProcedureDto>);
        
    }

    public async Task<Result<MaterialStandardTestProcedureDto>> GetMaterialStandardTestProcedure(Guid id)
    {
        var procedure = await context.MaterialStandardTestProcedures.FirstOrDefaultAsync(stp => stp.Id == id);
        
        return procedure is null ? 
            Error.NotFound("MaterialStandardTestProcedure.NotFound", "Standard test procedure not found") : 
            Result.Success(mapper.Map<MaterialStandardTestProcedureDto>(procedure));
    }

    public async Task<Result> UpdateMaterialStandardTestProcedure(Guid id, CreateMaterialStandardTestProcedureRequest request)
    {
        var procedure = await context.MaterialStandardTestProcedures.FirstOrDefaultAsync(stp => stp.Id == id);
        
        if (procedure is null)
        {
            return Error.NotFound("MaterialStandardTestProcedure.NotFound", "Standard test procedure not found");
        }
        
        mapper.Map(request, procedure);
        
        context.MaterialStandardTestProcedures.Update(procedure);
        await context.SaveChangesAsync();
        
        return Result.Success();
    }

    public async Task<Result> DeleteMaterialStandardTestProcedure(Guid id, Guid userId)
    {
        var procedure = await context.MaterialStandardTestProcedures
            .FirstOrDefaultAsync(stp => stp.Id == id);
        if (procedure is null)
        {
            return Error.NotFound("MaterialStandardTestProcedure.NotFound", "Standard test procedure not found");
        }
        
        procedure.DeletedAt = DateTime.UtcNow;
        procedure.LastDeletedById = userId;
        
        context.MaterialStandardTestProcedures.Update(procedure);
        await context.SaveChangesAsync();
        
        return Result.Success();
    }
}