using APP.IRepository;
using AutoMapper;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.RecoverableItemsReports;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class RecoverableItemReportRepository(ApplicationDbContext context, IMapper mapper) : IRecoverableItemReportRepository
{
    public async Task<Result<Guid>> CreateRecoverableItemReport(CreateRecoverableItemReportRequest request)
    {
        if (request.Quantity <= 0) return Error.Validation("Invalid.Quantity", "Quantity must be greater than 0.");
        
        var item = await context.Items.FirstOrDefaultAsync(i => i.Id == request.ItemId);
        if (item is null) return Error.NotFound("Item", "Item not found.");
        if (item.Classification == InventoryClassification.NonRecoverable) return Error.Validation("Item.NotRecoverable", "Item is not recoverable.");
        
        // item.AvailableQuantity -= request.Quantity;
        // context.Items.Update(item);
        // await context.SaveChangesAsync();
        
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