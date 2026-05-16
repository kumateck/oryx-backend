using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.PayrollPaymentFormats;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class PayrollPaymentFormatRepository(ApplicationDbContext context, IMapper mapper) : IPayrollPaymentFormatRepository
{
    public async Task<Result<Guid>> CreatePayrollPaymentFormat(CreatePayrollPaymentFormatRequest request)
    {
        var country = await context.Countries.AnyAsync(c => c.Id == request.CountryId);
        if (!country) return Error.NotFound("Country.NotFound", "Country not found");
        
        var format = mapper.Map<PayrollPaymentFormat>(request);
        await context.PayrollPaymentFormats.AddAsync(format);
        await context.SaveChangesAsync();
        return format.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<PayrollPaymentFormatDto>>>> GetPayrollPaymentFormats(int page, int pageSize, 
        string searchQuery, Guid? countryId = null)
    {
        var query = context.PayrollPaymentFormats.AsQueryable();
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, p => p.Name, p=> p.Code);
        }

        if (countryId.HasValue)
        {
            query = query.Where(p => p.CountryId == countryId.Value);
        }
        
        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize,
            mapper.Map<PayrollPaymentFormatDto>);
    }

    public async Task<Result<PayrollPaymentFormatDto>> GetPayrollPaymentFormat(Guid id)
    {
        var format = await context.PayrollPaymentFormats.FirstOrDefaultAsync(x => x.Id == id);
        return format is null ? 
            Error.NotFound("PayrollPaymentFormat.NotFound","Payroll payment format not found")
            : mapper.Map<PayrollPaymentFormatDto>(format);
    }

    public async Task<Result> UpdatePayrollPaymentFormat(Guid id, CreatePayrollPaymentFormatRequest request)
    {
        var format = await context.PayrollPaymentFormats.FirstOrDefaultAsync(x => x.Id == id);
        if (format is null)
            return Error.NotFound("PayrollPaymentFormat.NotFound", "Payroll payment format not found");
        
        mapper.Map(request, format);
        context.PayrollPaymentFormats.Update(format);
        
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeletePayrollPaymentFormat(Guid id, Guid userId)
    {
        var format = await context.PayrollPaymentFormats.FirstOrDefaultAsync(p => p.Id == id);
        if (format is null)
            return Error.NotFound("PayrollPaymentFormat.NotFound", "Payroll payment format not found");
        
        format.DeletedAt = DateTime.UtcNow;
        format.LastDeletedById = userId;
        
        await context.SaveChangesAsync();
        return Result.Success();
    }
}