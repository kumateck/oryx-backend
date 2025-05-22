using System.Diagnostics.Contracts;
using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.AnalyticalRawData;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class AnalyticalRawDataRepository(ApplicationDbContext context, IMapper mapper) : IAnalyticalRawDataRepository
{
    public async Task<Result<Guid>> CreateAnalyticalRawData(CreateAnalyticalRawDataRequest request)
    {
        var existingAnalyticalRawData = await context.AnalyticalRawData.FirstOrDefaultAsync(ad => ad.SpecNumber == request.SpecNumber);
        if (existingAnalyticalRawData is not null)
        {
            return Error.Validation("AnalyticalRawData.Exists", "Analytical raw data already exists.");
        }
        
        var form = await context.Forms.FirstOrDefaultAsync(f => f.Id == request.FormId && f.LastDeletedById == null);

        if (form.Name != "Analytical Raw Data")
        {
            return Error.Validation("AnalyticalRawData.InvalidForm", "Analytical raw data form is invalid.");
        }
        
        
        var stpNumber = await context.MaterialStandardTestProcedures
            .AnyAsync(mstp => mstp.StpNumber == request.StpNumber && mstp.LastDeletedById == null);

        if (!stpNumber)
        {
            return Error.Validation("AnalyticalRawData.StpNumberNotFound", "Stp number not found.");
        }
        
        var analyticalRawData = mapper.Map<AnalyticalRawData>(request);
        
        await context.AnalyticalRawData.AddAsync(analyticalRawData);
        await context.SaveChangesAsync();
        
        return analyticalRawData.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<AnalyticalRawDataDto>>>> GetAnalyticalRawData(int page, int pageSize, string searchQuery)
    {
        var query = context.AnalyticalRawData
            .AsSplitQuery()
            .Include(ad => ad.MaterialStandardTestProcedure)
            .Where(ad => ad.LastDeletedById == null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, ad => ad.SpecNumber);
        }
        
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, ad => ad.SpecNumber);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<AnalyticalRawDataDto>);
    }

    public async Task<Result<AnalyticalRawDataDto>> GetAnalyticalRawData(Guid id)
    {
        var analyticalRawData = await context.AnalyticalRawData
            .AsSplitQuery()
            .Include(ad => ad.MaterialStandardTestProcedure)
            .ThenInclude(ad => ad.Material)
            .FirstOrDefaultAsync(ad => ad.Id == id && ad.LastDeletedById == null);
        
        return analyticalRawData is null ?
            Error.NotFound("AnalyticalRawData.NotFound", "Analytical raw data not found") : 
            mapper.Map<AnalyticalRawDataDto>(analyticalRawData);
    }

    public async Task<Result> UpdateAnalyticalRawData(Guid id, CreateAnalyticalRawDataRequest request)
    {
        var analyticalRawData = await context.AnalyticalRawData
            .FirstOrDefaultAsync(ad => ad.Id == id && ad.LastDeletedById == null);

        if (analyticalRawData is null)
        {
            return Error.NotFound("AnalyticalRawData.NotFound", "Analytical raw data not found");
        }
        
        mapper.Map(request, analyticalRawData);
        
        context.AnalyticalRawData.Update(analyticalRawData);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteAnalyticalRawData(Guid id, Guid userId)
    {
        var analyticalRawData = await context.AnalyticalRawData
            .FirstOrDefaultAsync(ad => ad.Id == id && ad.LastDeletedById == null);
        if (analyticalRawData is null)
        {
            return Error.NotFound("AnalyticalRawData.NotFound", "Analytical raw data not found");
        }
        
        analyticalRawData.DeletedAt = DateTime.UtcNow;
        analyticalRawData.LastDeletedById = userId;
        
        context.AnalyticalRawData.Update(analyticalRawData);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}