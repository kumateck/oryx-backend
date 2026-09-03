using DOMAIN.Entities.Payroll;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class PayrollRepository
{
    public async Task<Result<Guid>> CreatePayGrade(CreatePayGradeRequest request)
    {
        var payGrade = mapper.Map<PayGrade>(request);
        await context.PayGrades.AddAsync(payGrade);
        await context.SaveChangesAsync();
        return payGrade.Id;
    }

    public async Task<Result<List<PayGradeDto>>> GetPayGrades()
    {
        var payGrades = await context.PayGrades.OrderBy(p => p.MinSalary).ToListAsync();
        return mapper.Map<List<PayGradeDto>>(payGrades);
    }

    public async Task<Result> UpdatePayGrade(Guid id, CreatePayGradeRequest request)
    {
        var payGrade = await context.PayGrades.FirstOrDefaultAsync(p => p.Id == id);
        if (payGrade is null)
        {
            return Error.NotFound("PayGrade.NotFound", "Pay grade not found");
        }

        mapper.Map(request, payGrade);
        context.PayGrades.Update(payGrade);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeletePayGrade(Guid id, Guid userId)
    {
        var payGrade = await context.PayGrades.FirstOrDefaultAsync(p => p.Id == id);
        if (payGrade is null)
        {
            return Error.NotFound("PayGrade.NotFound", "Pay grade not found");
        }

        payGrade.DeletedAt = DateTime.UtcNow;
        payGrade.LastDeletedById = userId;
        context.PayGrades.Update(payGrade);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<Guid>> CreatePayeTaxBand(CreatePayeTaxBandRequest request)
    {
        if (request.UpperBound.HasValue && request.UpperBound <= request.LowerBound)
        {
            return Error.Validation("PayeTaxBand.InvalidRange", "Upper bound must be greater than lower bound");
        }

        var band = mapper.Map<PayeTaxBand>(request);
        await context.PayeTaxBands.AddAsync(band);
        await context.SaveChangesAsync();
        return band.Id;
    }

    public async Task<Result<List<PayeTaxBandDto>>> GetPayeTaxBands()
    {
        var bands = await context.PayeTaxBands
            .Where(b => b.EffectiveTo == null)
            .OrderBy(b => b.LowerBound)
            .ToListAsync();
        return mapper.Map<List<PayeTaxBandDto>>(bands);
    }

    public async Task<Result> DeletePayeTaxBand(Guid id, Guid userId)
    {
        var band = await context.PayeTaxBands.FirstOrDefaultAsync(b => b.Id == id);
        if (band is null)
        {
            return Error.NotFound("PayeTaxBand.NotFound", "PAYE tax band not found");
        }

        band.DeletedAt = DateTime.UtcNow;
        band.LastDeletedById = userId;
        context.PayeTaxBands.Update(band);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<Guid>> CreateSsnitRate(CreateSsnitRateRequest request)
    {
        var rate = mapper.Map<SsnitRate>(request);
        await context.SsnitRates.AddAsync(rate);
        await context.SaveChangesAsync();
        return rate.Id;
    }

    public async Task<Result<List<SsnitRateDto>>> GetSsnitRates()
    {
        var rates = await context.SsnitRates
            .OrderByDescending(r => r.EffectiveFrom)
            .ToListAsync();
        return mapper.Map<List<SsnitRateDto>>(rates);
    }

    public async Task<Result> DeleteSsnitRate(Guid id, Guid userId)
    {
        var rate = await context.SsnitRates.FirstOrDefaultAsync(r => r.Id == id);
        if (rate is null)
        {
            return Error.NotFound("SsnitRate.NotFound", "SSNIT rate not found");
        }

        rate.DeletedAt = DateTime.UtcNow;
        rate.LastDeletedById = userId;
        context.SsnitRates.Update(rate);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
