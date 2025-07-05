using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Reports;
using SHARED;

namespace APP.IRepository;

public interface IReportRepository
{
    Task<Result<ProductionReportDto>> GetProductionReport(ReportFilter filter, Guid departmentId);
    Task<Result<List<MaterialDto>>> GetMaterialsBelowMinimumStockLevel(Guid departmentId);
}