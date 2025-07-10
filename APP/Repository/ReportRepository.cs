using APP.IRepository;
using AutoMapper;
using DOMAIN.Entities.AttendanceRecords;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.LeaveRequests;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.OvertimeRequests;
using DOMAIN.Entities.ProductionSchedules.StockTransfers;
using DOMAIN.Entities.Reports;
using DOMAIN.Entities.Reports.HumanResource;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ReportRepository(ApplicationDbContext context, IMapper mapper, IMaterialRepository materialRepository)
    : IReportRepository
{
    public async Task<Result<ProductionReportDto>> GetProductionReport(ReportFilter filter, Guid departmentId)
    {
        var purchaseRequisitions = context.Requisitions
            .Where(r => r.RequisitionType == RequisitionType.Purchase)
            .AsQueryable();

        var sourceRequisitions = context.SourceRequisitions.AsQueryable();
        var productionSchedules = context.ProductionSchedules.AsQueryable();
        var incomingStockTransfers = context.StockTransferSources
            .Where(s => s.FromDepartmentId == departmentId)
            .AsQueryable();
        var outGoingStockTransfers = context.StockTransferSources
            .Where(s => s.ToDepartmentId == departmentId)
            .AsQueryable();

        if (filter.StartDate.HasValue)
        {
            var start = filter.StartDate.Value;
            purchaseRequisitions = purchaseRequisitions.Where(r => r.CreatedAt >= start);
            sourceRequisitions = sourceRequisitions.Where(r => r.CreatedAt >= start);
            productionSchedules = productionSchedules.Where(p => p.CreatedAt >= start);
            incomingStockTransfers = incomingStockTransfers.Where(s => s.CreatedAt >= start);
            outGoingStockTransfers = outGoingStockTransfers.Where(s => s.CreatedAt >= start);
        }

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.AddDays(1);
            purchaseRequisitions = purchaseRequisitions.Where(r => r.CreatedAt < end);
            sourceRequisitions = sourceRequisitions.Where(r => r.CreatedAt < end);
            productionSchedules = productionSchedules.Where(p => p.CreatedAt < end);
            incomingStockTransfers = incomingStockTransfers.Where(s => s.CreatedAt < end);
            outGoingStockTransfers = outGoingStockTransfers.Where(s => s.CreatedAt < end);
        }

        var sourceRequisitionIds = await sourceRequisitions.Select(r => r.Id).ToListAsync();

        var purchaseOrderNumber = await context.PurchaseOrders
            .Where(p => sourceRequisitionIds.Contains(p.SourceRequisitionId))
            .CountAsync();

        return new ProductionReportDto
        {
            NumberOfPurchaseRequisitions = await purchaseRequisitions.CountAsync(),
            NumberOfNewPurchaseRequisitions = await purchaseRequisitions.CountAsync(p => p.Status == RequestStatus.New),
            NumberOfInProgressPurchaseRequisitions =
                await purchaseRequisitions.CountAsync(p => p.Status == RequestStatus.Pending),
            NumberOfCompletedPurchaseRequisitions = purchaseOrderNumber,

            NumberOfProductionSchedules = await productionSchedules.CountAsync(),
            NumberOfNewProductionSchedules =
                await productionSchedules.CountAsync(p => p.Status == ProductionStatus.New),
            NumberOfInProgressProductionSchedules =
                await productionSchedules.CountAsync(p => p.Status == ProductionStatus.InProgress),
            NumberOfCompletedProductionSchedules =
                await productionSchedules.CountAsync(p => p.Status == ProductionStatus.Completed),

            NumberOfIncomingStockTransfers = await incomingStockTransfers.CountAsync(),
            NumberOfIncomingPendingStockTransfers =
                await incomingStockTransfers.CountAsync(s => s.Status == StockTransferStatus.InProgress),
            NumberOfIncomingCompletedStockTransfers =
                await incomingStockTransfers.CountAsync(s => s.Status == StockTransferStatus.Approved),

            NumberOfOutgoingStockTransfers = await outGoingStockTransfers.CountAsync(),
            NumberOfOutgoingPendingStockTransfers =
                await outGoingStockTransfers.CountAsync(s => s.Status == StockTransferStatus.InProgress),
            NumberOfOutgoingCompletedStockTransfers =
                await outGoingStockTransfers.CountAsync(s => s.Status == StockTransferStatus.Approved),
        };
    }


    public async Task<Result<List<MaterialDto>>> GetMaterialsBelowMinimumStockLevel(Guid departmentId)
    {
        var materialDepartments = await context.MaterialDepartments
            .AsSplitQuery()
            .Include(md => md.Material)
            .Where(md => md.DepartmentId == departmentId)
            .ToListAsync();

        var rawWarehouse = await context.Warehouses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.DepartmentId == departmentId && w.Type == WarehouseType.RawMaterialStorage);

        var packageWarehouse = await context.Warehouses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.DepartmentId == departmentId && w.Type == WarehouseType.PackagedStorage);

        if (rawWarehouse == null || packageWarehouse == null)
            return Error.Failure("Department.Warehouse", "One or more required warehouses not found.");

        List<Material> materialsBelowMinStock = [];

        foreach (var materialDepartment in materialDepartments)
        {
            var material = materialDepartment.Material;
            var warehouseId = material.Kind == MaterialKind.Raw ? rawWarehouse.Id : packageWarehouse.Id;

            var stockResult = await materialRepository.GetMaterialStockInWarehouse(material.Id, warehouseId);

            if (stockResult.IsFailure) continue;

            var stock = stockResult.Value;

            if (stock < materialDepartment.MinimumStockLevel)
            {
                materialsBelowMinStock.Add(material);
            }
        }

        return mapper.Map<List<MaterialDto>>(materialsBelowMinStock);
    }

    public async Task<Result<HrDashboardDto>> GetHumanResourceDashboardReport(ReportFilter filter)
    {
        var leaveRequests = context.LeaveRequests.AsQueryable();
        var overtimeRequests = context.OvertimeRequests.AsQueryable();
        var employees = context.Employees.AsQueryable();

        if (filter.StartDate.HasValue)
        {
            leaveRequests = leaveRequests.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            overtimeRequests = overtimeRequests.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            leaveRequests = leaveRequests.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
            overtimeRequests = overtimeRequests.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
        }

        return new HrDashboardDto
        {
            NumberOfOvertimeRequests = overtimeRequests.Count(),
            NumberOfApprovedOvertimeRequests =
                await overtimeRequests.CountAsync(or => or.Status == OvertimeStatus.Approved),
            NumberOfPendingOvertimeRequests =
                await overtimeRequests.CountAsync(or => or.Status == OvertimeStatus.Pending),
            NumberOfExpiredOvertimeRequests =
                await overtimeRequests.CountAsync(or => or.Status == OvertimeStatus.Expired),
            NumberOfCasualEmployees = await employees.CountAsync(e => e.Type == EmployeeType.Casual),
            NumberOfPermanentEmployees = await employees.CountAsync(e => e.Type == EmployeeType.Permanent),
            NumberOfLeaveRequests = await leaveRequests.CountAsync(),
            NumberOfPendingLeaveRequests = await leaveRequests.CountAsync(lr => lr.LeaveStatus == LeaveStatus.Pending),
            NumberOfExpiredLeaveRequests = await leaveRequests.CountAsync(lr => lr.LeaveStatus == LeaveStatus.Expired),
            NumberOfRejectedLeaveRequests = await leaveRequests.CountAsync(lr => lr.LeaveStatus == LeaveStatus.Rejected),
            AttendanceStats = await GetAttendanceStatsAsync(filter.StartDate, filter.EndDate)
        };

    }

    public async Task<Result<PermanentStaffGradeReportDto>> GetPermanentStaffGradeReport(Guid? departmentId)
    {

        var employees = context.Employees
            .Include(e => e.Department)
            .Where(e => e.Type == EmployeeType.Permanent);

        if (departmentId.HasValue)
        {
            employees = employees.Where(e => e.DepartmentId == departmentId.Value);
        }

        var groupedResults = await employees
            .GroupBy(e => e.Department.Name)
            .Select(g => new PermanentStaffGradeCountDto
            {
                Department = g.Key,
                SeniorMgtMale = g.Count(e => e.Level == EmployeeLevel.SeniorManagement && e.Gender == Gender.Male),
                SeniorMgtFemale = g.Count(e => e.Level == EmployeeLevel.SeniorManagement && e.Gender == Gender.Female),
                SeniorStaffMale = g.Count(e => e.Level == EmployeeLevel.SeniorStaff && e.Gender == Gender.Male),
                SeniorStaffFemale = g.Count(e => e.Level == EmployeeLevel.SeniorStaff && e.Gender == Gender.Female),
                JuniorStaffMale = g.Count(e => e.Level == EmployeeLevel.JuniorStaff && e.Gender == Gender.Male),
                JuniorStaffFemale = g.Count(e => e.Level == EmployeeLevel.JuniorStaff && e.Gender == Gender.Female)
            })
            .ToListAsync();

        var total = new PermanentStaffGradeTotalDto
        {
            SeniorMgtMale = groupedResults.Sum(x => x.SeniorMgtMale),
            SeniorMgtFemale = groupedResults.Sum(x => x.SeniorMgtFemale),
            SeniorStaffMale = groupedResults.Sum(x => x.SeniorStaffMale),
            SeniorStaffFemale = groupedResults.Sum(x => x.SeniorStaffFemale),
            JuniorStaffMale = groupedResults.Sum(x => x.JuniorStaffMale),
            JuniorStaffFemale = groupedResults.Sum(x => x.JuniorStaffFemale)
        };

        return new PermanentStaffGradeReportDto { Departments = groupedResults, Totals = total };
        
    }

    private async Task<AttendanceStatsDto> GetAttendanceStatsAsync(DateTime? startDate, DateTime? endDate)
    {
        var presentEmployees = await context.AttendanceRecords
            .CountAsync(a => a.TimeStamp >= startDate && a.TimeStamp < endDate && a.WorkState == WorkState.CheckIn);

        var totalEmployees = await context.Employees.ToListAsync();

        var absentEmployees = totalEmployees.Count - presentEmployees;

        var rate = totalEmployees.Count == 0 ? 0 : presentEmployees * 100 / totalEmployees.Count;

        return new AttendanceStatsDto
        {
            NumberOfPresentEmployees = presentEmployees,
            NumberOfAbsentEmployees = absentEmployees,
            AttendanceRate = rate
        };
    }
    
    public async Task<Result<List<EmployeeMovementReportDto>>> GetEmployeeMovementReport(MovementReportFilter filter)
    {
        var start = filter.StartDate ?? DateTime.UtcNow.AddMonths(-1); 
        var end = filter.EndDate ?? DateTime.UtcNow;

        var query = context.Employees
            .Include(e => e.Department)
            .Where(e => e.DateEmployed >= start && e.DateEmployed <= end);

        if (filter.DepartmentId.HasValue)
            query = query.Where(e => e.DepartmentId == filter.DepartmentId.Value);

        var employees = await query.ToListAsync();

        var grouped = employees.GroupBy(e => e.Department?.Name ?? "Unassigned");

        var result = new List<EmployeeMovementReportDto>();

        foreach (var group in grouped)
        {
            var dto = new EmployeeMovementReportDto { DepartmentName = group.Key };

            foreach (var emp in group)
            {
                var isCasual = emp.Type == EmployeeType.Casual;

                if (emp.DateEmployed >= start && emp.DateEmployed <= end)
                {
                    if (!isCasual) dto.PermanentNew++;
                    else dto.CasualNew++;
                }

                if (emp.ExitDate >= start && emp.ExitDate <= end)
                {
                    switch (emp.InactiveStatus)
                    {
                        case EmployeeInactiveStatus.Resignation:
                            if (isCasual) dto.CasualResignation++;
                            else dto.PermanentResignation++;
                            break;
                        case EmployeeInactiveStatus.Termination:
                            if (isCasual) dto.CasualTermination++;
                            else dto.PermanentTermination++;
                            break;
                        case EmployeeInactiveStatus.SummaryDismissed:
                            if (isCasual) dto.CasualSDVP++;
                            else dto.PermanentSDVP++;
                            break;
                        case EmployeeInactiveStatus.Transfer:
                            dto.PermanentTransfer++; // assuming transfers are only permanent
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            result.Add(dto);
        }
        return result;
    }

    public async Task<Result<StaffTotalReport>> GetStaffTotalReport(MovementReportFilter filter)
    {

        var employees = context.Employees
            .Include(e => e.Department)
            .Where(e => (e.Type == EmployeeType.Casual  || e.Type == EmployeeType.Permanent) && !e.InactiveStatus.HasValue);

        if (filter.DepartmentId.HasValue)
        {
            employees = employees.Where(e => e.DepartmentId == filter.DepartmentId.Value);
        }

        if (filter.StartDate.HasValue && filter.EndDate.HasValue)
        {
            employees = employees.Where(e =>
                e.DateEmployed.Date >= filter.StartDate.Value.Date &&
                e.DateEmployed.Date <= filter.EndDate.Value.Date);
        }

        var groupedResults = await employees
            .GroupBy(e => e.Department.Name ?? "Unassigned")
            .Select(g => new StaffTotalSummary
            {
                Department = g.Key,
                TotalPermanentStaff = g.Count(e => e.Type == EmployeeType.Permanent),
                TotalCasualStaff = g.Count(e => e.Type == EmployeeType.Casual)
            })
            .ToListAsync();

        var total = new StaffGrandTotal
        {
            TotalPermanentStaff = groupedResults.Sum(x => x.TotalPermanentStaff),
            TotalCasualStaff = groupedResults.Sum(x => x.TotalCasualStaff)
        };
        

        return new StaffTotalReport
        {
            Departments = groupedResults,
            Totals = total
        };
    }
}
    
    
