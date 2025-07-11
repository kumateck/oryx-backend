using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Reports;
using DOMAIN.Entities.Reports.HumanResource;
using SHARED;

namespace APP.IRepository;

public interface IReportRepository
{
    Task<Result<ProductionReportDto>> GetProductionReport(ReportFilter filter, Guid departmentId);
    Task<Result<List<MaterialDto>>> GetMaterialsBelowMinimumStockLevel(Guid departmentId);
    
    Task<Result<HrDashboardDto>> GetHumanResourceDashboardReport(ReportFilter filter);
    
    Task<Result<PermanentStaffGradeReportDto>> GetPermanentStaffGradeReport(Guid? departmentId);
    
    Task<Result<EmployeeMovementReportDto>> GetEmployeeMovementReport(MovementReportFilter filter);
    
    Task<Result<StaffTotalReport>> GetStaffTotalReport(MovementReportFilter filter);
}