using DOMAIN.Entities.RecoverableItemsReports;
using SHARED;

namespace APP.IRepository;

public interface IRecoverableItemReportRepository
{
    Task<Result<Guid>> CreateRecoverableItemReport(CreateRecoverableItemReportRequest request);
    Task<Result<List<RecoverableItemReportDto>>> GetRecoverableItemReport();
}