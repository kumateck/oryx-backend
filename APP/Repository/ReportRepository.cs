using APP.IRepository;
using AutoMapper;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.AttendanceRecords;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.LeaveRequests;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.OvertimeRequests;
using DOMAIN.Entities.ProductionSchedules.StockTransfers;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Reports;
using DOMAIN.Entities.Reports.HumanResource;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SHARED;

namespace APP.Repository;

public class ReportRepository(ApplicationDbContext context, IMapper mapper, IMaterialRepository materialRepository, ILogger<ReportRepository> logger)
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
            NumberOfPurchaseRequisitions = await purchaseRequisitions.CountAsync(p => p.DepartmentId == departmentId),
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


    public async Task<Result<List<MaterialWithStockDto>>> GetMaterialsBelowMinimumStockLevel(Guid departmentId)
    {
        var materialDepartments = await context.MaterialDepartments
            .AsSplitQuery()
            .Include(md => md.Material).Include(materialDepartment => materialDepartment.UoM)
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

        List<MaterialWithStockDto> materialsBelowMinStock = [];

        foreach (var materialDepartment in materialDepartments)
        {
            var material = materialDepartment.Material;
            var warehouseId = material.Kind == MaterialKind.Raw ? rawWarehouse.Id : packageWarehouse.Id;

            var stockResult = await materialRepository.GetMaterialStockInWarehouse(material.Id, warehouseId);

            if (stockResult.IsFailure) continue;

            var stock = stockResult.Value;

            if (stock < materialDepartment.MinimumStockLevel)
            {
                materialsBelowMinStock.Add(new MaterialWithStockDto
                {
                    Material = mapper.Map<MaterialDto>(material),
                    StockQuantity = stock,
                    UoM = mapper.Map<UnitOfMeasureDto>(materialDepartment.UoM)
                });
            }
        }

        return materialsBelowMinStock;
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
    
    public async Task<Result<EmployeeMovementReportDto>> GetEmployeeMovementReport(MovementReportFilter filter)
    {
        var start = filter.StartDate ?? DateTime.UtcNow.AddMonths(-1); 
        var end = filter.EndDate ?? DateTime.UtcNow;

        // Get employees who were either hired or left during the period
        var query = context.Employees
            .Include(e => e.Department)
            .Where(e => 
                (e.DateEmployed >= start && e.DateEmployed <= end) || 
                (e.Status == EmployeeStatus.Inactive && 
                 e.ExitDate.HasValue && 
                 e.ExitDate >= start && 
                 e.ExitDate <= end) 
            );

        if (filter.DepartmentId.HasValue)
            query = query.Where(e => e.DepartmentId == filter.DepartmentId.Value);

        var employees = await query.ToListAsync();

        var grouped = employees.GroupBy(e => e.Department?.Name ?? "Unassigned");

        var departments = new List<EmployeeMovementCountDto>();
        var totals = new EmployeeMovementGrandTotalDto();

        foreach (var group in grouped)
        {
            var dto = new EmployeeMovementCountDto { DepartmentName = group.Key };

            foreach (var emp in group)
            {
                var isCasual = emp.Type == EmployeeType.Casual;

                // Count new hires during the period
                if (emp.DateEmployed >= start && emp.DateEmployed <= end)
                {
                    if (isCasual)
                    {
                        dto.CasualNew++;
                        totals.CasualNew++;
                    }
                    else
                    {
                        dto.PermanentNew++;
                        totals.PermanentNew++;
                    }
                }

                // Count exits during the period (only for inactive employees)
                if (emp.Status == EmployeeStatus.Inactive && 
                    emp.ExitDate.HasValue && 
                    emp.ExitDate >= start && 
                    emp.ExitDate <= end && 
                    emp.InactiveStatus.HasValue)
                {
                    switch (emp.InactiveStatus.Value)
                    {
                        case EmployeeInactiveStatus.Resignation:
                            if (isCasual)
                            {
                                dto.CasualResignation++;
                                totals.CasualResignation++;
                            }
                            else
                            {
                                dto.PermanentResignation++;
                                totals.PermanentResignation++;
                            }
                            break;
                            
                        case EmployeeInactiveStatus.Termination:
                        case EmployeeInactiveStatus.Deceased:
                            if (isCasual)
                            {
                                dto.CasualTermination++;
                                totals.CasualTermination++;
                            }
                            else
                            {
                                dto.PermanentTermination++;
                                totals.PermanentTermination++;
                            }
                            break;
                            
                        case EmployeeInactiveStatus.SummaryDismissed:
                            if (isCasual)
                            {
                                dto.CasualSDVP++;
                                totals.CasualSDVP++;
                            }
                            else
                            {
                                dto.PermanentSDVP++;
                                totals.PermanentSDVP++;
                            }
                            break;
                            
                        case EmployeeInactiveStatus.Transfer:
                            // Transfers are typically permanent employees
                            if (!isCasual)
                            {
                                dto.PermanentTransfer++;
                                totals.PermanentTransfer++;
                            }
                            break;
                            
                        case EmployeeInactiveStatus.VacatedPost:
                            // These might need separate handling depending on your business rules
                            // For now, treating them as terminations
                            if (isCasual)
                            {
                                dto.CasualSDVP++;
                                totals.CasualSDVP++;
                            }
                            else
                            {
                                dto.PermanentSDVP++;
                                totals.PermanentSDVP++;
                            }
                            break;
                            
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            
            departments.Add(dto);
        }

        var result = new EmployeeMovementReportDto
        {
            Departments = departments,
            Totals = totals
        };

        return Result.Success(result);
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
        

        return Result.Success(new StaffTotalReport
        {
            Departments = groupedResults,
            Totals = total
        });
    }

    public async Task<Result<StaffGenderRatioReport>> GetStaffGenderRatioReport(MovementReportFilter filter)
    {

        var query = context.Employees
            .Include(e => e.Department)
            .Where(e => e.Status == EmployeeStatus.Active); // hired before or during the period

        if (filter.DepartmentId.HasValue)
            query = query.Where(e => e.DepartmentId == filter.DepartmentId.Value);

        if (filter.StartDate.HasValue && filter.EndDate.HasValue)
        {
            query = query.Where(e =>e.DateEmployed.Date >= filter.StartDate.Value.Date &&
                                     e.DateEmployed.Date <= filter.EndDate.Value.Date);
        }

        var employees = await query.ToListAsync();

        var grouped = employees.GroupBy(e => e.Department?.Name ?? "Unassigned");

        var departments = new List<StaffGenderRatioCountDto>();
        var totals = new StaffGenderRatioTotalDto();

        foreach (var group in grouped)
        {
            var dto = new StaffGenderRatioCountDto
            {
                Department = group.Key
            };

            foreach (var emp in group)
            {
                var isCasual = emp.Type == EmployeeType.Casual;
                var isMale = emp.Gender == Gender.Male;
                var isFemale = emp.Gender == Gender.Female;

                if (isCasual && isMale) dto.NumberOfCasualMale++;
                switch (isCasual)
                {
                    case true when isFemale:
                        dto.NumberOfCasualFemale++;
                        break;
                    case false when isMale:
                        dto.NumberOfPermanentMale++;
                        break;
                }

                if (!isCasual && isFemale) dto.NumberOfPermanentFemale++;
            }

            // Add to totals
            totals.NumberOfCasualMale += dto.NumberOfCasualMale;
            totals.NumberOfCasualFemale += dto.NumberOfCasualFemale;
            totals.NumberOfPermanentMale += dto.NumberOfPermanentMale;
            totals.NumberOfPermanentFemale += dto.NumberOfPermanentFemale;

            departments.Add(dto);
        }

        var result = new StaffGenderRatioReport
        {
            Departments = departments,
            Totals = totals
        };

        return Result.Success(result);
    }

public async Task<Result<StaffLeaveSummaryReportDto>> GetStaffLeaveSummaryReport(MovementReportFilter filter)
{
    logger.LogInformation("Generating Staff Leave Summary Report with filter: {@Filter}", filter);

    var start = filter.StartDate?.Date ?? new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    var end = filter.EndDate?.Date ?? DateTime.UtcNow.Date;
    var today = DateTime.UtcNow.Date;

    logger.LogInformation("Date range resolved: {Start} - {End}", start, end);

    var employeesQuery = context.Employees
        .Include(e => e.Department)
        .Where(e => e.Status == EmployeeStatus.Active);

    if (filter.DepartmentId.HasValue)
    {
        employeesQuery = employeesQuery.Where(e => e.DepartmentId == filter.DepartmentId);
        logger.LogInformation("Filtering by department: {DepartmentId}", filter.DepartmentId);
    }

    var employees = await employeesQuery.ToListAsync();
    logger.LogInformation("Fetched {EmployeeCount} active employees", employees.Count);

    var employeeIds = employees.Select(e => e.Id).ToList();

    var leaveRequests = await context.LeaveRequests
        .Where(l =>
            employeeIds.Contains(l.EmployeeId) &&
            l.Approved &&
            l.RequestCategory == RequestCategory.LeaveRequest &&
            l.StartDate <= end &&
            l.EndDate >= start)
        .ToListAsync();

    logger.LogInformation("Fetched {LeaveRequestCount} approved leave requests in date range", leaveRequests.Count);

    var grouped = employees.GroupBy(e => e.Department?.Name ?? "Unassigned");

    var report = new StaffLeaveSummaryReportDto();

    foreach (var group in grouped)
    {
        var departmentName = group.Key ?? "Unassigned";
        var count = group.Count(e => e.DateEmployed.AddMonths(12) <= today);
        var totalEntitlement = group.Sum(e => e.AnnualLeaveDays);
        var deptEmployeeIds = group.Select(e => e.Id).ToList();

        var daysUsed = leaveRequests
            .Where(r => deptEmployeeIds.Contains(r.EmployeeId))
            .Sum(r => (r.PaidDays ?? 0) + (r.UnpaidDays ?? 0));

        logger.LogInformation("Dept: {Dept}, DueForLeave: {Count}, TotalLeave: {Entitlement}, DaysUsed: {Used}",
            departmentName, count, totalEntitlement, daysUsed);

        var dto = new StaffLeaveSummaryDto
        {
            DepartmentName = departmentName,
            StaffDueForLeave = count,
            TotalLeaveEntitlement = totalEntitlement,
            DaysUsed = daysUsed
        };

        report.Departments.Add(dto);
        
    }

    return Result.Success(report);
}

    public async Task<Result<StaffTurnoverReportDto>> GetStaffTurnoverReport(MovementReportFilter filter)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<QaDashboardDto>> GetQaDashboardReport(ReportFilter filter)
    {
        var approvals = context.Approvals.AsQueryable();
        var analyticalTestRequests = context.AnalyticalTestRequests.AsQueryable();
        var approvedManufacturers = context.Manufacturers.AsQueryable();
        var products = context.Products.AsQueryable();
        var materials = context.Materials.AsQueryable();
        var bmrRequests = context.BatchManufacturingRecords.AsQueryable();
        var billingSheetApprovals = context.BillingSheetApprovals.AsQueryable();
        var leaveRequestApprovals = context.LeaveRequestApprovals.AsQueryable();
        var purchaseOrderApprovals = context.PurchaseOrderApprovals.AsQueryable();
        var requisitionApprovals = context.RequisitionApprovals.AsQueryable();
        var responseApprovals = context.ResponseApprovals.AsQueryable();
        var staffRequisitionApprovals = context.StaffRequisitionApprovals.AsQueryable();
        
        
        if (filter.StartDate.HasValue)
        {
            approvals = approvals.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            analyticalTestRequests = analyticalTestRequests.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            approvedManufacturers = approvedManufacturers.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            products = products.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            materials = materials.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            bmrRequests = bmrRequests.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            requisitionApprovals = requisitionApprovals.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            responseApprovals = responseApprovals.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            billingSheetApprovals = billingSheetApprovals.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            leaveRequestApprovals = leaveRequestApprovals.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            purchaseOrderApprovals =  purchaseOrderApprovals.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            staffRequisitionApprovals = staffRequisitionApprovals.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            approvals = approvals.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
            analyticalTestRequests = analyticalTestRequests.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
            approvedManufacturers = approvedManufacturers.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
            products = products.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
            materials = materials.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
            bmrRequests = bmrRequests.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
            requisitionApprovals = requisitionApprovals.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
            responseApprovals = responseApprovals.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
            billingSheetApprovals = billingSheetApprovals.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
            leaveRequestApprovals = leaveRequestApprovals.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
            purchaseOrderApprovals = purchaseOrderApprovals.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
            staffRequisitionApprovals = staffRequisitionApprovals.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
        }

        return new QaDashboardDto
        {
            NumberOfBmrRequests = bmrRequests.Count(),
            NumberOfPendingBmrRequests = bmrRequests.Count(bmr => bmr.Status == BatchManufacturingStatus.New),
            NumberOfApprovedBmrRequests = bmrRequests.Count(bmr => bmr.Status == BatchManufacturingStatus.Approved),
            NumberOfRejectBmrRequests = bmrRequests.Count(bmr => bmr.Status == BatchManufacturingStatus.Rejected),
            NumberOfAnalyticalTestRequests = analyticalTestRequests.Count(),
            NumberOfExpiredAnalyticalTestRequests = 
                await analyticalTestRequests.CountAsync(or => or.ExpiryDate > filter.StartDate && or.ExpiryDate <= filter.EndDate),
            NumberOfApprovals = await approvals.CountAsync(),
            NumberOfPendingApprovals = await requisitionApprovals.CountAsync(s => s.Status == ApprovalStatus.Pending) 
                                       + await billingSheetApprovals.CountAsync(s => s.Status == ApprovalStatus.Pending) 
                                    + await leaveRequestApprovals.CountAsync(s => s.Status == ApprovalStatus.Pending) 
                                       + await purchaseOrderApprovals.CountAsync(s => s.Status == ApprovalStatus.Pending)
                                    + await staffRequisitionApprovals.CountAsync(s => s.Status == ApprovalStatus.Pending) 
                                       + await purchaseOrderApprovals.CountAsync(s => s.Status == ApprovalStatus.Pending)
                                    + await responseApprovals.CountAsync(s => s.Status == ApprovalStatus.Pending),
            
            NumberOfRejectedApprovals = await requisitionApprovals.CountAsync(s => s.Status == ApprovalStatus.Rejected) 
                                        + await billingSheetApprovals.CountAsync(s => s.Status == ApprovalStatus.Rejected) 
                                        + await leaveRequestApprovals.CountAsync(s => s.Status == ApprovalStatus.Rejected) 
                                        + await purchaseOrderApprovals.CountAsync(s => s.Status == ApprovalStatus.Rejected)
                                        + await staffRequisitionApprovals.CountAsync(s => s.Status == ApprovalStatus.Rejected) 
                                        + await purchaseOrderApprovals.CountAsync(s => s.Status == ApprovalStatus.Rejected)
                                        + await responseApprovals.CountAsync(s => s.Status == ApprovalStatus.Rejected),
            NumberOfManufacturers = await approvedManufacturers.CountAsync(),
            NumberOfNewManufacturers = await approvedManufacturers.CountAsync(),
            NumberOfExpiredManufacturers = await approvedManufacturers.CountAsync(am =>am.ValidityDate.HasValue && am.ValidityDate.Value < DateTime.UtcNow),
            NumberOfProducts = await products.CountAsync(),
            NumberOfPackingMaterials = await materials.CountAsync(m => m.Kind == MaterialKind.Package),
            NumberOfRawMaterials = await materials.CountAsync(m => m.Kind == MaterialKind.Raw)
        };

    }

    public async Task<Result<WarehouseReportDto>> GetWarehouseReport(ReportFilter filter, Guid departmentId)
    {
        var stockRequisitions = context.Requisitions
            .Where(r => r.RequisitionType == RequisitionType.Stock && r.DepartmentId == departmentId)
            .AsQueryable();
        
        var incomingStockTransfers = context.StockTransferSources
            .Where(s => s.FromDepartmentId == departmentId)
            .AsQueryable();

        var shipments = context.ShipmentDocuments
            .AsSplitQuery()
            .Include(s => s.ShipmentInvoice)
            .ThenInclude(si => si.Items)
            .Where(s =>
                s.ShipmentInvoice.Items.Any(item =>
                    context.PurchaseOrders
                        .Where(po => po.Id == item.PurchaseOrderId)
                        .Select(po => po.SourceRequisitionId)
                        .Join(
                            context.SourceRequisitions.Include(sr => sr.Items),
                            poSrcId => poSrcId,
                            sr => sr.Id,
                            (poSrcId, sr) => sr.Items.Select(i => i.RequisitionId)
                        )
                        .SelectMany(ids => ids)
                        .Join(
                            context.Requisitions,
                            reqId => reqId,
                            r => r.Id,
                            (reqId, r) => r.DepartmentId
                        )
                        .Distinct()
                        .Contains(departmentId)
                )
            )
            .AsQueryable();
        
        
        if (filter.StartDate.HasValue)
        {
            var start = filter.StartDate.Value;
            stockRequisitions = stockRequisitions.Where(r => r.CreatedAt >= start);
            incomingStockTransfers = incomingStockTransfers.Where(s => s.CreatedAt >= start);
            shipments = shipments.Where(s => s.CreatedAt >= start);
        }

        
        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.AddDays(1);
            stockRequisitions = stockRequisitions.Where(r => r.CreatedAt < end);
            incomingStockTransfers = incomingStockTransfers.Where(r => r.CreatedAt < end);
            shipments = shipments.Where(r => r.CreatedAt < end);
        }
        
        return new WarehouseReportDto
        {
            NumberOfStockRequisitions = await stockRequisitions.CountAsync(),
            NumberOfNewStockRequisitions = await stockRequisitions.CountAsync(s => s.Status == RequestStatus.New),
            NumberOfInProgressStockRequisitions =  await stockRequisitions.CountAsync(s => s.Status == RequestStatus.Pending),
            NumberOfCompletedStockRequisitions =  await stockRequisitions.CountAsync(s => s.Status == RequestStatus.Completed),
            NumberOfIncomingStockTransfers = await incomingStockTransfers.CountAsync(),
            NumberOfIncomingPendingStockTransfers = await incomingStockTransfers.CountAsync(s => s.Status == StockTransferStatus.InProgress),
            NumberOfIncomingCompletedStockTransfers = await incomingStockTransfers.CountAsync(s => s.Status == StockTransferStatus.Issued),
            NumberOfShipments =  await shipments.CountAsync(),
            NumberOfInTransitShipments = await shipments.CountAsync(s => s.Status == ShipmentStatus.InTransit),
            NumberOfArrivedShipments = await shipments.CountAsync(s => s.Status == ShipmentStatus.Arrived),
            NumberOfClearedShipments = await shipments.CountAsync(s => s.Status == ShipmentStatus.Cleared),
        };
    }
    
    public async Task<Result<List<MaterialBatchReservedQuantityReportDto>>> GetReservedMaterialBatchesForDepartment(ReportFilter filter, Guid departmentId)
    {
        var department = await context.Departments.FirstOrDefaultAsync(d => d.Id == departmentId);
        if(department is null) return Error.NotFound("Department", "Department not found.");
        
        var materialBatchReserved = context.MaterialBatchReservedQuantities
            .AsSplitQuery()
            .Include(m => m.MaterialBatch).ThenInclude(b => b.Material)
            .Include(m => m.Warehouse)
            .Where(m => m.Warehouse.DepartmentId == departmentId)
            .AsQueryable();
        
        if (filter.MaterialKind.HasValue)
        {
            materialBatchReserved = materialBatchReserved
                .Where(m => m.MaterialBatch.Material.Kind == filter.MaterialKind);
        }

        if (filter.StartDate.HasValue)
        {
            var start = filter.StartDate.Value;
            materialBatchReserved = materialBatchReserved.Where(r => r.CreatedAt >= start);
        }

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.AddDays(1);
            materialBatchReserved = materialBatchReserved.Where(r => r.CreatedAt < end);
        }

        return await materialBatchReserved.Select(item => new MaterialBatchReservedQuantityReportDto
        {
            Warehouse = mapper.Map<CollectionItemDto>(item.Warehouse),
            Material = mapper.Map<CollectionItemDto>(item.MaterialBatch.Material),
            UoM = mapper.Map<UnitOfMeasureDto>(item.UoM),
            Quantity = item.Quantity
        }).ToListAsync();
    }

    public async Task<Result<IEnumerable<DistributedRequisitionMaterialDto>>> GetMaterialsReadyForChecklist(ReportFilter filter, Guid userId)
    {

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return UserErrors.NotFound(userId);

        var warehouses = await context.Warehouses.Where(w => w.DepartmentId == user.DepartmentId).ToListAsync();

        var rawMaterialWarehouse = warehouses.FirstOrDefault(w => w.Type == WarehouseType.RawMaterialStorage);

        if (rawMaterialWarehouse is null)
            return Error.NotFound("Warehouse.Raw", "This user has no raw material configured for his department");

        var packageMaterialWarehouse = warehouses.FirstOrDefault(w => w.Type == WarehouseType.PackagedStorage);
        
        if (packageMaterialWarehouse is null)
            return Error.NotFound("Warehouse.Package", "This user has no packaging material configured for his department");
        
        var query = context.DistributedRequisitionMaterials
            .Include(drm => drm.ShipmentInvoice)
            .Include(drm => drm.Material)
            .Include(drm => drm.RequisitionItem)
            .Include(drm => drm.WarehouseArrivalLocation)
            .Include(drm=>drm.MaterialItemDistributions)
            .Include(sr=>sr.CheckLists)
            .ThenInclude(cl=>cl.MaterialBatches)
            .Where(drm => drm.Status == DistributedRequisitionMaterialStatus.Distributed)
            .AsQueryable();

        query = filter.MaterialKind == MaterialKind.Raw
            ? query.Where(q => q.WarehouseArrivalLocation.WarehouseId == rawMaterialWarehouse.Id)
            : query.Where(q => q.WarehouseArrivalLocation.WarehouseId == packageMaterialWarehouse.Id);
        
        if (filter.StartDate.HasValue)
        {
            var start = filter.StartDate.Value;
            query = query.Where(r => r.CreatedAt >= start);
        }

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.AddDays(1);
            query = query.Where(r => r.CreatedAt < end);
        }
        
        return mapper.Map<List<DistributedRequisitionMaterialDto>>(await query.ToListAsync());
    }

    public async Task<Result<List<MaterialBatchDto>>> GetMaterialsReadyForAssignment(ReportFilter filter,
        Guid departmentId)
    {
        var query = context.MaterialBatches
            .AsSplitQuery()
            .Include(m => m.Checklist.DistributedRequisitionMaterial.WarehouseArrivalLocation.Warehouse)
            .Include(m => m.Material)
            .Where(m =>
                m.Checklist.DistributedRequisitionMaterial.WarehouseArrivalLocation.Warehouse.DepartmentId == departmentId &&
                m.Status == BatchStatus.Approved)
            .AsQueryable();

        if (filter.MaterialKind.HasValue)
        {
            query = query.Where(b => b.Material.Kind == filter.MaterialKind);
        }
          
        if (filter.StartDate.HasValue)
        {
            var start = filter.StartDate.Value;
            query = query.Where(r => r.CreatedAt >= start);
        }

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.AddDays(1);
            query = query.Where(r => r.CreatedAt < end);
        }
        
        return mapper.Map<List<MaterialBatchDto>>(await query.ToListAsync());
    }

    public async Task<Result<LogisticsReportDto>> GetLogisticsReport(ReportFilter filter, Guid departmentId)
    {
        throw new NotImplementedException();
    }
}
