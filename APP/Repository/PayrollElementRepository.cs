using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.PayrollElements;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class PayrollElementRepository(ApplicationDbContext context, IMapper mapper) : IPayrollElementRepository
{
    public async Task<Result<Guid>> CreatePayrollElement(CreatePayrollElementRequest request)
    {
        var exists = await context.PayrollElements.AnyAsync(p => p.PayrollCompanyId == request.PayrollCompanyId);
        
        if (exists) return Error.NotFound("PayrollElement.Exists", "Payroll element exists");
        
        var element  = mapper.Map<PayrollElement>(request);
        await context.PayrollElements.AddAsync(element);
        await context.SaveChangesAsync();
        return element.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<PayrollElementDto>>>> GetPayrollElements(int page, int pageSize, string searchQuery, Guid? payrollCompanyId = null,
        PayrollElementType? type = null)
    {
        var query = context.PayrollElements
            .Include(p => p.Versions)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q=>q.Name);
        }
        
        if (payrollCompanyId.HasValue) 
        {
            query = query.Where(p => p.PayrollCompanyId == payrollCompanyId.Value);
        }

        if (Enum.TryParse<PayrollElementType>(searchQuery, true, out var typeEnum))
        {
            query = query.Where(p => p.Type == typeEnum);
        }
        
        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, 
            mapper.Map<PayrollElementDto>);
    }

    public async Task<Result<PayrollElementDto>> GetPayrollElement(Guid id)
    {
        var element = await context.PayrollElements
            .Include(e =>e.Versions)
            .FirstOrDefaultAsync(p => p.Id == id);
        return element is null ? 
            Error.NotFound("PayrollElement.NotFound","Payroll element not found") :
            mapper.Map<PayrollElementDto>(element);
    }

    public async Task<Result> UpdatePayrollElement(Guid id, CreatePayrollElementRequest request)
    {
        var element = await context.PayrollElements.FirstOrDefaultAsync(p => p.Id == id);
        if (element is null) return Error.NotFound("PayrollElement.NotFound", "Payroll element not found");
        
        mapper.Map(request, element);
        context.PayrollElements.Update(element);
        
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeletePayrollElement(Guid id, Guid userId)
    {
        var element = await context.PayrollElements.FirstOrDefaultAsync(p => p.Id == id);
        if (element is null) return Error.NotFound("PayrollElement.NotFound", "Payroll element not found");
        
        element.LastDeletedById = userId;
        element.DeletedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task<Result<Guid>> CreatePayrollElementVersion(CreatePayrollElementVersionRequest request, Guid payrollElementId)
    {
        var exists = await context.PayrollElements.AnyAsync(p => p.Id == payrollElementId);
        
        if (!exists) return Error.NotFound("PayrollElement.NotFound", "Payroll element not found");
        
        var latestVersion = await context.PayrollElementVersions
            .Where(p => p.PayrollElementId == payrollElementId)
            .OrderByDescending(p => p.VersionNumber)
            .FirstOrDefaultAsync();
        
        var version  = mapper.Map<PayrollElementVersion>(request);
        version.VersionNumber = (latestVersion?.VersionNumber ?? 0) + 1;
        version.PayrollElementId = payrollElementId;
        
        await context.PayrollElementVersions.AddAsync(version);
        await context.SaveChangesAsync();
        return version.Id;
    }
    

    public async Task<Result<PayrollElementVersionDto>> GetPayrollElementVersion(Guid id)
    {
        var element = await context.PayrollElementVersions
            .FirstOrDefaultAsync(p => p.Id == id);
        return element is null ? 
            Error.NotFound("PayrollElementVersion.NotFound","Payroll element version not found") :
            mapper.Map<PayrollElementVersionDto>(element);
    }

    public async Task<Result> UpdatePayrollElementVersion(Guid id, CreatePayrollElementVersionRequest request)
    {
        var version = await context.PayrollElementVersions.FirstOrDefaultAsync(p => p.Id == id);
        if (version is null) return Error.NotFound("PayrollElementVersion.NotFound", "Payroll element version not found");
        
        if (version.Status != PayrollElementVersionStatus.Draft) 
            return Error.Validation("PayrollElementVersion.NotDraft", "Only draft versions can be updated");
        
        
        mapper.Map(request, version);
        context.PayrollElementVersions.Update(version);
        
        await context.SaveChangesAsync();
        return Result.Success();
    }
    
}