using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.MaterialAnalyticalRawData;
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
        
        var form = await context.Forms.FirstOrDefaultAsync(f => f.Id == request.FormId && f.LastDeletedById == null);

        if (form is null)
        {
            return Error.Validation("Form.Invalid", "Form is invalid.");
        }
        
        
        var stpNumber = await context.MaterialStandardTestProcedures
            .AnyAsync(mstp => mstp.StpNumber == request.StpNumber && mstp.LastDeletedById == null);

        if (!stpNumber)
        {
            return Error.Validation("MaterialAnalyticalRawData.StpNumberNotFound", "Stp number not found.");
        }
        
        var analyticalRawData = mapper.Map<MaterialAnalyticalRawData>(request);
        
        await context.MaterialAnalyticalRawData.AddAsync(analyticalRawData);
        await context.SaveChangesAsync();
        
        return analyticalRawData.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<MaterialAnalyticalRawDataDto>>>> GetAnalyticalRawData(int page, int pageSize, string searchQuery, int materialKind = 0)
    {
        var query = context.MaterialAnalyticalRawData
            .AsSplitQuery()
            .Include(ad => ad.MaterialStandardTestProcedure)
                .ThenInclude(ad => ad.Material)
            .Include(ad => ad.Form)
            .Where(ad => ad.LastDeletedById == null)
            .Where(ad => (int) ad.MaterialStandardTestProcedure.Material.MaterialCategory.MaterialKind == materialKind)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, 
                ad => ad.SpecNumber,
                ad => ad.StpNumber);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<MaterialAnalyticalRawDataDto>);
    }

    public async Task<Result<MaterialAnalyticalRawDataDto>> GetAnalyticalRawData(Guid id)
    {
        var analyticalRawData = await context.MaterialAnalyticalRawData
            .AsSplitQuery()
            .Include(ad => ad.MaterialStandardTestProcedure)
            .ThenInclude(ad => ad.Material)
            .Include(ad => ad.Form)
            .FirstOrDefaultAsync(ad => ad.Id == id && ad.LastDeletedById == null);

        return analyticalRawData is null
            ? Error.NotFound("MaterialAnalyticalRawData.NotFound", "Analytical raw data not found")
            : mapper.Map<MaterialAnalyticalRawDataDto>(analyticalRawData,
                opts =>
                {
                    opts.Items[AppConstants.ModelType] = nameof(MaterialAnalyticalRawData);
                });
}

    public async Task<Result> UpdateAnalyticalRawData(Guid id, CreateMaterialAnalyticalRawDataRequest request)
    {
        var analyticalRawData = await context.MaterialAnalyticalRawData
            .FirstOrDefaultAsync(ad => ad.Id == id && ad.LastDeletedById == null);

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
            .FirstOrDefaultAsync(ad => ad.Id == id && ad.LastDeletedById == null);
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
}