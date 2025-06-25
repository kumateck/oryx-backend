using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.MaterialStandardTestProcedures;
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
            return Error.Validation("MaterialStandardTestProcedure.Exists", "Material Standard test procedure already exists.");
        }
        
        var material = await context.Materials.FirstOrDefaultAsync(m => m.Id == request.MaterialId);

        if (material == null)
        {
            return Error.Validation("Invalid.Material", "Invalid material");
        }
        
        var materialStandardTestProcedure = mapper.Map<MaterialStandardTestProcedure>(request);
        await context.MaterialStandardTestProcedures.AddAsync(materialStandardTestProcedure);
        
        await context.SaveChangesAsync();
        return materialStandardTestProcedure.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<MaterialStandardTestProcedureDto>>>> GetMaterialStandardTestProcedures(int page, int pageSize, string searchQuery, MaterialKind materialKind = 0)
    {
        var query = context.MaterialStandardTestProcedures
            .AsQueryable()
            .Include(stp => stp.Material)
            .ThenInclude(m => m.MaterialCategory)
            .Where(stp =>  stp.Material.MaterialCategory.MaterialKind == materialKind)
            .AsSplitQuery();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, stp => stp.StpNumber);
        }

        return await PaginationHelper
            .GetPaginatedResultAsync(query,
                page,
                pageSize,
                entity => mapper.Map<MaterialStandardTestProcedureDto>(
                    entity,
                    opts => { opts.Items[AppConstants.ModelType] = nameof(MaterialStandardTestProcedure); }
                ));
    }

    public async Task<Result<MaterialStandardTestProcedureDto>> GetMaterialStandardTestProcedure(Guid id)
    {
        var procedure = await context.MaterialStandardTestProcedures
            .Include(stp => stp.Material)
            .FirstOrDefaultAsync(stp => stp.Id == id);
        
        return procedure is null ? 
            Error.NotFound("MaterialStandardTestProcedure.NotFound", "Material Standard test procedure not found") : 
            mapper.Map<MaterialStandardTestProcedureDto>(procedure
            , opts => {opts.Items[AppConstants.ModelType] = nameof(MaterialStandardTestProcedure);});
    }

    public async Task<Result> UpdateMaterialStandardTestProcedure(Guid id, CreateMaterialStandardTestProcedureRequest request)
    {
        var procedure = await context.MaterialStandardTestProcedures
            .FirstOrDefaultAsync(stp => stp.Id == id);
        
        if (procedure is null)
        {
            return Error.NotFound("MaterialStandardTestProcedure.NotFound", "Material Standard test procedure not found");
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
            return Error.NotFound("MaterialStandardTestProcedure.NotFound", "Material Standard test procedure not found");
        }
        
        procedure.DeletedAt = DateTime.UtcNow;
        procedure.LastDeletedById = userId;
        
        context.MaterialStandardTestProcedures.Update(procedure);
        await context.SaveChangesAsync();
        
        return Result.Success();
    }
}