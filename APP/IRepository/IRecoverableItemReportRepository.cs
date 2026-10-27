using DOMAIN.Entities.Items;
using DOMAIN.Entities.RecoverableItemsReports;
using SHARED;

namespace APP.IRepository;

public interface IRecoverableItemReportRepository
{
    Task<Result<Guid>> CreateRecoverableItemReport(CreateRecoverableItemReportRequest request, Guid actor);
    Task<Result<List<RecoverableItemReportDto>>> GetRecoverableItemReport(IReadOnlyCollection<Store> readableStores);
}
