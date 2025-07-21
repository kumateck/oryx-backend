using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.MaterialAnalyticalRawData;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.UniformityOfWeights;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class MaterialAnalyticalRawDataRepository(ApplicationDbContext context, IMapper mapper) : IMaterialAnalyticalRawDataRepository
{
    public async Task<Result<Guid>> CreateAnalyticalRawData(CreateMaterialAnalyticalRawDataRequest request)
    {
        var existingAnalyticalRawData = await context.MaterialAnalyticalRawData.FirstOrDefaultAsync(ad => ad.SpecNumber == request.SpecNumber);
        if (existingAnalyticalRawData is not null)
        {
            return Error.Validation("MaterialAnalyticalRawData.Exists", "Analytical raw data already exists.");
        }
        
        var form = await context.Forms.FirstOrDefaultAsync(f => f.Id == request.FormId);

        if (form is null)
        {
            return Error.Validation("Form.Invalid", "Form is invalid.");
        }
        
        
        var stpNumber = await context.MaterialStandardTestProcedures
            .AnyAsync(mstp => mstp.Id == request.StpId);

        if (!stpNumber)
        {
            return Error.Validation("MaterialAnalyticalRawData.StpNumberNotFound", "Stp number not found.");
        }
        
        var analyticalRawData = mapper.Map<MaterialAnalyticalRawData>(request);
        
        await context.MaterialAnalyticalRawData.AddAsync(analyticalRawData);
        await context.SaveChangesAsync();
        
        return analyticalRawData.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<MaterialAnalyticalRawDataDto>>>> GetAnalyticalRawData(int page, int pageSize, string searchQuery, MaterialKind materialKind)
    {
        var query = context.MaterialAnalyticalRawData
            .AsSplitQuery()
            .Include(ad => ad.MaterialStandardTestProcedure)
                .ThenInclude(ad => ad.Material)
            .Include(ad => ad.Form)
            .Where(ad => ad.MaterialStandardTestProcedure.Material.Kind == materialKind)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, 
                ad => ad.SpecNumber);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            entity => mapper.Map<MaterialAnalyticalRawDataDto>(entity, opts =>
                opts.Items[AppConstants.ModelType] = nameof(MaterialAnalyticalRawData)));
    }

    public async Task<Result<MaterialAnalyticalRawDataDto>> GetAnalyticalRawData(Guid id)
    {
        var analyticalRawData = await context.MaterialAnalyticalRawData
            .AsSplitQuery()
            .Include(ad => ad.MaterialStandardTestProcedure)
            .ThenInclude(ad => ad.Material)
            .Include(ad => ad.Form)
            .FirstOrDefaultAsync(ad => ad.Id == id);

        return analyticalRawData is null
            ? Error.NotFound("MaterialAnalyticalRawData.NotFound", "Analytical raw data not found")
            : mapper.Map<MaterialAnalyticalRawDataDto>(analyticalRawData,
                opts =>
                {
                    opts.Items[AppConstants.ModelType] = nameof(MaterialAnalyticalRawData);
                });
    }
    
    public async Task<Result<MaterialAnalyticalRawDataDto>> GetAnalyticalRawDataByMaterial(Guid id)
    {
        var analyticalRawData = await context.MaterialAnalyticalRawData
            .AsSplitQuery()
            .Include(ad => ad.MaterialStandardTestProcedure)
            .ThenInclude(ad => ad.Material)
            .Include(ad => ad.Form)
            .FirstOrDefaultAsync(ad => ad.MaterialStandardTestProcedure.MaterialId == id);
        
        if(analyticalRawData is null) return Error.NotFound("MaterialAnalyticalRawData.NotFound", "No material standard test procedure for this material found.");
        
        return mapper.Map<MaterialAnalyticalRawDataDto>(analyticalRawData, opt =>
        {
            opt.Items[AppConstants.ModelType] = nameof(MaterialAnalyticalRawData);
        });
    }
    
    public async Task<Result<MaterialAnalyticalRawDataDto>> GetAnalyticalRawDataByMaterialBatch(Guid id)
    {
        var batch = await context.MaterialBatches.FirstOrDefaultAsync(m => m.Id == id);
        if(batch is null) return Error.NotFound("MaterialBatch.NotFound", "Batch not found.");

        var analyticalRawData = await context.MaterialAnalyticalRawData
            .AsSplitQuery()
            .Include(ad => ad.MaterialStandardTestProcedure)
            .ThenInclude(ad => ad.Material)
            .Include(ad => ad.Form)
            .FirstOrDefaultAsync(ad => ad.MaterialStandardTestProcedure.MaterialId == batch.MaterialId);
        
        if(analyticalRawData is null) return Error.NotFound("MaterialAnalyticalRawData.NotFound", "No material standard test procedure for this material found.");
        
        return mapper.Map<MaterialAnalyticalRawDataDto>(analyticalRawData, opt =>
        {
            opt.Items[AppConstants.ModelType] = nameof(MaterialAnalyticalRawData);
        });
    }

    public async Task<Result> UpdateAnalyticalRawData(Guid id, CreateMaterialAnalyticalRawDataRequest request)
    {
        var analyticalRawData = await context.MaterialAnalyticalRawData
            .FirstOrDefaultAsync(ad => ad.Id == id);

        if (analyticalRawData is null)
        {
            return Error.NotFound("MaterialAnalyticalRawData.NotFound", "Analytical raw data not found");
        }
        
        mapper.Map(request, analyticalRawData);
        
        context.MaterialAnalyticalRawData.Update(analyticalRawData);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteAnalyticalRawData(Guid id, Guid userId)
    {
        var analyticalRawData = await context.MaterialAnalyticalRawData
            .FirstOrDefaultAsync(ad => ad.Id == id);
        if (analyticalRawData is null)
        {
            return Error.NotFound("MaterialAnalyticalRawData.NotFound", "Analytical raw data not found");
        }
        
        analyticalRawData.DeletedAt = DateTime.UtcNow;
        analyticalRawData.LastDeletedById = userId;
        
        context.MaterialAnalyticalRawData.Update(analyticalRawData);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> StartTestForMaterialBatch(Guid id)
    {
        var materialBatch = await context.MaterialBatches.FirstOrDefaultAsync(b => b.Id == id);
        if(materialBatch is null) return Error.NotFound("MaterialBatch.NotFound", "MaterialBatch not found");

        materialBatch.Status = BatchStatus.Testing;
        context.MaterialBatches.Update(materialBatch);
        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task<Result<Guid>> CreateUniformityOfWeight(CreateUniformityOfWeight request)
    {
        var entity = mapper.Map<UniformityOfWeight>(request);
        await context.UniformityOfWeights.AddAsync(entity);
        await context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<UniformityOfWeightDto>>>> GetUniformityOfWeights(int page, int pageSize, string searchQuery)
    {
        var query = context.UniformityOfWeights
            .AsSplitQuery()
            .Include(u => u.DisintegrationInstrument)
            .Include(u => u.HardnessInstrument)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, u => u.Name);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            entity => mapper.Map<UniformityOfWeightDto>(entity, opts =>
                opts.Items[AppConstants.ModelType] = nameof(UniformityOfWeight)));
    }

    public async Task<Result<UniformityOfWeightDto>> GetUniformityOfWeight(Guid id)
    {
        var entity = await context.UniformityOfWeights
            .Include(u => u.DisintegrationInstrument)
            .Include(u => u.HardnessInstrument)
            .FirstOrDefaultAsync(u => u.Id == id);

        return entity is null
            ? Error.NotFound("UniformityOfWeight.NotFound", "Entry not found")
            : mapper.Map<UniformityOfWeightDto>(entity, opts =>
                opts.Items[AppConstants.ModelType] = nameof(UniformityOfWeight));
    }

    public async Task<Result> UpdateUniformityOfWeight(Guid id, CreateUniformityOfWeight request)
    {
        var entity = await context.UniformityOfWeights.FirstOrDefaultAsync(u => u.Id == id);
        if (entity is null)
        {
            return Error.NotFound("UniformityOfWeight.NotFound", "Entry not found");
        }

        mapper.Map(request, entity);
        context.UniformityOfWeights.Update(entity);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteUniformityOfWeight(Guid id, Guid userId)
    {
        var entity = await context.UniformityOfWeights.FirstOrDefaultAsync(u => u.Id == id);
        if (entity is null)
        {
            return Error.NotFound("UniformityOfWeight.NotFound", "Entry not found");
        }

        entity.DeletedAt = DateTime.UtcNow;
        entity.LastDeletedById = userId;
        context.UniformityOfWeights.Update(entity);
        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task<Result<Guid>> SubmitUniformityOfWeightResponse(CreateUniformityOfWeightResponse request)
    {
        var entity = mapper.Map<UniformityOfWeightResponse>(request);
        await context.UniformityOfWeightResponses.AddAsync(entity);
        await context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<Result<IEnumerable<UniformityOfWeightResponseDto>>> GetResponsesByMaterialBatchId(Guid uniformityOfWeightId, Guid materialBatchId)
    {
        var responses = await context.UniformityOfWeightResponses
            .AsSplitQuery()
            .Include(r => r.UniformityOfWeight)
            .Include(r => r.MaterialBatch)
            .Where(r => r.UniformityOfWeightId == uniformityOfWeightId
                        && r.MaterialBatchId == materialBatchId)
            .ToListAsync();

        return mapper.Map<List<UniformityOfWeightResponseDto>>(responses);
    }
}