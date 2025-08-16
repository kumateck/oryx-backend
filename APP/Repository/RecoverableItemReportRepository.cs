using APP.IRepository;
using AutoMapper;
using DOMAIN.Entities.RecoverableItemsReports;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class RecoverableItemReportRepository(ApplicationDbContext context, IMapper mapper) : IRecoverableItemReportRepository
{
    public async Task<Result<Guid>> CreateRecoverableItemReport(CreateRecoverableItemReportRequest request)
    {
        var report = mapper.Map<RecoverableItemReport>(request);
        await context.RecoverableItemReports.AddAsync(report);
        await context.SaveChangesAsync();
        return report.Id;
    }

    public async Task<Result<List<RecoverableItemReportDto>>> GetRecoverableItemReport()
    {
        var query = await context.RecoverableItemReports.ToListAsync();
        return mapper.Map<List<RecoverableItemReportDto>>(query);
    }
}