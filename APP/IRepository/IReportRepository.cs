using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Reports;
using DOMAIN.Entities.Reports.HumanResource;
using SHARED;

namespace APP.IRepository;

public interface IReportRepository
{
    Task<Result<ProductionReportDto>> GetProductionReport(ReportFilter filter, Guid departmentId);
    Task<Result<List<MaterialWithStockDto>>> GetMaterialsBelowMinimumStockLevel(Guid departmentId);
    Task<Result<HrDashboardDto>> GetHumanResourceDashboardReport(ReportFilter filter);
    Task<Result<PermanentStaffGradeReportDto>> GetPermanentStaffGradeReport(Guid? departmentId);
    
    Task<Result<EmployeeMovementReportDto>> GetEmployeeMovementReport(MovementReportFilter filter);
    
    Task<Result<StaffTotalReport>> GetStaffTotalReport(MovementReportFilter filter);
    
    Task<Result<StaffGenderRatioReport>> GetStaffGenderRatioReport(MovementReportFilter filter);

    Task<Result<WarehouseReportDto>> GetWarehouseReport(ReportFilter filter, Guid departmentId);
    Task<Result<List<MaterialBatchReservedQuantityReportDto>>> GetReservedMaterialBatchesForDepartment(
        ReportFilter filter, Guid departmentId);
}