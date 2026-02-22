using APP.IRepository;
using AutoMapper;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.AttendanceRecords;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.LeaveRequests;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.OvertimeRequests;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.ProductionOrders;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.ProductionSchedules.StockTransfers;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.Reports;
using DOMAIN.Entities.Reports.FinishedGoodsTransferNotes;
using DOMAIN.Entities.Reports.HumanResource;
using DOMAIN.Entities.Reports.Procurement;
using DOMAIN.Entities.Reports.PurchaseOrder;
using DOMAIN.Entities.Reports.Shipments;
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

            var stockResult = await materialRepository.GetMassMaterialStockInWarehouse(material.Id, warehouseId);

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


    public async Task<Result<HrDashboardDto>> GetHumanResourceDashboardReport(MovementReportFilter filter,
        Guid? designationId, EmployeeType? employeeType, Gender? gender)
    {

        var leaveRequests = context.LeaveRequests.AsQueryable();
        var overtimeRequests = context.OvertimeRequests.AsQueryable();
        var employees = context.Employees.AsQueryable();
        var staffRequisitions = context.StaffRequisitions.AsQueryable();

        if (filter.StartDate.HasValue)
        {
            leaveRequests = leaveRequests.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            overtimeRequests = overtimeRequests.Where(or => or.CreatedAt >= filter.StartDate.Value);
            staffRequisitions = staffRequisitions.Where(sr => sr.CreatedAt >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.AddDays(1);
            leaveRequests = leaveRequests.Where(lr => lr.CreatedAt < end);
            overtimeRequests = overtimeRequests.Where(or => or.CreatedAt < end);
            staffRequisitions = staffRequisitions.Where(sr => sr.CreatedAt < end);
        }


        if (filter.DepartmentId.HasValue)
        {
            employees = employees.Where(e => e.DepartmentId == filter.DepartmentId.Value);
            leaveRequests = leaveRequests.Where(lr => lr.Employee.DepartmentId == filter.DepartmentId.Value);
            overtimeRequests = overtimeRequests.Where(or => or.Employees.Any(e => e.DepartmentId == filter.DepartmentId.Value));
            staffRequisitions = staffRequisitions.Where(sr => sr.DepartmentId == filter.DepartmentId.Value);
        }


        if (designationId.HasValue)
        {
            employees = employees.Where(e => e.DesignationId == designationId.Value);
            leaveRequests = leaveRequests.Where(lr => lr.Employee.DesignationId == designationId.Value);
            overtimeRequests = overtimeRequests.Where(or => or.Employees.Any(e => e.DesignationId == designationId.Value));
            staffRequisitions = staffRequisitions.Where(sr => sr.DesignationId == designationId.Value);
        }


        if (employeeType.HasValue)
        {
            employees = employees.Where(e => e.Type == employeeType.Value);
            leaveRequests = leaveRequests.Where(lr => lr.Employee.Type == employeeType.Value);
            overtimeRequests = overtimeRequests.Where(or => or.Employees.Any(e => e.Type == employeeType.Value));
        }

        if (gender.HasValue)
        {
            employees = employees.Where(e => e.Gender == gender.Value);
            leaveRequests = leaveRequests.Where(lr => lr.Employee.Gender == gender.Value);
            overtimeRequests = overtimeRequests.Where(or => or.Employees.Any(e => e.Gender == gender.Value));
        }

        var employeeStats = await employees.GroupBy(e => 1).Select(g => new
        {
            TotalCasual = g.Count(e => e.Type == EmployeeType.Casual),
            TotalPermanent = g.Count(e => e.Type == EmployeeType.Permanent),
            ActiveCasual = g.Count(e => e.Type == EmployeeType.Casual && e.Status == EmployeeStatus.Active),
            ActivePermanent = g.Count(e => e.Type == EmployeeType.Permanent && e.Status == EmployeeStatus.Active),
            InactiveCasual = g.Count(e => e.Type == EmployeeType.Casual && e.Status == EmployeeStatus.Inactive),
            InactivePermanent = g.Count(e => e.Type == EmployeeType.Permanent && e.Status == EmployeeStatus.Inactive),
            NewCasual = g.Count(e => e.Type == EmployeeType.Casual && e.Status == EmployeeStatus.New),
            NewPermanent = g.Count(e => e.Type == EmployeeType.Permanent && e.Status == EmployeeStatus.New),
            Male = g.Count(e => e.Gender == Gender.Male),
            Female = g.Count(e => e.Gender == Gender.Female)
        }).FirstOrDefaultAsync();


        var leaveStats = await leaveRequests.GroupBy(lr => 1).Select(g => new
        {
            Total = g.Count(),
            Pending = g.Count(lr => lr.LeaveStatus == LeaveStatus.Pending),
            Expired = g.Count(lr => lr.LeaveStatus == LeaveStatus.Expired),
            Rejected = g.Count(lr => lr.LeaveStatus == LeaveStatus.Rejected),

            Absence = g.Count(lr => lr.RequestCategory == RequestCategory.AbsenceRequest),
            PendingAbsence = g.Count(lr => lr.RequestCategory == RequestCategory.AbsenceRequest && lr.LeaveStatus == LeaveStatus.Pending),
            ApprovedAbsence = g.Count(lr => lr.RequestCategory == RequestCategory.AbsenceRequest && lr.LeaveStatus == LeaveStatus.Approved),
            RejectedAbsence = g.Count(lr => lr.RequestCategory == RequestCategory.AbsenceRequest && lr.LeaveStatus == LeaveStatus.Rejected),

            ExitPass = g.Count(lr => lr.RequestCategory == RequestCategory.ExitPassRequest),
            PendingExitPass = g.Count(lr => lr.RequestCategory == RequestCategory.ExitPassRequest && lr.LeaveStatus == LeaveStatus.Pending),
            ApprovedExitPass = g.Count(lr => lr.RequestCategory == RequestCategory.ExitPassRequest && lr.LeaveStatus == LeaveStatus.Approved),
            RejectedExitPass = g.Count(lr => lr.RequestCategory == RequestCategory.ExitPassRequest && lr.LeaveStatus == LeaveStatus.Rejected),

            OfficialDuty = g.Count(lr => lr.RequestCategory == RequestCategory.OfficialDuty),
            ApprovedOfficialDuty = g.Count(lr => lr.RequestCategory == RequestCategory.OfficialDuty && lr.LeaveStatus == LeaveStatus.Approved),
            RejectedOfficialDuty = g.Count(lr => lr.RequestCategory == RequestCategory.OfficialDuty && lr.LeaveStatus == LeaveStatus.Rejected),
            PendingOfficialDuty = g.Count(lr => lr.RequestCategory == RequestCategory.OfficialDuty && lr.LeaveStatus == LeaveStatus.Pending)
        }).FirstOrDefaultAsync();


        var overtimeStats = await overtimeRequests.GroupBy(or => 1).Select(g => new
        {
            Total = g.Count(),
            Approved = g.Count(or => or.Status == OvertimeStatus.Approved),
            Pending = g.Count(or => or.Status == OvertimeStatus.Pending),
            Expired = g.Count(or => or.Status == OvertimeStatus.Expired)
        }).FirstOrDefaultAsync();


        var staffRequisitionCount = await staffRequisitions.CountAsync();

        var ratio = employeeStats?.Female > 0
            ? (decimal)employeeStats.Male / employeeStats.Female
            : 0;

        return new HrDashboardDto
        {
            NumberOfOvertimeRequests = overtimeStats?.Total ?? 0,
            NumberOfApprovedOvertimeRequests = overtimeStats?.Approved ?? 0,
            NumberOfPendingOvertimeRequests = overtimeStats?.Pending ?? 0,
            NumberOfExpiredOvertimeRequests = overtimeStats?.Expired ?? 0,

            NumberOfCasualEmployees = employeeStats?.TotalCasual ?? 0,
            NumberOfPermanentEmployees = employeeStats?.TotalPermanent ?? 0,
            NumberOfActiveCasualEmployees = employeeStats?.ActiveCasual ?? 0,
            NumberOfActivePermanentEmployees = employeeStats?.ActivePermanent ?? 0,
            NumberOfInactiveCasualEmployees = employeeStats?.InactiveCasual ?? 0,
            NumberOfInactivePermanentEmployees = employeeStats?.InactivePermanent ?? 0,
            NumberOfNewCasualEmployees = employeeStats?.NewCasual ?? 0,
            NumberOfNewPermanentEmployees = employeeStats?.NewPermanent ?? 0,

            NumberOfLeaveRequests = leaveStats?.Total ?? 0,
            NumberOfPendingLeaveRequests = leaveStats?.Pending ?? 0,
            NumberOfExpiredLeaveRequests = leaveStats?.Expired ?? 0,
            NumberOfRejectedLeaveRequests = leaveStats?.Rejected ?? 0,

            NumberOfAbsenceRequests = leaveStats?.Absence ?? 0,
            NumberOfPendingAbsenceRequests = leaveStats?.PendingAbsence ?? 0,
            NumberOfApprovedAbsenceRequests = leaveStats?.ApprovedAbsence ?? 0,
            NumberOfRejectedAbsenceRequests = leaveStats?.RejectedAbsence ?? 0,

            NumberOfExitPasses = leaveStats?.ExitPass ?? 0,
            NumberOfPendingExitPasses = leaveStats?.PendingExitPass ?? 0,
            NumberOfApprovedExitPasses = leaveStats?.ApprovedExitPass ?? 0,
            NumberOfRejectedExitPasses = leaveStats?.RejectedExitPass ?? 0,

            NumberOfOfficialDutyLeaves = leaveStats?.OfficialDuty ?? 0,
            NumberOfApprovedOfficialDutyLeaves = leaveStats?.ApprovedOfficialDuty ?? 0,
            NumberOfRejectedOfficialDutyLeaves = leaveStats?.RejectedOfficialDuty ?? 0,
            NumberOfPendingOfficialDutyLeaves = leaveStats?.PendingOfficialDuty ?? 0,

            NumberOfStaffRequisitions = staffRequisitionCount,

            EmployeeGenderRatio = ratio,
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

                switch (emp.Status)
                {
                    // Count new hires during the period
                    case EmployeeStatus.New when isCasual:
                        dto.CasualNew++;
                        totals.CasualNew++;
                        break;
                    case EmployeeStatus.New:
                        dto.PermanentNew++;
                        totals.PermanentNew++;
                        break;
                    // Count exits during the period (only for inactive employees)
                    case EmployeeStatus.Inactive when
                        emp.ExitDate.HasValue &&
                        emp.ExitDate >= start &&
                        emp.ExitDate <= end &&
                        emp.InactiveStatus.HasValue:
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

                        break;
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
            .Where(e => (e.Type == EmployeeType.Casual || e.Type == EmployeeType.Permanent) && !e.InactiveStatus.HasValue);

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

    public Task<Result<StaffGenderRatioReport>> GetStaffGenderRatioReport(MovementReportFilter filter)
    {
        throw new NotImplementedException();
    }


    /*
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
    */

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



    public async Task<Result<StaffTurnoverReportDto>> GetStaffTurnoverReport(ReportFilter filter)
    {
        var currentYear = DateTime.UtcNow.Year;

        var startYear = filter.StartDate?.Year ?? currentYear;
        var endYear = filter.EndDate?.Year ?? currentYear;

        filter.StartDate = new DateTime(startYear, 1, 1);
        filter.EndDate = new DateTime(endYear, 12, 31);

        var leavers = await context.Employees
            .Where(e => e.ExitDate.HasValue &&
                        e.ExitDate.Value.Year == filter.StartDate.Value.Year)
            .ToListAsync();

        var startCount = await context.Employees
            .Where(e => e.DateEmployed <= filter.StartDate.Value &&
                        (!e.ExitDate.HasValue || e.ExitDate >= filter.EndDate.Value))
            .CountAsync();

        var endCount = await context.Employees
            .Where(e => e.DateEmployed <= filter.EndDate.Value &&
                        (!e.ExitDate.HasValue || e.ExitDate >= filter.EndDate.Value))
            .CountAsync();

        var averageEmployees = (startCount + endCount) / 2.0;
        var turnoverRate = averageEmployees == 0 ? 0 : leavers.Count / averageEmployees * 100.0;

        var exitedEmployees = await context.Employees
            .Include(e => e.Department)
            .Where(e => e.ExitDate.HasValue && e.ExitDate.Value.Year == filter.EndDate.Value.Year)
            .ToListAsync();

        var departmentSummaries = exitedEmployees
            .GroupBy(e => e.Department?.Name ?? "Unassigned")
            .Select(group => new StaffTurnoverCountDto
            {
                DepartmentName = group.Key,
                ExitReasons = group
                    .GroupBy(e => e.InactiveStatus?.ToString() ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count())
            })
            .ToList();

        var grandTotal = departmentSummaries.Sum(d => d.TotalLeavers);

        return new StaffTurnoverReportDto
        {
            TurnoverRate = Math.Round(turnoverRate, 2),
            GrandTotalLeavers = grandTotal,
            DepartmentSummaries = departmentSummaries
        };
    }

    public async Task<Result<QaDashboardDto>> GetQaDashboardReport(ReportFilter filter, Guid? productId)
    {
        var analyticalTestRequests = context.AnalyticalTestRequests
            .AsSplitQuery()
            .Include(p => p.ProductionScheduleProduct)
            .AsQueryable();
        var approvedManufacturers = context.Manufacturers.AsQueryable();
        var products = context.Products.AsQueryable();
        var materials = context.Materials.AsQueryable();
        var bmrRequests = context.BatchManufacturingRecords
            .AsSplitQuery()
            .Include(b => b.ProductionScheduleProduct)
            .ThenInclude(b => b.Product)
            .AsQueryable();
        var billingSheetApprovals = context.BillingSheetApprovals.AsQueryable();
        var leaveRequestApprovals = context.LeaveRequestApprovals.AsQueryable();
        var purchaseOrderApprovals = context.PurchaseOrderApprovals.AsQueryable();
        var requisitionApprovals = context.RequisitionApprovals
            .AsSplitQuery()
            .Include(r => r.Requisition)
            .ThenInclude(r => r.ProductionScheduleProduct)
            .ThenInclude(r => r.Product)
            .AsQueryable();
        var responseApprovals = context.ResponseApprovals.AsQueryable();
        var staffRequisitionApprovals = context.StaffRequisitionApprovals.AsQueryable();


        if (filter.StartDate.HasValue)
        {
            analyticalTestRequests = analyticalTestRequests.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            approvedManufacturers = approvedManufacturers.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            products = products.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            materials = materials.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            bmrRequests = bmrRequests.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            requisitionApprovals = requisitionApprovals.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            responseApprovals = responseApprovals.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            billingSheetApprovals = billingSheetApprovals.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            leaveRequestApprovals = leaveRequestApprovals.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            purchaseOrderApprovals = purchaseOrderApprovals.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            staffRequisitionApprovals = staffRequisitionApprovals.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
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

        if (productId.HasValue)
        {
            analyticalTestRequests = analyticalTestRequests.Where(lr => lr.ProductionScheduleProduct.ProductId == productId);
            products = products.Where(lr => lr.Id == productId);
            bmrRequests = bmrRequests.Where(lr =>
                lr.ProductionScheduleProduct != null &&
                lr.ProductionScheduleProduct.ProductId == productId);
            requisitionApprovals = requisitionApprovals.Where(lr =>
                lr.Requisition.ProductionScheduleProduct != null &&
                lr.Requisition.ProductionScheduleProduct.ProductId == productId);
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
            NumberOfApprovals = await requisitionApprovals.CountAsync() + await billingSheetApprovals.CountAsync()
                                + await leaveRequestApprovals.CountAsync() + await purchaseOrderApprovals.CountAsync()
                                + await staffRequisitionApprovals.CountAsync() + await purchaseOrderApprovals.CountAsync()
                                + await responseApprovals.CountAsync(),

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
            NumberOfApprovedManufacturers = await approvedManufacturers.CountAsync(am => am.ApprovedAt.HasValue),
            NumberOfExpiredManufacturers = await approvedManufacturers.CountAsync(am => am.ValidityDate.HasValue && am.ValidityDate.Value < DateTime.UtcNow),
            NumberOfProducts = await products.CountAsync(),
            NumberOfPackingMaterials = await materials.CountAsync(m => m.Kind == MaterialKind.Package),
            NumberOfRawMaterials = await materials.CountAsync(m => m.Kind == MaterialKind.Raw)
        };

    }

    public async Task<Result<QcDashboardDto>> GetQcDashboardReport(ReportFilter filter, Guid? productId, Guid? materialId)
    {
        var materialStp = context.MaterialStandardTestProcedures.AsQueryable();
        var productStp = context.ProductStandardTestProcedures.AsQueryable();

        var materialAnalyticalRawData = context.MaterialAnalyticalRawData.AsQueryable();
        var productAnalyticalRawData = context.ProductAnalyticalRawData.AsQueryable();

        var rawMaterialBatchTest = context.MaterialBatches.AsQueryable();
        // var approvals = context.Approvals.AsQueryable();
        // var billingSheetApprovals = context.BillingSheetApprovals.AsQueryable();
        // var leaveRequestApprovals = context.LeaveRequestApprovals.AsQueryable();
        // var purchaseOrderApprovals = context.PurchaseOrderApprovals.AsQueryable();
        // var requisitionApprovals = context.RequisitionApprovals.AsQueryable();
        // var responseApprovals = context.ResponseApprovals.AsQueryable();
        // var staffRequisitionApprovals = context.StaffRequisitionApprovals.AsQueryable();

        if (filter.StartDate.HasValue)
        {
            materialAnalyticalRawData = materialAnalyticalRawData.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            materialStp = materialStp.Where(ms => ms.CreatedAt >= filter.StartDate);
            productStp = productStp.Where(ms => ms.CreatedAt >= filter.StartDate);
            productAnalyticalRawData = productAnalyticalRawData.Where(lr => lr.CreatedAt >= filter.StartDate);
            rawMaterialBatchTest = rawMaterialBatchTest.Where(lr => lr.CreatedAt >= filter.StartDate);
            // approvals = approvals.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            // requisitionApprovals = requisitionApprovals.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            // responseApprovals = responseApprovals.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            // billingSheetApprovals = billingSheetApprovals.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            // leaveRequestApprovals = leaveRequestApprovals.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            // purchaseOrderApprovals =  purchaseOrderApprovals.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            // staffRequisitionApprovals = staffRequisitionApprovals.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            materialAnalyticalRawData = materialAnalyticalRawData.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
            materialStp = materialStp.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
            productStp = productStp.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
            productAnalyticalRawData = productAnalyticalRawData.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
            rawMaterialBatchTest = rawMaterialBatchTest.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
            // requisitionApprovals = requisitionApprovals.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
            // responseApprovals = responseApprovals.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
            // billingSheetApprovals = billingSheetApprovals.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
            // leaveRequestApprovals = leaveRequestApprovals.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
            // purchaseOrderApprovals = purchaseOrderApprovals.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
            // staffRequisitionApprovals = staffRequisitionApprovals.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
        }

        if (productId.HasValue)
        {
            productStp = productStp.Where(lr => lr.ProductId == productId);
            productAnalyticalRawData = productAnalyticalRawData.Where(lr => lr.ProductStandardTestProcedure.ProductId == productId);

        }

        if (materialId.HasValue)
        {
            materialAnalyticalRawData = materialAnalyticalRawData.Where(lr => lr.MaterialStandardTestProcedure.MaterialId == materialId);
            materialStp = materialStp.Where(lr => lr.MaterialId == materialId);
            rawMaterialBatchTest = rawMaterialBatchTest.Where(lr => lr.MaterialId == materialId);

        }
        return new QcDashboardDto
        {
            NumberOfStpRawMaterials = await materialStp.CountAsync(ms => ms.Material.Kind == MaterialKind.Raw),
            NumberOfStpPackingMaterials = await materialStp.CountAsync(ms => ms.Material.Kind == MaterialKind.Package),
            NumberOfStpProducts = await productStp.CountAsync(),
            NumberOfMaterialAnalyticalRawData = await materialAnalyticalRawData.CountAsync(m => m.MaterialStandardTestProcedure.Material.Kind == MaterialKind.Raw),
            NumberOfMaterialAnalyticalPackingData = await materialAnalyticalRawData.CountAsync(m => m.MaterialStandardTestProcedure.Material.Kind == MaterialKind.Package),
            NumberOfBatchTestCountRawMaterials = rawMaterialBatchTest.Count(rm => rm.Material.Kind == MaterialKind.Raw),
            NumberOfBatchTestPendingRawMaterials =
                await rawMaterialBatchTest.CountAsync(rm => rm.Status == BatchStatus.Received), //check this
            NumberOfBatchTestApprovedRawMaterials = await rawMaterialBatchTest.CountAsync(rm => rm.Status == BatchStatus.Approved),
            NumberOfBatchTestRejectedRawMaterials = await rawMaterialBatchTest.CountAsync(rm => rm.Status == BatchStatus.Rejected),
            NumberOfBulkProductAnalyticalRawData = await productAnalyticalRawData.CountAsync(p => p.Stage == TestStage.Bulk),
            NumberOfIntermediateProductAnalyticalRawData = await productAnalyticalRawData.CountAsync(p => p.Stage == TestStage.Intermediate),
            NumberOfFinishedProductAnalyticalRawData = await productAnalyticalRawData.CountAsync(p => p.Stage == TestStage.Finished),
            NumberOfRawMaterialSpecifications = await materialStp.CountAsync(ms => ms.Material.Kind == MaterialKind.Raw),
            NumberOfPackingMaterialSpecifications = await materialStp.CountAsync(ms => ms.Material.Kind == MaterialKind.Package),
            NumberOfIntermediateProductSpecifications = await productStp
                .CountAsync(p => productAnalyticalRawData
                    .Where(ar => ar.Stage == TestStage.Intermediate)
                    .Select(ar => ar.Id)
                    .Contains(p.ProductId)),
            NumberOfBulkProductSpecifications = await productStp.CountAsync(p => productAnalyticalRawData
                .Where(ar => ar.Stage == TestStage.Bulk)
                .Select(ar => ar.Id)
                .Contains(p.ProductId)),

            NumberOfFinishedProductSpecifications = await productStp.CountAsync(
                p => productAnalyticalRawData
                    .Where(ar => ar.Stage == TestStage.Finished)
                    .Select(ar => ar.Id)
                    .Contains(p.ProductId))


            // NumberOfApprovals = await approvals.CountAsync(),
            // NumberOfPendingApprovals = await requisitionApprovals.CountAsync(s => s.Status == ApprovalStatus.Pending) 
            //                            + await billingSheetApprovals.CountAsync(s => s.Status == ApprovalStatus.Pending) 
            //                         + await leaveRequestApprovals.CountAsync(s => s.Status == ApprovalStatus.Pending) 
            //                            + await purchaseOrderApprovals.CountAsync(s => s.Status == ApprovalStatus.Pending)
            //                         + await staffRequisitionApprovals.CountAsync(s => s.Status == ApprovalStatus.Pending) 
            //                            + await purchaseOrderApprovals.CountAsync(s => s.Status == ApprovalStatus.Pending)
            //                         + await responseApprovals.CountAsync(s => s.Status == ApprovalStatus.Pending),
            //
            // NumberOfRejectedApprovals = await requisitionApprovals.CountAsync(s => s.Status == ApprovalStatus.Rejected) 
            //                             + await billingSheetApprovals.CountAsync(s => s.Status == ApprovalStatus.Rejected) 
            //                             + await leaveRequestApprovals.CountAsync(s => s.Status == ApprovalStatus.Rejected) 
            //                             + await purchaseOrderApprovals.CountAsync(s => s.Status == ApprovalStatus.Rejected)
            //                             + await staffRequisitionApprovals.CountAsync(s => s.Status == ApprovalStatus.Rejected) 
            //                             + await purchaseOrderApprovals.CountAsync(s => s.Status == ApprovalStatus.Rejected)
            //                             + await responseApprovals.CountAsync(s => s.Status == ApprovalStatus.Rejected)
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
            NumberOfInProgressStockRequisitions = await stockRequisitions.CountAsync(s => s.Status == RequestStatus.Pending),
            NumberOfCompletedStockRequisitions = await stockRequisitions.CountAsync(s => s.Status == RequestStatus.Completed),
            NumberOfIncomingStockTransfers = await incomingStockTransfers.CountAsync(),
            NumberOfIncomingPendingStockTransfers = await incomingStockTransfers.CountAsync(s => s.Status == StockTransferStatus.InProgress),
            NumberOfIncomingCompletedStockTransfers = await incomingStockTransfers.CountAsync(s => s.Status == StockTransferStatus.Issued),
            NumberOfShipments = await shipments.CountAsync(s => s.Type == DocType.Shipment),
            NumberOfInTransitShipments = await shipments.CountAsync(s => s.Type == DocType.Shipment && s.Status == ShipmentStatus.InTransit),
            NumberOfArrivedShipments = await shipments.CountAsync(s => s.Type == DocType.Shipment && s.Status == ShipmentStatus.Arrived),
            NumberOfClearedShipments = await shipments.CountAsync(s => s.Type == DocType.Shipment && s.Status == ShipmentStatus.Cleared),
        };
    }

    public async Task<Result<List<MaterialBatchReservedQuantityReportDto>>> GetReservedMaterialBatchesForDepartment(ReportFilter filter, Guid departmentId)
    {
        var department = await context.Departments.FirstOrDefaultAsync(d => d.Id == departmentId);
        if (department is null) return Error.NotFound("Department", "Department not found.");

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
            .Include(drm => drm.WarehouseArrivalLocation)
            .Include(drm => drm.MaterialItemDistributions)
            .Include(sr => sr.CheckLists)
            .ThenInclude(cl => cl.MaterialBatches)
            .Where(drm => drm.Status == DistributedRequisitionMaterialStatus.Pending)
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

    public async Task<Result<LogisticsReportDto>> GetLogisticsReport(ReportFilter filter)
    {
        var shipmentInvoices = context.ShipmentInvoices.AsQueryable();

        var shipments = context.ShipmentDocuments.AsQueryable();

        var billingSheets = context.BillingSheets
            .AsSplitQuery()
            .Include(s => s.Invoice)
            .AsQueryable();

        if (filter.StartDate.HasValue)
        {
            var start = filter.StartDate.Value;
            shipmentInvoices = shipmentInvoices.Where(r => r.CreatedAt >= start);
            shipments = shipments.Where(r => r.CreatedAt >= start);
            billingSheets = billingSheets.Where(r => r.CreatedAt >= start);

        }

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.AddDays(1);
            shipmentInvoices = shipmentInvoices.Where(r => r.CreatedAt < end);
            shipments = shipments.Where(r => r.CreatedAt < end);
            billingSheets = billingSheets.Where(r => r.CreatedAt < end);
        }

        return new LogisticsReportDto
        {
            NumberOfInvoices = await shipmentInvoices.CountAsync(),
            NumberOfPaidInvoices = await shipmentInvoices.CountAsync(s => s.PaidAt.HasValue),
            NumberOfUnpaidInvoices = await shipmentInvoices.CountAsync(s => !s.PaidAt.HasValue),
            NumberOfShipments = await shipments.CountAsync(s => s.Type == DocType.Shipment),
            NumberOfNewShipments = await shipments.CountAsync(s => s.Type == DocType.Shipment && s.Status == ShipmentStatus.New),
            NumberOfInTransitShipments = await shipments.CountAsync(s => s.Type == DocType.Shipment && s.Status == ShipmentStatus.InTransit),
            NumberOfArrivedShipments = await shipments.CountAsync(s => s.Type == DocType.Shipment && s.Status == ShipmentStatus.Arrived),
            NumberOfClearedShipments = await shipments.CountAsync(s => s.Type == DocType.Shipment && s.Status == ShipmentStatus.Cleared),
            NumberOfBillingSheets = await billingSheets.CountAsync(),
            NumberOfPaidBillingSheets = await billingSheets.CountAsync(b => b.Status == BillingSheetStatus.Paid || b.Invoice.PaidAt.HasValue),
            NumberOfPendingBillingSheets = await billingSheets.CountAsync(b => b.Status == BillingSheetStatus.Pending),
            NumberOfWaybills = await shipments.CountAsync(s => s.Type == DocType.Waybill),
            NumberOfNewWaybills = await shipments.CountAsync(s => s.Type == DocType.Waybill && s.Status == ShipmentStatus.New),
            NumberOfInTransitWaybills = await shipments.CountAsync(s => s.Type == DocType.Waybill && s.Status == ShipmentStatus.InTransit),
            NumberOfArrivedWaybills = await shipments.CountAsync(s => s.Type == DocType.Waybill && s.Status == ShipmentStatus.Arrived),
            NumberOfClearedWaybills = await shipments.CountAsync(s => s.Type == DocType.Waybill && s.Status == ShipmentStatus.Cleared),
        };
    }
    public async Task<Result<List<FinishedGoodsTransferSummaryReportDto>>>
    GetFinishedGoodsTransferSummaryReport(ReportFilter filter, Guid? productId = null, Guid? warehouseId = null)
    {
        var query = context.FinishedGoodsTransferNotes
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(f => f.ProductPacking)
                .ThenInclude(pp => pp.Product)
            .Include(f => f.UoM)
            .Include(f => f.FromWarehouse)
            .Include(f => f.ToWarehouse)
            .Include(f => f.BatchManufacturingRecord)
            .Include(f => f.Approvals)
            .Where(f => f.IsApproved && f.TotalQuantity > 0 && !f.DeletedAt.HasValue);

        if (filter.StartDate.HasValue)
            query = query.Where(f => f.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(f => f.CreatedAt < filter.EndDate.Value.AddDays(1));

        if (productId.HasValue)
            query = query.Where(f => f.ProductPacking != null && f.ProductPacking.ProductId == productId.Value);

        if (warehouseId.HasValue)
            query = query.Where(f => f.ToWarehouseId == warehouseId.Value);

        var rawData = await query
            .Select(f => new
            {
                ProductName = f.ProductPacking != null && f.ProductPacking.Product != null
                    ? f.ProductPacking.Product.Name
                    : null,

                ProductCode = f.ProductPacking != null && f.ProductPacking.Product != null
                    ? f.ProductPacking.Product.Code
                    : null,

                BatchNumber = f.BatchManufacturingRecord != null
                    ? f.BatchManufacturingRecord.BatchNumber
                    : "No Batch",

                TotalQuantity = f.TotalQuantity,
                UomName = f.UoM != null ? f.UoM.Name : "N/A",

                ProductionDepartment = f.FromWarehouse != null
                    ? f.FromWarehouse.Name
                    : "Unknown Production Floor",

                DestinationWarehouse = f.ToWarehouse != null
                    ? f.ToWarehouse.Name
                    : "Unknown Warehouse",

                TransferDate = f.CreatedAt,

                AcceptedDate = f.Approvals.Any()
                    ? f.Approvals.Max(a => a.CreatedAt)
                    : (DateTime?)null
            })
            .Where(x => x.ProductName != null)
            .ToListAsync();

        if (rawData.Count == 0)
        {
            return Result.Success(
                new List<FinishedGoodsTransferSummaryReportDto>());
        }

        var groupedData = rawData
            .GroupBy(x => new
            {
                x.ProductName,
                x.ProductCode,
                x.UomName,
                x.ProductionDepartment,
                x.DestinationWarehouse
            })
            .Select(g => new
            {
                Key = g.Key,
                NumberOfBatches = g.Where(x => !string.IsNullOrEmpty(x.BatchNumber) && x.BatchNumber != "No Batch")
                    .Select(x => x.BatchNumber)
                    .Distinct()
                    .Count(),
                TotalQuantity = g.Sum(x => x.TotalQuantity),
                EarliestTransferDate = g.Min(x => x.TransferDate),
                LatestAcceptedDate = g.Max(x => x.AcceptedDate)
            })
            .OrderBy(g => g.Key.ProductName)
            .ThenBy(g => g.Key.ProductCode)
            .ToList();

        // Map to DTO
        var result = groupedData
            .Select((g, index) => new FinishedGoodsTransferSummaryReportDto
            {
                No = index + 1,
                ProductName = g.Key.ProductName,
                ProductCode = g.Key.ProductCode,
                NumberOfBatches = g.NumberOfBatches,
                TotalQuantity = g.TotalQuantity,
                UomName = g.Key.UomName,
                ProductionDepartment = g.Key.ProductionDepartment,
                DestinationWarehouse = g.Key.DestinationWarehouse,
                TransferDate = g.EarliestTransferDate,
                AcceptedDate = g.LatestAcceptedDate
            })
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<FinishedGoodsTransferDetailedReportDto>>>
    GetFinishedGoodsTransferDetailedReport(ReportFilter filter, Guid? productId = null, Guid? warehouseId = null)
    {
        var query = context.FinishedGoodsTransferNotes
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(f => f.ProductPacking)
                .ThenInclude(pp => pp.Product)
            .Include(f => f.UoM)
            .Include(f => f.FromWarehouse)
            .Include(f => f.ToWarehouse)
            .Include(f => f.BatchManufacturingRecord)
            .Include(f => f.Approvals)
            .Where(f => f.IsApproved && f.TotalQuantity > 0 && !f.DeletedAt.HasValue);

        if (filter.StartDate.HasValue)
            query = query.Where(f => f.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(f => f.CreatedAt < filter.EndDate.Value.AddDays(1));

        if (productId.HasValue)
            query = query.Where(f => f.ProductPacking != null && f.ProductPacking.ProductId == productId.Value);

        if (warehouseId.HasValue)
            query = query.Where(f => f.ToWarehouseId == warehouseId.Value);

        var rawData = await query
            .Select(f => new
            {
                ProductName = f.ProductPacking != null && f.ProductPacking.Product != null
                    ? f.ProductPacking.Product.Name
                    : null,

                ProductCode = f.ProductPacking != null && f.ProductPacking.Product != null
                    ? f.ProductPacking.Product.Code
                    : null,

                BatchNumber = f.BatchManufacturingRecord != null
                    ? f.BatchManufacturingRecord.BatchNumber
                    : "No Batch",

                QuantityTransferred = f.QuantityReceived > 0 ? f.QuantityReceived : f.TotalQuantity,

                ManufacturingDate = f.BatchManufacturingRecord != null
                    ? f.BatchManufacturingRecord.ManufacturingDate
                    : (DateTime?)null,

                ExpiryDate = f.BatchManufacturingRecord != null
                    ? f.BatchManufacturingRecord.ExpiryDate
                    : (DateTime?)null,

                PackingStyle = f.ProductPacking != null
                    ? f.ProductPacking.Name
                    : "N/A",

                UomName = f.UoM != null ? f.UoM.Name : "N/A",

                ProductionDepartment = f.FromWarehouse != null
                    ? f.FromWarehouse.Name
                    : "Unknown Production Floor",

                DestinationWarehouse = f.ToWarehouse != null
                    ? f.ToWarehouse.Name
                    : "Unknown Warehouse",

                TransferDate = f.CreatedAt,

                AcceptedDate = f.AcceptedAt


            })
            .Where(x => x.ProductName != null)
            .OrderBy(x => x.ProductName)
            .ThenBy(x => x.TransferDate)
            .ToListAsync();

        if (rawData.Count == 0)
        {
            return Result.Success(
                new List<FinishedGoodsTransferDetailedReportDto>());
        }

        var result = rawData
            .Select((item, index) => new FinishedGoodsTransferDetailedReportDto
            {
                No = index + 1,
                ProductName = item.ProductName,
                ProductCode = item.ProductCode,
                BatchNumber = item.BatchNumber,
                QuantityTransferred = item.QuantityTransferred,
                ManufacturingDate = item.ManufacturingDate,
                ExpiryDate = item.ExpiryDate,
                PackingStyle = item.PackingStyle,
                UomName = item.UomName,
                ProductionDepartment = item.ProductionDepartment,
                DestinationWarehouse = item.DestinationWarehouse,
                TransferDate = item.TransferDate,
                AcceptedDate = item.AcceptedDate,
            })
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<ProductStockSummaryReportDto>>> GetProductStockSummaryReport(
     Guid? productId = null,
     Guid? warehouseId = null,
     Guid? departmentId = null)
    {
        var query = context.FinishedGoodsTransferNotes
            .AsNoTracking()
            .IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(f => f.BatchManufacturingRecord)
                .ThenInclude(b => b.ProductionScheduleProduct)
                .ThenInclude(psp => psp.Product)
                .ThenInclude(p => p.Department)
            .Include(f => f.ProductPacking)
                .ThenInclude(pp => pp.Product)
                .ThenInclude(p => p.Department)
            .Include(f => f.UoM)
            .Include(f => f.ToWarehouse)
            .Where(f => f.IsApproved && !f.DeletedAt.HasValue)
            .AsEnumerable()
            .Where(f => f.RemainingQuantity > 0);

        if (productId.HasValue)
        {
            query = query.Where(f =>
                f.BatchManufacturingRecord?.ProductionScheduleProduct?.ProductId == productId.Value ||
                f.ProductPacking?.ProductId == productId.Value);
        }

        if (warehouseId.HasValue)
        {
            query = query.Where(f => f.ToWarehouseId == warehouseId.Value);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(f =>
                f.BatchManufacturingRecord?.ProductionScheduleProduct?.Product?.DepartmentId == departmentId.Value ||
                f.ProductPacking?.Product?.DepartmentId == departmentId.Value);
        }

        var rawData = query
            .Select(f => new
            {
                Product = f.BatchManufacturingRecord?.ProductionScheduleProduct?.Product
                          ?? f.ProductPacking?.Product,
                Warehouse = f.ToWarehouse?.Name ?? "Unknown Warehouse",
                f.BatchManufacturingRecord?.BatchNumber,
                CurrentStockQuantity = f.RemainingQuantity,
                UomName = f.UoM?.Name ?? "N/A"
            })
            .Select(x => new
            {
                ProductName = x.Product?.Name,
                ProductCode = x.Product?.Code,
                ProductionDepartment = x.Product?.Department?.Name ?? "Unknown Department",
                x.Warehouse,
                x.BatchNumber,
                x.CurrentStockQuantity,
                x.UomName
            })
            .Where(x => x.ProductName != null && x.ProductCode != null)
            .ToList();

        if (rawData.Count == 0)
        {
            return Result.Success(new List<ProductStockSummaryReportDto>());
        }

        var groupedData = rawData
            .GroupBy(x => new
            {
                x.ProductName,
                x.ProductCode,
                x.Warehouse,
                x.ProductionDepartment,
                x.UomName
            })
            .Select(g => new
            {
                Key = g.Key,
                NumberOfBatches = g.Where(x => !string.IsNullOrEmpty(x.BatchNumber))
                                   .Select(x => x.BatchNumber)
                                   .Distinct()
                                   .Count(),
                TotalQuantity = g.Sum(x => x.CurrentStockQuantity)
            })
            .OrderBy(g => g.Key.ProductName)
            .ThenBy(g => g.Key.Warehouse)
            .ThenBy(g => g.Key.ProductionDepartment)
            .ToList();

        var result = groupedData
            .Select((g, index) => new ProductStockSummaryReportDto
            {
                No = index + 1,
                ProductName = g.Key.ProductName,
                ProductCode = g.Key.ProductCode,
                Warehouse = g.Key.Warehouse,
                ProductionDepartment = g.Key.ProductionDepartment,
                NumberOfBatches = g.NumberOfBatches,
                TotalQuantity = g.TotalQuantity,
                UomName = g.Key.UomName
            })
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<ProductStockDetailedReportDto>>> GetProductStockDetailedReport(
     Guid? productId = null,
     Guid? warehouseId = null,
     Guid? departmentId = null,
     string batchNumber = null,
     DateTime? expiryDateFrom = null,
     DateTime? expiryDateTo = null)
    {
        var query = context.FinishedGoodsTransferNotes
            .AsNoTracking()
            .IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(f => f.BatchManufacturingRecord)
                .ThenInclude(b => b.ProductionScheduleProduct)
                .ThenInclude(psp => psp.Product)
                .ThenInclude(p => p.Department)
            .Include(f => f.ProductPacking)
                .ThenInclude(pp => pp.Product)
                .ThenInclude(p => p.Department)
            .Include(f => f.UoM)
            .Include(f => f.ToWarehouse)
            .Where(f => f.IsApproved && !f.DeletedAt.HasValue)
            .AsEnumerable()
            .Where(f => f.RemainingQuantity > 0);

        if (productId.HasValue)
        {
            query = query.Where(f =>
                f.BatchManufacturingRecord?.ProductionScheduleProduct?.ProductId == productId.Value ||
                f.ProductPacking?.ProductId == productId.Value);
        }

        if (warehouseId.HasValue)
        {
            query = query.Where(f => f.ToWarehouseId == warehouseId.Value);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(f =>
                f.BatchManufacturingRecord?.ProductionScheduleProduct?.Product?.DepartmentId == departmentId.Value ||
                f.ProductPacking?.Product?.DepartmentId == departmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(batchNumber))
        {
            query = query.Where(f => f.BatchManufacturingRecord?.BatchNumber.Contains(batchNumber) == true);
        }

        if (expiryDateFrom.HasValue)
        {
            query = query.Where(f => f.BatchManufacturingRecord?.ExpiryDate >= expiryDateFrom.Value);
        }

        if (expiryDateTo.HasValue)
        {
            query = query.Where(f => f.BatchManufacturingRecord?.ExpiryDate <= expiryDateTo.Value);
        }

        var rawData = query
            .Select(f => new
            {
                Product = f.BatchManufacturingRecord?.ProductionScheduleProduct?.Product
                          ?? f.ProductPacking?.Product,
                BatchNumber = f.BatchManufacturingRecord?.BatchNumber ?? "No Batch",
                f.BatchManufacturingRecord?.ManufacturingDate,
                f.BatchManufacturingRecord?.ExpiryDate,
                TotalQuantity = f.RemainingQuantity,
                UomName = f.UoM?.Name ?? "N/A",
                Warehouse = f.ToWarehouse?.Name ?? "Unknown Warehouse"
            })
            .Select(x => new
            {
                ProductName = x.Product?.Name,
                ProductCode = x.Product?.Code,
                ProductionDepartment = x.Product?.Department?.Name ?? "Unknown Department",
                x.BatchNumber,
                x.ManufacturingDate,
                x.ExpiryDate,
                x.TotalQuantity,
                x.UomName,
                x.Warehouse
            })
            .Where(x => x.ProductName != null && x.ProductCode != null)
            .OrderBy(x => x.ProductName)
            .ThenBy(x => x.BatchNumber)
            .ThenBy(x => x.ExpiryDate)
            .ToList();

        if (rawData.Count == 0)
        {
            return Result.Success(new List<ProductStockDetailedReportDto>());
        }

        var result = rawData
            .Select((item, index) => new ProductStockDetailedReportDto
            {
                No = index + 1,
                ProductName = item.ProductName,
                ProductCode = item.ProductCode,
                BatchNumber = item.BatchNumber,
                ManufacturingDate = item.ManufacturingDate,
                ExpiryDate = item.ExpiryDate,
                TotalQuantity = item.TotalQuantity,
                UomName = item.UomName,
                ProductionDepartment = item.ProductionDepartment,
                Warehouse = item.Warehouse
            })
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<ItemDto>>> GetItemsPerStoreType(
        Store? store,
        InventoryClassification? inventoryClassification,
        Guid? itemId,
        Guid? categoryId)
    {
        var query = context.Items
            .AsQueryable();

        if (store.HasValue)
        {
            query = query.Where(i => i.Store == store.Value);
        }

        if (inventoryClassification.HasValue)
        {
            query = query.Where(i => i.Classification == inventoryClassification.Value);
        }

        if (itemId.HasValue)
        {
            query = query.Where(i => i.Id == itemId.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(i => i.ItemCategoryId == categoryId.Value);
        }

        var items = await query
            .AsNoTracking()
            .Select(i => new ItemDto
            {
                Id = i.Id,
                Name = i.Name,
                Store = i.Store,
                Classification = i.Classification,

                ItemCategory = i.ItemCategory == null
                    ? null
                    : new ItemCategoryDto
                    {
                        Id = i.ItemCategory.Id,
                        Name = i.ItemCategory.Name
                    },

                UnitOfMeasure = i.UnitOfMeasure == null
                    ? null
                    : new UnitOfMeasureDto
                    {
                        Id = i.UnitOfMeasure.Id,
                        Name = i.UnitOfMeasure.Name
                    }
            })
            .ToListAsync();

        return Result.Success(items);
    }

    /// <summary>
    /// Provides a stock quantity overview per store type and item,
    /// showing total item quantities across all locations.
    /// </summary>
    public async Task<Result<List<StoreItemStockSummaryDto>>> GetStockSummaryPerStoreType()
    {
        var raw = await context.Items
            .AsNoTracking()
            .GroupBy(i => new
            {
                i.Store,
                i.Id,
                i.Name,
                i.Code,
                CategoryName = i.ItemCategory.Name,
                UomName = i.UnitOfMeasure.Name
            })
            .Select(g => new
            {
                g.Key.Store,
                g.Key.Id,
                g.Key.Name,
                g.Key.Code,
                g.Key.CategoryName,
                g.Key.UomName,
                TotalQuantity = g.Sum(x => x.AvailableQuantity)
            })
            .OrderBy(r => r.Store)
            .ThenBy(r => r.Name)
            .ToListAsync();

        var result = raw.Select((r, index) => new StoreItemStockSummaryDto
        {
            No = index + 1,
            Store = r.Store,
            ItemName = r.Name,
            ItemCode = r.Code,
            Category = r.CategoryName,
            TotalQuantity = r.TotalQuantity,
            UnitOfMeasure = r.UomName
        }).ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<VendorStoreItemStockSummaryDto>>>
        GetVendorItemMapping(
            Store? store,
            Guid? vendorId,
            Guid? itemId,
            Guid? categoryId,
            InventoryClassification? classification)
    {
        var query = context.VendorItems
            .AsNoTracking()
            .AsQueryable();

        if (store.HasValue)
            query = query.Where(v => v.Item.Store == store.Value);

        if (vendorId.HasValue)
            query = query.Where(v => v.VendorId == vendorId.Value);

        if (itemId.HasValue)
            query = query.Where(v => v.ItemId == itemId.Value);

        if (categoryId.HasValue)
            query = query.Where(v => v.Item.ItemCategoryId == categoryId.Value);

        if (classification.HasValue)
            query = query.Where(v => v.Item.Classification == classification.Value);

        var raw = await query
            .Select(v => new
            {
                v.Item.Store,
                ItemName = v.Item.Name,
                ItemCode = v.Item.Code,
                Category = v.Item.ItemCategory.Name,
                v.Item.Classification,
                UnitOfMeasure = v.Item.UnitOfMeasure.Name,
                VendorName = v.Vendor.Name
            })
            .OrderBy(r => r.Store)
            .ThenBy(r => r.ItemName)
            .ThenBy(r => r.ItemCode)
            .ThenBy(r => r.VendorName)
            .ToListAsync();

        var result = raw.Select((r, index) => new VendorStoreItemStockSummaryDto
        {
            No = index + 1,

            Store = r.Store,
            ItemName = r.ItemName,
            ItemCode = r.ItemCode,
            Category = r.Category,
            InventoryClassification = r.Classification,
            UnitOfMeasure = r.UnitOfMeasure,
            VendorName = r.VendorName
        }).ToList();

        return Result.Success(result);
    }
    public async Task<Result<DashboardKpiReportDto>> GetDashboardKpiReport(DashboardFilterDto filter)
    {

        DateTime? startDate = null;
        DateTime? endDate = null;

        switch (filter.DateFilter)
        {
            case DateFilterType.OneWeek:
                startDate = DateTime.UtcNow.AddDays(-7);
                endDate = DateTime.UtcNow;
                break;
            case DateFilterType.Custom:
                startDate = filter.CustomStartDate;
                endDate = filter.CustomEndDate;
                break;
            case DateFilterType.AllTime:
            default:

                break;
        }

        var productQuery = context.Products
        .AsNoTracking()
        .IgnoreQueryFilters()
        .Where(p => !p.DeletedAt.HasValue);

        if (filter.DepartmentId.HasValue)
        {
            productQuery = productQuery.Where(p => p.DepartmentId == filter.DepartmentId.Value);
        }
        if (filter.ProductId.HasValue)
        {
            productQuery = productQuery.Where(p => p.Id == filter.ProductId.Value);
        }


        if (startDate.HasValue)
            productQuery = productQuery.Where(p => p.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            productQuery = productQuery.Where(p => p.CreatedAt <= endDate.Value);

        var betaProductCount = await productQuery
            .Where(p => p.Division == Division.BetaLactam)
            .CountAsync();

        var nonBetaProductCount = await productQuery
            .Where(p => p.Division == Division.NonBetaLactam)
            .CountAsync();

        var totalProductCount = betaProductCount + nonBetaProductCount;


        var customerQuery = context.Customers
            .AsNoTracking()
            .Where(c => !c.DeletedAt.HasValue);


        if (startDate.HasValue)
            customerQuery = customerQuery.Where(c => c.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            customerQuery = customerQuery.Where(c => c.CreatedAt <= endDate.Value);

        var totalCustomers = await customerQuery.CountAsync();


        var productionOrderQuery = context.ProductionOrders
            .AsNoTracking()
            .Include(po => po.Products)
            .Where(po => !po.DeletedAt.HasValue);

        if (filter.DepartmentId.HasValue)
        {
            productionOrderQuery = productionOrderQuery
                .Where(po => po.Products.Any(p => p.Product != null &&
                                                 p.Product.DepartmentId == filter.DepartmentId.Value));
        }
        if (filter.ProductId.HasValue)
        {
            productionOrderQuery = productionOrderQuery
                .Where(po => po.Products.Any(p => p.ProductId == filter.ProductId.Value));
        }


        if (startDate.HasValue)
            productionOrderQuery = productionOrderQuery.Where(po => po.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            productionOrderQuery = productionOrderQuery.Where(po => po.CreatedAt <= endDate.Value);

        var pendingOrders = await productionOrderQuery
            .Where(po => po.Status == ProductionOrderStatus.Pending)
            .CountAsync();

        var partialPackingReadyOrders = await productionOrderQuery
            .Where(po => po.Status == ProductionOrderStatus.PartialPackingReady)
            .CountAsync();

        var fullPackingReadyOrders = await productionOrderQuery
            .Where(po => po.Status == ProductionOrderStatus.FullPackingReady)
            .CountAsync();

        var totalProductionOrders = pendingOrders + partialPackingReadyOrders + fullPackingReadyOrders;


        var fgtnQuery = context.FinishedGoodsTransferNotes
            .IgnoreQueryFilters()
            .Include(f => f.Approvals)
            .Include(f => f.ProductPacking)
                .ThenInclude(pp => pp.Product)
            .Include(f => f.BatchManufacturingRecord)
                .ThenInclude(bmr => bmr.ProductionScheduleProduct)
                .ThenInclude(psp => psp.Product)
            .Where(f => !f.DeletedAt.HasValue);

        if (filter.DepartmentId.HasValue)
        {
            fgtnQuery = fgtnQuery.Where(f =>
                (f.ProductPacking != null &&
                 f.ProductPacking.Product != null &&
                 f.ProductPacking.Product.DepartmentId == filter.DepartmentId.Value) ||
                (f.BatchManufacturingRecord != null &&
                 f.BatchManufacturingRecord.ProductionScheduleProduct != null &&
                 f.BatchManufacturingRecord.ProductionScheduleProduct.Product != null &&
                 f.BatchManufacturingRecord.ProductionScheduleProduct.Product.DepartmentId == filter.DepartmentId.Value));
        }
        if (filter.ProductId.HasValue)
        {
            fgtnQuery = fgtnQuery.Where(f =>
                (f.ProductPacking != null && f.ProductPacking.ProductId == filter.ProductId.Value) ||
                (f.BatchManufacturingRecord != null &&
                 f.BatchManufacturingRecord.ProductionScheduleProduct != null &&
                 f.BatchManufacturingRecord.ProductionScheduleProduct.ProductId == filter.ProductId.Value));
        }


        if (filter.MaterialId.HasValue)
        {
            fgtnQuery = fgtnQuery.Where(f =>
          f.BatchManufacturingRecord != null &&
          f.BatchManufacturingRecord.ProductionScheduleProduct != null &&
          f.BatchManufacturingRecord.ProductionScheduleProduct.Product != null &&
          f.BatchManufacturingRecord.ProductionScheduleProduct.Product.BillOfMaterials
              .Any(bom => bom.BillOfMaterialId == filter.MaterialId.Value)
      );

        }


        if (startDate.HasValue)
            fgtnQuery = fgtnQuery.Where(f => f.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            fgtnQuery = fgtnQuery.Where(f => f.CreatedAt <= endDate.Value);


        var pendingFgtn = await fgtnQuery
            .Where(f => !f.IsApproved || !f.Approvals.Any())
            .CountAsync();


        var acceptedFgtn = await fgtnQuery
            .Where(f => f.IsApproved && f.Approvals.Any())
            .CountAsync();

        var totalFgtn = pendingFgtn + acceptedFgtn;


        var dashboardKpi = new DashboardKpiReportDto
        {
            ProductCount = new ProductCountKpiDto
            {
                BetaProducts = betaProductCount,
                NonBetaProducts = nonBetaProductCount,
                TotalProducts = totalProductCount
            },
            TotalCustomers = totalCustomers,
            ProductionOrders = new ProductionOrderKpiDto
            {
                PendingProductionOrders = pendingOrders,
                PartialPackingReady = partialPackingReadyOrders,
                FullPackingReady = fullPackingReadyOrders,
                TotalProductionOrders = totalProductionOrders
            },
            FinishedGoodsTransferNotes = new FgtnKpiDto
            {
                PendingTransferNote = pendingFgtn,
                AcceptedTransferNote = acceptedFgtn,
                TotalFgtnTransferNotes = totalFgtn
            }
        };

        return Result.Success(dashboardKpi);
    }

    public async Task<Result<List<SupplierMaterialReportDto>>>
        GetSupplierMaterialAReport(SupplierMaterialFilters filters)
    {
        var baseQuery = context.SupplierManufacturers
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(sm => sm.UoM)
            .Include(sm => sm.Manufacturer)
            .Where(sm => sm.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(filters.MaterialName))
            baseQuery = baseQuery.Where(sm =>
                sm.Material != null &&
                sm.Material.Name.Contains(filters.MaterialName));

        if (filters.MaterialType.HasValue)
            baseQuery = baseQuery.Where(sm =>
                sm.Material != null &&
                sm.Material.Kind == filters.MaterialType.Value);

        if (!string.IsNullOrWhiteSpace(filters.SupplierName))
            baseQuery = baseQuery.Where(sm =>
                sm.Supplier != null &&
                sm.Supplier.Name.Contains(filters.SupplierName));

        if (!string.IsNullOrWhiteSpace(filters.ManufacturerName))
            baseQuery = baseQuery.Where(sm =>
                sm.Manufacturer != null &&
                sm.Manufacturer.Name.Contains(filters.ManufacturerName));

        if (filters.SupplierId.HasValue)
            baseQuery = baseQuery.Where(sm => sm.SupplierId == filters.SupplierId.Value);

        if (filters.ManufacturerId.HasValue)
            baseQuery = baseQuery.Where(sm => sm.ManufacturerId == filters.ManufacturerId.Value);

        if (filters.ValidityDateFrom.HasValue)
            baseQuery = baseQuery.Where(sm =>
                sm.Manufacturer.ValidityDate >= filters.ValidityDateFrom.Value);

        if (filters.ValidityDateTo.HasValue)
            baseQuery = baseQuery.Where(sm =>
                sm.Manufacturer.ValidityDate <= filters.ValidityDateTo.Value);

        var groups = await baseQuery
            .GroupBy(sm => new
            {
                sm.SupplierId,
                sm.ManufacturerId,
                sm.UoMId
            })
            .Select(g => new
            {
                g.Key.SupplierId,
                g.Key.ManufacturerId,
                g.Key.UoMId,

                Supplier = g.Select(x => x.Supplier).FirstOrDefault(),
                Manufacturer = g.Select(x => x.Manufacturer).FirstOrDefault(),
                Uom = g.Select(x => x.UoM).FirstOrDefault(),

                MaterialIds = g.Select(x => x.MaterialId)
                               .Where(id => id != null)
                               .Distinct()
                               .ToList()
            })
            .ToListAsync();

        if (groups.Count == 0)
            return Result.Success(new List<SupplierMaterialReportDto>());

        var manufacturerIds = groups
            .Select(g => g.ManufacturerId)
            .Distinct()
            .ToList();

        var manufacturerMaterials = await context.ManufacturerMaterials
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(mm =>
                mm.DeletedAt == null &&
                manufacturerIds.Contains(mm.ManufacturerId))
            .ToListAsync();

        var materialLookup = manufacturerMaterials
            .GroupBy(mm => mm.ManufacturerId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = groups.Select(g =>
        {
            var materialIdSet = g.MaterialIds.ToHashSet();

            materialLookup.TryGetValue(g.ManufacturerId, out var materialsForManufacturer);

            return new SupplierMaterialReportDto
            {
                Supplier = mapper.Map<SupplierListDto>(g.Supplier),
                Manufacturers = mapper.Map<ManufacturerListDto>(g.Manufacturer),
                Uom = mapper.Map<UnitOfMeasureDto>(g.Uom),
                Materials = mapper.Map<List<ManufacturerMaterialDto>>(
                    (object)materialsForManufacturer?
                        .Where(mm => materialIdSet.Contains(mm.MaterialId))
                        .ToList()
                    ?? new List<ManufacturerMaterialDto>()
                )
            };
        }).ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<VendorItemStoreSummaryDto>>>
        GetVendorItemMappingPerStoreTypeSummary(
            Guid? itemId,
            Guid? categoryId,
            InventoryClassification? classification,
            Store? store)
    {
        var query = context.VendorItems
            .AsNoTracking()
            .AsQueryable();


        if (itemId.HasValue)
        {
            query = query.Where(v => v.ItemId == itemId.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(v => v.Item.ItemCategoryId == categoryId.Value);
        }

        if (classification.HasValue)
        {
            query = query.Where(v => v.Item.Classification == classification.Value);
        }

        if (store.HasValue)
        {
            query = query.Where(v => v.Item.Store == store.Value);
        }


        var raw = await query
            .GroupBy(v => new
            {
                v.Item.Store,
                v.Item.Id,
                v.Item.Name,
                v.Item.Code,
                Category = v.Item.ItemCategory.Name,
                v.Item.Classification
            })
            .Select(g => new
            {
                g.Key.Store,
                g.Key.Name,
                g.Key.Code,
                g.Key.Category,
                g.Key.Classification,
                VendorCount = g.Select(x => x.VendorId).Distinct().Count()
            })
            .OrderBy(r => r.Store)
            .ThenBy(r => r.Name)
            .ToListAsync();

        var result = raw.Select((r, index) => new VendorItemStoreSummaryDto
        {
            No = index + 1,
            Store = r.Store,
            ItemName = r.Name,
            ItemCode = r.Code,
            Category = r.Category,
            Classification = r.Classification,
            VendorCount = r.VendorCount
        }).ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<ShipmentReportDto>>> GetShipmentReport(ShipmentReportFilter filter)
    {
        var baseQuery = context.BillingSheets
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(bs => bs.DeletedAt == null);
        if (filter.StartDate.HasValue)
        {
            baseQuery = baseQuery.Where(bs => bs.CreatedAt >= filter.StartDate.Value);
            ;
        }

        if (filter.EndDate.HasValue)
        {
            baseQuery = baseQuery.Where(bs => bs.CreatedAt <= filter.EndDate.Value);
        }
        if (filter.SupplierIds != null && filter.SupplierIds.Any())
        {
            baseQuery = baseQuery.Where(bs =>
                bs.SupplierId.HasValue &&
                filter.SupplierIds.Contains(bs.SupplierId.Value));
        }
        if (filter.Statuses != null && filter.Statuses.Any())
        {
            baseQuery = baseQuery.Where(bs => filter.Statuses.Contains(bs.Status.ToString()));
        }
        var groups = await baseQuery
            .GroupBy(bs => new
            {
                bs.Id,
                bs.SupplierId,
                bs.InvoiceId
            })
            .Select(g => new
            {
                BillingSheetId = g.Key.Id,
                g.Key.SupplierId,
                g.Key.InvoiceId,
                BillingSheet = g.First(),
                Supplier = g.Select(x => x.Supplier).FirstOrDefault(),
                Invoice = g.Select(x => x.Invoice).FirstOrDefault()
            })
            .ToListAsync();

        if (groups.Count == 0)
            return Result.Success(new List<ShipmentReportDto>());

        var supplierIds = groups
            .Select(g => g.SupplierId)
            .Where(id => id.HasValue)
            .Select(id => id.Value)
            .Distinct()
            .ToList();

        // Include TermsOfPayment in the query
        var purchaseOrders = await context.PurchaseOrders
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(po => po.TermsOfPayment)
            .Where(po => po.DeletedAt == null && supplierIds.Contains(po.SupplierId))
            .Select(po => new
            {
                po.Id,
                po.SupplierId,
                po.CreatedAt,
                TermsOfPaymentName = po.TermsOfPayment != null ? po.TermsOfPayment.Name : null,
                po.TotalCifValue
            })
            .ToListAsync();

        var purchaseOrderIds = purchaseOrders.Select(po => po.Id).ToList();

        var purchaseOrderItems = await context.PurchaseOrderItems
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(poi => poi.DeletedAt == null && purchaseOrderIds.Contains(poi.PurchaseOrderId))
            .Select(poi => new { poi.PurchaseOrderId, poi.MaterialId })
            .ToListAsync();

        var materialIds = purchaseOrderItems
            .Select(poi => poi.MaterialId)
            .Distinct()
            .ToList();

        var materials = await context.Materials
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(m => materialIds.Contains(m.Id))
            .Select(m => new { m.Id, m.Name })
            .ToListAsync();

        var materialLookup = materials.ToDictionary(m => m.Id, m => m.Name);
        var poiLookup = purchaseOrderItems
            .GroupBy(poi => poi.PurchaseOrderId)
            .ToDictionary(g => g.Key, g => g.Select(poi => poi.MaterialId).ToList());
        var poLookup = purchaseOrders
            .GroupBy(po => po.SupplierId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = groups.Select((g, index) =>
        {
            List<string> materialNames = new List<string>();
            string transactionType = "N/A";
            decimal transactionAmount = 0;

            if (g.SupplierId.HasValue && poLookup.TryGetValue(g.SupplierId.Value, out var supplierPOs))
            {
                var recentPO = supplierPOs.OrderByDescending(po => po.CreatedAt).FirstOrDefault();
                if (recentPO != null)
                {
                    transactionAmount = recentPO.TotalCifValue;
                    transactionType = recentPO.TermsOfPaymentName;

                    if (poiLookup.TryGetValue(recentPO.Id, out var matIds))
                    {
                        materialNames = matIds
                            .Select(id => materialLookup.TryGetValue(id, out var name) ? name : null)
                            .Where(n => n != null)
                            .ToList();
                    }
                }
            }

            return new ShipmentReportDto
            {
                No = index + 1,
                SupplierName = g.Supplier?.Name,
                InvoiceAmount = transactionAmount,
                Materials = materialNames,
                ExpectedArrivalDate = g.BillingSheet.ExpectedArrivalDate,
                FreeDays = g.BillingSheet.FreeTimeDuration,
                DemurrageStarts = g.BillingSheet.DemurrageStartDate,
                ContainerSize = g.BillingSheet.ContainerNumber,
                BillOfLadingNo = g.BillingSheet.BillOfLading,
                TransactionType = transactionType,
                Status = g.BillingSheet.Status.ToString()
            };
        }).ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<PurchaseOrderReportDto>>> GetPurchaseOrderReportAsync(PurchaseOrderFilter filter)
    {
        var baseQuery =context.PurchaseOrders
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(po => po.DeletedAt == null);

        if (filter.StartDate.HasValue)
        {
            baseQuery = baseQuery.Where(po => po.CreatedAt >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            baseQuery = baseQuery.Where(po => po.CreatedAt <= filter.EndDate.Value);
        }

        if (filter.SupplierIds.Any())
        {
            baseQuery = baseQuery.Where(po => filter.SupplierIds.Contains(po.SupplierId));
        }

        if (!string.IsNullOrWhiteSpace(filter.PoNumber))
        {
            baseQuery = baseQuery.Where(po => filter.PoNumber==po.Code);
        }
        if (filter.SupplierType.HasValue)
        {
            baseQuery = baseQuery.Where(po =>
                po.Supplier.Type == filter.SupplierType.Value);
        }

        var purchaseOrders = await baseQuery
            .Select(po => new
            {
                po.Id,
                po.Code,
                po.ProFormaInvoiceNumber,
                po.SupplierId,
                po.CreatedAt,
                po.ExpectedDeliveryDate,
            }).ToListAsync();
        var purchaseOrderIds= purchaseOrders.Select(o => o.Id).ToList();
        var supplierIds= purchaseOrders.Select(o=>o.SupplierId).Distinct().ToList();
        var suppliers= await context.Suppliers
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(s => supplierIds.Contains(s.Id) && s.DeletedAt == null)
            .Select(s=> new {s.Id, s.Name,s.CurrencyId})
            .ToListAsync();
        var supplierLookup = suppliers.ToDictionary(s => s.Id);

        var purchaseOrderItems = await context.PurchaseOrderItems
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(poi => poi.DeletedAt == null && purchaseOrderIds.Contains(poi.PurchaseOrderId))
            .Select(poi => new
                { poi.PurchaseOrderId, poi.MaterialId, poi.Quantity, poi.Price, poi.UoMId, poi.CurrencyId })
            .ToListAsync();
        var materialIds = purchaseOrderItems.Select(poi => poi.MaterialId).Distinct().ToList();
        var uomIds = purchaseOrderItems.Select(poi => poi.UoMId).Distinct().ToList();
        var currencyIds = suppliers
            .Where(s => s.CurrencyId.HasValue)
            .Select(s => s.CurrencyId.Value)
            .Distinct()
            .ToList();

        var materials = await context.Materials
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(m => materialIds.Contains(m.Id) && m.DeletedAt == null)
            .Select(m => new { m.Id, m.Name }).ToListAsync();
        var materialLookup = materials.ToDictionary(m => m.Id, m => m.Name);
        var uoms = await context.UnitOfMeasures
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(u => uomIds.Contains(u.Id) && u.DeletedAt == null)
            .Select(u => new { u.Id,u.Symbol })
            .ToListAsync();

        var uomLookup = uoms.ToDictionary(u => u.Id,u=> u.Symbol);
           var currencies = await context.Currencies
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(c => currencyIds.Contains(c.Id) && c.DeletedAt == null)
            .Select(c => new { c.Id, c.Symbol })
            .ToListAsync();
           var currencyLookup = currencies.ToDictionary(c => c.Id, c => c.Symbol);
        
           var result = purchaseOrders
               .SelectMany(po =>
               {
                 
                   var items = purchaseOrderItems
                       .Where(poi => poi.PurchaseOrderId == po.Id)
                       .ToList();

                   return items.Select(poi => new { po, poi });
               })
               .Select((x, index) =>
               {
                   var po = x.po;
                   var poi = x.poi;

                   return new PurchaseOrderReportDto
                   {
                       No = index + 1,

                       SupplierName = supplierLookup.TryGetValue(po.SupplierId, out var suppliers)
                           ? suppliers.Name
                           : null,
                       

                       ProformaInvoiceNumber = po.ProFormaInvoiceNumber,
                       PurchaseOrderNumber = po.Code,

                       MaterialName = materialLookup.TryGetValue(poi.MaterialId, out var mName) ? mName : null,

                       OrderQuantity = poi.Quantity,
                       UomName = uomLookup.TryGetValue(poi.UoMId, out var uName) ? uName : null,

                       UnitPrice = poi.Price,
                       CurrencySymbol =
                           (
                               poi.CurrencyId
                               ?? (supplierLookup.TryGetValue(po.SupplierId, out var supplier)
                                   ? supplier.CurrencyId
                                   : null)
                           ) is { } currencyId
                           && currencyLookup.TryGetValue(currencyId, out var cSymbol)
                               ? cSymbol
                               : null,



                       PurchaseOrderDate = po.CreatedAt,
                       ExpectedDeliverydate = po.ExpectedDeliveryDate
                   };
               })
               .ToList();

           return Result.Success(result);

        
    }
  public async Task<Result<List<PurchasedPoReportDto>>> GetPurchasedPoReportAsync(PurchaseOrderFilter filter)
{
    var baseQuery = context.PurchaseOrders
        .AsNoTracking()
        .IgnoreQueryFilters()
        .Where(po => po.DeletedAt == null);

    if (filter.StartDate.HasValue)
    {
        baseQuery = baseQuery.Where(po => po.CreatedAt >= filter.StartDate.Value);
    }

    if (filter.EndDate.HasValue)
    {
        baseQuery = baseQuery.Where(po => po.CreatedAt <= filter.EndDate.Value);
    }

    if (filter.SupplierIds.Any())
    {
        baseQuery = baseQuery.Where(po => filter.SupplierIds.Contains(po.SupplierId));
    }

    if (!string.IsNullOrWhiteSpace(filter.PoNumber))
    {
        baseQuery = baseQuery.Where(po => po.Code==filter.PoNumber);
    }
    if (filter.SupplierType.HasValue)
    {
        baseQuery = baseQuery.Where(po =>
            po.Supplier.Type == filter.SupplierType.Value);
    }


    var purchaseOrders = await baseQuery
        .Select(po => new
        {
            po.Id,
            po.Code,
            po.ProFormaInvoiceNumber,
            po.SupplierId
        })
        .ToListAsync();

    var proformaCodes = purchaseOrders
        .Where(po => !string.IsNullOrEmpty(po.ProFormaInvoiceNumber))
        .Select(po => po.ProFormaInvoiceNumber)
        .Distinct()
        .ToList();

    var shipmentInvoices = await context.ShipmentInvoices
        .AsNoTracking()
        .IgnoreQueryFilters()
        .Where(si => si.DeletedAt == null && proformaCodes.Contains(si.Code))
        .Select(si => new
        {
            si.Code,
            si.SupplierId,
            si.CreatedAt,
            si.CurrencyId
        })
        .ToListAsync();

    var shipmentLookup = shipmentInvoices
        .ToDictionary(si => si.Code);

    
    purchaseOrders = purchaseOrders
        .Where(po => po.ProFormaInvoiceNumber != null &&
                     shipmentLookup.ContainsKey(po.ProFormaInvoiceNumber))
        .ToList();

    var purchaseOrderIds = purchaseOrders.Select(po => po.Id).ToList();
    var supplierIds = purchaseOrders.Select(po => po.SupplierId).Distinct().ToList();

    var suppliers = await context.Suppliers
        .AsNoTracking()
        .IgnoreQueryFilters()
        .Where(s => supplierIds.Contains(s.Id))
        .Select(s => new
        {
            s.Id,
            s.Name,
            s.Type,
            s.CurrencyId
        })
        .ToListAsync();

    var supplierLookup = suppliers.ToDictionary(s => s.Id);

    var purchaseOrderItems = await context.PurchaseOrderItems
        .AsNoTracking()
        .IgnoreQueryFilters()
        .Where(poi => poi.DeletedAt == null &&
                      purchaseOrderIds.Contains(poi.PurchaseOrderId))
        .Select(poi => new
        {
            poi.PurchaseOrderId,
            poi.MaterialId,
            poi.Quantity,
            poi.Price,
            poi.UoMId,
            poi.CurrencyId,
            poi.QuantityInvoiced,
        })
        .ToListAsync();

    var materialIds = purchaseOrderItems.Select(poi => poi.MaterialId).Distinct().ToList();
    var uomIds = purchaseOrderItems.Select(poi => poi.UoMId).Distinct().ToList();

    var currencyIds = suppliers
        .Where(s => s.CurrencyId.HasValue)
        .Select(s => s.CurrencyId.Value)
        .Distinct()
        .ToList();


    var materials = await context.Materials
        .AsNoTracking()
        .IgnoreQueryFilters()
        .Where(m => materialIds.Contains(m.Id))
        .Select(m => new { m.Id, m.Name })
        .ToListAsync();

    var materialLookup = materials.ToDictionary(m => m.Id, m => m.Name);

    var uoms = await context.UnitOfMeasures
        .AsNoTracking()
        .IgnoreQueryFilters()
        .Where(u => uomIds.Contains(u.Id))
        .Select(u => new { u.Id, u.Symbol })
        .ToListAsync();

    var uomLookup = uoms.ToDictionary(u => u.Id, u => u.Symbol);

    var currencies = await context.Currencies
        .AsNoTracking()
        .IgnoreQueryFilters()
        .Where(c => currencyIds.Contains(c.Id))
        .Select(c => new { c.Id, c.Symbol })
        .ToListAsync();

    var currencyLookup = currencies.ToDictionary(c => c.Id, c => c.Symbol);

    var result = purchaseOrders
        .SelectMany(po =>
        {
            var invoice = shipmentLookup[po.ProFormaInvoiceNumber];

            var items = purchaseOrderItems
                .Where(poi => poi.PurchaseOrderId == po.Id)
                .ToList();

            return items.Select(poi => new { po, poi, invoice });
        })
        .Select((x, index) =>
        {
            var po = x.po;
            var poi = x.poi;
            var invoice = x.invoice;

            supplierLookup.TryGetValue(po.SupplierId, out var supplier);

           

            return new PurchasedPoReportDto
            {
                No = index + 1,

                SupplierName = supplier?.Name,
                SupplierType = supplier?.Type.ToString(),

                PoNumber = po.Code,
                InvoiceNumber = invoice.Code,

                MaterialName = materialLookup.TryGetValue(poi.MaterialId, out var mName)
                    ? mName
                    : null,

                OrderedQuantity = poi.Quantity,
                OrderedUom = uomLookup.TryGetValue(poi.UoMId, out var uSymbol)
                    ? uSymbol
                    : null,

                QuantityReceived = poi.QuantityInvoiced,
                ReceivedUom = uomLookup.TryGetValue(poi.UoMId, out var rSymbol)
                    ? rSymbol
                    : null,

                UnitCost = poi.Price,
                CurrencySymbol =
                    (
                        poi.CurrencyId ?? supplier?.CurrencyId
                    ) is { } currencyId
                    && currencyLookup.TryGetValue(currencyId, out var cSymbol)
                        ? cSymbol
                        : null,

                
                InvoiceDate = invoice.CreatedAt
            };
        })
        .ToList();

    return Result.Success(result);
}



public async Task<Result<ProductionDashboardDto>> GetProductionDashboard(Guid departmentId)
{
    var requisitionQuery = context.Requisitions
        .IgnoreQueryFilters()
        .Where(r => r.DepartmentId == departmentId && r.DeletedAt == null);

    var requisitionCounts = await requisitionQuery
        .GroupBy(r => r.Status)
        .Select(g => new
        {
            Status = g.Key,
            Count = g.Count()
        })
        .ToListAsync();

    var requisitionReport = new RequisitionReportDto
    {
        NewRequisitionsCount = requisitionCounts.FirstOrDefault(x => x.Status == RequestStatus.New)?.Count ?? 0,
        PendingRequisitionsCount = requisitionCounts.FirstOrDefault(x => x.Status == RequestStatus.Pending)?.Count ?? 0,
        CompletedRequisitionsCount = requisitionCounts.FirstOrDefault(x => x.Status == RequestStatus.Completed)?.Count ?? 0,
        SourcedRequisitionsCount = requisitionCounts.FirstOrDefault(x => x.Status == RequestStatus.Sourced)?.Count ?? 0,
        RejectedRequisitionsCount = requisitionCounts.FirstOrDefault(x => x.Status == RequestStatus.Rejected)?.Count ?? 0
    };

    var materialDepartments = await context.MaterialDepartments
        .AsNoTracking()
        .IgnoreQueryFilters()
        .Where(md => md.DepartmentId == departmentId && md.DeletedAt == null)
        .Select(md => new
        {
            md.MaterialId,
            md.ReOrderLevel,
            UomSymbol = md.UoM != null ? md.UoM.Symbol : null
        })
        .ToListAsync();

    var materialIds = materialDepartments.Select(md => md.MaterialId).Distinct().ToList();

    var materials = await context.Materials
        .AsNoTracking()
        .IgnoreQueryFilters()
        .Where(m => materialIds.Contains(m.Id))
        .Select(m => new
        {
            m.Id,
            m.Name,
            m.Code
        })
        .ToListAsync();

    var materialLookup = materials.ToDictionary(m => m.Id);

    var shelfQuantities = await context.ShelfMaterialBatches
        .AsNoTracking()
        .IgnoreQueryFilters()
        .Where(smb => smb.DeletedAt == null && materialIds.Contains(smb.MaterialBatch.MaterialId))
        .GroupBy(smb => smb.MaterialBatch.MaterialId)
        .Select(g => new
        {
            MaterialId = g.Key,
            TotalQuantity = g.Sum(x => (decimal?)x.Quantity) ?? 0
        })
        .ToListAsync();

    var quantityLookup = shelfQuantities.ToDictionary(q => q.MaterialId, q => q.TotalQuantity);

    var materialReport = materialDepartments
        .Select(md =>
        {
            quantityLookup.TryGetValue(md.MaterialId, out var currentQty);
            materialLookup.TryGetValue(md.MaterialId, out var material);

            return new MaterialReorderReportDto
            {
                MaterialName = material?.Name,
                MaterialCode = material?.Code,
                CurrentQuantity = currentQty,
                ReOrderLevel = md.ReOrderLevel,
                UomSymbol = md.UomSymbol,
            };
        })
        .Where(x => x.CurrentQuantity <= x.ReOrderLevel)
        .OrderBy(x => x.MaterialName)
        .ToList();

    var productionQuery = context.ProductionSchedules
        .IgnoreQueryFilters()
        .Where(p => p.DepartmentId == departmentId && p.DeletedAt == null);

    var productionCounts = await productionQuery
        .GroupBy(p => p.Status)
        .Select(g => new
        {
            Status = g.Key,
            Count = g.Count()
        })
        .ToListAsync();

    var productionReport = new ProductionScheduleStatusReportDto
    {
        NewScheduleCount = productionCounts.FirstOrDefault(x => x.Status == ProductionStatus.New)?.Count ?? 0,
        InProgressScheduleCount = productionCounts.FirstOrDefault(x => x.Status == ProductionStatus.InProgress)?.Count ?? 0,
        CompletedScheduleCount = productionCounts.FirstOrDefault(x => x.Status == ProductionStatus.Completed)?.Count ?? 0,
        DelayedScheduleCount = productionCounts.FirstOrDefault(x => x.Status == ProductionStatus.Delayed)?.Count ?? 0,
        CancelledScheduleCount = productionCounts.FirstOrDefault(x => x.Status == ProductionStatus.Cancelled)?.Count ?? 0
    };

   
    var stockQuery = context.StockTransferSources
        .IgnoreQueryFilters()
        .Where(s => s.FromDepartmentId == departmentId && s.DeletedAt == null);

    var stockCounts = await stockQuery
        .GroupBy(s => s.Status)
        .Select(g => new
        {
            Status = g.Key,
            Count = g.Count()
        })
        .ToListAsync();

    var stockReport = new StockTransferStatusReportDto
    {
        InProgressCount = stockCounts.FirstOrDefault(x => x.Status == StockTransferStatus.InProgress)?.Count ?? 0,
        ApprovedCount = stockCounts.FirstOrDefault(x => x.Status == StockTransferStatus.Approved)?.Count ?? 0,
        IssuedCount = stockCounts.FirstOrDefault(x => x.Status == StockTransferStatus.Issued)?.Count ?? 0,
        RejectedCount = stockCounts.FirstOrDefault(x => x.Status == StockTransferStatus.Rejected)?.Count ?? 0
    };

    var dashboard = new ProductionDashboardDto
    {
        RequisitionReport = requisitionReport,
        MaterialsBelowReorderLevel = materialReport,
        ProductionScheduleReport = productionReport,
        StockTransferReport = stockReport
    };

    return Result.Success(dashboard);
}

}


