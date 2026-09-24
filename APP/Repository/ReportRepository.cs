using APP.IRepository;
using AutoMapper;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.AttendanceRecords;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.ItemStockRequisitions;
using DOMAIN.Entities.JobRequests;
using DOMAIN.Entities.LeaveRequests;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.OvertimeRequests;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.StaffRequisitions;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.ProductionOrders;
using DOMAIN.Entities.ProductionSchedules.StockTransfers;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.Reports;
using DOMAIN.Entities.Grns;
using DOMAIN.Entities.Reports.FinishedGoodsTransferNotes;
using DOMAIN.Entities.Reports.GeneralInventory;
using DOMAIN.Entities.Reports.HrDashboardKpi;
using DOMAIN.Entities.Reports.HumanResource;
using DOMAIN.Entities.Reports.Procurement;
using DOMAIN.Entities.Reports.PurchaseOrder;
using DOMAIN.Entities.Reports.Services;
using DOMAIN.Entities.Reports.Shipments;
using DOMAIN.Entities.Reports.Warehouse;
using DOMAIN.Entities.Reports.WarehouseDashboardKpi;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SHARED;

namespace APP.Repository;

public partial class ReportRepository(
    ApplicationDbContext context,
    IMapper mapper,
    IMaterialRepository materialRepository,
    ILogger<ReportRepository> logger
) : IReportRepository
{
    public async Task<Result<ProductionReportDto>> GetProductionReport(
        ReportFilter filter,
        Guid departmentId
    )
    {
        var purchaseRequisitions = context
            .Requisitions.Where(r => r.RequisitionType == RequisitionType.Purchase)
            .AsQueryable();

        var sourceRequisitions = context.SourceRequisitions.AsQueryable();
        var productionSchedules = context.ProductionSchedules.AsQueryable();
        var incomingStockTransfers = context
            .StockTransferSources.Where(s => s.FromDepartmentId == departmentId)
            .AsQueryable();
        var outGoingStockTransfers = context
            .StockTransferSources.Where(s => s.ToDepartmentId == departmentId)
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

        var purchaseOrderNumber = await context
            .PurchaseOrders.Where(p => sourceRequisitionIds.Contains(p.SourceRequisitionId))
            .CountAsync();

        return new ProductionReportDto
        {
            NumberOfPurchaseRequisitions = await purchaseRequisitions.CountAsync(p =>
                p.DepartmentId == departmentId
            ),
            NumberOfNewPurchaseRequisitions = await purchaseRequisitions.CountAsync(p =>
                p.Status == RequestStatus.New
            ),
            NumberOfInProgressPurchaseRequisitions = await purchaseRequisitions.CountAsync(p =>
                p.Status == RequestStatus.Pending
            ),
            NumberOfCompletedPurchaseRequisitions = purchaseOrderNumber,

            NumberOfProductionSchedules = await productionSchedules.CountAsync(),
            NumberOfNewProductionSchedules = await productionSchedules.CountAsync(p =>
                p.Status == ProductionStatus.New
            ),
            NumberOfInProgressProductionSchedules = await productionSchedules.CountAsync(p =>
                p.Status == ProductionStatus.InProgress
            ),
            NumberOfCompletedProductionSchedules = await productionSchedules.CountAsync(p =>
                p.Status == ProductionStatus.Completed
            ),

            NumberOfIncomingStockTransfers = await incomingStockTransfers.CountAsync(),
            NumberOfIncomingPendingStockTransfers = await incomingStockTransfers.CountAsync(s =>
                s.Status == StockTransferStatus.InProgress
            ),
            NumberOfIncomingCompletedStockTransfers = await incomingStockTransfers.CountAsync(s =>
                s.Status == StockTransferStatus.Approved
            ),

            NumberOfOutgoingStockTransfers = await outGoingStockTransfers.CountAsync(),
            NumberOfOutgoingPendingStockTransfers = await outGoingStockTransfers.CountAsync(s =>
                s.Status == StockTransferStatus.InProgress
            ),
            NumberOfOutgoingCompletedStockTransfers = await outGoingStockTransfers.CountAsync(s =>
                s.Status == StockTransferStatus.Approved
            ),
        };
    }

    public async Task<Result<List<MaterialWithStockDto>>> GetMaterialsBelowMinimumStockLevel(
        Guid departmentId
    )
    {
        var materialDepartments = await context
            .MaterialDepartments.AsSplitQuery()
            .Include(md => md.Material)
            .Include(materialDepartment => materialDepartment.UoM)
            .Where(md => md.DepartmentId == departmentId)
            .ToListAsync();

        var rawWarehouse = await context
            .Warehouses
            .FirstOrDefaultAsync(w =>
                w.DepartmentId == departmentId && w.Type == WarehouseType.RawMaterialStorage
            );

        var packageWarehouse = await context
            .Warehouses
            .FirstOrDefaultAsync(w =>
                w.DepartmentId == departmentId && w.Type == WarehouseType.PackagedStorage
            );

        if (rawWarehouse == null || packageWarehouse == null)
            return Error.Failure(
                "Department.Warehouse",
                "One or more required warehouses not found."
            );

        List<MaterialWithStockDto> materialsBelowMinStock = [];

        foreach (var materialDepartment in materialDepartments)
        {
            var material = materialDepartment.Material;
            var warehouseId =
                material.Kind == MaterialKind.Raw ? rawWarehouse.Id : packageWarehouse.Id;

            var stockResult = await materialRepository.GetMassMaterialStockInWarehouse(
                material.Id,
                warehouseId
            );

            if (stockResult.IsFailure)
                continue;

            var stock = stockResult.Value;

            if (stock < materialDepartment.MinimumStockLevel)
            {
                materialsBelowMinStock.Add(
                    new MaterialWithStockDto
                    {
                        Material = mapper.Map<MaterialDto>(material),
                        StockQuantity = stock,
                        UoM = mapper.Map<UnitOfMeasureDto>(materialDepartment.UoM),
                    }
                );
            }
        }

        return materialsBelowMinStock;
    }

    public async Task<Result<HrDashboardDto>> GetHumanResourceDashboardReport(
        MovementReportFilter filter,
        Guid? designationId,
        EmployeeType? employeeType,
        Gender? gender
    )
    {
        var leaveRequests = context.LeaveRequests.AsQueryable();
        var overtimeRequests = context.OvertimeRequests.AsQueryable();
        var employees = context.Employees.AsQueryable();
        var staffRequisitions = context.StaffRequisitions.AsQueryable();

        if (filter.StartDate.HasValue)
        {
            leaveRequests = leaveRequests.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            overtimeRequests = overtimeRequests.Where(or => or.CreatedAt >= filter.StartDate.Value);
            staffRequisitions = staffRequisitions.Where(sr =>
                sr.CreatedAt >= filter.StartDate.Value
            );
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
            leaveRequests = leaveRequests.Where(lr =>
                lr.Employee.DepartmentId == filter.DepartmentId.Value
            );
            overtimeRequests = overtimeRequests.Where(or =>
                or.Employees.Any(e => e.DepartmentId == filter.DepartmentId.Value)
            );
            staffRequisitions = staffRequisitions.Where(sr =>
                sr.DepartmentId == filter.DepartmentId.Value
            );
        }

        if (designationId.HasValue)
        {
            employees = employees.Where(e => e.DesignationId == designationId.Value);
            leaveRequests = leaveRequests.Where(lr =>
                lr.Employee.DesignationId == designationId.Value
            );
            overtimeRequests = overtimeRequests.Where(or =>
                or.Employees.Any(e => e.DesignationId == designationId.Value)
            );
            staffRequisitions = staffRequisitions.Where(sr =>
                sr.DesignationId == designationId.Value
            );
        }

        if (employeeType.HasValue)
        {
            employees = employees.Where(e => e.Type == employeeType.Value);
            leaveRequests = leaveRequests.Where(lr => lr.Employee.Type == employeeType.Value);
            overtimeRequests = overtimeRequests.Where(or =>
                or.Employees.Any(e => e.Type == employeeType.Value)
            );
        }

        if (gender.HasValue)
        {
            employees = employees.Where(e => e.Gender == gender.Value);
            leaveRequests = leaveRequests.Where(lr => lr.Employee.Gender == gender.Value);
            overtimeRequests = overtimeRequests.Where(or =>
                or.Employees.Any(e => e.Gender == gender.Value)
            );
        }

        var employeeStats = await employees
            .GroupBy(e => 1)
            .Select(g => new
            {
                TotalCasual = g.Count(e => e.Type == EmployeeType.Casual),
                TotalPermanent = g.Count(e => e.Type == EmployeeType.Permanent),
                ActiveCasual = g.Count(e =>
                    e.Type == EmployeeType.Casual && e.Status == EmployeeStatus.Active
                ),
                ActivePermanent = g.Count(e =>
                    e.Type == EmployeeType.Permanent && e.Status == EmployeeStatus.Active
                ),
                InactiveCasual = g.Count(e =>
                    e.Type == EmployeeType.Casual && e.Status == EmployeeStatus.Inactive
                ),
                InactivePermanent = g.Count(e =>
                    e.Type == EmployeeType.Permanent && e.Status == EmployeeStatus.Inactive
                ),
                NewCasual = g.Count(e =>
                    e.Type == EmployeeType.Casual && e.Status == EmployeeStatus.New
                ),
                NewPermanent = g.Count(e =>
                    e.Type == EmployeeType.Permanent && e.Status == EmployeeStatus.New
                ),
                Male = g.Count(e => e.Gender == Gender.Male),
                Female = g.Count(e => e.Gender == Gender.Female),
            })
            .FirstOrDefaultAsync();

        var leaveStats = await leaveRequests
            .GroupBy(lr => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Pending = g.Count(lr => lr.LeaveStatus == LeaveStatus.Pending),
                Expired = g.Count(lr => lr.LeaveStatus == LeaveStatus.Expired),
                Rejected = g.Count(lr => lr.LeaveStatus == LeaveStatus.Rejected),

                Absence = g.Count(lr => lr.RequestCategory == RequestCategory.AbsenceRequest),
                PendingAbsence = g.Count(lr =>
                    lr.RequestCategory == RequestCategory.AbsenceRequest
                    && lr.LeaveStatus == LeaveStatus.Pending
                ),
                ApprovedAbsence = g.Count(lr =>
                    lr.RequestCategory == RequestCategory.AbsenceRequest
                    && lr.LeaveStatus == LeaveStatus.Approved
                ),
                RejectedAbsence = g.Count(lr =>
                    lr.RequestCategory == RequestCategory.AbsenceRequest
                    && lr.LeaveStatus == LeaveStatus.Rejected
                ),

                ExitPass = g.Count(lr => lr.RequestCategory == RequestCategory.ExitPassRequest),
                PendingExitPass = g.Count(lr =>
                    lr.RequestCategory == RequestCategory.ExitPassRequest
                    && lr.LeaveStatus == LeaveStatus.Pending
                ),
                ApprovedExitPass = g.Count(lr =>
                    lr.RequestCategory == RequestCategory.ExitPassRequest
                    && lr.LeaveStatus == LeaveStatus.Approved
                ),
                RejectedExitPass = g.Count(lr =>
                    lr.RequestCategory == RequestCategory.ExitPassRequest
                    && lr.LeaveStatus == LeaveStatus.Rejected
                ),

                OfficialDuty = g.Count(lr => lr.RequestCategory == RequestCategory.OfficialDuty),
                ApprovedOfficialDuty = g.Count(lr =>
                    lr.RequestCategory == RequestCategory.OfficialDuty
                    && lr.LeaveStatus == LeaveStatus.Approved
                ),
                RejectedOfficialDuty = g.Count(lr =>
                    lr.RequestCategory == RequestCategory.OfficialDuty
                    && lr.LeaveStatus == LeaveStatus.Rejected
                ),
                PendingOfficialDuty = g.Count(lr =>
                    lr.RequestCategory == RequestCategory.OfficialDuty
                    && lr.LeaveStatus == LeaveStatus.Pending
                ),
            })
            .FirstOrDefaultAsync();

        var overtimeStats = await overtimeRequests
            .GroupBy(or => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Approved = g.Count(or => or.Status == OvertimeStatus.Approved),
                Pending = g.Count(or => or.Status == OvertimeStatus.Pending),
                Expired = g.Count(or => or.Status == OvertimeStatus.Expired),
            })
            .FirstOrDefaultAsync();

        var staffRequisitionCount = await staffRequisitions.CountAsync();

        var ratio =
            employeeStats?.Female > 0 ? (decimal)employeeStats.Male / employeeStats.Female : 0;

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
            AttendanceStats = await GetAttendanceStatsAsync(filter.StartDate, filter.EndDate),
        };
    }

    public async Task<Result<PermanentStaffGradeReportDto>> GetPermanentStaffGradeReport(
        Guid? departmentId
    )
    {
        var employees = context
            .Employees.Include(e => e.Department)
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
                SeniorMgtMale = g.Count(e =>
                    e.Level == EmployeeLevel.SeniorManagement && e.Gender == Gender.Male
                ),
                SeniorMgtFemale = g.Count(e =>
                    e.Level == EmployeeLevel.SeniorManagement && e.Gender == Gender.Female
                ),
                SeniorStaffMale = g.Count(e =>
                    e.Level == EmployeeLevel.SeniorStaff && e.Gender == Gender.Male
                ),
                SeniorStaffFemale = g.Count(e =>
                    e.Level == EmployeeLevel.SeniorStaff && e.Gender == Gender.Female
                ),
                JuniorStaffMale = g.Count(e =>
                    e.Level == EmployeeLevel.JuniorStaff && e.Gender == Gender.Male
                ),
                JuniorStaffFemale = g.Count(e =>
                    e.Level == EmployeeLevel.JuniorStaff && e.Gender == Gender.Female
                ),
            })
            .ToListAsync();

        var total = new PermanentStaffGradeTotalDto
        {
            SeniorMgtMale = groupedResults.Sum(x => x.SeniorMgtMale),
            SeniorMgtFemale = groupedResults.Sum(x => x.SeniorMgtFemale),
            SeniorStaffMale = groupedResults.Sum(x => x.SeniorStaffMale),
            SeniorStaffFemale = groupedResults.Sum(x => x.SeniorStaffFemale),
            JuniorStaffMale = groupedResults.Sum(x => x.JuniorStaffMale),
            JuniorStaffFemale = groupedResults.Sum(x => x.JuniorStaffFemale),
        };

        return new PermanentStaffGradeReportDto { Departments = groupedResults, Totals = total };
    }

    private async Task<AttendanceStatsDto> GetAttendanceStatsAsync(
        DateTime? startDate,
        DateTime? endDate
    )
    {
        var presentEmployees = await context.AttendanceRecords.CountAsync(a =>
            a.TimeStamp >= startDate && a.TimeStamp < endDate && a.WorkState == WorkState.CheckIn
        );

        var totalEmployees = await context.Employees.ToListAsync();

        var absentEmployees = totalEmployees.Count - presentEmployees;

        var rate = totalEmployees.Count == 0 ? 0 : presentEmployees * 100 / totalEmployees.Count;

        return new AttendanceStatsDto
        {
            NumberOfPresentEmployees = presentEmployees,
            NumberOfAbsentEmployees = absentEmployees,
            AttendanceRate = rate,
        };
    }

    public async Task<Result<EmployeeMovementReportDto>> GetEmployeeMovementReport(
        MovementReportFilter filter
    )
    {
        var start = filter.StartDate.HasValue
            ? DateTime.SpecifyKind(filter.StartDate.Value, DateTimeKind.Utc)
            : DateTime.UtcNow.AddMonths(-1);
        var end = filter.EndDate.HasValue
            ? DateTime.SpecifyKind(filter.EndDate.Value, DateTimeKind.Utc)
            : DateTime.UtcNow;

        var query = context.Employees
            .AsSplitQuery()
            .IgnoreAutoIncludes()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(e => e.Department)
            .Where(e =>
                (e.DateEmployed >= start && e.DateEmployed <= end)
                || (
                    e.Status == EmployeeStatus.Inactive
                    && e.ExitDate.HasValue
                    && e.ExitDate >= start
                    && e.ExitDate <= end
                )
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

                // New hire: DateEmployed falls within the period
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
                    continue;
                }

                // Exit: Status is Inactive and ExitDate falls within the period
                if (
                    emp.Status == EmployeeStatus.Inactive
                    && emp.ExitDate.HasValue
                    && emp.ExitDate >= start
                    && emp.ExitDate <= end
                    && emp.InactiveStatus.HasValue
                )
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
                        case EmployeeInactiveStatus.VacatedPost:
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
                            if (!isCasual)
                            {
                                dto.PermanentTransfer++;
                                totals.PermanentTransfer++;
                            }
                            break;
                    }
                }
            }

            departments.Add(dto);
        }

        var result = new EmployeeMovementReportDto { Departments = departments, Totals = totals };

        return Result.Success(result);
    }

    public async Task<Result<StaffTotalReport>> GetStaffTotalReport(MovementReportFilter filter)
    {
        var employees = context.Employees
            .AsSplitQuery()
            .IgnoreAutoIncludes()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => e.Status == EmployeeStatus.Active
                && (e.Type == EmployeeType.Casual || e.Type == EmployeeType.Permanent));

        if (filter.DepartmentId.HasValue)
            employees = employees.Where(e => e.DepartmentId == filter.DepartmentId.Value);

        var groupedResults = await employees
            .GroupBy(e => e.Department != null ? e.Department.Name : "Unassigned")
            .Select(g => new StaffTotalSummary
            {
                Department = g.Key,
                TotalPermanentStaff = g.Count(e => e.Type == EmployeeType.Permanent),
                TotalCasualStaff = g.Count(e => e.Type == EmployeeType.Casual),
            })
            .ToListAsync();

        var total = new StaffGrandTotal
        {
            TotalPermanentStaff = groupedResults.Sum(x => x.TotalPermanentStaff),
            TotalCasualStaff = groupedResults.Sum(x => x.TotalCasualStaff),
        };

        return Result.Success(
            new StaffTotalReport { Departments = groupedResults, Totals = total }
        );
    }

    public async Task<Result<StaffGenderRatioReport>> GetStaffGenderRatioReport(
        MovementReportFilter filter
    )
    {
        var query = context
            .Employees.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(employee =>
                employee.Status == EmployeeStatus.Active
                && (employee.Type == EmployeeType.Casual
                    || employee.Type == EmployeeType.Permanent)
            );

        if (filter.DepartmentId.HasValue)
            query = query.Where(employee => employee.DepartmentId == filter.DepartmentId.Value);

        if (filter.StartDate.HasValue)
        {
            var start = filter.StartDate.Value.Date;
            query = query.Where(employee => employee.DateEmployed >= start);
        }

        if (filter.EndDate.HasValue)
        {
            var endExclusive = filter.EndDate.Value.Date.AddDays(1);
            query = query.Where(employee => employee.DateEmployed < endExclusive);
        }

        var employees = await query
            .Select(employee => new
            {
                Department = employee.Department != null
                    ? employee.Department.Name
                    : "Unassigned",
                employee.Type,
                employee.Gender,
            })
            .ToListAsync();

        var departments = employees
            .GroupBy(employee => employee.Department)
            .Select(group => new StaffGenderRatioCountDto
            {
                Department = group.Key,
                NumberOfCasualMale = group.Count(employee =>
                    employee.Type == EmployeeType.Casual && employee.Gender == Gender.Male
                ),
                NumberOfCasualFemale = group.Count(employee =>
                    employee.Type == EmployeeType.Casual && employee.Gender == Gender.Female
                ),
                NumberOfPermanentMale = group.Count(employee =>
                    employee.Type == EmployeeType.Permanent && employee.Gender == Gender.Male
                ),
                NumberOfPermanentFemale = group.Count(employee =>
                    employee.Type == EmployeeType.Permanent && employee.Gender == Gender.Female
                ),
            })
            .OrderBy(department => department.Department)
            .ToList();

        var totals = new StaffGenderRatioTotalDto
        {
            NumberOfCasualMale = departments.Sum(item => item.NumberOfCasualMale),
            NumberOfCasualFemale = departments.Sum(item => item.NumberOfCasualFemale),
            NumberOfPermanentMale = departments.Sum(item => item.NumberOfPermanentMale),
            NumberOfPermanentFemale = departments.Sum(item => item.NumberOfPermanentFemale),
        };

        return Result.Success(
            new StaffGenderRatioReport { Departments = departments, Totals = totals }
        );
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

    public async Task<Result<StaffLeaveSummaryReportDto>> GetStaffLeaveSummaryReport(
        MovementReportFilter filter
    )
    {
        logger.LogInformation(
            "Generating Staff Leave Summary Report with filter: {@Filter}",
            filter
        );

        var start =
            filter.StartDate?.Date
            ?? new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = filter.EndDate?.Date ?? DateTime.UtcNow.Date;
        var today = DateTime.UtcNow.Date;

        logger.LogInformation("Date range resolved: {Start} - {End}", start, end);

        var employeesQuery = context
            .Employees.Include(e => e.Department)
            .Where(e => e.Status == EmployeeStatus.Active);

        if (filter.DepartmentId.HasValue)
        {
            employeesQuery = employeesQuery.Where(e => e.DepartmentId == filter.DepartmentId);
            logger.LogInformation("Filtering by department: {DepartmentId}", filter.DepartmentId);
        }

        var employees = await employeesQuery.ToListAsync();
        logger.LogInformation("Fetched {EmployeeCount} active employees", employees.Count);

        var employeeIds = employees.Select(e => e.Id).ToList();

        var leaveRequests = await context
            .LeaveRequests.Where(l =>
                employeeIds.Contains(l.EmployeeId)
                && l.Approved
                && l.RequestCategory == RequestCategory.LeaveRequest
                && l.StartDate <= end
                && l.EndDate >= start
            )
            .ToListAsync();

        logger.LogInformation(
            "Fetched {LeaveRequestCount} approved leave requests in date range",
            leaveRequests.Count
        );

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

            logger.LogInformation(
                "Dept: {Dept}, DueForLeave: {Count}, TotalLeave: {Entitlement}, DaysUsed: {Used}",
                departmentName,
                count,
                totalEntitlement,
                daysUsed
            );

            var dto = new StaffLeaveSummaryDto
            {
                DepartmentName = departmentName,
                StaffDueForLeave = count,
                TotalLeaveEntitlement = totalEntitlement,
                DaysUsed = daysUsed,
            };

            report.Departments.Add(dto);
        }

        return Result.Success(report);
    }

    public async Task<Result<StaffTurnoverReportDto>> GetStaffTurnoverReport(ReportFilter filter)
    {
        var start = filter.StartDate.HasValue
            ? DateTime.SpecifyKind(filter.StartDate.Value, DateTimeKind.Utc)
            : DateTime.UtcNow.AddMonths(-1);
        var end = filter.EndDate.HasValue
            ? DateTime.SpecifyKind(filter.EndDate.Value, DateTimeKind.Utc)
            : DateTime.UtcNow;

        var employees = await context.Employees
            .AsSplitQuery()
            .IgnoreAutoIncludes()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(e => e.Department)
            .Where(e => e.DateEmployed <= end)
            .ToListAsync();

        var headcountAtStart = employees.Count(e =>
            e.DateEmployed <= start && (!e.ExitDate.HasValue || e.ExitDate > start));

        var headcountAtEnd = employees.Count(e =>
            e.DateEmployed <= end && (!e.ExitDate.HasValue || e.ExitDate > end));

        var averageHeadcount = (headcountAtStart + headcountAtEnd) / 2.0;

        var leavers = employees.Where(e =>
            e.ExitDate.HasValue && e.ExitDate >= start && e.ExitDate <= end
            && e.Status == EmployeeStatus.Inactive
            && e.InactiveStatus.HasValue
        ).ToList();

        var grandTotalLeavers = leavers.Count;
        var organisationTurnover = averageHeadcount > 0
            ? Math.Round(grandTotalLeavers / averageHeadcount * 100, 2)
            : 0;

        var departmentGroups = leavers.GroupBy(e => e.Department?.Name ?? "Unassigned");

        var departments = new List<StaffTurnoverCountDto>();
        var no = 1;

        foreach (var dept in departmentGroups)
        {
            var deptLeavers = dept.ToList();
            var deptTotalLeavers = deptLeavers.Count;

            var deptHeadcountStart = employees.Count(e =>
                e.Department?.Name == dept.Key
                && e.DateEmployed <= start
                && (!e.ExitDate.HasValue || e.ExitDate > start));

            var deptHeadcountEnd = employees.Count(e =>
                e.Department?.Name == dept.Key
                && e.DateEmployed <= end
                && (!e.ExitDate.HasValue || e.ExitDate > end));

            var deptAvgHeadcount = (deptHeadcountStart + deptHeadcountEnd) / 2.0;
            var deptTurnover = deptAvgHeadcount > 0
                ? Math.Round(deptTotalLeavers / deptAvgHeadcount * 100, 2)
                : 0;

            var exitReasonGroups = deptLeavers.GroupBy(e => e.InactiveStatus.ToString());

            foreach (var reason in exitReasonGroups)
            {
                departments.Add(new StaffTurnoverCountDto
                {
                    No = no++,
                    Department = dept.Key,
                    ExitReason = reason.Key,
                    LeaverCount = reason.Count(),
                    TotalLeavers = deptTotalLeavers,
                    AverageHeadcount = Math.Round(deptAvgHeadcount, 1),
                    DepartmentalTurnover = deptTurnover,
                });
            }
        }

        return Result.Success(new StaffTurnoverReportDto
        {
            Departments = departments,
            OrganisationTurnover = organisationTurnover,
            GrandTotalLeavers = grandTotalLeavers,
        });
    }

    public async Task<Result<QaDashboardDto>> GetQaDashboardReport(
        ReportFilter filter,
        Guid? productId
    )
    {
        var analyticalTestRequests = context
            .AnalyticalTestRequests.AsSplitQuery()
            .Include(p => p.ProductionScheduleProduct)
            .AsQueryable();
        var approvedManufacturers = context.Manufacturers.AsQueryable();
        var products = context.Products.AsQueryable();
        var materials = context.Materials.AsQueryable();
        var bmrRequests = context
            .BatchManufacturingRecords.AsSplitQuery()
            .Include(b => b.ProductionScheduleProduct)
                .ThenInclude(b => b.Product)
            .AsQueryable();
        var billingSheetApprovals = context.BillingSheetApprovals.AsQueryable();
        var leaveRequestApprovals = context.LeaveRequestApprovals.AsQueryable();
        var purchaseOrderApprovals = context.PurchaseOrderApprovals.AsQueryable();
        var requisitionApprovals = context
            .RequisitionApprovals.AsSplitQuery()
            .Include(r => r.Requisition)
                .ThenInclude(r => r.ProductionScheduleProduct)
                    .ThenInclude(r => r.Product)
            .AsQueryable();
        var responseApprovals = context.ResponseApprovals.AsQueryable();
        var staffRequisitionApprovals = context.StaffRequisitionApprovals.AsQueryable();

        if (filter.StartDate.HasValue)
        {
            analyticalTestRequests = analyticalTestRequests.Where(lr =>
                lr.CreatedAt >= filter.StartDate.Value
            );
            approvedManufacturers = approvedManufacturers.Where(lr =>
                lr.CreatedAt >= filter.StartDate.Value
            );
            products = products.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            materials = materials.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            bmrRequests = bmrRequests.Where(lr => lr.CreatedAt >= filter.StartDate.Value);
            requisitionApprovals = requisitionApprovals.Where(lr =>
                lr.CreatedAt >= filter.StartDate.Value
            );
            responseApprovals = responseApprovals.Where(lr =>
                lr.CreatedAt >= filter.StartDate.Value
            );
            billingSheetApprovals = billingSheetApprovals.Where(lr =>
                lr.CreatedAt >= filter.StartDate.Value
            );
            leaveRequestApprovals = leaveRequestApprovals.Where(lr =>
                lr.CreatedAt >= filter.StartDate.Value
            );
            purchaseOrderApprovals = purchaseOrderApprovals.Where(lr =>
                lr.CreatedAt >= filter.StartDate.Value
            );
            staffRequisitionApprovals = staffRequisitionApprovals.Where(lr =>
                lr.CreatedAt >= filter.StartDate.Value
            );
        }

        if (filter.EndDate.HasValue)
        {
            analyticalTestRequests = analyticalTestRequests.Where(lr =>
                lr.CreatedAt < filter.EndDate.Value.AddDays(1)
            );
            approvedManufacturers = approvedManufacturers.Where(lr =>
                lr.CreatedAt < filter.EndDate.Value.AddDays(1)
            );
            products = products.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
            materials = materials.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
            bmrRequests = bmrRequests.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
            requisitionApprovals = requisitionApprovals.Where(lr =>
                lr.CreatedAt < filter.EndDate.Value.AddDays(1)
            );
            responseApprovals = responseApprovals.Where(lr =>
                lr.CreatedAt < filter.EndDate.Value.AddDays(1)
            );
            billingSheetApprovals = billingSheetApprovals.Where(lr =>
                lr.CreatedAt < filter.EndDate.Value.AddDays(1)
            );
            leaveRequestApprovals = leaveRequestApprovals.Where(lr =>
                lr.CreatedAt < filter.EndDate.Value.AddDays(1)
            );
            purchaseOrderApprovals = purchaseOrderApprovals.Where(lr =>
                lr.CreatedAt < filter.EndDate.Value.AddDays(1)
            );
            staffRequisitionApprovals = staffRequisitionApprovals.Where(lr =>
                lr.CreatedAt < filter.EndDate.Value.AddDays(1)
            );
        }

        if (productId.HasValue)
        {
            analyticalTestRequests = analyticalTestRequests.Where(lr =>
                lr.ProductionScheduleProduct.ProductId == productId
            );
            products = products.Where(lr => lr.Id == productId);
            bmrRequests = bmrRequests.Where(lr =>
                lr.ProductionScheduleProduct != null
                && lr.ProductionScheduleProduct.ProductId == productId
            );
            requisitionApprovals = requisitionApprovals.Where(lr =>
                lr.Requisition.ProductionScheduleProduct != null
                && lr.Requisition.ProductionScheduleProduct.ProductId == productId
            );
        }

        if (filter.MaterialKind.HasValue)
        {
            materials = materials.Where(m => m.Kind == filter.MaterialKind.Value);
        }

        return new QaDashboardDto
        {
            NumberOfBmrRequests = bmrRequests.Count(),
            NumberOfPendingBmrRequests = bmrRequests.Count(bmr =>
                bmr.Status == BatchManufacturingStatus.New
            ),
            NumberOfApprovedBmrRequests = bmrRequests.Count(bmr =>
                bmr.Status == BatchManufacturingStatus.Approved
            ),
            NumberOfRejectBmrRequests = bmrRequests.Count(bmr =>
                bmr.Status == BatchManufacturingStatus.Rejected
            ),
            NumberOfAnalyticalTestRequests = analyticalTestRequests.Count(),
            NumberOfExpiredAnalyticalTestRequests = await analyticalTestRequests.CountAsync(or =>
                or.ExpiryDate > filter.StartDate && or.ExpiryDate <= filter.EndDate
            ),
            NumberOfApprovals =
                await requisitionApprovals.CountAsync()
                + await billingSheetApprovals.CountAsync()
                + await leaveRequestApprovals.CountAsync()
                + await purchaseOrderApprovals.CountAsync()
                + await staffRequisitionApprovals.CountAsync()
                + await purchaseOrderApprovals.CountAsync()
                + await responseApprovals.CountAsync(),

            NumberOfPendingApprovals =
                await requisitionApprovals.CountAsync(s => s.Status == ApprovalStatus.Pending)
                + await billingSheetApprovals.CountAsync(s => s.Status == ApprovalStatus.Pending)
                + await leaveRequestApprovals.CountAsync(s => s.Status == ApprovalStatus.Pending)
                + await purchaseOrderApprovals.CountAsync(s => s.Status == ApprovalStatus.Pending)
                + await staffRequisitionApprovals.CountAsync(s =>
                    s.Status == ApprovalStatus.Pending
                )
                + await purchaseOrderApprovals.CountAsync(s => s.Status == ApprovalStatus.Pending)
                + await responseApprovals.CountAsync(s => s.Status == ApprovalStatus.Pending),

            NumberOfRejectedApprovals =
                await requisitionApprovals.CountAsync(s => s.Status == ApprovalStatus.Rejected)
                + await billingSheetApprovals.CountAsync(s => s.Status == ApprovalStatus.Rejected)
                + await leaveRequestApprovals.CountAsync(s => s.Status == ApprovalStatus.Rejected)
                + await purchaseOrderApprovals.CountAsync(s => s.Status == ApprovalStatus.Rejected)
                + await staffRequisitionApprovals.CountAsync(s =>
                    s.Status == ApprovalStatus.Rejected
                )
                + await purchaseOrderApprovals.CountAsync(s => s.Status == ApprovalStatus.Rejected)
                + await responseApprovals.CountAsync(s => s.Status == ApprovalStatus.Rejected),
            NumberOfManufacturers = await approvedManufacturers.CountAsync(),
            NumberOfNewManufacturers = await approvedManufacturers.CountAsync(),
            NumberOfApprovedManufacturers = await approvedManufacturers.CountAsync(am =>
                am.ApprovedAt.HasValue
            ),
            NumberOfExpiredManufacturers = await approvedManufacturers.CountAsync(am =>
                am.ValidityDate.HasValue && am.ValidityDate.Value < DateTime.UtcNow
            ),
            NumberOfProducts = await products.CountAsync(),
            NumberOfPackingMaterials = await materials.CountAsync(m =>
                m.Kind == MaterialKind.Package
            ),
            NumberOfRawMaterials = await materials.CountAsync(m => m.Kind == MaterialKind.Raw),
        };
    }

    public async Task<Result<QcDashboardDto>> GetQcDashboardReport(
        ReportFilter filter,
        Guid? productId,
        Guid? materialId
    )
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
            materialAnalyticalRawData = materialAnalyticalRawData.Where(lr =>
                lr.CreatedAt >= filter.StartDate.Value
            );
            materialStp = materialStp.Where(ms => ms.CreatedAt >= filter.StartDate);
            productStp = productStp.Where(ms => ms.CreatedAt >= filter.StartDate);
            productAnalyticalRawData = productAnalyticalRawData.Where(lr =>
                lr.CreatedAt >= filter.StartDate
            );
            rawMaterialBatchTest = rawMaterialBatchTest.Where(lr =>
                lr.CreatedAt >= filter.StartDate
            );
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
            materialAnalyticalRawData = materialAnalyticalRawData.Where(lr =>
                lr.CreatedAt < filter.EndDate.Value.AddDays(1)
            );
            materialStp = materialStp.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
            productStp = productStp.Where(lr => lr.CreatedAt < filter.EndDate.Value.AddDays(1));
            productAnalyticalRawData = productAnalyticalRawData.Where(lr =>
                lr.CreatedAt < filter.EndDate.Value.AddDays(1)
            );
            rawMaterialBatchTest = rawMaterialBatchTest.Where(lr =>
                lr.CreatedAt < filter.EndDate.Value.AddDays(1)
            );
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
            productAnalyticalRawData = productAnalyticalRawData.Where(lr =>
                lr.ProductStandardTestProcedure.ProductId == productId
            );
        }

        if (materialId.HasValue)
        {
            materialAnalyticalRawData = materialAnalyticalRawData.Where(lr =>
                lr.MaterialStandardTestProcedure.MaterialId == materialId
            );
            materialStp = materialStp.Where(lr => lr.MaterialId == materialId);
            rawMaterialBatchTest = rawMaterialBatchTest.Where(lr => lr.MaterialId == materialId);
        }

        if (filter.MaterialKind.HasValue)
        {
            materialStp = materialStp.Where(ms => ms.Material.Kind == filter.MaterialKind.Value);
            materialAnalyticalRawData = materialAnalyticalRawData.Where(m =>
                m.MaterialStandardTestProcedure.Material.Kind == filter.MaterialKind.Value
            );
            rawMaterialBatchTest = rawMaterialBatchTest.Where(rm =>
                rm.Material.Kind == filter.MaterialKind.Value
            );
        }
        return new QcDashboardDto
        {
            NumberOfStpRawMaterials = await materialStp.CountAsync(ms =>
                ms.Material.Kind == MaterialKind.Raw
            ),
            NumberOfStpPackingMaterials = await materialStp.CountAsync(ms =>
                ms.Material.Kind == MaterialKind.Package
            ),
            NumberOfStpProducts = await productStp.CountAsync(),
            NumberOfMaterialAnalyticalRawData = await materialAnalyticalRawData.CountAsync(m =>
                m.MaterialStandardTestProcedure.Material.Kind == MaterialKind.Raw
            ),
            NumberOfMaterialAnalyticalPackingData = await materialAnalyticalRawData.CountAsync(m =>
                m.MaterialStandardTestProcedure.Material.Kind == MaterialKind.Package
            ),
            NumberOfBatchTestCountRawMaterials = rawMaterialBatchTest.Count(rm =>
                rm.Material.Kind == MaterialKind.Raw
            ),
            NumberOfBatchTestPendingRawMaterials = await rawMaterialBatchTest.CountAsync(rm =>
                rm.Status == BatchStatus.Received
            ), //check this
            NumberOfBatchTestApprovedRawMaterials = await rawMaterialBatchTest.CountAsync(rm =>
                rm.Status == BatchStatus.Approved
            ),
            NumberOfBatchTestRejectedRawMaterials = await rawMaterialBatchTest.CountAsync(rm =>
                rm.Status == BatchStatus.Rejected
            ),
            NumberOfBulkProductAnalyticalRawData = await productAnalyticalRawData.CountAsync(p =>
                p.Stage == TestStage.Bulk
            ),
            NumberOfIntermediateProductAnalyticalRawData =
                await productAnalyticalRawData.CountAsync(p => p.Stage == TestStage.Intermediate),
            NumberOfFinishedProductAnalyticalRawData = await productAnalyticalRawData.CountAsync(
                p => p.Stage == TestStage.Finished
            ),
            NumberOfRawMaterialSpecifications = await materialStp.CountAsync(ms =>
                ms.Material.Kind == MaterialKind.Raw
            ),
            NumberOfPackingMaterialSpecifications = await materialStp.CountAsync(ms =>
                ms.Material.Kind == MaterialKind.Package
            ),
            NumberOfIntermediateProductSpecifications = await productStp.CountAsync(p =>
                productAnalyticalRawData
                    .Where(ar => ar.Stage == TestStage.Intermediate)
                    .Select(ar => ar.Id)
                    .Contains(p.ProductId)
            ),
            NumberOfBulkProductSpecifications = await productStp.CountAsync(p =>
                productAnalyticalRawData
                    .Where(ar => ar.Stage == TestStage.Bulk)
                    .Select(ar => ar.Id)
                    .Contains(p.ProductId)
            ),

            NumberOfFinishedProductSpecifications = await productStp.CountAsync(p =>
                productAnalyticalRawData
                    .Where(ar => ar.Stage == TestStage.Finished)
                    .Select(ar => ar.Id)
                    .Contains(p.ProductId)
            ),

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

    public async Task<Result<WarehouseReportDto>> GetWarehouseReport(
        ReportFilter filter,
        Guid departmentId
    )
    {
        var stockRequisitions = context
            .Requisitions.Where(r =>
                r.RequisitionType == RequisitionType.Stock && r.DepartmentId == departmentId
            )
            .AsQueryable();

        var incomingStockTransfers = context
            .StockTransferSources.Where(s => s.FromDepartmentId == departmentId)
            .AsQueryable();

        var shipments = context
            .ShipmentDocuments.AsSplitQuery()
            .Include(s => s.ShipmentInvoice)
                .ThenInclude(si => si.Items)
            .Where(s =>
                s.ShipmentInvoice.Items.Any(item =>
                    context
                        .PurchaseOrders.Where(po => po.Id == item.PurchaseOrderId)
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
            NumberOfNewStockRequisitions = await stockRequisitions.CountAsync(s =>
                s.Status == RequestStatus.New
            ),
            NumberOfInProgressStockRequisitions = await stockRequisitions.CountAsync(s =>
                s.Status == RequestStatus.Pending
            ),
            NumberOfCompletedStockRequisitions = await stockRequisitions.CountAsync(s =>
                s.Status == RequestStatus.Completed
            ),
            NumberOfIncomingStockTransfers = await incomingStockTransfers.CountAsync(),
            NumberOfIncomingPendingStockTransfers = await incomingStockTransfers.CountAsync(s =>
                s.Status == StockTransferStatus.InProgress
            ),
            NumberOfIncomingCompletedStockTransfers = await incomingStockTransfers.CountAsync(s =>
                s.Status == StockTransferStatus.Issued
            ),
            NumberOfShipments = await shipments.CountAsync(s => s.Type == DocType.Shipment),
            NumberOfInTransitShipments = await shipments.CountAsync(s =>
                s.Type == DocType.Shipment && s.Status == ShipmentStatus.InTransit
            ),
            NumberOfArrivedShipments = await shipments.CountAsync(s =>
                s.Type == DocType.Shipment && s.Status == ShipmentStatus.Arrived
            ),
            NumberOfClearedShipments = await shipments.CountAsync(s =>
                s.Type == DocType.Shipment && s.Status == ShipmentStatus.Cleared
            ),
        };
    }

    public async Task<
        Result<List<MaterialBatchReservedQuantityReportDto>>
    > GetReservedMaterialBatchesForDepartment(ReportFilter filter, Guid departmentId)
    {
        var department = await context.Departments.FirstOrDefaultAsync(d => d.Id == departmentId);
        if (department is null)
            return Error.NotFound("Department", "Department not found.");

        var materialBatchReserved = context
            .MaterialBatchReservedQuantities.AsSplitQuery()
            .Include(m => m.MaterialBatch)
                .ThenInclude(b => b.Material)
            .Include(m => m.Warehouse)
            .Where(m => m.Warehouse.DepartmentId == departmentId)
            .AsQueryable();

        if (filter.MaterialKind.HasValue)
        {
            materialBatchReserved = materialBatchReserved.Where(m =>
                m.MaterialBatch.Material.Kind == filter.MaterialKind
            );
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

        return await materialBatchReserved
            .Select(item => new MaterialBatchReservedQuantityReportDto
            {
                Warehouse = mapper.Map<CollectionItemDto>(item.Warehouse),
                Material = mapper.Map<CollectionItemDto>(item.MaterialBatch.Material),
                UoM = mapper.Map<UnitOfMeasureDto>(item.UoM),
                Quantity = item.Quantity,
            })
            .ToListAsync();
    }

    public async Task<Result<List<MaterialBatchDto>>> GetMaterialsReadyForAssignment(
        ReportFilter filter,
        Guid departmentId
    )
    {
        var query = context
            .MaterialBatches.AsSplitQuery()
            .Include(m =>
                m.Checklist.DistributedRequisitionMaterial.WarehouseArrivalLocation.Warehouse
            )
            .Include(m => m.Material)
            .Where(m =>
                m.Checklist
                    .DistributedRequisitionMaterial
                    .WarehouseArrivalLocation
                    .Warehouse
                    .DepartmentId == departmentId
                && m.Status == BatchStatus.Approved
            )
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

        var billingSheets = context
            .BillingSheets.AsSplitQuery()
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
            NumberOfNewShipments = await shipments.CountAsync(s =>
                s.Type == DocType.Shipment && s.Status == ShipmentStatus.New
            ),
            NumberOfInTransitShipments = await shipments.CountAsync(s =>
                s.Type == DocType.Shipment && s.Status == ShipmentStatus.InTransit
            ),
            NumberOfArrivedShipments = await shipments.CountAsync(s =>
                s.Type == DocType.Shipment && s.Status == ShipmentStatus.Arrived
            ),
            NumberOfClearedShipments = await shipments.CountAsync(s =>
                s.Type == DocType.Shipment && s.Status == ShipmentStatus.Cleared
            ),
            NumberOfBillingSheets = await billingSheets.CountAsync(),
            NumberOfPaidBillingSheets = await billingSheets.CountAsync(b =>
                b.Status == BillingSheetStatus.Paid || b.Invoice.PaidAt.HasValue
            ),
            NumberOfPendingBillingSheets = await billingSheets.CountAsync(b =>
                b.Status == BillingSheetStatus.Pending
            ),
            NumberOfWaybills = await shipments.CountAsync(s => s.Type == DocType.Waybill),
            NumberOfNewWaybills = await shipments.CountAsync(s =>
                s.Type == DocType.Waybill && s.Status == ShipmentStatus.New
            ),
            NumberOfInTransitWaybills = await shipments.CountAsync(s =>
                s.Type == DocType.Waybill && s.Status == ShipmentStatus.InTransit
            ),
            NumberOfArrivedWaybills = await shipments.CountAsync(s =>
                s.Type == DocType.Waybill && s.Status == ShipmentStatus.Arrived
            ),
            NumberOfClearedWaybills = await shipments.CountAsync(s =>
                s.Type == DocType.Waybill && s.Status == ShipmentStatus.Cleared
            ),
        };
    }

    public async Task<
        Result<List<FinishedGoodsTransferSummaryReportDto>>
    > GetFinishedGoodsTransferSummaryReport(
        ReportFilter filter,
        Guid? productId = null,
        Guid? warehouseId = null
    )
    {
        var query = context
            .FinishedGoodsTransferNotes.AsNoTracking()
            
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
            query = query.Where(f =>
                f.ProductPacking != null && f.ProductPacking.ProductId == productId.Value
            );

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
                f.TotalQuantity,
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
                    : (DateTime?)null,
            })
            .Where(x => x.ProductName != null)
            .ToListAsync();

        if (rawData.Count == 0)
        {
            return Result.Success(new List<FinishedGoodsTransferSummaryReportDto>());
        }

        var groupedData = rawData
            .GroupBy(x => new
            {
                x.ProductName,
                x.ProductCode,
                x.UomName,
                x.ProductionDepartment,
                x.DestinationWarehouse,
            })
            .Select(g => new
            {
                g.Key,
                NumberOfBatches = g.Where(x =>
                        !string.IsNullOrEmpty(x.BatchNumber) && x.BatchNumber != "No Batch"
                    )
                    .Select(x => x.BatchNumber)
                    .Distinct()
                    .Count(),
                TotalQuantity = g.Sum(x => x.TotalQuantity),
                EarliestTransferDate = g.Min(x => x.TransferDate),
                LatestAcceptedDate = g.Max(x => x.AcceptedDate),
            })
            .OrderBy(g => g.Key.ProductName)
            .ThenBy(g => g.Key.ProductCode)
            .ToList();

        // Map to DTO
        var result = groupedData
            .Select(
                (g, index) =>
                    new FinishedGoodsTransferSummaryReportDto
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
                        AcceptedDate = g.LatestAcceptedDate,
                    }
            )
            .ToList();

        return Result.Success(result);
    }

    public async Task<
        Result<List<FinishedGoodsTransferDetailedReportDto>>
    > GetFinishedGoodsTransferDetailedReport(
        ReportFilter filter,
        Guid? productId = null,
        Guid? warehouseId = null
    )
    {
        var query = context
            .FinishedGoodsTransferNotes.AsNoTracking()
            
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
            query = query.Where(f =>
                f.ProductPacking != null && f.ProductPacking.ProductId == productId.Value
            );

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
                    : null,

                ExpiryDate = f.BatchManufacturingRecord != null
                    ? f.BatchManufacturingRecord.ExpiryDate
                    : null,

                PackingStyle = f.ProductPacking != null ? f.ProductPacking.Name : "N/A",

                UomName = f.UoM != null ? f.UoM.Name : "N/A",

                ProductionDepartment = f.FromWarehouse != null
                    ? f.FromWarehouse.Name
                    : "Unknown Production Floor",

                DestinationWarehouse = f.ToWarehouse != null
                    ? f.ToWarehouse.Name
                    : "Unknown Warehouse",

                TransferDate = f.CreatedAt,

                AcceptedDate = f.AcceptedAt,
            })
            .Where(x => x.ProductName != null)
            .OrderBy(x => x.ProductName)
            .ThenBy(x => x.TransferDate)
            .ToListAsync();

        if (rawData.Count == 0)
        {
            return Result.Success(new List<FinishedGoodsTransferDetailedReportDto>());
        }

        var result = rawData
            .Select(
                (item, index) =>
                    new FinishedGoodsTransferDetailedReportDto
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
                    }
            )
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<ProductStockSummaryReportDto>>> GetProductStockSummaryReport(
        Guid? productId = null,
        Guid? warehouseId = null,
        Guid? departmentId = null
    )
    {
        var query = context
            .FinishedGoodsTransferNotes.AsNoTracking()
            
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
            .Where(f => f.IsApproved && !f.DeletedAt.HasValue);

        var notes = await query.Where(f => f.TotalQuantity - f.AllocatedQuantity > 0).ToListAsync();

        var filteredNotes = notes.AsEnumerable();

        if (productId.HasValue)
        {
            filteredNotes = filteredNotes.Where(f =>
                f.BatchManufacturingRecord?.ProductionScheduleProduct?.ProductId == productId.Value
                || f.ProductPacking?.ProductId == productId.Value
            );
        }

        if (warehouseId.HasValue)
        {
            filteredNotes = filteredNotes.Where(f => f.ToWarehouseId == warehouseId.Value);
        }

        if (departmentId.HasValue)
        {
            filteredNotes = filteredNotes.Where(f =>
                f.BatchManufacturingRecord?.ProductionScheduleProduct?.Product?.DepartmentId
                    == departmentId.Value
                || f.ProductPacking?.Product?.DepartmentId == departmentId.Value
            );
        }

        var rawData = filteredNotes
            .Select(f => new
            {
                Product = f.BatchManufacturingRecord?.ProductionScheduleProduct?.Product
                    ?? f.ProductPacking?.Product,
                Warehouse = f.ToWarehouse?.Name ?? "Unknown Warehouse",
                f.BatchManufacturingRecord?.BatchNumber,
                CurrentStockQuantity = f.RemainingQuantity,
                UomName = f.UoM?.Name ?? "N/A",
            })
            .Select(x => new
            {
                ProductName = x.Product?.Name,
                ProductCode = x.Product?.Code,
                ProductionDepartment = x.Product?.Department?.Name ?? "Unknown Department",
                x.Warehouse,
                x.BatchNumber,
                x.CurrentStockQuantity,
                x.UomName,
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
                x.UomName,
            })
            .Select(g => new
            {
                g.Key,
                NumberOfBatches = g.Where(x => !string.IsNullOrEmpty(x.BatchNumber))
                    .Select(x => x.BatchNumber)
                    .Distinct()
                    .Count(),
                TotalQuantity = g.Sum(x => x.CurrentStockQuantity),
            })
            .OrderBy(g => g.Key.ProductName)
            .ThenBy(g => g.Key.Warehouse)
            .ThenBy(g => g.Key.ProductionDepartment)
            .ToList();

        var result = groupedData
            .Select(
                (g, index) =>
                    new ProductStockSummaryReportDto
                    {
                        No = index + 1,
                        ProductName = g.Key.ProductName,
                        ProductCode = g.Key.ProductCode,
                        Warehouse = g.Key.Warehouse,
                        ProductionDepartment = g.Key.ProductionDepartment,
                        NumberOfBatches = g.NumberOfBatches,
                        TotalQuantity = g.TotalQuantity,
                        UomName = g.Key.UomName,
                    }
            )
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<ProductStockDetailedReportDto>>> GetProductStockDetailedReport(
        Guid? productId = null,
        Guid? warehouseId = null,
        Guid? departmentId = null,
        string batchNumber = null,
        DateTime? expiryDateFrom = null,
        DateTime? expiryDateTo = null
    )
    {
        var query = context
            .FinishedGoodsTransferNotes.AsNoTracking()
            
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
            .Where(f => f.IsApproved && !f.DeletedAt.HasValue);

        var notes = await query.Where(f => f.TotalQuantity - f.AllocatedQuantity > 0).ToListAsync();

        var filteredNotes = notes.AsEnumerable();

        if (productId.HasValue)
        {
            filteredNotes = filteredNotes.Where(f =>
                f.BatchManufacturingRecord?.ProductionScheduleProduct?.ProductId == productId.Value
                || f.ProductPacking?.ProductId == productId.Value
            );
        }

        if (warehouseId.HasValue)
        {
            filteredNotes = filteredNotes.Where(f => f.ToWarehouseId == warehouseId.Value);
        }

        if (departmentId.HasValue)
        {
            filteredNotes = filteredNotes.Where(f =>
                f.BatchManufacturingRecord?.ProductionScheduleProduct?.Product?.DepartmentId
                    == departmentId.Value
                || f.ProductPacking?.Product?.DepartmentId == departmentId.Value
            );
        }

        if (!string.IsNullOrWhiteSpace(batchNumber))
        {
            filteredNotes = filteredNotes.Where(f =>
                f.BatchManufacturingRecord?.BatchNumber.Contains(batchNumber) == true
            );
        }

        if (expiryDateFrom.HasValue)
        {
            filteredNotes = filteredNotes.Where(f =>
                f.BatchManufacturingRecord?.ExpiryDate >= expiryDateFrom.Value
            );
        }

        if (expiryDateTo.HasValue)
        {
            filteredNotes = filteredNotes.Where(f =>
                f.BatchManufacturingRecord?.ExpiryDate <= expiryDateTo.Value
            );
        }

        var rawData = filteredNotes
            .Select(f => new
            {
                Product = f.BatchManufacturingRecord?.ProductionScheduleProduct?.Product
                    ?? f.ProductPacking?.Product,
                BatchNumber = f.BatchManufacturingRecord?.BatchNumber ?? "No Batch",
                f.BatchManufacturingRecord?.ManufacturingDate,
                f.BatchManufacturingRecord?.ExpiryDate,
                TotalQuantity = f.RemainingQuantity,
                UomName = f.UoM?.Name ?? "N/A",
                Warehouse = f.ToWarehouse?.Name ?? "Unknown Warehouse",
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
                x.Warehouse,
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
            .Select(
                (item, index) =>
                    new ProductStockDetailedReportDto
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
                        Warehouse = item.Warehouse,
                    }
            )
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<ItemDto>>> GetItemsPerStoreType(
        Store? store,
        InventoryClassification? inventoryClassification,
        Guid? itemId,
        Guid? categoryId
    )
    {
        var query = context.Items.AsQueryable();

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

                ItemCategory =
                    i.ItemCategory == null
                        ? null
                        : new ItemCategoryDto
                        {
                            Id = i.ItemCategory.Id,
                            Name = i.ItemCategory.Name,
                        },

                UnitOfMeasure =
                    i.UnitOfMeasure == null
                        ? null
                        : new UnitOfMeasureDto
                        {
                            Id = i.UnitOfMeasure.Id,
                            Name = i.UnitOfMeasure.Name,
                        },
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
        var raw = await context
            .Items.AsNoTracking()
            .GroupBy(i => new
            {
                i.Store,
                i.Id,
                i.Name,
                i.Code,
                CategoryName = i.ItemCategory.Name,
                UomName = i.UnitOfMeasure.Name,
            })
            .Select(g => new
            {
                g.Key.Store,
                g.Key.Id,
                g.Key.Name,
                g.Key.Code,
                g.Key.CategoryName,
                g.Key.UomName,
                TotalQuantity = g.Sum(x => x.AvailableQuantity),
            })
            .OrderBy(r => r.Store)
            .ThenBy(r => r.Name)
            .ToListAsync();

        var result = raw.Select(
                (r, index) =>
                    new StoreItemStockSummaryDto
                    {
                        No = index + 1,
                        Store = r.Store,
                        ItemName = r.Name,
                        ItemCode = r.Code,
                        Category = r.CategoryName,
                        TotalQuantity = r.TotalQuantity,
                        UnitOfMeasure = r.UomName,
                    }
            )
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<VendorStoreItemStockSummaryDto>>> GetVendorItemMapping(
        Store? store,
        Guid? vendorId,
        Guid? itemId,
        Guid? categoryId,
        InventoryClassification? classification
    )
    {
        var query = context.VendorItems.AsNoTracking().AsQueryable();

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
                VendorName = v.Vendor.Name,
            })
            .OrderBy(r => r.Store)
            .ThenBy(r => r.ItemName)
            .ThenBy(r => r.ItemCode)
            .ThenBy(r => r.VendorName)
            .ToListAsync();

        var result = raw.Select(
                (r, index) =>
                    new VendorStoreItemStockSummaryDto
                    {
                        No = index + 1,

                        Store = r.Store,
                        ItemName = r.ItemName,
                        ItemCode = r.ItemCode,
                        Category = r.Category,
                        InventoryClassification = r.Classification,
                        UnitOfMeasure = r.UnitOfMeasure,
                        VendorName = r.VendorName,
                    }
            )
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<DashboardKpiReportDto>> GetDashboardKpiReport(
        DashboardFilterDto filter
    )
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

        var productQuery = context
            .Products.AsNoTracking()
            
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

        var customerQuery = context.Customers.AsNoTracking().Where(c => !c.DeletedAt.HasValue);

        if (startDate.HasValue)
            customerQuery = customerQuery.Where(c => c.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            customerQuery = customerQuery.Where(c => c.CreatedAt <= endDate.Value);

        var totalCustomers = await customerQuery.CountAsync();

        var productionOrderQuery = context
            .ProductionOrders.AsNoTracking()
            .Include(po => po.Products)
            .Where(po => !po.DeletedAt.HasValue);

        if (filter.DepartmentId.HasValue)
        {
            productionOrderQuery = productionOrderQuery.Where(po =>
                po.Products.Any(p =>
                    p.Product != null && p.Product.DepartmentId == filter.DepartmentId.Value
                )
            );
        }
        if (filter.ProductId.HasValue)
        {
            productionOrderQuery = productionOrderQuery.Where(po =>
                po.Products.Any(p => p.ProductId == filter.ProductId.Value)
            );
        }

        if (startDate.HasValue)
            productionOrderQuery = productionOrderQuery.Where(po =>
                po.CreatedAt >= startDate.Value
            );

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

        var totalProductionOrders =
            pendingOrders + partialPackingReadyOrders + fullPackingReadyOrders;

        var fgtnQuery = context
            .FinishedGoodsTransferNotes
            .Include(f => f.Approvals)
            .Include(f => f.UoM)
            .Include(f => f.ProductPacking)
                .ThenInclude(pp => pp.Product)
            .Include(f => f.BatchManufacturingRecord)
                .ThenInclude(bmr => bmr.ProductionScheduleProduct)
                    .ThenInclude(psp => psp.Product)
            .Where(f => !f.DeletedAt.HasValue);

        if (filter.DepartmentId.HasValue)
        {
            fgtnQuery = fgtnQuery.Where(f =>
                (
                    f.ProductPacking != null
                    && f.ProductPacking.Product != null
                    && f.ProductPacking.Product.DepartmentId == filter.DepartmentId.Value
                )
                || (
                    f.BatchManufacturingRecord != null
                    && f.BatchManufacturingRecord.ProductionScheduleProduct != null
                    && f.BatchManufacturingRecord.ProductionScheduleProduct.Product != null
                    && f.BatchManufacturingRecord.ProductionScheduleProduct.Product.DepartmentId
                        == filter.DepartmentId.Value
                )
            );
        }
        if (filter.ProductId.HasValue)
        {
            fgtnQuery = fgtnQuery.Where(f =>
                (f.ProductPacking != null && f.ProductPacking.ProductId == filter.ProductId.Value)
                || (
                    f.BatchManufacturingRecord != null
                    && f.BatchManufacturingRecord.ProductionScheduleProduct != null
                    && f.BatchManufacturingRecord.ProductionScheduleProduct.ProductId
                        == filter.ProductId.Value
                )
            );
        }

        if (filter.MaterialId.HasValue)
        {
            fgtnQuery = fgtnQuery.Where(f =>
                f.BatchManufacturingRecord != null
                && f.BatchManufacturingRecord.ProductionScheduleProduct != null
                && f.BatchManufacturingRecord.ProductionScheduleProduct.Product != null
                && f.BatchManufacturingRecord.ProductionScheduleProduct.Product.BillOfMaterials.Any(
                    bom => bom.BillOfMaterialId == filter.MaterialId.Value
                )
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

        var inventorySummary = await fgtnQuery
            .Where(f => f.IsApproved && f.TotalQuantity - f.AllocatedQuantity > 0)
            .GroupBy(f => f.UoM == null ? "Unspecified" : f.UoM.Symbol)
            .Select(g => new FgtnInventoryQuantityDto
            {
                Uom = g.Key,
                AvailableQuantity = g.Sum(f => f.TotalQuantity - f.AllocatedQuantity),
                BatchCount = g.Select(f => f.BatchManufacturingRecordId).Distinct().Count(),
            })
            .OrderBy(x => x.Uom)
            .ToListAsync();

        var dispatchQuery = context
            .DistributedFinishedProducts.IgnoreQueryFilters()
            .Where(d => d.DeletedAt == null);

        if (filter.DepartmentId.HasValue)
            dispatchQuery = dispatchQuery.Where(d =>
                d.Product != null && d.Product.DepartmentId == filter.DepartmentId.Value
            );

        if (filter.ProductId.HasValue)
            dispatchQuery = dispatchQuery.Where(d => d.ProductId == filter.ProductId.Value);

        if (filter.MaterialId.HasValue)
            dispatchQuery = dispatchQuery.Where(d =>
                d.Product != null
                && d.Product.BillOfMaterials.Any(bom =>
                    bom.BillOfMaterialId == filter.MaterialId.Value
                )
            );

        if (startDate.HasValue)
            dispatchQuery = dispatchQuery.Where(d => d.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            dispatchQuery = dispatchQuery.Where(d => d.CreatedAt <= endDate.Value);

        var awaitingArrival = await dispatchQuery.CountAsync(d =>
            d.Status == DistributedFinishedProductStatus.Distributed
        );
        var arrived = await dispatchQuery.CountAsync(d =>
            d.Status == DistributedFinishedProductStatus.Arrived
        );

        var dashboardKpi = new DashboardKpiReportDto
        {
            ProductCount = new ProductCountKpiDto
            {
                BetaProducts = betaProductCount,
                NonBetaProducts = nonBetaProductCount,
                TotalProducts = totalProductCount,
            },
            TotalCustomers = totalCustomers,
            ProductionOrders = new ProductionOrderKpiDto
            {
                PendingProductionOrders = pendingOrders,
                PartialPackingReady = partialPackingReadyOrders,
                FullPackingReady = fullPackingReadyOrders,
                TotalProductionOrders = totalProductionOrders,
            },
            FinishedGoodsTransferNotes = new FgtnKpiDto
            {
                PendingTransferNote = pendingFgtn,
                AcceptedTransferNote = acceptedFgtn,
                TotalFgtnTransferNotes = totalFgtn,
            },
            InventorySummary = inventorySummary,
            DispatchPipeline = new FgtnDispatchPipelineDto
            {
                AwaitingArrival = awaitingArrival,
                Arrived = arrived,
                Total = awaitingArrival + arrived,
            },
        };

        return Result.Success(dashboardKpi);
    }

    public async Task<Result<List<SupplierMaterialReportDto>>> GetSupplierMaterialAReport(
        SupplierMaterialFilters filters
    )
    {
        var baseQuery = context
            .SupplierManufacturers.AsNoTracking()
            
            .Include(sm => sm.UoM)
            .Include(sm => sm.Manufacturer)
            .Where(sm => sm.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(filters.MaterialName))
            baseQuery = baseQuery.Where(sm =>
                sm.Material != null && sm.Material.Name.Contains(filters.MaterialName)
            );

        if (filters.MaterialType.HasValue)
            baseQuery = baseQuery.Where(sm =>
                sm.Material != null && sm.Material.Kind == filters.MaterialType.Value
            );

        if (!string.IsNullOrWhiteSpace(filters.SupplierName))
            baseQuery = baseQuery.Where(sm =>
                sm.Supplier != null && sm.Supplier.Name.Contains(filters.SupplierName)
            );

        if (!string.IsNullOrWhiteSpace(filters.ManufacturerName))
            baseQuery = baseQuery.Where(sm =>
                sm.Manufacturer != null && sm.Manufacturer.Name.Contains(filters.ManufacturerName)
            );

        if (filters.SupplierId.HasValue)
            baseQuery = baseQuery.Where(sm => sm.SupplierId == filters.SupplierId.Value);

        if (filters.ManufacturerId.HasValue)
            baseQuery = baseQuery.Where(sm => sm.ManufacturerId == filters.ManufacturerId.Value);

        if (filters.ValidityDateFrom.HasValue)
            baseQuery = baseQuery.Where(sm =>
                sm.Manufacturer.ValidityDate >= filters.ValidityDateFrom.Value
            );

        if (filters.ValidityDateTo.HasValue)
            baseQuery = baseQuery.Where(sm =>
                sm.Manufacturer.ValidityDate <= filters.ValidityDateTo.Value
            );

        var groups = await baseQuery
            .GroupBy(sm => new
            {
                sm.SupplierId,
                sm.ManufacturerId,
                sm.UoMId,
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
                    .ToList(),
            })
            .ToListAsync();

        if (groups.Count == 0)
            return Result.Success(new List<SupplierMaterialReportDto>());

        var manufacturerIds = groups.Select(g => g.ManufacturerId).Distinct().ToList();

        var manufacturerMaterials = await context
            .ManufacturerMaterials.AsNoTracking()
            
            .Where(mm => mm.DeletedAt == null && manufacturerIds.Contains(mm.ManufacturerId))
            .ToListAsync();

        var materialLookup = manufacturerMaterials
            .GroupBy(mm => mm.ManufacturerId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = groups
            .Select(g =>
            {
                var materialIdSet = g.MaterialIds.ToHashSet();

                materialLookup.TryGetValue(g.ManufacturerId, out var materialsForManufacturer);

                return new SupplierMaterialReportDto
                {
                    Supplier = mapper.Map<SupplierListDto>(g.Supplier),
                    Manufacturers = mapper.Map<ManufacturerListDto>(g.Manufacturer),
                    Uom = mapper.Map<UnitOfMeasureDto>(g.Uom),
                    Materials = mapper.Map<List<ManufacturerMaterialDto>>(
                        (object)
                            materialsForManufacturer
                                ?.Where(mm => materialIdSet.Contains(mm.MaterialId))
                                .ToList()
                            ?? new List<ManufacturerMaterialDto>()
                    ),
                };
            })
            .ToList();

        return Result.Success(result);
    }

    public async Task<
        Result<List<VendorItemStoreSummaryDto>>
    > GetVendorItemMappingPerStoreTypeSummary(
        Guid? itemId,
        Guid? categoryId,
        InventoryClassification? classification,
        Store? store
    )
    {
        var query = context.VendorItems.AsNoTracking().AsQueryable();

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
                v.Item.Classification,
            })
            .Select(g => new
            {
                g.Key.Store,
                g.Key.Name,
                g.Key.Code,
                g.Key.Category,
                g.Key.Classification,
                VendorCount = g.Select(x => x.VendorId).Distinct().Count(),
            })
            .OrderBy(r => r.Store)
            .ThenBy(r => r.Name)
            .ToListAsync();

        var result = raw.Select(
                (r, index) =>
                    new VendorItemStoreSummaryDto
                    {
                        No = index + 1,
                        Store = r.Store,
                        ItemName = r.Name,
                        ItemCode = r.Code,
                        Category = r.Category,
                        Classification = r.Classification,
                        VendorCount = r.VendorCount,
                    }
            )
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<ShipmentReportDto>>> GetShipmentReport(
        ShipmentReportFilter filter
    )
    {
        var baseQuery = context
            .BillingSheets.AsNoTracking()
            
            .Where(bs => bs.DeletedAt == null);
        if (filter.StartDate.HasValue)
        {
            baseQuery = baseQuery.Where(bs => bs.CreatedAt >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            baseQuery = baseQuery.Where(bs => bs.CreatedAt <= filter.EndDate.Value);
        }
        if (filter.SupplierIds != null && filter.SupplierIds.Any())
        {
            baseQuery = baseQuery.Where(bs =>
                bs.SupplierId.HasValue && filter.SupplierIds.Contains(bs.SupplierId.Value)
            );
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
                bs.InvoiceId,
            })
            .Select(g => new
            {
                BillingSheetId = g.Key.Id,
                g.Key.SupplierId,
                g.Key.InvoiceId,
                BillingSheet = g.First(),
                Supplier = g.Select(x => x.Supplier).FirstOrDefault(),
                Invoice = g.Select(x => x.Invoice).FirstOrDefault(),
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
        var purchaseOrders = await context
            .PurchaseOrders.AsNoTracking()
            
            .Include(po => po.TermsOfPayment)
            .Where(po => po.DeletedAt == null && supplierIds.Contains(po.SupplierId))
            .Select(po => new
            {
                po.Id,
                po.SupplierId,
                po.CreatedAt,
                TermsOfPaymentName = po.TermsOfPayment != null ? po.TermsOfPayment.Name : null,
                po.TotalCifValue,
            })
            .ToListAsync();

        var purchaseOrderIds = purchaseOrders.Select(po => po.Id).ToList();

        var purchaseOrderItems = await context
            .PurchaseOrderItems.AsNoTracking()
            
            .Where(poi => poi.DeletedAt == null && purchaseOrderIds.Contains(poi.PurchaseOrderId))
            .Select(poi => new { poi.PurchaseOrderId, poi.MaterialId })
            .ToListAsync();

        var materialIds = purchaseOrderItems.Select(poi => poi.MaterialId).Distinct().ToList();

        var materials = await context
            .Materials.AsNoTracking()
            
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

        var result = groups
            .Select(
                (g, index) =>
                {
                    List<string> materialNames = new List<string>();
                    string transactionType = "N/A";
                    decimal transactionAmount = 0;

                    if (
                        g.SupplierId.HasValue
                        && poLookup.TryGetValue(g.SupplierId.Value, out var supplierPOs)
                    )
                    {
                        var recentPo = supplierPOs
                            .OrderByDescending(po => po.CreatedAt)
                            .FirstOrDefault();
                        if (recentPo != null)
                        {
                            transactionAmount = recentPo.TotalCifValue;
                            transactionType = recentPo.TermsOfPaymentName;

                            if (poiLookup.TryGetValue(recentPo.Id, out var matIds))
                            {
                                materialNames = matIds
                                    .Select(id => materialLookup.GetValueOrDefault(id))
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
                        Status = g.BillingSheet.Status.ToString(),
                    };
                }
            )
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<PurchaseOrderReportDto>>> GetPurchaseOrderReportAsync(
        PurchaseOrderFilter filter
    )
    {
        var baseQuery = context
            .PurchaseOrders.AsNoTracking()
            
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
            baseQuery = baseQuery.Where(po => filter.PoNumber == po.Code);
        }
        if (filter.SupplierType.HasValue)
        {
            baseQuery = baseQuery.Where(po => po.Supplier.Type == filter.SupplierType.Value);
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
            })
            .ToListAsync();
        var purchaseOrderIds = purchaseOrders.Select(o => o.Id).ToList();
        var supplierIds = purchaseOrders.Select(o => o.SupplierId).Distinct().ToList();
        var suppliers = await context
            .Suppliers.AsNoTracking()
            
            .Where(s => supplierIds.Contains(s.Id) && s.DeletedAt == null)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.CurrencyId,
            })
            .ToListAsync();
        var supplierLookup = suppliers.ToDictionary(s => s.Id);

        var purchaseOrderItems = await context
            .PurchaseOrderItems.AsNoTracking()
            
            .Where(poi => poi.DeletedAt == null && purchaseOrderIds.Contains(poi.PurchaseOrderId))
            .Select(poi => new
            {
                poi.PurchaseOrderId,
                poi.MaterialId,
                poi.Quantity,
                poi.Price,
                poi.UoMId,
                poi.CurrencyId,
                poi.PriceUoM,
            })
            .ToListAsync();
        var materialIds = purchaseOrderItems.Select(poi => poi.MaterialId).Distinct().ToList();
        var uomIds = purchaseOrderItems.Select(poi => poi.UoMId).Distinct().ToList();
        var currencyIds = suppliers
            .Where(s => s.CurrencyId.HasValue)
            .Select(s => s.CurrencyId.Value)
            .Distinct()
            .ToList();

        var materials = await context
            .Materials.AsNoTracking()
            
            .Where(m => materialIds.Contains(m.Id) && m.DeletedAt == null)
            .Select(m => new { m.Id, m.Name })
            .ToListAsync();
        var materialLookup = materials.ToDictionary(m => m.Id, m => m.Name);
        var uoms = await context
            .UnitOfMeasures.AsNoTracking()
            
            .Where(u => uomIds.Contains(u.Id) && u.DeletedAt == null)
            .Select(u => new { u.Id, u.Symbol })
            .ToListAsync();

        var uomLookup = uoms.ToDictionary(u => u.Id, u => u.Symbol);
        var currencies = await context
            .Currencies.AsNoTracking()
            
            .Where(c => currencyIds.Contains(c.Id) && c.DeletedAt == null)
            .Select(c => new { c.Id, c.Symbol })
            .ToListAsync();
        var currencyLookup = currencies.ToDictionary(c => c.Id, c => c.Symbol);

        var result = purchaseOrders
            .SelectMany(po =>
            {
                var items = purchaseOrderItems.Where(poi => poi.PurchaseOrderId == po.Id).ToList();

                return items.Select(poi => new { po, poi });
            })
            .Select(
                (x, index) =>
                {
                    var po = x.po;
                    var poi = x.poi;

                    return new PurchaseOrderReportDto
                    {
                        No = index + 1,

                        SupplierName = supplierLookup.TryGetValue(po.SupplierId, out var vendor)
                            ? vendor.Name
                            : null,

                        ProformaInvoiceNumber = po.ProFormaInvoiceNumber,
                        PurchaseOrderNumber = po.Code,

                        MaterialName = materialLookup.GetValueOrDefault(poi.MaterialId),

                        OrderQuantity = poi.Quantity,
                        UomName = uomLookup.GetValueOrDefault(poi.UoMId),

                        UnitPrice = poi.Price,
                        PriceUoM = poi.PriceUoM,
                        CurrencySymbol =
                            (
                                poi.CurrencyId
                                ?? (
                                    supplierLookup.TryGetValue(po.SupplierId, out var supplier)
                                        ? supplier.CurrencyId
                                        : null
                                )
                            )
                                is { } currencyId
                            && currencyLookup.TryGetValue(currencyId, out var cSymbol)
                                ? cSymbol
                                : null,

                        PurchaseOrderDate = po.CreatedAt,
                        ExpectedDeliverydate = po.ExpectedDeliveryDate,
                    };
                }
            )
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<PurchasedPoReportDto>>> GetPurchasedPoReportAsync(
        PurchaseOrderFilter filter
    )
    {
        var baseQuery = context
            .PurchaseOrders.AsNoTracking()
            
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
            baseQuery = baseQuery.Where(po => po.Code == filter.PoNumber);
        }
        if (filter.SupplierType.HasValue)
        {
            baseQuery = baseQuery.Where(po => po.Supplier.Type == filter.SupplierType.Value);
        }

        var purchaseOrders = await baseQuery
            .Select(po => new
            {
                po.Id,
                po.Code,
                po.ProFormaInvoiceNumber,
                po.SupplierId,
            })
            .ToListAsync();

        var proformaCodes = purchaseOrders
            .Where(po => !string.IsNullOrEmpty(po.ProFormaInvoiceNumber))
            .Select(po => po.ProFormaInvoiceNumber)
            .Distinct()
            .ToList();

        var shipmentInvoices = await context
            .ShipmentInvoices.AsNoTracking()
            
            .Where(si => si.DeletedAt == null && proformaCodes.Contains(si.Code))
            .Select(si => new
            {
                si.Code,
                si.SupplierId,
                si.CreatedAt,
                si.CurrencyId,
            })
            .ToListAsync();

        var shipmentLookup = shipmentInvoices.ToDictionary(si => si.Code);

        purchaseOrders = purchaseOrders
            .Where(po =>
                po.ProFormaInvoiceNumber != null
                && shipmentLookup.ContainsKey(po.ProFormaInvoiceNumber)
            )
            .ToList();

        var purchaseOrderIds = purchaseOrders.Select(po => po.Id).ToList();
        var supplierIds = purchaseOrders.Select(po => po.SupplierId).Distinct().ToList();

        var suppliers = await context
            .Suppliers.AsNoTracking()
            
            .Where(s => supplierIds.Contains(s.Id))
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Type,
                s.CurrencyId,
            })
            .ToListAsync();

        var supplierLookup = suppliers.ToDictionary(s => s.Id);

        var purchaseOrderItems = await context
            .PurchaseOrderItems.AsNoTracking()
            
            .Where(poi => poi.DeletedAt == null && purchaseOrderIds.Contains(poi.PurchaseOrderId))
            .Select(poi => new
            {
                poi.PurchaseOrderId,
                poi.MaterialId,
                poi.Quantity,
                poi.Price,
                poi.UoMId,
                poi.CurrencyId,
                poi.QuantityInvoiced,
                poi.PriceUoM,
            })
            .ToListAsync();

        var materialIds = purchaseOrderItems.Select(poi => poi.MaterialId).Distinct().ToList();
        var uomIds = purchaseOrderItems.Select(poi => poi.UoMId).Distinct().ToList();

        var currencyIds = suppliers
            .Where(s => s.CurrencyId.HasValue)
            .Select(s => s.CurrencyId.Value)
            .Distinct()
            .ToList();

        var materials = await context
            .Materials.AsNoTracking()
            
            .Where(m => materialIds.Contains(m.Id))
            .Select(m => new { m.Id, m.Name })
            .ToListAsync();

        var materialLookup = materials.ToDictionary(m => m.Id, m => m.Name);

        var uoms = await context
            .UnitOfMeasures.AsNoTracking()
            
            .Where(u => uomIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Symbol })
            .ToListAsync();

        var uomLookup = uoms.ToDictionary(u => u.Id, u => u.Symbol);

        var currencies = await context
            .Currencies.AsNoTracking()
            
            .Where(c => currencyIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Symbol })
            .ToListAsync();

        var currencyLookup = currencies.ToDictionary(c => c.Id, c => c.Symbol);

        var result = purchaseOrders
            .SelectMany(po =>
            {
                var invoice = shipmentLookup[po.ProFormaInvoiceNumber];

                var items = purchaseOrderItems.Where(poi => poi.PurchaseOrderId == po.Id).ToList();

                return items.Select(poi => new
                {
                    po,
                    poi,
                    invoice,
                });
            })
            .Select(
                (x, index) =>
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

                        MaterialName = materialLookup.GetValueOrDefault(poi.MaterialId),

                        OrderedQuantity = poi.Quantity,
                        OrderedUom = uomLookup.GetValueOrDefault(poi.UoMId),

                        QuantityReceived = poi.QuantityInvoiced,
                        ReceivedUom = uomLookup.GetValueOrDefault(poi.UoMId),

                        UnitCost = poi.Price,
                        PriceUoM = poi.PriceUoM,
                        CurrencySymbol =
                            (poi.CurrencyId ?? supplier?.CurrencyId) is { } currencyId
                            && currencyLookup.TryGetValue(currencyId, out var cSymbol)
                                ? cSymbol
                                : null,

                        InvoiceDate = invoice.CreatedAt,
                    };
                }
            )
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<ProductionDashboardDto>> GetProductionDashboard(Guid departmentId)
    {
        var requisitionQuery = context
            .Requisitions
            .Where(r => r.DepartmentId == departmentId && r.DeletedAt == null);

        var requisitionCounts = await requisitionQuery
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var requisitionReport = new RequisitionReportDto
        {
            NewRequisitionsCount =
                requisitionCounts.FirstOrDefault(x => x.Status == RequestStatus.New)?.Count ?? 0,
            PendingRequisitionsCount =
                requisitionCounts.FirstOrDefault(x => x.Status == RequestStatus.Pending)?.Count
                ?? 0,
            CompletedRequisitionsCount =
                requisitionCounts.FirstOrDefault(x => x.Status == RequestStatus.Completed)?.Count
                ?? 0,
            SourcedRequisitionsCount =
                requisitionCounts.FirstOrDefault(x => x.Status == RequestStatus.Sourced)?.Count
                ?? 0,
            RejectedRequisitionsCount =
                requisitionCounts.FirstOrDefault(x => x.Status == RequestStatus.Rejected)?.Count
                ?? 0,
        };

        var materialDepartments = await context
            .MaterialDepartments.AsNoTracking()
            
            .Where(md => md.DepartmentId == departmentId && md.DeletedAt == null)
            .Select(md => new
            {
                md.MaterialId,
                md.ReOrderLevel,
                UomSymbol = md.UoM != null ? md.UoM.Symbol : null,
            })
            .ToListAsync();

        var materialIds = materialDepartments.Select(md => md.MaterialId).Distinct().ToList();

        var materials = await context
            .Materials.AsNoTracking()
            
            .Where(m => materialIds.Contains(m.Id))
            .Select(m => new
            {
                m.Id,
                m.Name,
                m.Code,
            })
            .ToListAsync();

        var materialLookup = materials.ToDictionary(m => m.Id);

        var shelfQuantities = await context
            .ShelfMaterialBatches.AsNoTracking()
            
            .Where(smb =>
                smb.DeletedAt == null && materialIds.Contains(smb.MaterialBatch.MaterialId)
            )
            .GroupBy(smb => smb.MaterialBatch.MaterialId)
            .Select(g => new
            {
                MaterialId = g.Key,
                TotalQuantity = g.Sum(x => (decimal?)x.Quantity) ?? 0,
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

        var productionQuery = context
            .ProductionSchedules
            .Where(p => p.DepartmentId == departmentId && p.DeletedAt == null);

        var productionCounts = await productionQuery
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var productionReport = new ProductionScheduleStatusReportDto
        {
            NewScheduleCount =
                productionCounts.FirstOrDefault(x => x.Status == ProductionStatus.New)?.Count ?? 0,
            InProgressScheduleCount =
                productionCounts.FirstOrDefault(x => x.Status == ProductionStatus.InProgress)?.Count
                ?? 0,
            CompletedScheduleCount =
                productionCounts.FirstOrDefault(x => x.Status == ProductionStatus.Completed)?.Count
                ?? 0,
            DelayedScheduleCount =
                productionCounts.FirstOrDefault(x => x.Status == ProductionStatus.Delayed)?.Count
                ?? 0,
            CancelledScheduleCount =
                productionCounts.FirstOrDefault(x => x.Status == ProductionStatus.Cancelled)?.Count
                ?? 0,
        };

        var stockQuery = context
            .StockTransferSources
            .Where(s => s.FromDepartmentId == departmentId && s.DeletedAt == null);

        var stockCounts = await stockQuery
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var stockReport = new StockTransferStatusReportDto
        {
            InProgressCount =
                stockCounts.FirstOrDefault(x => x.Status == StockTransferStatus.InProgress)?.Count
                ?? 0,
            ApprovedCount =
                stockCounts.FirstOrDefault(x => x.Status == StockTransferStatus.Approved)?.Count
                ?? 0,
            IssuedCount =
                stockCounts.FirstOrDefault(x => x.Status == StockTransferStatus.Issued)?.Count ?? 0,
            RejectedCount =
                stockCounts.FirstOrDefault(x => x.Status == StockTransferStatus.Rejected)?.Count
                ?? 0,
        };

        var dashboard = new ProductionDashboardDto
        {
            RequisitionReport = requisitionReport,
            MaterialsBelowReorderLevel = materialReport,
            ProductionScheduleReport = productionReport,
            StockTransferReport = stockReport,
        };

        return Result.Success(dashboard);
    }

    public async Task<Result<ProcurementDashboardDto>> GetProcurementDashboard(DateFilter filter)
    {
        DateTime now = DateTime.UtcNow;
        DateTime? startDate = filter switch
        {
            DateFilter.Today => now.Date,
            DateFilter.ThisWeek => now.Date.AddDays(-(int)now.DayOfWeek),
            DateFilter.ThisMonth => now.Date.AddDays(1 - now.Day),
            _ => null,
        };

        // Purchase Orders
        var purchaseOrderCounts = await context
            .PurchaseOrders
            .Where(po => po.DeletedAt == null && (startDate == null || po.CreatedAt >= startDate))
            .GroupBy(po => po.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        var purchaseOrderLookup = purchaseOrderCounts.ToDictionary(x => x.Key, x => x.Count);

        var purchaseOrderReport = new PurchaseOrderStatusReportDto
        {
            NewCount = purchaseOrderLookup.GetValueOrDefault(PurchaseOrderStatus.New),
            PendingCount = purchaseOrderLookup.GetValueOrDefault(PurchaseOrderStatus.Pending),
            DeliveredCount = purchaseOrderLookup.GetValueOrDefault(PurchaseOrderStatus.Delivered),
            AttachedCount = purchaseOrderLookup.GetValueOrDefault(PurchaseOrderStatus.Attached),
            PendingCheckCount = purchaseOrderLookup.GetValueOrDefault(
                PurchaseOrderStatus.PendingCheck
            ),
            CheckedCount = purchaseOrderLookup.GetValueOrDefault(PurchaseOrderStatus.Checked),
            ApprovedCount = purchaseOrderLookup.GetValueOrDefault(PurchaseOrderStatus.Approved),
            CompletedCount = purchaseOrderLookup.GetValueOrDefault(PurchaseOrderStatus.Completed),
            PartiallyLinkedCount = purchaseOrderLookup.GetValueOrDefault(
                PurchaseOrderStatus.PartiallyLinked
            ),
            LinkedCount = purchaseOrderLookup.GetValueOrDefault(PurchaseOrderStatus.Linked),
            RevisedCount = purchaseOrderLookup.GetValueOrDefault(PurchaseOrderStatus.Revised),
            CancelledCount = purchaseOrderLookup.GetValueOrDefault(PurchaseOrderStatus.Cancelled),
        };

        // Requisitions
        var requisitionCounts = await context
            .Requisitions
            .Where(r => r.DeletedAt == null && (startDate == null || r.CreatedAt >= startDate))
            .GroupBy(r => r.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        var requisitionLookup = requisitionCounts.ToDictionary(x => x.Key, x => x.Count);

        var requisitionReport = new RequisitionReportDto
        {
            NewRequisitionsCount = requisitionLookup.GetValueOrDefault(RequestStatus.New),
            PendingRequisitionsCount = requisitionLookup.GetValueOrDefault(RequestStatus.Pending),
            CompletedRequisitionsCount = requisitionLookup.GetValueOrDefault(
                RequestStatus.Completed
            ),
            SourcedRequisitionsCount = requisitionLookup.GetValueOrDefault(RequestStatus.Sourced),
            RejectedRequisitionsCount = requisitionLookup.GetValueOrDefault(RequestStatus.Rejected),
        };

        // Material Distribution
        var distributionCounts = await context
            .DistributeMaterials
            .Where(dm => dm.DeletedAt == null && (startDate == null || dm.CreatedAt >= startDate))
            .GroupBy(dm => dm.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        var distributionLookup = distributionCounts.ToDictionary(x => x.Key, x => x.Count);

        var distributionReport = new MaterialDistributionStatusCountDto
        {
            PendingCount = distributionLookup.GetValueOrDefault(DistributeMaterialStatus.Pending),
            DistributedCount = distributionLookup.GetValueOrDefault(
                DistributeMaterialStatus.Distributed
            ),
        };

        // Supplier Quotation Items
        var quotationCounts = await context
            .SupplierQuotationItems
            .Where(sqi =>
                sqi.DeletedAt == null && (startDate == null || sqi.CreatedAt >= startDate)
            )
            .GroupBy(sqi => new { SupplierType = sqi.SupplierQuotation.Supplier.Type, sqi.Status })
            .Select(g => new
            {
                g.Key.SupplierType,
                g.Key.Status,
                Count = g.Count(),
            })
            .ToListAsync();

        var quotationLookup = quotationCounts.ToDictionary(
            x => (x.SupplierType, x.Status),
            x => x.Count
        );

        var supplierQuotationReport = new SupplierQuotationItemStatusReportDto
        {
            Local = new SupplierQuotationStatusCountDto
            {
                NotProcessedCount = quotationLookup.GetValueOrDefault(
                    (SupplierType.Local, SupplierQuotationItemStatus.NotProcessed)
                ),
                ProcessedCount = quotationLookup.GetValueOrDefault(
                    (SupplierType.Local, SupplierQuotationItemStatus.Processed)
                ),
                NotUsedCount = quotationLookup.GetValueOrDefault(
                    (SupplierType.Local, SupplierQuotationItemStatus.NotUsed)
                ),
            },
            Foreign = new SupplierQuotationStatusCountDto
            {
                NotProcessedCount = quotationLookup.GetValueOrDefault(
                    (SupplierType.Foreign, SupplierQuotationItemStatus.NotProcessed)
                ),
                ProcessedCount = quotationLookup.GetValueOrDefault(
                    (SupplierType.Foreign, SupplierQuotationItemStatus.Processed)
                ),
                NotUsedCount = quotationLookup.GetValueOrDefault(
                    (SupplierType.Foreign, SupplierQuotationItemStatus.NotUsed)
                ),
            },
        };

        return Result.Success(
            new ProcurementDashboardDto
            {
                RequisitionReport = requisitionReport,
                PurchaseOrderStatus = purchaseOrderReport,
                SalesQuotation = supplierQuotationReport,
                MaterialDistributionStatus = distributionReport,
            }
        );
    }

    public async Task<Result<WarehouseDashboardReportDto>> GetWarehouseDashboard(
        Guid departmentId,
        DateFilter filter
    )
    {
        DateTime now = DateTime.UtcNow;
        DateTime? startDate = filter switch
        {
            DateFilter.Today => now.Date,
            DateFilter.ThisWeek => now.Date.AddDays(-(int)now.DayOfWeek),
            DateFilter.ThisMonth => now.Date.AddDays(1 - now.Day),
            _ => null,
        };

        var baseQuery = context
            .StockTransferSources
            .Where(st => st.DeletedAt == null && (startDate == null || st.CreatedAt >= startDate));

        var incomingCounts = await baseQuery
            .Where(st => st.FromDepartmentId == departmentId)
            .GroupBy(st => st.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        var outgoingCounts = await baseQuery
            .Where(st => st.ToDepartmentId == departmentId)
            .GroupBy(st => st.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        var requisitionCounts = await context
            .Requisitions
            .Where(r =>
                r.DeletedAt == null
                && r.DepartmentId == departmentId
                && (startDate == null || r.CreatedAt >= startDate)
            )
            .GroupBy(r => r.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        var incomingLookup = incomingCounts.ToDictionary(x => x.Key, x => x.Count);
        var outgoingLookup = outgoingCounts.ToDictionary(x => x.Key, x => x.Count);
        var requisitionLookup = requisitionCounts.ToDictionary(x => x.Key, x => x.Count);

        return Result.Success(
            new WarehouseDashboardReportDto
            {
                StockTransfers = new StockTransferDashboardDto
                {
                    Incoming = new StockTransferStatusCountDto
                    {
                        InProgressCount = incomingLookup.GetValueOrDefault(
                            StockTransferStatus.InProgress
                        ),
                        ApprovedCount = incomingLookup.GetValueOrDefault(
                            StockTransferStatus.Approved
                        ),
                        IssuedCount = incomingLookup.GetValueOrDefault(StockTransferStatus.Issued),
                        RejectedCount = incomingLookup.GetValueOrDefault(
                            StockTransferStatus.Rejected
                        ),
                    },
                    Outgoing = new StockTransferStatusCountDto
                    {
                        InProgressCount = outgoingLookup.GetValueOrDefault(
                            StockTransferStatus.InProgress
                        ),
                        ApprovedCount = outgoingLookup.GetValueOrDefault(
                            StockTransferStatus.Approved
                        ),
                        IssuedCount = outgoingLookup.GetValueOrDefault(StockTransferStatus.Issued),
                        RejectedCount = outgoingLookup.GetValueOrDefault(
                            StockTransferStatus.Rejected
                        ),
                    },
                },
                StockRequisitions = new StockRequisitionStatusCountDto
                {
                    NewCount = requisitionLookup.GetValueOrDefault(RequestStatus.New),
                    PendingCount = requisitionLookup.GetValueOrDefault(RequestStatus.Pending),
                    SourcedCount = requisitionLookup.GetValueOrDefault(RequestStatus.Sourced),
                    CompletedCount = requisitionLookup.GetValueOrDefault(RequestStatus.Completed),
                    RejectedCount = requisitionLookup.GetValueOrDefault(RequestStatus.Rejected),
                },
            }
        );
    }

    public async Task<Result<List<ExpiredMaterialReportDto>>> GetExpiredMaterials(Guid departmentId)
    {
        var now = DateTime.UtcNow;

        var expiredMaterials = await context
            .ShelfMaterialBatches
            .Where(smb =>
                smb.DeletedAt == null
                && smb.Quantity > 0
                && smb.MaterialBatch.DeletedAt == null
                && smb.MaterialBatch.ExpiryDate <= now
                && smb.WarehouseLocationShelf
                    .WarehouseLocationRack
                    .WarehouseLocation
                    .Warehouse
                    .DepartmentId == departmentId
            )
            .Select(smb => new ExpiredMaterialReportDto
            {
                MaterialName = smb.MaterialBatch.Material.Name,
                MaterialCode = smb.MaterialBatch.Material.Code,
                BatchNumber = smb.MaterialBatch.BatchNumber,
                QuantityOnShelf = smb.Quantity,
                UomSymbol = smb.MaterialBatch.UoM.Symbol,
                ExpiryDate = smb.MaterialBatch.ExpiryDate,
                DateReceived = smb.MaterialBatch.DateReceived,
                ShelfName = smb.WarehouseLocationShelf.Name,
            })
            .OrderBy(x => x.ExpiryDate)
            .ToListAsync();

        return Result.Success(expiredMaterials);
    }

    public async Task<Result<List<ReservedMaterialReportDto>>> GetReservedMaterials(
        Guid? departmentId = null,
        Guid? materialId = null
    )
    {
        var reservedMaterialQuery = context
            .MaterialBatchReservedQuantities.AsSplitQuery()
            
            .Where(r =>
                r.DeletedAt == null
                && (!departmentId.HasValue || r.Warehouse.DepartmentId == departmentId.Value)
                && (!materialId.HasValue || r.MaterialBatch.MaterialId == materialId.Value)
            )
            .AsNoTracking();

        var reservedMaterials = await reservedMaterialQuery
            .Select(r => new ReservedMaterialReportDto
            {
                MaterialName = r.MaterialBatch.Material.Name,
                MaterialCode = r.MaterialBatch.Material.Code,
                ProductName = r.ProductionScheduleProduct.Product.Name,
                ProductCode = r.ProductionScheduleProduct.Product.Code,
                ReservedQuantity = r.Quantity,
                UomSymbol = r.UoM.Symbol,
                WarehouseName = r.Warehouse.Name,
                DepartmentName = r.Warehouse.Department.Name,
                DateTime = r.CreatedAt,
                Schedule = r.ProductionScheduleProduct.ProductionSchedule.Code,
                ProductBatchNumber = r.ProductionScheduleProduct.BatchNumber,
                ArNumber = context
                    .MaterialSamplings.Where(s => s.MaterialBatchId == r.MaterialBatchId)
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => s.ArNumber)
                    .FirstOrDefault(),
                ManufacturingDate = r.MaterialBatch.ManufacturingDate,
                ExpiryDate = r.MaterialBatch.ExpiryDate,
                BalanceQuantity =
                    r.MaterialBatch.TotalQuantity
                    - r.MaterialBatch.ConsumedQuantity
                    - r.MaterialBatch.ReservedQuantities.Where(rq => rq.DeletedAt == null)
                        .Sum(rq => rq.Quantity),
                MaterialBatchNumber = r.MaterialBatch.BatchNumber,
            })
            .ToListAsync();

        return Result.Success(reservedMaterials);
    }

    public async Task<Result<List<MaterialReorderReportDto>>> GetMaterialsBelowReorderLevel(
        Guid departmentId
    )
    {
        var materialDepartments = await context
            .MaterialDepartments
            .Where(md => md.DepartmentId == departmentId && md.DeletedAt == null)
            .Select(md => new
            {
                md.MaterialId,
                md.ReOrderLevel,
                UomSymbol = md.UoM.Symbol,
            })
            .ToListAsync();

        var materialIds = materialDepartments.Select(md => md.MaterialId).Distinct().ToList();

        var materials = await context
            .Materials.AsNoTracking()
            
            .Where(m => materialIds.Contains(m.Id))
            .Select(m => new
            {
                m.Id,
                m.Name,
                m.Code,
            })
            .ToListAsync();

        var materialLookup = materials.ToDictionary(m => m.Id);

        var shelfQuantities = await context
            .ShelfMaterialBatches.AsNoTracking()
            
            .Where(smb =>
                smb.DeletedAt == null && materialIds.Contains(smb.MaterialBatch.MaterialId)
            )
            .GroupBy(smb => smb.MaterialBatch.MaterialId)
            .Select(g => new
            {
                MaterialId = g.Key,
                TotalQuantity = g.Sum(x => (decimal?)x.Quantity) ?? 0,
            })
            .ToListAsync();

        var quantityLookup = shelfQuantities.ToDictionary(q => q.MaterialId, q => q.TotalQuantity);

        var report = materialDepartments
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

        return Result.Success(report);
    }

    public async Task<Result<MaterialsChecklistReportDto>> GetMaterialsChecklist(Guid departmentId)
    {
        var query = context
            .DistributedRequisitionMaterials.AsNoTracking()
            
            .Where(drm =>
                drm.DeletedAt == null
                && drm.WarehouseArrivalLocation.Warehouse.DepartmentId == departmentId
            );

        var allMaterials = await query
            .Select(drm => new
            {
                drm.Material.Name,
                drm.Material.Code,
                drm.Quantity,
                UomSymbol = drm.UoM.Symbol,
                drm.CheckedAt,
            })
            .ToListAsync();

        var report = new MaterialsChecklistReportDto
        {
            IncomingMaterials = allMaterials
                .Where(x => x.CheckedAt == null)
                .Select(x => new MaterialChecklistItemDto
                {
                    MaterialName = x?.Name,
                    MaterialCode = x?.Code,
                    Quantity = x?.Quantity ?? 0,
                    UomSymbol = x?.UomSymbol,
                })
                .ToList(),

            CheckedMaterials = allMaterials
                .Where(x => x.CheckedAt != null)
                .Select(x => new MaterialChecklistItemDto
                {
                    MaterialName = x.Name,
                    MaterialCode = x.Code,
                    Quantity = x.Quantity,
                    UomSymbol = x.UomSymbol,
                })
                .ToList(),
        };

        return Result.Success(report);
    }

    public async Task<Result<ShipmentStatusReportDto>> GetShipmentStatusReport(DateFilter filter)
    {
        DateTime now = DateTime.UtcNow;
        DateTime? startDate = filter switch
        {
            DateFilter.Today => now.Date,
            DateFilter.ThisWeek => now.Date.AddDays(-(int)now.DayOfWeek),
            DateFilter.ThisMonth => now.Date.AddDays(1 - now.Day),
            _ => null,
        };

        var shipmentCounts = await context
            .ShipmentDocuments
            .Where(sd => sd.DeletedAt == null && (startDate == null || sd.CreatedAt >= startDate))
            .GroupBy(sd => sd.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var lookup = shipmentCounts.ToDictionary(x => x.Status, x => x.Count);

        return Result.Success(
            new ShipmentStatusReportDto
            {
                NewCount = lookup.GetValueOrDefault(ShipmentStatus.New, 0),
                AtPortCount = lookup.GetValueOrDefault(ShipmentStatus.AtPort, 0),
                ClearedCount = lookup.GetValueOrDefault(ShipmentStatus.Cleared, 0),
                InTransitCount = lookup.GetValueOrDefault(ShipmentStatus.InTransit, 0),
                ArrivedCount = lookup.GetValueOrDefault(ShipmentStatus.Arrived, 0),
            }
        );
    }

    public async Task<Result<GeneralInventoryDashboardDto>> GetGeneralInventoryDashboard(
        DateFilter filter
    )
    {
        DateTime now = DateTime.UtcNow;
        DateTime? startDate = filter switch
        {
            DateFilter.Today => now.Date,
            DateFilter.ThisWeek => now.Date.AddDays(-(int)now.DayOfWeek),
            DateFilter.ThisMonth => now.Date.AddDays(1 - now.Day),
            _ => null,
        };
        var itemsCount = await context
            .Items
            .Where(i => i.DeletedAt == null && (startDate == null || i.CreatedAt >= startDate))
            .GroupBy(i => i.Store)
            .Select(g => new { Store = g.Key, Count = g.Count() })
            .ToListAsync();
        var itemsLookup = itemsCount.ToDictionary(x => x.Store, x => x.Count);
        var itemsDashboard = new ItemCountDto
        {
            EquipmentStoreCount = itemsLookup.GetValueOrDefault(Store.EquipmentStore, 0),
            GeneralSoreCount = itemsLookup.GetValueOrDefault(Store.GeneralStore, 0),
            ItStoreCount = itemsLookup.GetValueOrDefault(Store.ItStore, 0),
            ReagentStoreCount = itemsLookup.GetValueOrDefault(Store.ReagentStore, 0),
        };

        var itemRequisitionCount = await context
            .ItemStockRequisitions
            .Where(i => i.DeletedAt == null && (startDate == null || i.CreatedAt >= startDate))
            .GroupBy(i => i.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var requisitionLookup = itemRequisitionCount.ToDictionary(x => x.Status, x => x.Count);
        var itemStockRequisition = new ItemStockRequisitionCountDto
        {
            PartialCount = requisitionLookup.GetValueOrDefault(
                IssueItemStockRequisitionStatus.Partial,
                0
            ),
            PendingCount = requisitionLookup.GetValueOrDefault(
                IssueItemStockRequisitionStatus.Pending,
                0
            ),
            CompletedCount = requisitionLookup.GetValueOrDefault(
                IssueItemStockRequisitionStatus.Completed,
                0
            ),
        };

        var totalVendors = await context
            .Vendors
            .Where(v => v.DeletedAt == null)
            .CountAsync();

        var dashboard = new GeneralInventoryDashboardDto
        {
            ItemCounts = itemsDashboard,
            ItemStockRequisitions = itemStockRequisition,
            Totalvendors = totalVendors,
        };
        return Result.Success(dashboard);
    }

    public async Task<Result<List<ItemBelowReorderDto>>> GetItemBelowReorder()
    {
        var items = await context
            .Items.AsNoTracking()
            
            .Where(i => i.DeletedAt == null && i.IsActive && i.AvailableQuantity <= i.ReorderLevel)
            .Select(i => new ItemBelowReorderDto
            {
                ItemName = i.Name,
                Code = i.Code,
                CurrentQuantity = i.AvailableQuantity,
                ReorderLevel = i.ReorderLevel,
            })
            .OrderBy(i => i.ItemName)
            .ToListAsync();

        return Result.Success(items);
    }

    public async Task<Result<ServicesDashboardReportDto>> GetServicesDashboard(DateFilter filter)
    {
        DateTime now = DateTime.UtcNow;
        DateTime? startDate = filter switch
        {
            DateFilter.Today => now.Date,
            DateFilter.ThisWeek => now.Date.AddDays(-(int)now.DayOfWeek),
            DateFilter.ThisMonth => now.Date.AddDays(1 - now.Day),
            _ => null,
        };
        var totalService = await context
            .Services
            .Where(s => s.DeletedAt == null)
            .CountAsync();

        var jobrequisiition = await context
            .JobRequests
            .Where(j => j.DeletedAt == null)
            .GroupBy(j => j.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();
        var jobRequestLookup = jobrequisiition.ToDictionary(x => x.Status, x => x.Count);
        var jobRequisitionCount = new JobRequisitionCountDto
        {
            Pending = jobRequestLookup.GetValueOrDefault(JobRequestStatus.Pending, 0),
            SentToExternal = jobRequestLookup.GetValueOrDefault(JobRequestStatus.SentToExternal),
            Completed = jobRequestLookup.GetValueOrDefault(JobRequestStatus.Completed, 0),
            Acknowledged = jobRequestLookup.GetValueOrDefault(JobRequestStatus.Acknowledged, 0),
            Cancelled = jobRequestLookup.GetValueOrDefault(JobRequestStatus.Cancelled, 0),
            Approved = jobRequestLookup.GetValueOrDefault(JobRequestStatus.Approved, 0),
            Assigned = jobRequestLookup.GetValueOrDefault(JobRequestStatus.Assigned, 0),
            QuotationReceived = jobRequestLookup.GetValueOrDefault(
                JobRequestStatus.QuotationReceived,
                0
            ),
            ContractorSelected = jobRequestLookup.GetValueOrDefault(
                JobRequestStatus.ContractorSelected,
                0
            ),
            JobStarted = jobRequestLookup.GetValueOrDefault(JobRequestStatus.JobStarted, 0),
        };

        var contractorsCount = await context
            .ServiceProviders
            .Where(i => i.DeletedAt == null && (startDate == null || i.CreatedAt >= startDate))
            .CountAsync();

        var dashboard = new ServicesDashboardReportDto
        {
            TotalServiceCount = totalService,
            TotalContractorsCount = contractorsCount,
            JobRequisitionCount = jobRequisitionCount,
        };
        return Result.Success(dashboard);
    }

    public async Task<Result<List<InvoicedProductsSummaryReportDto>>> GetInvoicedProductsSummary(
        InvoicedProductFilters filters
    )
    {
        var invoicesQuery = context
            .ProformaInvoices.AsNoTracking()
            .AsSplitQuery()
            
            .Where(p => !p.DeletedAt.HasValue);

        if (filters.StartDate.HasValue)
            invoicesQuery = invoicesQuery.Where(p => p.CreatedAt >= filters.StartDate.Value);

        if (filters.EndDate.HasValue)
            invoicesQuery = invoicesQuery.Where(p => p.CreatedAt <= filters.EndDate.Value);

        if (filters.CustomerId.HasValue)
            invoicesQuery = invoicesQuery.Where(p =>
                p.AllocateProductionOrder.ProductionOrder.CustomerId == filters.CustomerId.Value
            );

        if (filters.ProductId.HasValue)
            invoicesQuery = invoicesQuery.Where(p =>
                p.Products.Any(pr => pr.ProductId == filters.ProductId.Value)
            );

        if (filters.WarehouseDivision.HasValue)
            invoicesQuery = invoicesQuery.Where(p =>
                p.Products.Any(pr => pr.Product.Division == filters.WarehouseDivision.Value)
            );

        var invoices = await invoicesQuery
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.AllocateProductionOrder)
                .ThenInclude(p => p.ProductionOrder)
                    .ThenInclude(p => p.Customer)
            .Include(p => p.Products)
                .ThenInclude(p => p.Product)
            .Include(p => p.AllocateProductionOrder)
                .ThenInclude(p => p.Products)
                    .ThenInclude(p => p.FulfilledQuantities)
                        .ThenInclude(p => p.FinishedGoodsTransferNote)
                            .ThenInclude(p => p.BatchManufacturingRecord)
            .ToListAsync();

        var query = invoices.SelectMany(invoice =>
            invoice.Products.Select(p => new
            {
                Invoice = invoice,
                p.Product,
                InvoiceProduct = p,
                Allocation = invoice.AllocateProductionOrder,
                invoice.AllocateProductionOrder.ProductionOrder,
                invoice.AllocateProductionOrder.ProductionOrder.Customer,
            })
        );

        var data = query
            .Select(x => new InvoicedProductsSummaryReportDto
            {
                InvoiceNo = x.Invoice.Code,
                InvoiceDate = x.Invoice.CreatedAt,

                CustomerName = x.Customer.Name,

                OrderNo = x.ProductionOrder.Code,
                OrderDate = x.ProductionOrder.CreatedAt,

                NoOfBatches = x
                    .Allocation.Products.Where(op => op.ProductId == x.Product.Id)
                    .SelectMany(op => op.FulfilledQuantities)
                    .Select(q => q.FinishedGoodsTransferNote.BatchManufacturingRecord.BatchNumber)
                    .Distinct()
                    .Count(),

                OrderQuantity = x.InvoiceProduct.Quantity,
                QuantityAllocated = x
                    .Allocation.Products.Where(op => op.ProductId == x.Product.Id)
                    .SelectMany(op => op.FulfilledQuantities)
                    .Sum(q => q.Quantity),

                Uom = x.Product.BaseUoM.Symbol,
                UnitPrice = x.Product.Price,

                ProductName = x.Product.Name,
                ProductCode = x.Product.Code,
            })
            .ToList();

        for (var i = 0; i < data.Count; i++)
            data[i].No = i + 1;

        return Result.Success(data);
    }

    public async Task<
        Result<List<InvoicedProductsDetailedReportDto>>
    > GetInvoicedProductsDetailedReport(InvoicedProductFilters filters)
    {
        var invoicesQuery = context
            .ProformaInvoices.AsNoTracking()
            .AsSplitQuery()
            
            .Where(p => !p.DeletedAt.HasValue);

        if (filters.StartDate.HasValue)
            invoicesQuery = invoicesQuery.Where(p => p.CreatedAt >= filters.StartDate.Value);

        if (filters.EndDate.HasValue)
            invoicesQuery = invoicesQuery.Where(p => p.CreatedAt <= filters.EndDate.Value);

        if (filters.CustomerId.HasValue)
            invoicesQuery = invoicesQuery.Where(p =>
                p.AllocateProductionOrder.ProductionOrder.CustomerId == filters.CustomerId.Value
            );

        if (filters.ProductId.HasValue)
            invoicesQuery = invoicesQuery.Where(p =>
                p.Products.Any(pr => pr.ProductId == filters.ProductId.Value)
            );

        if (filters.WarehouseDivision.HasValue)
            invoicesQuery = invoicesQuery.Where(p =>
                p.Products.Any(pr => pr.Product.Division == filters.WarehouseDivision.Value)
            );

        var invoices = await invoicesQuery
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.AllocateProductionOrder)
                .ThenInclude(p => p.ProductionOrder)
                    .ThenInclude(p => p.Customer)
            .Include(p => p.Products)
                .ThenInclude(p => p.Product)
            .Include(p => p.AllocateProductionOrder)
                .ThenInclude(p => p.Products)
                    .ThenInclude(p => p.FulfilledQuantities)
                        .ThenInclude(p => p.FinishedGoodsTransferNote)
                            .ThenInclude(p => p.BatchManufacturingRecord)
            .ToListAsync();

        var query = invoices.SelectMany(invoice =>
            invoice.Products.SelectMany(p =>
                invoice
                    .AllocateProductionOrder.Products.Where(op => op.ProductId == p.ProductId)
                    .SelectMany(pop =>
                        pop.FulfilledQuantities.Select(fq => new
                        {
                            Invoice = invoice,
                            InvoiceProduct = p,
                            p.Product,
                            Allocation = invoice.AllocateProductionOrder,
                            invoice.AllocateProductionOrder.ProductionOrder,
                            Fulfilled = fq,
                            Batch = fq.FinishedGoodsTransferNote.BatchManufacturingRecord,
                        })
                    )
            )
        );

        var data = query
            .Select(x => new InvoicedProductsDetailedReportDto
            {
                InvoiceNo = x.Invoice.Code,
                InvoiceDate = x.Invoice.CreatedAt,

                CustomerName = x.ProductionOrder.Customer.Name,

                OrderNo = x.ProductionOrder.Code,
                OrderDate = x.ProductionOrder.CreatedAt,

                ProductName = x.Product.Name,
                ProductCode = x.Product.Code,

                BatchNo = x.Batch.BatchNumber,

                OrderQuantity = x.InvoiceProduct.Quantity,

                QuantityAllocated = x.Fulfilled.Quantity,

                Uom = x.Product.BaseUoM.Symbol,

                UnitPrice = x.Product.Price,

                AllocationStatus = x.Allocation.Status,
            })
            .ToList();

        for (var i = 0; i < data.Count; i++)
            data[i].No = i + 1;

        return Result.Success(data);
    }

 
    public async Task<Result<IEnumerable<DockToStockTimeDto>>> GetDockToStockTime(
        WarehouseKpiFilterDto filter, Guid? departmentId)
    {
        DateTime now = DateTime.UtcNow;
        DateTime? startDate = null;
        DateTime? endDate = null;

        if (filter.CustomStartDate.HasValue || filter.CustomEndDate.HasValue)
        {
            startDate = filter.CustomStartDate;
            endDate = filter.CustomEndDate;
        }
        else if (filter.DatePreset.HasValue)
        {
            switch (filter.DatePreset.Value)
            {
                case DateFilter.Today:
                    startDate = now.Date;
                    break;
                case DateFilter.ThisWeek:
                    startDate = now.Date.AddDays(-(int)now.DayOfWeek);
                    break;
                case DateFilter.ThisMonth:
                    startDate = now.Date.AddDays(1 - now.Day);
                    break;
            }
        }

        var periodLabel = GetPeriodLabel(filter, startDate, endDate);

        var drmQuery = context
            .DistributedRequisitionMaterials.IgnoreQueryFilters().AsNoTracking()
            .Where(drm =>
                drm.CheckedAt.HasValue
                && drm.GrnGeneratedAt.HasValue
                && !drm.DeletedAt.HasValue
            );

        if (departmentId.HasValue)
        {
            drmQuery = drmQuery.Where(drm =>
                drm.DistributedRequisitionItems.Any(dri => dri.Warehouse.DepartmentId == departmentId.Value));
        }

        if (filter.WarehouseId.HasValue)
        {
            var warehouseId = filter.WarehouseId.Value;
            drmQuery = drmQuery.Where(drm =>
                drm.DistributedRequisitionItems.Any(dri => dri.WarehouseId == warehouseId));
        }

        if (startDate.HasValue)
            drmQuery = drmQuery.Where(drm => drm.CheckedAt >= startDate.Value);

        if (endDate.HasValue)
            drmQuery = drmQuery.Where(drm => drm.CheckedAt <= endDate.Value);
        
        var itemQuery = drmQuery.SelectMany(drm => drm.DistributedRequisitionItems);

        if (departmentId.HasValue)
            itemQuery = itemQuery.Where(dri =>
                dri.Warehouse.DepartmentId == departmentId.Value);

        if (filter.WarehouseId.HasValue)
            itemQuery = itemQuery.Where(dri =>
                dri.WarehouseId == filter.WarehouseId.Value);

        if (filter.WarehouseType.HasValue)
            itemQuery = itemQuery.Where(dri =>
                dri.Warehouse.Type == filter.WarehouseType.Value);

        if (filter.Division.HasValue)
            itemQuery = itemQuery.Where(dri =>
                dri.Warehouse.Division == filter.Division.Value);

        var records = await itemQuery
            .Select(dri => new
            {
                DepartmentName = dri.Warehouse.Department.Name,
                WarehouseName = dri.Warehouse.Name,
                CheckedAt = dri.DistributedRequisitionMaterial.CheckedAt.Value,
                GrnGeneratedAt = dri.DistributedRequisitionMaterial.GrnGeneratedAt.Value
            })
            .ToListAsync();

        var grouped = records
            .GroupBy(r => new { r.DepartmentName, r.WarehouseName })
            .Select(g =>
            {
                var hours = g
                    .Select(r => (r.GrnGeneratedAt - r.CheckedAt).TotalHours)
                    .OrderBy(h => h)
                    .ToList();

                double median;
                int count = hours.Count;

                if (count == 0)
                    median = 0;
                else if (count % 2 == 1)
                    median = hours[count / 2];
                else
                    median = (hours[count / 2 - 1] + hours[count / 2]) / 2.0;

                return new DockToStockTimeDto
                {
                    Department = g.Key.DepartmentName,
                    Warehouse = g.Key.WarehouseName,
                    Period = periodLabel,
                    AverageHours = Math.Round(g.Average(r => (r.GrnGeneratedAt - r.CheckedAt).TotalHours), 2),
                    MedianHours = Math.Round(median, 2),
                    TotalRecords = count
                };
            })
            .OrderBy(d => d.Department)
            .ThenBy(d => d.Warehouse)
            .ToList();

        return Result.Success(grouped.AsEnumerable());
    }

    public async Task<Result<IEnumerable<StockTransferFulfilmentRateDto>>> GetStockTransferFulfilmentRate(
        WarehouseKpiFilterDto filter,
        Guid? departmentId)
    {
        DateTime now = DateTime.UtcNow;
        DateTime? startDate = null;
        DateTime? endDate = null;

        if (filter.CustomStartDate.HasValue || filter.CustomEndDate.HasValue)
        {
            startDate = filter.CustomStartDate;
            endDate = filter.CustomEndDate;
        }
        else if (filter.DatePreset.HasValue)
        {
            switch (filter.DatePreset.Value)
            {
                case DateFilter.Today:
                    startDate = now.Date;
                    break;
                case DateFilter.ThisWeek:
                    startDate = now.Date.AddDays(-(int)now.DayOfWeek);
                    break;
                case DateFilter.ThisMonth:
                    startDate = now.Date.AddDays(1 - now.Day);
                    break;
            }
        }

        var baseQuery = context
            .StockTransferSources.IgnoreQueryFilters().AsNoTracking()
            .Where(sts => !sts.DeletedAt.HasValue);

        if (startDate.HasValue)
            baseQuery = baseQuery.Where(sts => sts.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            baseQuery = baseQuery.Where(sts => sts.CreatedAt <= endDate.Value);

        if (filter.FromDepartmentId.HasValue)
            baseQuery = baseQuery.Where(sts => sts.FromDepartmentId == filter.FromDepartmentId.Value);

        if (filter.ToDepartmentId.HasValue)
            baseQuery = baseQuery.Where(sts => sts.ToDepartmentId == filter.ToDepartmentId.Value);

        if (departmentId.HasValue)
        {
            var outgoingCounts = await baseQuery
                .Where(sts => sts.FromDepartmentId == departmentId.Value)
                .GroupBy(sts => new { DeptName = sts.FromDepartment.Name, sts.Status })
                .Select(g => new { g.Key.DeptName, g.Key.Status, Count = g.Count() })
                .ToListAsync();

            var incomingCounts = await baseQuery
                .Where(sts => sts.ToDepartmentId == departmentId.Value)
                .GroupBy(sts => new { DeptName = sts.ToDepartment.Name, sts.Status })
                .Select(g => new { g.Key.DeptName, g.Key.Status, Count = g.Count() })
                .ToListAsync();

            var results = new List<StockTransferFulfilmentRateDto>();
            var deptName = outgoingCounts.Select(x => x.DeptName).FirstOrDefault()
                ?? incomingCounts.Select(x => x.DeptName).FirstOrDefault()
                ?? "Unknown";

            var outgoingLookup = outgoingCounts.ToDictionary(x => x.Status, x => x.Count);
            var incomingLookup = incomingCounts.ToDictionary(x => x.Status, x => x.Count);

            results.Add(BuildFulfilmentDto(deptName, "Outgoing", outgoingLookup));
            results.Add(BuildFulfilmentDto(deptName, "Incoming", incomingLookup));

            return Result.Success(results.AsEnumerable());
        }
        else
        {
            var outgoingByDept = await baseQuery
                .GroupBy(sts => new { DeptId = sts.FromDepartmentId, DeptName = sts.FromDepartment.Name, sts.Status })
                .Select(g => new { g.Key.DeptName, g.Key.Status, Count = g.Count() })
                .ToListAsync();

            var incomingByDept = await baseQuery
                .GroupBy(sts => new { DeptId = sts.ToDepartmentId, DeptName = sts.ToDepartment.Name, sts.Status })
                .Select(g => new { g.Key.DeptName, g.Key.Status, Count = g.Count() })
                .ToListAsync();

            var results = new List<StockTransferFulfilmentRateDto>();

            var outgoingGroups = outgoingByDept
                .GroupBy(x => x.DeptName)
                .ToList();

            foreach (var group in outgoingGroups)
            {
                var lookup = group.ToDictionary(x => x.Status, x => x.Count);
                results.Add(BuildFulfilmentDto(group.Key, "Outgoing", lookup));
            }

            var incomingGroups = incomingByDept
                .GroupBy(x => x.DeptName)
                .ToList();

            foreach (var group in incomingGroups)
            {
                var lookup = group.ToDictionary(x => x.Status, x => x.Count);
                results.Add(BuildFulfilmentDto(group.Key, "Incoming", lookup));
            }

            return Result.Success(results.AsEnumerable());
        }
    }

    public async Task<Result<IEnumerable<ReceivingPipelineSnapshotDto>>> GetReceivingPipelineSnapshot(
        WarehouseKpiFilterDto filter, Guid? departmentId)
    {
        var baseQuery = context
            .DistributedRequisitionMaterials.IgnoreQueryFilters().AsNoTracking()
            .Where(drm => !drm.DeletedAt.HasValue
                && drm.WarehouseArrivalLocation != null);

        if (departmentId.HasValue)
            baseQuery = baseQuery.Where(drm =>
                drm.WarehouseArrivalLocation.Warehouse.DepartmentId == departmentId.Value);

        if (filter.WarehouseId.HasValue)
            baseQuery = baseQuery.Where(drm =>
                drm.WarehouseArrivalLocation.WarehouseId == filter.WarehouseId.Value);

        if (filter.WarehouseType.HasValue)
            baseQuery = baseQuery.Where(drm =>
                drm.WarehouseArrivalLocation.Warehouse.Type == filter.WarehouseType.Value);

        if (filter.Division.HasValue)
            baseQuery = baseQuery.Where(drm =>
                drm.WarehouseArrivalLocation.Warehouse.Division == filter.Division.Value);

        if (departmentId.HasValue)
        {
            var stageCounts = await baseQuery
                .GroupBy(drm => drm.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var stageLookup = stageCounts.ToDictionary(s => s.Status, s => s.Count);

            var pending = stageLookup.GetValueOrDefault(DistributedRequisitionMaterialStatus.Pending);
            var arrived = stageLookup.GetValueOrDefault(DistributedRequisitionMaterialStatus.Arrived);
            var checkedStatus = stageLookup.GetValueOrDefault(DistributedRequisitionMaterialStatus.Checked);
            var grnGenerated = stageLookup.GetValueOrDefault(DistributedRequisitionMaterialStatus.GrnGenerated);
            var distributedTotal = stageLookup.GetValueOrDefault(DistributedRequisitionMaterialStatus.Distributed);

            var assigned = 0;

            if (distributedTotal > 0)
            {
                assigned = await baseQuery
                    .Where(drm => drm.Status == DistributedRequisitionMaterialStatus.Distributed)
                    .CountAsync(drm =>
                        drm.CheckLists.Any(cl =>
                            cl.MaterialBatches.Any(mb => mb.ShelfMaterialBatches.Any())));
            }

            var distributed = distributedTotal - assigned;
            var total = pending + arrived + checkedStatus + grnGenerated + distributed + assigned;

            var deptName = await baseQuery
                .Select(drm => drm.WarehouseArrivalLocation.Warehouse.Department.Name)
                .FirstOrDefaultAsync() ?? "Unknown";

            return Result.Success(new List<ReceivingPipelineSnapshotDto>
            {
                new ReceivingPipelineSnapshotDto
                {
                    Department = deptName,
                    Pending = pending,
                    Arrived = arrived,
                    Checked = checkedStatus,
                    GrnGenerated = grnGenerated,
                    Distributed = distributed,
                    Assigned = assigned,
                    Total = total
                }
            }.AsEnumerable());
        }
        else
        {
            var deptGroups = await baseQuery
                .GroupBy(drm => drm.WarehouseArrivalLocation.Warehouse.Department.Name)
                .ToListAsync();

            var results = new List<ReceivingPipelineSnapshotDto>();

            foreach (var group in deptGroups)
            {
                var deptName = group.Key;
                var stageCounts = group
                    .GroupBy(drm => drm.Status)
                    .ToDictionary(s => s.Key, s => s.Count());

                var pending = stageCounts.GetValueOrDefault(DistributedRequisitionMaterialStatus.Pending);
                var arrived = stageCounts.GetValueOrDefault(DistributedRequisitionMaterialStatus.Arrived);
                var checkedStatus = stageCounts.GetValueOrDefault(DistributedRequisitionMaterialStatus.Checked);
                var grnGenerated = stageCounts.GetValueOrDefault(DistributedRequisitionMaterialStatus.GrnGenerated);
                var distributedTotal = stageCounts.GetValueOrDefault(DistributedRequisitionMaterialStatus.Distributed);

                var assigned = 0;

                if (distributedTotal > 0)
                {
                    var distributedIds = group
                        .Where(drm => drm.Status == DistributedRequisitionMaterialStatus.Distributed)
                        .Select(drm => drm.Id)
                        .ToList();

                    assigned = await context.Checklists
                        .IgnoreQueryFilters().AsNoTracking()
                        .CountAsync(cl =>
                            distributedIds.Contains(cl.DistributedRequisitionMaterialId)
                            && cl.MaterialBatches.Any(mb => mb.ShelfMaterialBatches.Any()));
                }

                var distributed = distributedTotal - assigned;
                var total = pending + arrived + checkedStatus + grnGenerated + distributed + assigned;

                results.Add(new ReceivingPipelineSnapshotDto
                {
                    Department = deptName,
                    Pending = pending,
                    Arrived = arrived,
                    Checked = checkedStatus,
                    GrnGenerated = grnGenerated,
                    Distributed = distributed,
                    Assigned = assigned,
                    Total = total
                });
            }

            return Result.Success(results.AsEnumerable());
        }
    }

    private static StockTransferFulfilmentRateDto BuildFulfilmentDto(
        string department, string direction,
        Dictionary<StockTransferStatus, int> lookup)
    {
        var pending = lookup.GetValueOrDefault(StockTransferStatus.InProgress);
        var approved = lookup.GetValueOrDefault(StockTransferStatus.Approved);
        var issued = lookup.GetValueOrDefault(StockTransferStatus.Issued);
        var total = pending + approved + issued
            + lookup.GetValueOrDefault(StockTransferStatus.Rejected);

        return new StockTransferFulfilmentRateDto
        {
            Department = department,
            Direction = direction,
            TotalTransfers = total,
            Pending = pending,
            Approved = approved,
            Issued = issued,
            FulfilmentPercentage = total > 0
                ? Math.Round((decimal)issued / total * 100, 2)
                : 0
        };
    }

    public async Task<Result<IEnumerable<ExpiryRiskIndexDto>>> GetExpiryRiskIndex(
        WarehouseKpiFilterDto filter, Guid? departmentId)
    {
        var today = DateTime.UtcNow.Date;
        var threshold30 = today.AddDays(30);
        var threshold60 = today.AddDays(60);
        var threshold90 = today.AddDays(90);

        var query = context
            .ShelfMaterialBatches.IgnoreQueryFilters().AsNoTracking()
            .Where(smb =>
                !smb.DeletedAt.HasValue
                && smb.Quantity > 0
                && smb.MaterialBatch.ExpiryDate.HasValue
                && !smb.MaterialBatch.DeletedAt.HasValue
                && !smb.MaterialBatch.Material.IsUnlimited
            );

        if (departmentId.HasValue)
            query = query.Where(smb =>
                smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.Warehouse.DepartmentId == departmentId.Value);

        if (filter.WarehouseId.HasValue)
            query = query.Where(smb =>
                smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.WarehouseId == filter.WarehouseId.Value);

        if (filter.WarehouseType.HasValue)
            query = query.Where(smb =>
                smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.Warehouse.Type == filter.WarehouseType.Value);

        if (filter.Division.HasValue)
            query = query.Where(smb =>
                smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.Warehouse.Division == filter.Division.Value);

        var grouped = await query
            .GroupBy(smb => new
            {
                DepartmentName = smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.Warehouse.Department.Name,
                WarehouseName = smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.Warehouse.Name,
                WindowCode = smb.MaterialBatch.ExpiryDate <= threshold30 ? 0
                    : smb.MaterialBatch.ExpiryDate <= threshold60 ? 1
                    : smb.MaterialBatch.ExpiryDate <= threshold90 ? 2
                    : 3,
                UomSymbol = smb.UoM != null ? smb.UoM.Symbol : smb.MaterialBatch.UoM.Symbol
            })
            .Select(g => new
            {
                g.Key.DepartmentName,
                g.Key.WarehouseName,
                g.Key.WindowCode,
                g.Key.UomSymbol,
                BatchCount = g.Select(smb => smb.MaterialBatchId).Distinct().Count(),
                TotalQuantity = g.Sum(smb => smb.Quantity)
            })
            .ToListAsync();

        var windowLabels = new Dictionary<int, string>
        {
            [0] = "≤ 30 days",
            [1] = "31–60 days",
            [2] = "61–90 days",
            [3] = "> 90 days"
        };

        var result = grouped
            .Select(g => new ExpiryRiskIndexDto
            {
                Department = g.DepartmentName,
                Warehouse = g.WarehouseName,
                ExpiryWindow = windowLabels.GetValueOrDefault(g.WindowCode, "Unknown"),
                BatchCount = g.BatchCount,
                TotalQuantity = g.TotalQuantity.ToString("0.############################"),
                Uom = g.UomSymbol
            })
            .OrderBy(d => d.Department)
            .ThenBy(d => d.Warehouse)
            .ThenBy(d => d.ExpiryWindow)
            .ToList();

        if (filter.ExpiryWindow.HasValue)
        {
            var targetLabel = filter.ExpiryWindow.Value switch
            {
                ExpiryWindowFilter.Within30Days => "≤ 30 days",
                ExpiryWindowFilter.Within31To60Days => "31–60 days",
                ExpiryWindowFilter.Within61To90Days => "61–90 days",
                ExpiryWindowFilter.Over90Days => "> 90 days",
                _ => null
            };
            if (targetLabel != null)
                result = result.Where(r => r.ExpiryWindow == targetLabel).ToList();
        }

        return Result.Success(result.AsEnumerable());
    }

    public async Task<Result<IEnumerable<ReorderAlertCountDto>>> GetReorderAlertCount(
        WarehouseKpiFilterDto filter)
    {
        var mdQuery = context
            .MaterialDepartments.IgnoreQueryFilters().AsNoTracking()
            .Where(md => md.DeletedAt == null);

        if (filter.DepartmentId.HasValue)
            mdQuery = mdQuery.Where(md => md.DepartmentId == filter.DepartmentId.Value);

        var materialDepartments = await mdQuery
            .Select(md => new
            {
                md.MaterialId,
                md.ReOrderLevel,
                DepartmentName = md.Department.Name,
                UomSymbol = md.UoM.Symbol
            })
            .ToListAsync();

        var materialIds = materialDepartments.Select(md => md.MaterialId).Distinct().ToList();

        if (materialIds.Count == 0)
            return Result.Success(Enumerable.Empty<ReorderAlertCountDto>());

        var materialsQuery = context
            .Materials.IgnoreQueryFilters().AsNoTracking()
            .Where(m => materialIds.Contains(m.Id) && !m.DeletedAt.HasValue);

        if (filter.MaterialKind.HasValue)
            materialsQuery = materialsQuery.Where(m => m.Kind == filter.MaterialKind.Value);

        var materials = await materialsQuery
            .Select(m => new { m.Id, m.Name, m.Code })
            .ToListAsync();

        var filteredIds = materials.Select(m => m.Id).ToList();
        var activeMdIds = materialDepartments
            .Where(md => filteredIds.Contains(md.MaterialId))
            .ToList();

        if (activeMdIds.Count == 0)
            return Result.Success(Enumerable.Empty<ReorderAlertCountDto>());

        var shelfQuantities = await context
            .ShelfMaterialBatches.IgnoreQueryFilters().AsNoTracking()
            .Where(smb =>
                !smb.DeletedAt.HasValue
                && filteredIds.Contains(smb.MaterialBatch.MaterialId)
            )
            .GroupBy(smb => smb.MaterialBatch.MaterialId)
            .Select(g => new
            {
                MaterialId = g.Key,
                TotalQuantity = g.Sum(x => (decimal?)x.Quantity) ?? 0
            })
            .ToListAsync();

        var quantityLookup = shelfQuantities.ToDictionary(q => q.MaterialId, q => q.TotalQuantity);
        var materialLookup = materials.ToDictionary(m => m.Id);

        var result = activeMdIds
            .Select(md =>
            {
                var currentQty = quantityLookup.GetValueOrDefault(md.MaterialId);
                var material = materialLookup.GetValueOrDefault(md.MaterialId);
                return new ReorderAlertCountDto
                {
                    Department = md.DepartmentName,
                    Material = material?.Name,
                    MaterialCode = material?.Code,
                    CurrentStock = currentQty,
                    ReorderLevel = md.ReOrderLevel,
                    Shortfall = md.ReOrderLevel > currentQty
                        ? md.ReOrderLevel - currentQty
                        : 0,
                    Uom = md.UomSymbol
                };
            })
            .Where(x => x.CurrentStock <= x.ReorderLevel)
            .OrderBy(x => x.Department)
            .ThenBy(x => x.Material)
            .ToList();

        return Result.Success(result.AsEnumerable());
    }

    public async Task<Result<IEnumerable<SwapRequestActivityDto>>> GetSwapRequestActivity(
        WarehouseKpiFilterDto filter,
        Guid? departmentId)
    {
        DateTime now = DateTime.UtcNow;
        DateTime? startDate = null;
        DateTime? endDate = null;

        if (filter.CustomStartDate.HasValue || filter.CustomEndDate.HasValue)
        {
            startDate = filter.CustomStartDate;
            endDate = filter.CustomEndDate;
        }
        else if (filter.DatePreset.HasValue)
        {
            switch (filter.DatePreset.Value)
            {
                case DateFilter.Today:
                    startDate = now.Date;
                    break;
                case DateFilter.ThisWeek:
                    startDate = now.Date.AddDays(-(int)now.DayOfWeek);
                    break;
                case DateFilter.ThisMonth:
                    startDate = now.Date.AddDays(1 - now.Day);
                    break;
            }
        }

        var periodLabel = GetPeriodLabel(filter, startDate, endDate);

        var baseQuery = context
            .SwapRequests.IgnoreQueryFilters().AsNoTracking()
            .Where(sr => !sr.DeletedAt.HasValue);

        if (startDate.HasValue)
            baseQuery = baseQuery.Where(sr => sr.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            baseQuery = baseQuery.Where(sr => sr.CreatedAt <= endDate.Value);

        if (filter.WarehouseId.HasValue)
        {
            var warehouseId = filter.WarehouseId.Value;
            baseQuery = baseQuery.Where(sr =>
                sr.FirstWarehouseId == warehouseId
                || sr.SecondWarehouseId == warehouseId);
        }


        if (filter.WarehouseType.HasValue)
            baseQuery = baseQuery.Where(sr =>
                sr.FirstWarehouse.Type == filter.WarehouseType.Value
                || sr.SecondWarehouse.Type == filter.WarehouseType.Value);

        if (filter.Division.HasValue)
            baseQuery = baseQuery.Where(sr =>
                sr.FirstWarehouse.Division == filter.Division.Value
                || sr.SecondWarehouse.Division == filter.Division.Value);

        if (departmentId.HasValue)
        {
            baseQuery = baseQuery.Where(sr =>
                sr.FirstWarehouse.DepartmentId == departmentId.Value
                || sr.SecondWarehouse.DepartmentId == departmentId.Value);

            var counts = await baseQuery
                .GroupBy(sr => sr.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var lookup = counts.ToDictionary(x => x.Status, x => x.Count);

            var deptName = await baseQuery
                .Select(sr => sr.FirstWarehouse.Department.Name)
                .FirstOrDefaultAsync() ?? "Unknown";

            return Result.Success(new List<SwapRequestActivityDto>
            {
                new SwapRequestActivityDto
                {
                    Department = deptName,
                    Period = periodLabel,
                    Pending = lookup.GetValueOrDefault(SwapRequestStatus.Pending),
                    Approved = lookup.GetValueOrDefault(SwapRequestStatus.Approved),
                    Rejected = lookup.GetValueOrDefault(SwapRequestStatus.Rejected),
                    Total = lookup.GetValueOrDefault(SwapRequestStatus.Pending)
                        + lookup.GetValueOrDefault(SwapRequestStatus.Approved)
                        + lookup.GetValueOrDefault(SwapRequestStatus.Rejected)
                }
            }.AsEnumerable());
        }
        else
        {
            var deptGroups = await baseQuery
                .GroupBy(sr => sr.FirstWarehouse.Department.Name)
                .Select(g => new
                {
                    Department = g.Key,
                    Pending = g.Count(sr => sr.Status == SwapRequestStatus.Pending),
                    Approved = g.Count(sr => sr.Status == SwapRequestStatus.Approved),
                    Rejected = g.Count(sr => sr.Status == SwapRequestStatus.Rejected)
                })
                .ToListAsync();

            var results = deptGroups
                .Select(g => new SwapRequestActivityDto
                {
                    Department = g.Department,
                    Period = periodLabel,
                    Pending = g.Pending,
                    Approved = g.Approved,
                    Rejected = g.Rejected,
                    Total = g.Pending + g.Approved + g.Rejected
                })
                .ToList();

            return Result.Success(results.AsEnumerable());
        }
    }

    public async Task<Result<IEnumerable<MaterialMovementCountDto>>> GetMaterialMovementCount(
        WarehouseKpiFilterDto filter, Guid? departmentId)
    {
        DateTime now = DateTime.UtcNow;
        DateTime? startDate = null;
        DateTime? endDate = null;

        if (filter.CustomStartDate.HasValue || filter.CustomEndDate.HasValue)
        {
            startDate = filter.CustomStartDate;
            endDate = filter.CustomEndDate;
        }
        else if (filter.DatePreset.HasValue)
        {
            switch (filter.DatePreset.Value)
            {
                case DateFilter.Today:
                    startDate = now.Date;
                    break;
                case DateFilter.ThisWeek:
                    startDate = now.Date.AddDays(-(int)now.DayOfWeek);
                    break;
                case DateFilter.ThisMonth:
                    startDate = now.Date.AddDays(1 - now.Day);
                    break;
            }
        }

        var periodLabel = GetPeriodLabel(filter, startDate, endDate);

        var baseQuery = context
            .ShelfMaterialBatches.IgnoreQueryFilters().AsNoTracking()
            .Where(smb => !smb.DeletedAt.HasValue);

        if (departmentId.HasValue)
            baseQuery = baseQuery.Where(smb =>
                smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.Warehouse.DepartmentId == departmentId.Value);

        if (filter.WarehouseId.HasValue)
        {
            var warehouseId = filter.WarehouseId.Value;
            baseQuery = baseQuery.Where(smb =>
                smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.WarehouseId == warehouseId);
        }


        if (filter.WarehouseType.HasValue)
            baseQuery = baseQuery.Where(smb =>
                smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.Warehouse.Type == filter.WarehouseType.Value);

        if (filter.Division.HasValue)
            baseQuery = baseQuery.Where(smb =>
                smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.Warehouse.Division == filter.Division.Value);

        if (departmentId.HasValue)
        {
            var deptName = await baseQuery
                .Select(smb => smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.Warehouse.Department.Name)
                .FirstOrDefaultAsync() ?? "Unknown";

            var newPutawayQuery = baseQuery;
            if (startDate.HasValue)
                newPutawayQuery = newPutawayQuery.Where(smb => smb.CreatedAt >= startDate.Value);
            if (endDate.HasValue)
                newPutawayQuery = newPutawayQuery.Where(smb => smb.CreatedAt <= endDate.Value);

            var newPutaways = await newPutawayQuery.CountAsync();

            var adjustmentsQuery = baseQuery.Where(smb =>
                smb.UpdatedAt.HasValue
                && smb.UpdatedAt.Value.Date != smb.CreatedAt.Date);
            if (startDate.HasValue)
                adjustmentsQuery = adjustmentsQuery.Where(smb => smb.UpdatedAt >= startDate.Value);
            if (endDate.HasValue)
                adjustmentsQuery = adjustmentsQuery.Where(smb => smb.UpdatedAt <= endDate.Value);

            var adjustments = await adjustmentsQuery.CountAsync();

            return Result.Success(new List<MaterialMovementCountDto>
            {
                new MaterialMovementCountDto
                {
                    Department = deptName,
                    Period = periodLabel,
                    NewPutaways = newPutaways,
                    Adjustments = adjustments,
                    TotalMovements = newPutaways + adjustments
                }
            }.AsEnumerable());
        }
        else
        {
            var allIds = await baseQuery
                .Select(smb => new
                {
                    smb.Id,
                    DeptName = smb.WarehouseLocationShelf.WarehouseLocationRack
                        .WarehouseLocation.Warehouse.Department.Name,
                    smb.CreatedAt,
                    smb.UpdatedAt
                })
                .ToListAsync();

            var deptGroups = allIds
                .GroupBy(x => x.DeptName)
                .ToList();

            var results = new List<MaterialMovementCountDto>();

            foreach (var group in deptGroups)
            {
                var deptName = group.Key;
                var groupList = group.ToList();

                var newPutawayItems = groupList;
                if (startDate.HasValue)
                    newPutawayItems = newPutawayItems.Where(x => x.CreatedAt >= startDate.Value).ToList();
                if (endDate.HasValue)
                    newPutawayItems = newPutawayItems.Where(x => x.CreatedAt <= endDate.Value).ToList();

                var newPutaways = newPutawayItems.Count();

                var adjustments = groupList.Count(x =>
                    x.UpdatedAt.HasValue
                    && x.UpdatedAt.Value.Date != x.CreatedAt.Date
                    && (!startDate.HasValue || x.UpdatedAt >= startDate.Value)
                    && (!endDate.HasValue || x.UpdatedAt <= endDate.Value));

                results.Add(new MaterialMovementCountDto
                {
                    Department = deptName,
                    Period = periodLabel,
                    NewPutaways = newPutaways,
                    Adjustments = adjustments,
                    TotalMovements = newPutaways + adjustments
                });
            }

            return Result.Success(results.AsEnumerable());
        }
    }

    public async Task<Result<List<MaterialsStockSummaryDto>>> GetMaterialsStockSummary(
        Guid? departmentId = null, MaterialKind? materialKind = null, Guid? materialId = null)
    {
        var query = context.ShelfMaterialBatches.IgnoreQueryFilters().AsNoTracking()
            .Where(smb => !smb.DeletedAt.HasValue && smb.Quantity > 0
                && !smb.MaterialBatch.DeletedAt.HasValue
                && !smb.MaterialBatch.Material.DeletedAt.HasValue);

        if (departmentId.HasValue)
            query = query.Where(smb =>
                smb.MaterialBatch.Material.Departments.Any(md =>
                    md.DepartmentId == departmentId.Value && !md.DeletedAt.HasValue));

        if (materialKind.HasValue)
            query = query.Where(smb => smb.MaterialBatch.Material.Kind == materialKind.Value);

        if (materialId.HasValue)
            query = query.Where(smb => smb.MaterialBatch.MaterialId == materialId.Value);

        var rawData = await query
            .Select(smb => new
            {
                smb.MaterialBatch.Material.Name,
                smb.MaterialBatch.Material.Code,
                smb.MaterialBatch.Material.Kind,
                smb.MaterialBatch.MaterialId,
                smb.MaterialBatch.Id,
                smb.Quantity,
                DepartmentName = smb.MaterialBatch.Material.Departments
                    .Where(md => !md.DeletedAt.HasValue)
                    .Select(md => md.Department.Name)
                    .FirstOrDefault(),
                WarehouseType = smb.WarehouseLocationShelf
                    .WarehouseLocationRack.WarehouseLocation.Warehouse.Type,
                UomSymbol = smb.UoM != null ? smb.UoM.Symbol : smb.MaterialBatch.UoM.Symbol,
            })
            .ToListAsync();

        var grouped = rawData
            .GroupBy(x => new
            {
                x.Name,
                x.Code,
                x.Kind,
                x.DepartmentName,
                x.WarehouseType,
                x.UomSymbol,
            })
            .Select(g => new MaterialsStockSummaryDto
            {
                MaterialName = g.Key.Name,
                MaterialCode = g.Key.Code,
                MaterialType = g.Key.Kind == MaterialKind.Raw ? "Raw" : "Packaging",
                ProductionDepartment = g.Key.DepartmentName ?? "Unknown",
                WarehouseType = g.Key.WarehouseType == WarehouseType.RawMaterialStorage
                    ? "Raw Warehouse"
                    : g.Key.WarehouseType == WarehouseType.PackagedStorage
                        ? "Packaging Warehouse"
                        : g.Key.WarehouseType.ToString(),
                NoOfBatches = g.Select(x => x.Id).Distinct().Count(),
                TotalQuantity = g.Sum(x => x.Quantity),
                UOM = g.Key.UomSymbol,
            })
            .OrderBy(x => x.MaterialName)
            .ThenBy(x => x.ProductionDepartment)
            .ThenBy(x => x.WarehouseType)
            .ToList();

        var result = grouped.Select((item, i) => { item.No = i + 1; return item; }).ToList();
        return Result.Success(result);
    }

    public async Task<Result<List<MaterialsStockBatchDetailDto>>> GetMaterialsStockBatchDetail(
        Guid? departmentId = null, MaterialKind? materialKind = null,
        string batchNumber = null, DateTime? expiryDateFrom = null, DateTime? expiryDateTo = null)
    {
        var query = context.ShelfMaterialBatches.IgnoreQueryFilters().AsNoTracking()
            .Where(smb => !smb.DeletedAt.HasValue && smb.Quantity > 0
                && !smb.MaterialBatch.DeletedAt.HasValue
                && !smb.MaterialBatch.Material.DeletedAt.HasValue);

        if (departmentId.HasValue)
            query = query.Where(smb =>
                smb.MaterialBatch.Material.Departments.Any(md =>
                    md.DepartmentId == departmentId.Value && !md.DeletedAt.HasValue));

        if (materialKind.HasValue)
            query = query.Where(smb => smb.MaterialBatch.Material.Kind == materialKind.Value);

        if (!string.IsNullOrEmpty(batchNumber))
            query = query.Where(smb => smb.MaterialBatch.BatchNumber.Contains(batchNumber));

        if (expiryDateFrom.HasValue)
            query = query.Where(smb => smb.MaterialBatch.ExpiryDate >= expiryDateFrom.Value);

        if (expiryDateTo.HasValue)
            query = query.Where(smb => smb.MaterialBatch.ExpiryDate <= expiryDateTo.Value);

        var rawData = await query
            .Select(smb => new
            {
                smb.MaterialBatch.Material.Name,
                smb.MaterialBatch.Material.Code,
                smb.MaterialBatch.Material.Kind,
                smb.MaterialBatch.BatchNumber,
                smb.MaterialBatch.ManufacturingDate,
                smb.MaterialBatch.ExpiryDate,
                smb.Quantity,
                DepartmentName = smb.MaterialBatch.Material.Departments
                    .Where(md => !md.DeletedAt.HasValue)
                    .Select(md => md.Department.Name)
                    .FirstOrDefault(),
                WarehouseType = smb.WarehouseLocationShelf
                    .WarehouseLocationRack.WarehouseLocation.Warehouse.Type,
                UomSymbol = smb.UoM != null ? smb.UoM.Symbol : smb.MaterialBatch.UoM.Symbol,
                StorageLocation = smb.WarehouseLocationShelf.WarehouseLocationRack
                        .WarehouseLocation.Warehouse.Name + " > "
                    + smb.WarehouseLocationShelf.WarehouseLocationRack
                        .WarehouseLocation.Name + " > "
                    + smb.WarehouseLocationShelf.WarehouseLocationRack.Name + " > "
                    + smb.WarehouseLocationShelf.Name,
            })
            .ToListAsync();

        var result = rawData
            .OrderBy(x => x.Name)
            .ThenBy(x => x.BatchNumber)
            .Select((x, i) => new MaterialsStockBatchDetailDto
            {
                No = i + 1,
                MaterialName = x.Name,
                MaterialCode = x.Code,
                MaterialType = x.Kind == MaterialKind.Raw ? "Raw" : "Packaging",
                BatchNo = x.BatchNumber,
                ManufacturingDate = x.ManufacturingDate,
                ExpiryDate = x.ExpiryDate,
                QuantityAvailable = x.Quantity,
                UOM = x.UomSymbol,
                ProductionDepartment = x.DepartmentName ?? "Unknown",
                WarehouseType = x.WarehouseType == WarehouseType.RawMaterialStorage
                    ? "Raw Warehouse"
                    : x.WarehouseType == WarehouseType.PackagedStorage
                        ? "Packaging Warehouse"
                        : x.WarehouseType.ToString(),
                StorageLocation = x.StorageLocation,
            })
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<ShelfUtilisationDetailDto>>> GetShelfUtilisationDetail(
        Guid? warehouseId = null, Guid? locationId = null, OccupancyStatus? occupancyStatus = null,
        Guid? departmentId = null)
    {
        var shelves = await context.WarehouseLocationShelves.IgnoreQueryFilters().AsNoTracking()
            .Where(s => !s.DeletedAt.HasValue)
            .Select(s => new
            {
                s.Id,
                s.Code,
                s.Name,
                WarehouseName = s.WarehouseLocationRack.WarehouseLocation.Warehouse.Name,
                WarehouseType = s.WarehouseLocationRack.WarehouseLocation.Warehouse.Type,
                LocationName = s.WarehouseLocationRack.WarehouseLocation.Name,
                RackName = s.WarehouseLocationRack.Name,
                LocationId = s.WarehouseLocationRack.WarehouseLocationId,
                s.WarehouseLocationRack.WarehouseLocation.WarehouseId
            })
            .ToListAsync();

        if (warehouseId.HasValue)
            shelves = shelves.Where(x => x.WarehouseId == warehouseId.Value).ToList();

        if (locationId.HasValue)
            shelves = shelves.Where(x => x.LocationId == locationId.Value).ToList();

        var shelfIds = shelves.Select(s => s.Id).ToList();

        var batchSummariesQuery = context.ShelfMaterialBatches.IgnoreQueryFilters().AsNoTracking()
            .Where(smb => shelfIds.Contains(smb.WarehouseLocationShelfId)
                && !smb.DeletedAt.HasValue && smb.Quantity > 0);

        if (departmentId.HasValue)
            batchSummariesQuery = batchSummariesQuery.Where(smb =>
                smb.MaterialBatch.Material.Departments.Any(md =>
                    md.DepartmentId == departmentId.Value && !md.DeletedAt.HasValue));

        var batchSummaries = await batchSummariesQuery
            .GroupBy(smb => smb.WarehouseLocationShelfId)
            .Select(g => new
            {
                ShelfId = g.Key,
                DistinctBatches = g.Select(x => x.MaterialBatchId).Distinct().Count(),
                TotalQuantity = g.Sum(x => x.Quantity),
                LastUpdated = g.Max(x => (DateTime?)x.CreatedAt),
            })
            .ToListAsync();

        var lookup = batchSummaries.ToDictionary(x => x.ShelfId);

        var mapped = shelves.Select(s =>
        {
            var summary = lookup.GetValueOrDefault(s.Id);
            var hasBatches = summary != null && summary.DistinctBatches > 0;
            var batchCount = hasBatches ? summary.DistinctBatches : 0;
            var status = !hasBatches ? "Empty"
                : batchCount == 1 ? "Single"
                : "Multiple";

            return new
            {
                s.Code,
                s.Name,
                s.WarehouseName,
                s.WarehouseType,
                s.LocationName,
                s.RackName,
                OccupancyStatus = status,
                DistinctBatches = batchCount,
                TotalQuantity = hasBatches ? summary.TotalQuantity : 0m,
                LastUpdated = hasBatches ? summary.LastUpdated : null,
            };
        });

        if (occupancyStatus.HasValue)
        {
            var target = occupancyStatus.Value.ToString();
            mapped = mapped.Where(x => x.OccupancyStatus == target);
        }

        var result = mapped
            .OrderBy(x => x.WarehouseName)
            .ThenBy(x => x.LocationName)
            .ThenBy(x => x.RackName)
            .ThenBy(x => x.Code)
            .Select((x, i) => new ShelfUtilisationDetailDto
            {
                No = i + 1,
                Warehouse = x.WarehouseName,
                WarehouseType = x.WarehouseType == WarehouseType.RawMaterialStorage
                    ? "Raw Material"
                    : x.WarehouseType == WarehouseType.PackagedStorage
                        ? "Packaging"
                        : x.WarehouseType.ToString(),
                Location = x.LocationName,
                Rack = x.RackName,
                ShelfCode = x.Code,
                ShelfName = x.Name,
                OccupancyStatus = x.OccupancyStatus,
                DistinctBatches = x.DistinctBatches,
                TotalQuantity = x.TotalQuantity,
                LastUpdated = x.LastUpdated,
            })
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<GoodsReceivingRegisterDto>>> GetGoodsReceivingRegister(
        ReportFilter filter, string grnStatus = null, Guid? supplierId = null,
        Guid? departmentId = null)
    {
        var query = context.Grns.IgnoreQueryFilters().AsNoTracking()
            .Where(g => !g.DeletedAt.HasValue);

        if (filter.StartDate.HasValue)
            query = query.Where(g => g.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.AddDays(1);
            query = query.Where(g => g.CreatedAt < end);
        }

        if (!string.IsNullOrEmpty(grnStatus) && Enum.TryParse<Status>(grnStatus, true, out var parsedStatus))
            query = query.Where(g => g.Status == parsedStatus);

        if (departmentId.HasValue)
            query = query.Where(g => g.MaterialBatches.Any(mb =>
                !mb.DeletedAt.HasValue && mb.Material.Departments.Any(md =>
                    md.DepartmentId == departmentId.Value && !md.DeletedAt.HasValue)));

        var rawData = await query
            .SelectMany(g => g.MaterialBatches)
            .Select(mb => new
            {
                mb.Grn.GrnNumber,
                mb.Grn.Status,
                mb.Grn.CreatedAt,
                mb.Grn.CarrierName,
                mb.Grn.VehicleNumber,
                mb.Grn.DeclarationNumber,
                mb.Grn.Remarks,
                MaterialName = mb.Material.Name,
                QuantityReceived = mb.TotalQuantity,
                UomSymbol = mb.UoM.Symbol,
                BatchNumbers = mb.BatchNumber,
                SupplierName = mb.Checklist != null ? mb.Checklist.Supplier.Name : null,
                SupplierId = mb.Checklist != null ? (Guid?)mb.Checklist.Supplier.Id : null,
            })
            .ToListAsync();

        if (supplierId.HasValue)
            rawData = rawData.Where(x => x.SupplierId == supplierId.Value).ToList();

        var result = rawData
            .OrderByDescending(x => x.CreatedAt)
            .Select((x, i) => new GoodsReceivingRegisterDto
            {
                No = i + 1,
                GrnNumber = x.GrnNumber,
                GrnStatus = x.Status.ToString(),
                GrnGeneratedAt = x.CreatedAt,
                CarrierName = x.CarrierName,
                VehicleNumber = x.VehicleNumber,
                DeclarationNumber = x.DeclarationNumber,
                Remarks = x.Remarks,
                Supplier = x.SupplierName ?? "N/A",
                Material = x.MaterialName,
                QuantityReceived = x.QuantityReceived,
                UOM = x.UomSymbol,
                MaterialBatches = x.BatchNumbers,
            })
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<ReceivingPerformanceDetailDto>>> GetReceivingPerformanceDetail(
        ReportFilter filter, Guid? warehouseId = null, Guid? supplierId = null,
        Guid? departmentId = null)
    {
        var query = context.DistributedRequisitionMaterials.IgnoreQueryFilters().AsNoTracking()
            .Where(drm => !drm.DeletedAt.HasValue);

        if (filter.StartDate.HasValue)
            query = query.Where(drm => drm.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.AddDays(1);
            query = query.Where(drm => drm.CreatedAt < end);
        }

        if (supplierId.HasValue)
            query = query.Where(drm => drm.ShipmentInvoice.SupplierId == supplierId.Value);

        if (departmentId.HasValue)
            query = query.Where(drm => drm.Material.Departments.Any(md =>
                md.DepartmentId == departmentId.Value && !md.DeletedAt.HasValue));

        var rawData = await query
            .SelectMany(drm => drm.DistributedRequisitionItems)
            .Select(dri => new
            {
                WarehouseName = dri.Warehouse.Name,
                dri.WarehouseId,
                SupplierName = dri.DistributedRequisitionMaterial.ShipmentInvoice.Supplier.Name,
                MaterialName = dri.DistributedRequisitionMaterial.Material.Name,
                dri.Quantity,
                ArrivedAt = dri.DistributedRequisitionMaterial.ArrivedAt ?? (DateTime?)dri.DistributedRequisitionMaterial.CreatedAt,
                dri.DistributedRequisitionMaterial.CheckedAt,
                dri.DistributedRequisitionMaterial.GrnGeneratedAt,
                dri.DistributedRequisitionMaterial.DistributedAt,
                ApprovedAt = dri.DistributedRequisitionMaterial.CheckLists
                    .SelectMany(cl => cl.MaterialBatches)
                    .Where(mb => !mb.DeletedAt.HasValue)
                    .Select(mb => mb.DateApproved)
                    .FirstOrDefault(),
                AssignedAt = dri.DistributedRequisitionMaterial.Material.Batches
                    .SelectMany(mb => mb.ShelfMaterialBatches)
                    .Where(smb => !smb.DeletedAt.HasValue)
                    .Select(smb => (DateTime?)smb.CreatedAt)
                    .FirstOrDefault(),
            })
            .ToListAsync();

        if (warehouseId.HasValue)
            rawData = rawData.Where(x => x.WarehouseId == warehouseId.Value).ToList();

        var result = rawData
            .OrderByDescending(x => x.ArrivedAt)
            .Select((x, i) =>
            {
                var arrivalToCheck = x.ArrivedAt.HasValue && x.CheckedAt.HasValue
                    ? (x.CheckedAt.Value - x.ArrivedAt.Value).TotalHours : (double?)null;
                var checkToGrn = x.CheckedAt.HasValue && x.GrnGeneratedAt.HasValue
                    ? (x.GrnGeneratedAt.Value - x.CheckedAt.Value).TotalHours : (double?)null;
                var grnToApproved = x.GrnGeneratedAt.HasValue && x.ApprovedAt.HasValue
                    ? (x.ApprovedAt.Value - x.GrnGeneratedAt.Value).TotalHours : (double?)null;
                var grnToDistribute = x.GrnGeneratedAt.HasValue && x.DistributedAt.HasValue
                    ? (x.DistributedAt.Value - x.GrnGeneratedAt.Value).TotalHours : (double?)null;
                var distributeToAssigned = x.DistributedAt.HasValue && x.AssignedAt.HasValue
                    ? (x.AssignedAt.Value - x.DistributedAt.Value).TotalHours : (double?)null;

                var total = (arrivalToCheck ?? 0) + (checkToGrn ?? 0) + (grnToApproved ?? 0)
                    + (grnToDistribute ?? 0) + (distributeToAssigned ?? 0);

                return new ReceivingPerformanceDetailDto
                {
                    No = i + 1,
                    Warehouse = x.WarehouseName,
                    Supplier = x.SupplierName,
                    Material = x.MaterialName,
                    Quantity = x.Quantity,
                    ArrivedAt = x.ArrivedAt,
                    CheckedAt = x.CheckedAt,
                    GrnGeneratedAt = x.GrnGeneratedAt,
                    ApprovedAt = x.ApprovedAt,
                    DistributedAt = x.DistributedAt,
                    AssignedAt = x.AssignedAt,
                    ArrivalToCheckHours = arrivalToCheck,
                    CheckToGrnHours = checkToGrn,
                    GrnToApprovedHours = grnToApproved,
                    GrnToDistributeHours = grnToDistribute,
                    DistributeToAssignedHours = distributeToAssigned,
                    TotalDockToStockHours = Math.Round(total, 4),
                };
            })
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<PutawayRegisterDto>>> GetPutawayRegister(
        ReportFilter filter, Guid? warehouseId = null, Guid? employeeId = null,
        Guid? departmentId = null)
    {
        var query = context.ShelfMaterialBatches.IgnoreQueryFilters().AsNoTracking()
            .Where(smb => !smb.DeletedAt.HasValue && smb.Quantity > 0);

        if (filter.StartDate.HasValue)
            query = query.Where(smb => smb.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.AddDays(1);
            query = query.Where(smb => smb.CreatedAt < end);
        }

        if (employeeId.HasValue)
            query = query.Where(smb => smb.CreatedById == employeeId.Value);

        if (departmentId.HasValue)
            query = query.Where(smb => smb.MaterialBatch.Material.Departments.Any(md =>
                md.DepartmentId == departmentId.Value && !md.DeletedAt.HasValue));

        var data = await query
            .Select(smb => new
            {
                smb.MaterialBatch.Material.Name,
                smb.MaterialBatch.BatchNumber,
                smb.Quantity,
                UomSymbol = smb.UoM != null ? smb.UoM.Symbol : smb.MaterialBatch.UoM.Symbol,
                FromLocation = smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.Warehouse.Name,
                ToShelf = smb.WarehouseLocationShelf.WarehouseLocationRack
                        .WarehouseLocation.Warehouse.Name + " > "
                    + smb.WarehouseLocationShelf.WarehouseLocationRack
                        .WarehouseLocation.Name + " > "
                    + smb.WarehouseLocationShelf.WarehouseLocationRack.Name + " > "
                    + smb.WarehouseLocationShelf.Name,
                smb.CreatedAt,
                PutawayBy = smb.CreatedBy.FirstName + " " + smb.CreatedBy.LastName,
                smb.Note,
                smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.WarehouseId,
            })
            .ToListAsync();

        if (warehouseId.HasValue)
            data = data.Where(x => x.WarehouseId == warehouseId.Value).ToList();

        var result = data
            .OrderByDescending(x => x.CreatedAt)
            .Select((x, i) => new PutawayRegisterDto
            {
                No = i + 1,
                Material = x.Name,
                BatchNo = x.BatchNumber,
                Quantity = x.Quantity.ToString("0"),
                UOM = x.UomSymbol,
                FromLocation = x.FromLocation,
                ToShelf = x.ToShelf,
                PutawayDate = x.CreatedAt,
                PutawayBy = x.PutawayBy,
                Note = x.Note,
            })
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<StockAdjustmentAuditDto>>> GetStockAdjustmentAudit(
        ReportFilter filter, string reasonCode = null, Guid? warehouseId = null,
        Guid? departmentId = null)
    {
        var query = context.StockAdjustmentLines.IgnoreQueryFilters().AsNoTracking()
            .Where(sal => !sal.DeletedAt.HasValue && sal.ShelfMaterialBatchId.HasValue);

        if (filter.StartDate.HasValue)
            query = query.Where(sal => sal.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.AddDays(1);
            query = query.Where(sal => sal.CreatedAt < end);
        }

        if (!string.IsNullOrEmpty(reasonCode))
            query = query.Where(sal => sal.ReasonCode == reasonCode);

        if (departmentId.HasValue)
            query = query.Where(sal => sal.ShelfMaterialBatch.MaterialBatch.Material.Departments.Any(md =>
                md.DepartmentId == departmentId.Value && !md.DeletedAt.HasValue));

        var data = await query
            .Select(sal => new
            {
                sal.StockAdjustment.AdjustmentNumber,
                sal.StockAdjustment.AdjustmentDate,
                MaterialName = sal.ShelfMaterialBatch.MaterialBatch.Material.Name,
                BatchNo = sal.ShelfMaterialBatch.MaterialBatch.BatchNumber,
                Shelf = sal.ShelfMaterialBatch.WarehouseLocationShelf.WarehouseLocationRack
                        .WarehouseLocation.Warehouse.Name + " > "
                    + sal.ShelfMaterialBatch.WarehouseLocationShelf.WarehouseLocationRack
                        .WarehouseLocation.Name + " > "
                    + sal.ShelfMaterialBatch.WarehouseLocationShelf
                        .WarehouseLocationRack.Name + " > "
                    + sal.ShelfMaterialBatch.WarehouseLocationShelf.Name,
                sal.SystemQuantitySnapshot,
                sal.PhysicalCount,
                sal.Variance,
                sal.ReasonCode,
                sal.Notes,
                AdjustedBy = sal.CreatedBy.FirstName + " " + sal.CreatedBy.LastName,
                sal.ShelfMaterialBatch.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.WarehouseId,
            })
            .ToListAsync();

        if (warehouseId.HasValue)
            data = data.Where(x => x.WarehouseId == warehouseId.Value).ToList();

        var result = data
            .OrderByDescending(x => x.AdjustmentDate)
            .ThenBy(x => x.MaterialName)
            .Select((x, i) => new StockAdjustmentAuditDto
            {
                No = i + 1,
                AdjustmentNo = x.AdjustmentNumber,
                AdjustmentDate = x.AdjustmentDate,
                Material = x.MaterialName,
                BatchNo = x.BatchNo,
                Shelf = x.Shelf,
                SystemQuantity = x.SystemQuantitySnapshot,
                PhysicalCount = x.PhysicalCount,
                Variance = x.Variance,
                ReasonCode = x.ReasonCode,
                Notes = x.Notes,
                AdjustedBy = x.AdjustedBy,
            })
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<BatchTraceabilityDto>>> GetBatchTraceability(
        Guid materialBatchId, ReportFilter filter, Guid? departmentId = null)
    {
        var batchEntity = await context.MaterialBatches.IgnoreQueryFilters().AsNoTracking()
            .Where(mb => mb.Id == materialBatchId && !mb.DeletedAt.HasValue)
            .FirstOrDefaultAsync();

        if (batchEntity == null)
            return Result.Success(new List<BatchTraceabilityDto>());

        var uomSymbol = await context.UnitOfMeasures.IgnoreQueryFilters().AsNoTracking()
            .Where(u => u.Id == batchEntity.UoMId)
            .Select(u => u.Symbol)
            .FirstOrDefaultAsync();

        var grnNumber = batchEntity.GrnId.HasValue
            ? await context.Grns.IgnoreQueryFilters().AsNoTracking()
                .Where(g => g.Id == batchEntity.GrnId.Value)
                .Select(g => g.GrnNumber)
                .FirstOrDefaultAsync()
            : null;

        var batch = new
        {
            batchEntity.BatchNumber,
            batchEntity.TotalQuantity,
            UomSymbol = uomSymbol,
            GrnNumber = grnNumber,
            batchEntity.DateReceived,
            batchEntity.DateApproved,
            GrnGeneratedAt = batchEntity.GrnId.HasValue ? (DateTime?)batchEntity.CreatedAt : null,
        };

        var events = new List<BatchTraceabilityDto>();
        int seq = 0;

        events.Add(new BatchTraceabilityDto
        {
            No = ++seq,
            EventType = "Received",
            EventDate = batch.DateReceived,
            Quantity = batch.TotalQuantity,
            UOM = batch.UomSymbol,
            ReferenceDocument = batch.GrnNumber,
            Remarks = "Initial receipt",
        });

        var putaways = await context.ShelfMaterialBatches.IgnoreQueryFilters().AsNoTracking()
            .Where(smb => smb.MaterialBatchId == materialBatchId && !smb.DeletedAt.HasValue)
            .Select(smb => new BatchTraceabilityDto
            {
                No = 0,
                EventType = "Putaway",
                EventDate = smb.CreatedAt,
                ToLocation = smb.WarehouseLocationShelf.WarehouseLocationRack
                        .WarehouseLocation.Warehouse.Name + " > "
                    + smb.WarehouseLocationShelf.WarehouseLocationRack
                        .WarehouseLocation.Name + " > "
                    + smb.WarehouseLocationShelf.WarehouseLocationRack.Name + " > "
                    + smb.WarehouseLocationShelf.Name,
                Quantity = smb.Quantity,
                UOM = smb.UoM != null ? smb.UoM.Symbol : batch.UomSymbol,
                PerformedBy = smb.CreatedBy.FirstName + " " + smb.CreatedBy.LastName,
            })
            .ToListAsync();

        foreach (var p in putaways) { p.No = ++seq; events.Add(p); }

        var transfers = await context.MassMaterialBatchMovements.IgnoreQueryFilters().AsNoTracking()
            .Where(m => m.BatchId == materialBatchId && !m.DeletedAt.HasValue)
            .Select(m => new BatchTraceabilityDto
            {
                No = 0,
                EventType = "Transferred",
                EventDate = m.MovedAt,
                FromLocation = m.FromWarehouse.Name,
                ToLocation = m.ToWarehouse.Name,
                Quantity = m.Quantity,
                PerformedBy = m.MovedBy.FirstName + " " + m.MovedBy.LastName,
                Remarks = m.MovementType.ToString(),
            })
            .ToListAsync();

        foreach (var t in transfers) { t.No = ++seq; events.Add(t); }

        var reservations = await context.MaterialBatchReservedQuantities.IgnoreQueryFilters().AsNoTracking()
            .Where(r => r.MaterialBatchId == materialBatchId && !r.DeletedAt.HasValue)
            .Select(r => new BatchTraceabilityDto
            {
                No = 0,
                EventType = "Reserved",
                EventDate = r.CreatedAt,
                ToLocation = r.Warehouse.Name,
                Quantity = r.Quantity,
                UOM = r.UoM.Symbol,
                ReferenceDocument = r.ProductionScheduleProduct.ProductionSchedule.Code,
                Remarks = "Reserved for production",
            })
            .ToListAsync();

        foreach (var r in reservations) { r.No = ++seq; events.Add(r); }

        var consumptions = await context.MaterialBatchEvents.IgnoreQueryFilters().AsNoTracking()
            .Where(e => e.BatchId == materialBatchId && e.Type == EventType.Consumed && !e.DeletedAt.HasValue)
            .Select(e => new BatchTraceabilityDto
            {
                No = 0,
                EventType = "Consumed",
                EventDate = e.ConsumedAt ?? e.CreatedAt,
                ToLocation = e.ConsumptionWarehouse.Name,
                Quantity = e.Quantity,
                PerformedBy = e.User.FirstName + " " + e.User.LastName,
            })
            .ToListAsync();

        foreach (var c in consumptions) { c.No = ++seq; events.Add(c); }

        events = events.OrderBy(e => e.EventDate).ThenBy(e => e.No).ToList();
        for (int i = 0; i < events.Count; i++) events[i].No = i + 1;

        return Result.Success(events);
    }

    public async Task<Result<List<InterWarehouseSwapRequestDto>>> GetInterWarehouseSwapRequests(
        ReportFilter filter, Guid? warehouseId = null, string status = null,
        Guid? departmentId = null)
    {
        var query = context.SwapRequests.IgnoreQueryFilters().AsNoTracking()
            .Where(sr => !sr.DeletedAt.HasValue);

        if (filter.StartDate.HasValue)
            query = query.Where(sr => sr.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.AddDays(1);
            query = query.Where(sr => sr.CreatedAt < end);
        }

        if (warehouseId.HasValue)
            query = query.Where(sr =>
                sr.FirstWarehouseId == warehouseId.Value
                || sr.SecondWarehouseId == warehouseId.Value);

        if (departmentId.HasValue)
            query = query.Where(sr => sr.FirstSwapShelfMaterialBatches.Any(sw =>
                sw.MaterialBatch.Material.Departments.Any(md =>
                    md.DepartmentId == departmentId.Value && !md.DeletedAt.HasValue)));

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<SwapRequestStatus>(status, true, out var parsed))
            query = query.Where(sr => sr.Status == parsed);

        var requests = await query
            .Select(sr => new
            {
                FirstWarehouseName = sr.FirstWarehouse.Name,
                SecondWarehouseName = sr.SecondWarehouse.Name,
                sr.Status,
                sr.CreatedAt,
                sr.ActionedAt,
                sr.ActionNote,
                ActionedByFirstName = sr.ActionedBy.FirstName,
                ActionedByLastName = sr.ActionedBy.LastName,
                Requester = sr.CreatedBy.FirstName + " " + sr.CreatedBy.LastName,
                Batches = sr.FirstSwapShelfMaterialBatches.Select(sw => new
                {
                    MaterialName = sw.MaterialBatch.Material.Name,
                    sw.MaterialBatch.BatchNumber,
                    sw.Quantity,
                    UomSymbol = sw.UoM.Symbol,
                }).ToList()
            })
            .ToListAsync();

        var flat = requests.SelectMany(sr => sr.Batches.Select(b => new
        {
            sr.FirstWarehouseName,
            sr.SecondWarehouseName,
            sr.Status,
            sr.CreatedAt,
            sr.ActionedAt,
            sr.ActionNote,
            sr.ActionedByFirstName,
            sr.ActionedByLastName,
            b.MaterialName,
            b.BatchNumber,
            b.Quantity,
            b.UomSymbol,
            sr.Requester,
        })).ToList();

        var result = flat
            .OrderByDescending(x => x.CreatedAt)
            .Select((x, i) => new InterWarehouseSwapRequestDto
            {
                No = i + 1,
                FirstWarehouse = x.FirstWarehouseName,
                SecondWarehouse = x.SecondWarehouseName,
                Status = x.Status.ToString(),
                Material = x.MaterialName,
                BatchNo = x.BatchNumber,
                Quantity = x.Quantity,
                UOM = x.UomSymbol,
                Requester = x.Requester,
                ActionedBy = x.ActionedByFirstName != null ? x.ActionedByFirstName + " " + x.ActionedByLastName : null,
                ActionedAt = x.ActionedAt,
                ActionNote = x.ActionNote,
            })
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<StockTransferInterDepartmentDetailDto>>> GetStockTransferInterDepartmentDetail(
        ReportFilter filter, Guid? fromDepartmentId = null, Guid? toDepartmentId = null,
        string status = null, Guid? departmentId = null)
    {
        var query = context.StockTransferSources.IgnoreQueryFilters().AsNoTracking()
            .Where(sts => !sts.DeletedAt.HasValue && !sts.StockTransfer.DeletedAt.HasValue);

        if (filter.StartDate.HasValue)
            query = query.Where(sts => sts.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.AddDays(1);
            query = query.Where(sts => sts.CreatedAt < end);
        }

        if (fromDepartmentId.HasValue)
            query = query.Where(sts => sts.FromDepartmentId == fromDepartmentId.Value);

        if (toDepartmentId.HasValue)
            query = query.Where(sts => sts.ToDepartmentId == toDepartmentId.Value);

        if (departmentId.HasValue)
            query = query.Where(sts => sts.StockTransfer.Material.Departments.Any(md =>
                md.DepartmentId == departmentId.Value && !md.DeletedAt.HasValue));

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<StockTransferStatus>(status, true, out var parsed))
            query = query.Where(sts => sts.Status == parsed);

        var data = await query
            .Select(sts => new
            {
                sts.StockTransfer.Code,
                MaterialName = sts.StockTransfer.Material.Name,
                UomSymbol = sts.StockTransfer.UoM.Symbol,
                sts.StockTransfer.RequiredQuantity,
                sts.StockTransfer.Reason,
                FromDeptName = sts.FromDepartment.Name,
                ToDeptName = sts.ToDepartment.Name,
                sts.Status,
                sts.CreatedAt,
                sts.ApprovedAt,
                ApprovedByFirstName = sts.ApprovedBy.FirstName,
                ApprovedByLastName = sts.ApprovedBy.LastName,
                sts.IssuedAt,
                IssuedByFirstName = sts.IssuedBy.FirstName,
                IssuedByLastName = sts.IssuedBy.LastName,
            })
            .ToListAsync();

        var result = data
            .OrderByDescending(x => x.ApprovedAt ?? x.CreatedAt)
            .Select((x, i) => new StockTransferInterDepartmentDetailDto
            {
                No = i + 1,
                TransferCode = x.Code,
                Material = x.MaterialName,
                UOM = x.UomSymbol,
                RequiredQuantity = x.RequiredQuantity,
                FromDepartment = x.FromDeptName,
                ToDepartment = x.ToDeptName,
                Status = x.Status.ToString(),
                ApprovedAt = x.ApprovedAt,
                ApprovedBy = x.ApprovedByFirstName != null ? x.ApprovedByFirstName + " " + x.ApprovedByLastName : null,
                IssuedAt = x.IssuedAt,
                IssuedBy = x.IssuedByFirstName != null ? x.IssuedByFirstName + " " + x.IssuedByLastName : null,
                Reason = x.Reason,
            })
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<MaterialExpiryProjectionDto>>> GetMaterialExpiryProjection(
        ExpiryWindowFilter? window = null, Guid? warehouseId = null, Guid? materialId = null,
        Guid? departmentId = null)
    {
        var today = DateTime.UtcNow.Date;

        var query = context.ShelfMaterialBatches.IgnoreQueryFilters().AsNoTracking()
            .Where(smb => !smb.DeletedAt.HasValue && smb.Quantity > 0
                && !smb.MaterialBatch.DeletedAt.HasValue
                && smb.MaterialBatch.ExpiryDate.HasValue);

        if (warehouseId.HasValue)
            query = query.Where(smb =>
                smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.WarehouseId == warehouseId.Value);

        if (materialId.HasValue)
            query = query.Where(smb => smb.MaterialBatch.MaterialId == materialId.Value);

        if (departmentId.HasValue)
            query = query.Where(smb => smb.MaterialBatch.Material.Departments.Any(md =>
                md.DepartmentId == departmentId.Value && !md.DeletedAt.HasValue));

        var data = await query
            .Select(smb => new
            {
                smb.MaterialBatch.Material.Name,
                smb.MaterialBatch.Material.Code,
                smb.MaterialBatch.BatchNumber,
                smb.MaterialBatch.ExpiryDate,
                smb.Quantity,
                UomSymbol = smb.UoM != null ? smb.UoM.Symbol : smb.MaterialBatch.UoM.Symbol,
                WarehouseName = smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.Warehouse.Name,
                Shelf = smb.WarehouseLocationShelf.WarehouseLocationRack
                        .WarehouseLocation.Warehouse.Name + " > "
                    + smb.WarehouseLocationShelf.WarehouseLocationRack
                        .WarehouseLocation.Name + " > "
                    + smb.WarehouseLocationShelf.WarehouseLocationRack.Name + " > "
                    + smb.WarehouseLocationShelf.Name,
                smb.CreatedAt,
            })
            .ToListAsync();

        var mapped = data.Select(x =>
        {
            var daysUntilExpiry = x.ExpiryDate.HasValue
                ? (int?)(x.ExpiryDate.Value.Date - today).TotalDays : null;
            var daysOnShelf = (int)(today - x.CreatedAt.Date).TotalDays;
            string windowStr;
            if (daysUntilExpiry < 0) windowStr = "Expired";
            else if (daysUntilExpiry <= 30) windowStr = "≤ 30 days";
            else if (daysUntilExpiry <= 60) windowStr = "31–60 days";
            else if (daysUntilExpiry <= 90) windowStr = "61–90 days";
            else windowStr = "> 90 days";

            return new { x.Name, x.Code, x.BatchNumber, x.ExpiryDate, daysUntilExpiry, windowStr, x.Quantity, x.UomSymbol, x.WarehouseName, x.Shelf, daysOnShelf };
        });

        if (window.HasValue)
        {
            mapped = mapped.Where(x =>
            {
                return window.Value switch
                {
                    ExpiryWindowFilter.Within30Days     => x.daysUntilExpiry is >= 0 and <= 30,
                    ExpiryWindowFilter.Within31To60Days => x.daysUntilExpiry is >= 31 and <= 60,
                    ExpiryWindowFilter.Within61To90Days => x.daysUntilExpiry is >= 61 and <= 90,
                    ExpiryWindowFilter.Over90Days       => x.daysUntilExpiry > 90,
                    _                                   => true
                };
            });
        }

        var result = mapped
            .OrderBy(x => x.ExpiryDate)
            .Select((x, i) => new MaterialExpiryProjectionDto
            {
                No = i + 1,
                Material = x.Name,
                MaterialCode = x.Code,
                BatchNo = x.BatchNumber,
                ExpiryDate = x.ExpiryDate,
                DaysUntilExpiry = x.daysUntilExpiry,
                ExpiryWindow = x.windowStr,
                Quantity = x.Quantity,
                UOM = x.UomSymbol,
                Warehouse = x.WarehouseName,
                Shelf = x.Shelf,
                DaysOnShelf = x.daysOnShelf,
            })
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<SlowMovingInventoryDto>>> GetSlowMovingInventory(
        InactivityThreshold threshold = InactivityThreshold.Days90, Guid? warehouseId = null,
        Guid? materialId = null, Guid? departmentId = null)
    {
        var query = context.ShelfMaterialBatches.IgnoreQueryFilters().AsNoTracking()
            .Where(smb => !smb.DeletedAt.HasValue && smb.Quantity > 0
                && !smb.MaterialBatch.DeletedAt.HasValue);

        if (warehouseId.HasValue)
            query = query.Where(smb =>
                smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.WarehouseId == warehouseId.Value);

        if (materialId.HasValue)
            query = query.Where(smb => smb.MaterialBatch.MaterialId == materialId.Value);

        if (departmentId.HasValue)
            query = query.Where(smb => smb.MaterialBatch.Material.Departments.Any(md =>
                md.DepartmentId == departmentId.Value && !md.DeletedAt.HasValue));

        var data = await query
            .Select(smb => new
            {
                smb.MaterialBatch.Material.Name,
                smb.MaterialBatch.Material.Code,
                smb.MaterialBatch.BatchNumber,
                smb.Quantity,
                UomSymbol = smb.UoM != null ? smb.UoM.Symbol : smb.MaterialBatch.UoM.Symbol,
                smb.UpdatedAt,
                smb.CreatedAt,
                WarehouseName = smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.Warehouse.Name,
                Shelf = smb.WarehouseLocationShelf.WarehouseLocationRack
                        .WarehouseLocation.Warehouse.Name + " > "
                    + smb.WarehouseLocationShelf.WarehouseLocationRack
                        .WarehouseLocation.Name + " > "
                    + smb.WarehouseLocationShelf.WarehouseLocationRack.Name + " > "
                    + smb.WarehouseLocationShelf.Name,
            })
            .ToListAsync();

        var today = DateTime.UtcNow.Date;
        var filtered = data
            .Select(x =>
            {
                var lastMove = x.UpdatedAt ?? x.CreatedAt;
                var daysSince = (int)(today - lastMove.Date).TotalDays;
                return new { x.Name, x.Code, x.BatchNumber, x.Quantity, x.UomSymbol, lastMove, daysSince, x.WarehouseName, x.Shelf };
            })
            .Where(x => x.daysSince >= (int)threshold)
            .OrderByDescending(x => x.daysSince)
            .ToList();

        var result = filtered
            .Select((x, i) => new SlowMovingInventoryDto
            {
                No = i + 1,
                Material = x.Name,
                MaterialCode = x.Code,
                BatchNo = x.BatchNumber,
                Quantity = x.Quantity,
                UOM = x.UomSymbol,
                LastMovementDate = x.lastMove,
                DaysSinceLastMove = x.daysSince,
                Warehouse = x.WarehouseName,
                Shelf = x.Shelf,
                EstValue = 0,
            })
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<BinCardTransactionLedgerDto>>> GetBinCardTransactionLedger(
        Guid materialBatchId, ReportFilter filter, Guid? departmentId = null)
    {
        var raw = await context.BinCardInformation.IgnoreQueryFilters().AsNoTracking()
            .Where(b => b.MaterialBatchId == materialBatchId && !b.DeletedAt.HasValue)
            .Select(b => new
            {
                b.CreatedAt,
                b.Description,
                b.WayBill,
                b.ArNumber,
                Supplier = b.Supplier ?? b.MaterialBatch.Checklist.Supplier.Name,
                Manufacturer = b.Manufacturer ?? b.MaterialBatch.Checklist.Manufacturer.Name,
                b.QuantityReceived,
                b.QuantityIssued,
                b.BalanceQuantity,
                b.UoMId,
                ProductName = b.Product != null ? b.Product.Name : null,
            })
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();

        var uomIds = raw.Where(x => x.UoMId.HasValue)
            .Select(x => x.UoMId.Value).Distinct().ToList();

        var uomSymbols = uomIds.Count > 0
            ? await context.UnitOfMeasures.IgnoreQueryFilters().AsNoTracking()
                .Where(u => uomIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Symbol)
            : new Dictionary<Guid, string>();

        var data = raw
            .Select((x, i) => new BinCardTransactionLedgerDto
            {
                No = i + 1,
                Date = x.CreatedAt,
                Description = x.Description,
                WayBill = x.WayBill,
                ArNumber = x.ArNumber,
                Supplier = x.Supplier,
                Manufacturer = x.Manufacturer,
                QuantityReceived = x.QuantityReceived,
                QuantityIssued = x.QuantityIssued,
                BalanceQuantity = x.BalanceQuantity,
                UOM = x.UoMId.HasValue && uomSymbols.TryGetValue(x.UoMId.Value, out var sym) ? sym : null,
                Product = x.ProductName,
            })
            .ToList();

        return Result.Success(data);
    }

    public async Task<Result<IEnumerable<OperationsSummaryDto>>> GetOperationsSummary(
        ReportFilter filter, Guid? warehouseId = null, Guid? departmentId = null)
    {
        var startDate = filter.StartDate ?? DateTime.UtcNow.Date;
        var endDate = filter.EndDate.HasValue
            ? filter.EndDate.Value.AddDays(1)
            : startDate.AddDays(1);

        var periodLabel = filter.StartDate.HasValue || filter.EndDate.HasValue
            ? $"{startDate:yyyy-MM-dd} to {filter.EndDate?.AddDays(1):yyyy-MM-dd}"
            : "Today";

        if (departmentId.HasValue)
        {
            var deptId = departmentId.Value;

            var deptName = await context.Departments.IgnoreQueryFilters().AsNoTracking()
                .Where(d => d.Id == deptId)
                .Select(d => d.Name)
                .FirstOrDefaultAsync() ?? "Unknown";

            var materialsReceived = await context.DistributedRequisitionMaterials
                .IgnoreQueryFilters().AsNoTracking()
                .Where(drm => !drm.DeletedAt.HasValue && drm.ArrivedAt >= startDate && drm.ArrivedAt < endDate
                    && drm.WarehouseArrivalLocation != null
                    && drm.WarehouseArrivalLocation.Warehouse.DepartmentId == deptId)
                .CountAsync();

            var grnsCreated = await context.Grns.IgnoreQueryFilters().AsNoTracking()
                .Where(g => !g.DeletedAt.HasValue && g.CreatedAt >= startDate && g.CreatedAt < endDate)
                .CountAsync();

            var batchesPutAway = await context.ShelfMaterialBatches.IgnoreQueryFilters().AsNoTracking()
                .Where(smb => !smb.DeletedAt.HasValue
                    && smb.CreatedAt >= startDate && smb.CreatedAt < endDate
                    && smb.WarehouseLocationShelf.WarehouseLocationRack
                        .WarehouseLocation.Warehouse.DepartmentId == deptId)
                .CountAsync();

            var stockAdjustments = await context.StockAdjustments.IgnoreQueryFilters().AsNoTracking()
                .Where(sa => !sa.DeletedAt.HasValue
                    && sa.CreatedAt >= startDate && sa.CreatedAt < endDate
                    && sa.Lines.Any(l => l.ShelfMaterialBatch != null
                        && l.ShelfMaterialBatch.WarehouseLocationShelf.WarehouseLocationRack
                            .WarehouseLocation.Warehouse.DepartmentId == deptId))
                .CountAsync();

            var stockTransfers = await context.StockTransferSources.IgnoreQueryFilters().AsNoTracking()
                .Where(sts => !sts.DeletedAt.HasValue
                    && sts.CreatedAt >= startDate && sts.CreatedAt < endDate
                    && (sts.FromDepartmentId == deptId || sts.ToDepartmentId == deptId))
                .CountAsync();

            var swapsApproved = await context.SwapRequests.IgnoreQueryFilters().AsNoTracking()
                .Where(sr => !sr.DeletedAt.HasValue && sr.Status == SwapRequestStatus.Approved
                    && sr.CreatedAt >= startDate && sr.CreatedAt < endDate
                    && (sr.FirstWarehouse.DepartmentId == deptId
                        || sr.SecondWarehouse.DepartmentId == deptId))
                .CountAsync();

            var checklistsDone = await context.Checklists.IgnoreQueryFilters().AsNoTracking()
                .Where(cl => !cl.DeletedAt.HasValue && cl.CheckedAt.HasValue
                    && cl.CheckedAt >= startDate && cl.CheckedAt < endDate
                    && cl.DistributedRequisitionMaterial.WarehouseArrivalLocation != null
                    && cl.DistributedRequisitionMaterial.WarehouseArrivalLocation
                        .Warehouse.DepartmentId == deptId)
                .CountAsync();

            return Result.Success(new List<OperationsSummaryDto>
            {
                new OperationsSummaryDto
                {
                    Department = deptName,
                    Period = periodLabel,
                    MaterialsReceived = materialsReceived,
                    GrnsCreated = grnsCreated,
                    BatchesPutAway = batchesPutAway,
                    StockAdjustments = stockAdjustments,
                    StockTransfers = stockTransfers,
                    SwapsApproved = swapsApproved,
                    ChecklistsDone = checklistsDone,
                }
            }.AsEnumerable());
        }
        else
        {
            var deptIds = await context.Departments.IgnoreQueryFilters().AsNoTracking()
                .Where(d => !d.DeletedAt.HasValue && d.Type == DepartmentType.Production)
                .Select(d => new { d.Id, d.Name })
                .ToListAsync();

            var results = new List<OperationsSummaryDto>();

            foreach (var dept in deptIds)
            {
                var materialsReceived = await context.DistributedRequisitionMaterials
                    .IgnoreQueryFilters().AsNoTracking()
                    .Where(drm => !drm.DeletedAt.HasValue && drm.ArrivedAt >= startDate && drm.ArrivedAt < endDate
                        && drm.WarehouseArrivalLocation != null
                        && drm.WarehouseArrivalLocation.Warehouse.DepartmentId == dept.Id)
                    .CountAsync();

                var batchesPutAway = await context.ShelfMaterialBatches.IgnoreQueryFilters().AsNoTracking()
                    .Where(smb => !smb.DeletedAt.HasValue
                        && smb.CreatedAt >= startDate && smb.CreatedAt < endDate
                        && smb.WarehouseLocationShelf.WarehouseLocationRack
                            .WarehouseLocation.Warehouse.DepartmentId == dept.Id)
                    .CountAsync();

                var stockAdjustments = await context.StockAdjustments.IgnoreQueryFilters().AsNoTracking()
                    .Where(sa => !sa.DeletedAt.HasValue
                        && sa.CreatedAt >= startDate && sa.CreatedAt < endDate
                        && sa.Lines.Any(l => l.ShelfMaterialBatch != null
                            && l.ShelfMaterialBatch.WarehouseLocationShelf.WarehouseLocationRack
                                .WarehouseLocation.Warehouse.DepartmentId == dept.Id))
                    .CountAsync();

                var stockTransfers = await context.StockTransferSources.IgnoreQueryFilters().AsNoTracking()
                    .Where(sts => !sts.DeletedAt.HasValue
                        && sts.CreatedAt >= startDate && sts.CreatedAt < endDate
                        && (sts.FromDepartmentId == dept.Id || sts.ToDepartmentId == dept.Id))
                    .CountAsync();

                var swapsApproved = await context.SwapRequests.IgnoreQueryFilters().AsNoTracking()
                    .Where(sr => !sr.DeletedAt.HasValue && sr.Status == SwapRequestStatus.Approved
                        && sr.CreatedAt >= startDate && sr.CreatedAt < endDate
                        && (sr.FirstWarehouse.DepartmentId == dept.Id
                            || sr.SecondWarehouse.DepartmentId == dept.Id))
                    .CountAsync();

                var checklistsDone = await context.Checklists.IgnoreQueryFilters().AsNoTracking()
                    .Where(cl => !cl.DeletedAt.HasValue && cl.CheckedAt.HasValue
                        && cl.CheckedAt >= startDate && cl.CheckedAt < endDate
                        && cl.DistributedRequisitionMaterial.WarehouseArrivalLocation != null
                        && cl.DistributedRequisitionMaterial.WarehouseArrivalLocation
                            .Warehouse.DepartmentId == dept.Id)
                    .CountAsync();

                results.Add(new OperationsSummaryDto
                {
                    Department = dept.Name,
                    Period = periodLabel,
                    MaterialsReceived = materialsReceived,
                    GrnsCreated = 0,
                    BatchesPutAway = batchesPutAway,
                    StockAdjustments = stockAdjustments,
                    StockTransfers = stockTransfers,
                    SwapsApproved = swapsApproved,
                    ChecklistsDone = checklistsDone,
                });
            }

            return Result.Success(results.AsEnumerable());
        }
    }

    public async Task<Result<List<QcPendingDto>>> GetQcPendingReport(
        Guid? warehouseId = null, Guid? supplierId = null, Guid? departmentId = null)
    {
        var query = context.DistributedRequisitionMaterials.IgnoreQueryFilters().AsNoTracking()
            .Where(drm => !drm.DeletedAt.HasValue
                && drm.ArrivedAt.HasValue
                && (drm.CheckLists == null || !drm.CheckLists.Any(cl => cl.CheckedAt.HasValue)));

        if (warehouseId.HasValue)
            query = query.Where(drm =>
                drm.DistributedRequisitionItems.Any(dri => dri.WarehouseId == warehouseId.Value));

        if (supplierId.HasValue) 
            query = query.Where(drm => drm.ShipmentInvoice.SupplierId == supplierId.Value);

        if (departmentId.HasValue)
            query = query.Where(drm => drm.Material.Departments.Any(md =>
                md.DepartmentId == departmentId.Value && !md.DeletedAt.HasValue));

        var today = DateTime.UtcNow.Date;

        var data = await query
            .Select(drm => new
            {
                drm.Material.Name,
                drm.Material.Code,
                SupplierName = drm.ShipmentInvoice.Supplier.Name,
                drm.Quantity,
                UomSymbol = drm.UoM.Symbol,
                drm.ArrivedAt,
                WarehouseName = drm.WarehouseArrivalLocation.Warehouse.Name,
                ArrivalLocation = drm.WarehouseArrivalLocation.Name,
            })
            .ToListAsync();

        var result = data
            .Select(x => new
            {
                x.Name,
                x.Code,
                x.SupplierName,
                x.Quantity,
                x.UomSymbol,
                x.ArrivedAt,
                DaysWaiting = x.ArrivedAt.HasValue ? (int)(today - x.ArrivedAt.Value.Date).TotalDays : 0,
                x.WarehouseName,
                x.ArrivalLocation,
            })
            .OrderByDescending(x => x.DaysWaiting)
            .Select((x, i) => new QcPendingDto
            {
                No = i + 1,
                Material = x.Name,
                MaterialCode = x.Code,
                Supplier = x.SupplierName,
                Quantity = x.Quantity,
                UOM = x.UomSymbol,
                ChecklistDate = x.ArrivedAt,
                DaysWaiting = x.DaysWaiting,
                Warehouse = x.WarehouseName,
                ArrivalLocation = x.ArrivalLocation,
            })
            .ToList();

        return Result.Success(result);
    }
    
    public async Task<Result<List<ReorderLevelVsStockDto>>> GetReorderLevelVsStock(
        Guid? departmentId = null, MaterialKind? materialKind = null)
    {
        var mdQuery = context.MaterialDepartments.IgnoreQueryFilters().AsNoTracking()
            .Where(md => !md.DeletedAt.HasValue && !md.Material.DeletedAt.HasValue);

        if (departmentId.HasValue)
            mdQuery = mdQuery.Where(md => md.DepartmentId == departmentId.Value);

        if (materialKind.HasValue)
            mdQuery = mdQuery.Where(md => md.Material.Kind == materialKind.Value);

        var materialDepts = await mdQuery
            .Select(md => new
            {
                md.MaterialId,
                MaterialName = md.Material.Name,
                md.Material.Code,
                md.Material.Kind,
                UomSymbol = md.UoM.Symbol,
                DepartmentName = md.Department.Name,
                md.ReOrderLevel,
            })
            .ToListAsync();

        var materialIds = materialDepts.Select(m => m.MaterialId).Distinct().ToList();

        var stockData = await context.ShelfMaterialBatches.IgnoreQueryFilters().AsNoTracking()
            .Where(smb => materialIds.Contains(smb.MaterialBatch.MaterialId)
                && !smb.DeletedAt.HasValue && smb.Quantity > 0)
            .GroupBy(smb => smb.MaterialBatch.MaterialId)
            .Select(g => new { MaterialId = g.Key, CurrentStock = g.Sum(smb => smb.Quantity) })
            .ToListAsync();

        var stockLookup = stockData.ToDictionary(x => x.MaterialId, x => x.CurrentStock);

        var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-12);
        var consumptionData = await context.BinCardInformation.IgnoreQueryFilters().AsNoTracking()
            .Where(b => materialIds.Contains(b.MaterialBatch.MaterialId)
                && b.QuantityIssued > 0
                && !b.DeletedAt.HasValue
                && b.CreatedAt >= twelveMonthsAgo)
            .GroupBy(b => b.MaterialBatch.MaterialId)
            .Select(g => new { MaterialId = g.Key, TotalIssued = g.Sum(b => b.QuantityIssued) })
            .ToListAsync();

        var consumptionLookup = consumptionData.ToDictionary(x => x.MaterialId, x => x.TotalIssued / 12m);

        var result = materialDepts
            .GroupBy(md => md.MaterialId)
            .SelectMany(g =>
            {
                var currentStock = stockLookup.GetValueOrDefault(g.Key, 0m);
                var avgMonthlyConsumption = consumptionLookup.GetValueOrDefault(g.Key, 0m);
                var first = g.First();
                decimal monthsOfStock;
                if (avgMonthlyConsumption > 0)
                    monthsOfStock = currentStock / avgMonthlyConsumption;
                else if (first.ReOrderLevel > 0)
                    monthsOfStock = currentStock / first.ReOrderLevel;
                else
                    monthsOfStock = 0;
                var suggestedOrder = Math.Max(0, first.ReOrderLevel - currentStock);
                var risk = monthsOfStock < 1 ? "High" : monthsOfStock <= 3 ? "Medium" : "Low";

                return g.Select(md => new ReorderLevelVsStockDto
                {
                    Material = md.MaterialName,
                    MaterialCode = md.Code,
                    MaterialType = md.Kind == MaterialKind.Raw ? "Raw" : "Packaging",
                    CurrentStock = currentStock,
                    ReorderLevel = md.ReOrderLevel,
                    AvgMonthlyConsumption = Math.Round(avgMonthlyConsumption, 2),
                    MonthsOfStock = Math.Round(monthsOfStock, 2),
                    SuggestedOrderQty = Math.Round(suggestedOrder, 2),
                    StockOutRisk = risk,
                    UOM = md.UomSymbol,
                    Department = md.DepartmentName,
                });
            })
            .OrderBy(x => x.Material)
            .Select((x, i) => { x.No = i + 1; return x; })
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<InventoryValuationSummaryDto>>> GetInventoryValuationSummary(
        WarehouseType? warehouseType = null, Division? division = null,
        UnitOfMeasureCategory? uomGroup = null, Guid? departmentId = null)
    {
        var query = context.ShelfMaterialBatches.IgnoreQueryFilters().AsNoTracking()
            .Where(smb => !smb.DeletedAt.HasValue && smb.Quantity > 0
                && !smb.MaterialBatch.DeletedAt.HasValue
                && !smb.MaterialBatch.Material.DeletedAt.HasValue);

        if (warehouseType.HasValue)
            query = query.Where(smb =>
                smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.Warehouse.Type == warehouseType.Value);

        if (division.HasValue)
            query = query.Where(smb =>
                smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.Warehouse.Department.Division == division.Value);

        if (departmentId.HasValue)
            query = query.Where(smb =>
                smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.Warehouse.DepartmentId == departmentId.Value);

        var data = await query
            .Select(smb => new
            {
                WarehouseType = smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.Warehouse.Type,
                smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.Warehouse.Department.Division,
                smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.Warehouse.Name,
                smb.MaterialBatch.MaterialId,
                smb.Quantity,
                UomCategory = smb.UoM != null ? smb.UoM.Category : smb.MaterialBatch.UoM.Category,
                UomSymbol = smb.UoM != null ? smb.UoM.Symbol : smb.MaterialBatch.UoM.Symbol,
            })
            .ToListAsync();

        if (uomGroup.HasValue)
            data = data.Where(x => x.UomCategory == uomGroup.Value).ToList();

        var materialIds = data.Select(x => x.MaterialId).Distinct().ToList();

        var latestCosts = await context.PurchaseOrderItems.IgnoreQueryFilters().AsNoTracking()
            .Where(poi => materialIds.Contains(poi.MaterialId) && !poi.DeletedAt.HasValue
                && !poi.PurchaseOrder.DeletedAt.HasValue)
            .GroupBy(poi => poi.MaterialId)
            .Select(g => g.OrderByDescending(poi => poi.PurchaseOrder.CreatedAt)
                .Select(poi => new { poi.MaterialId, poi.Price })
                .First())
            .ToDictionaryAsync(x => x.MaterialId, x => x.Price);

        var grouped = data
            .GroupBy(x => new
            {
                x.WarehouseType,
                x.Division,
                x.Name,
                x.UomCategory,
            })
            .Select(g =>
            {
                var totalValue = g.Sum(x =>
                {
                    var unitCost = latestCosts.GetValueOrDefault(x.MaterialId, 0m);
                    return x.Quantity * unitCost;
                });

                var totalQty = g.Sum(x => x.Quantity);
                var itemCount = g.Select(x => x.MaterialId).Distinct().Count();

                var uomGroupLabel = g.Key.UomCategory == UnitOfMeasureCategory.Weight ? "Weight (kg/g)"
                    : g.Key.UomCategory == UnitOfMeasureCategory.Volume ? "Volume (L/ml)"
                    : g.Key.UomCategory == UnitOfMeasureCategory.Countable ? "Count (units/bottles/vials/packs)"
                    : g.Key.UomCategory.ToString();

                return new
                {
                    g.Key.WarehouseType,
                    g.Key.Division,
                    g.Key.Name,
                    itemCount,
                    uomGroupLabel,
                    totalQty,
                    totalValue,
                };
            })
            .OrderBy(x => x.WarehouseType)
            .ThenBy(x => x.Division)
            .ThenBy(x => x.Name)
            .ToList();

        var grandTotal = grouped.Sum(x => x.totalValue);

        var result = grouped
            .Select((x, i) => new InventoryValuationSummaryDto
            {
                No = i + 1,
                WarehouseType = x.WarehouseType.ToString(),
                Division = x.Division.ToString(),
                Warehouse = x.Name,
                ItemMaterialCount = x.itemCount,
                UomGroup = x.uomGroupLabel,
                TotalQuantity = x.totalQty.ToString("0.##"),
                EstUnitCost = grandTotal > 0 && x.totalQty > 0
                    ? Math.Round(x.totalValue / x.totalQty, 4) : 0,
                TotalValue = x.totalValue.ToString("0.##"),
                PercentOfTotalInventory = grandTotal > 0
                    ? Math.Round(x.totalValue / grandTotal * 100, 2) : 0,
            })
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<InventoryValuationDetailDto>>> GetInventoryValuationDetail(
        WarehouseType? warehouseType = null, Division? division = null,
        Guid? warehouseId = null, string valuationType = null,
        Guid? materialId = null, DateTime? expiryDateFrom = null, DateTime? expiryDateTo = null,
        Guid? departmentId = null)
    {
        var today = DateTime.UtcNow.Date;

        var query = context.ShelfMaterialBatches.IgnoreQueryFilters().AsNoTracking()
            .Where(smb => !smb.DeletedAt.HasValue && smb.Quantity > 0
                && !smb.MaterialBatch.DeletedAt.HasValue
                && !smb.MaterialBatch.Material.DeletedAt.HasValue);

        if (warehouseType.HasValue)
            query = query.Where(smb =>
                smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.Warehouse.Type == warehouseType.Value);

        if (division.HasValue)
            query = query.Where(smb =>
                smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.Warehouse.Department.Division == division.Value);

        if (departmentId.HasValue)
            query = query.Where(smb =>
                smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.Warehouse.DepartmentId == departmentId.Value);

        if (warehouseId.HasValue)
            query = query.Where(smb =>
                smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.WarehouseId == warehouseId.Value);

        if (materialId.HasValue)
            query = query.Where(smb => smb.MaterialBatch.MaterialId == materialId.Value);

        if (expiryDateFrom.HasValue)
            query = query.Where(smb => smb.MaterialBatch.ExpiryDate >= expiryDateFrom.Value);

        if (expiryDateTo.HasValue)
            query = query.Where(smb => smb.MaterialBatch.ExpiryDate <= expiryDateTo.Value);

        var data = await query
            .Select(smb => new
            {
                WarehouseType = smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.Warehouse.Type,
                smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.Warehouse.Department.Division,
                WarehouseName = smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.Warehouse.Name,
                MaterialName = smb.MaterialBatch.Material.Name,
                smb.MaterialBatch.Material.Code,
                smb.MaterialBatch.BatchNumber,
                smb.MaterialBatch.MaterialId,
                smb.Quantity,
                UomCategory = smb.UoM != null ? smb.UoM.Category : smb.MaterialBatch.UoM.Category,
                UomSymbol = smb.UoM != null ? smb.UoM.Symbol : smb.MaterialBatch.UoM.Symbol,
                smb.MaterialBatch.ExpiryDate,
                smb.CreatedAt,
                ShelfLocation = smb.WarehouseLocationShelf.WarehouseLocationRack
                        .WarehouseLocation.Warehouse.Name + " > "
                    + smb.WarehouseLocationShelf.WarehouseLocationRack
                        .WarehouseLocation.Name + " > "
                    + smb.WarehouseLocationShelf.WarehouseLocationRack.Name + " > "
                    + smb.WarehouseLocationShelf.Name,
            })
            .ToListAsync();

        var materialIds = data.Select(x => x.MaterialId).Distinct().ToList();

        var latestCosts = await context.PurchaseOrderItems.IgnoreQueryFilters().AsNoTracking()
            .Where(poi => materialIds.Contains(poi.MaterialId) && !poi.DeletedAt.HasValue
                && !poi.PurchaseOrder.DeletedAt.HasValue)
            .GroupBy(poi => poi.MaterialId)
            .Select(g => g.OrderByDescending(poi => poi.PurchaseOrder.CreatedAt)
                .Select(poi => new { poi.MaterialId, poi.Price })
                .First())
            .ToDictionaryAsync(x => x.MaterialId, x => x.Price);

        var mapped = data.Select(x =>
        {
            var unitCost = latestCosts.GetValueOrDefault(x.MaterialId, 0m);
            var totalValue = x.Quantity * unitCost;
            var daysOnShelf = (int)(today - x.CreatedAt.Date).TotalDays;

            return new
            {
                x.WarehouseType,
                x.Division,
                x.WarehouseName,
                x.MaterialName,
                x.Code,
                x.BatchNumber,
                x.UomSymbol,
                x.Quantity,
                unitCost,
                totalValue,
                x.ShelfLocation,
                x.ExpiryDate,
                daysOnShelf,
            };
        });

        var result = mapped
            .OrderBy(x => x.WarehouseType)
            .ThenBy(x => x.WarehouseName)
            .ThenBy(x => x.MaterialName)
            .Select((x, i) => new InventoryValuationDetailDto
            {
                No = i + 1,
                WarehouseType = x.WarehouseType.ToString(),
                Division = x.Division.ToString(),
                MaterialItem = x.MaterialName,
                Code = x.Code,
                BatchNo = x.BatchNumber,
                ValuationType = "UOM-Based",
                Warehouse = x.WarehouseName,
                UOM = x.UomSymbol,
                QuantityUOM = x.Quantity.ToString("0.##"),
                UnitSize = null,
                ItemCount = x.Quantity,
                EstUnitCostVal = Math.Round(x.unitCost, 4),
                CostBasis = "Per UOM",
                TotalValue = x.totalValue.ToString("0.##"),
                ShelfLocation = x.ShelfLocation,
                ExpiryDate = x.ExpiryDate,
                DaysOnShelf = x.daysOnShelf,
            })
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<WarehouseEmployeeActivityDto>>> GetWarehouseEmployeeActivity(
        ReportFilter filter, Guid? employeeId = null, Guid? departmentId = null)
    {
        var startDate = filter.StartDate ?? DateTime.MinValue;
        var endDate = filter.EndDate.HasValue
            ? filter.EndDate.Value.AddDays(1)
            : DateTime.MaxValue;

        var checklists = await context.Checklists.IgnoreQueryFilters().AsNoTracking()
            .Where(cl => !cl.DeletedAt.HasValue && cl.CreatedAt >= startDate && cl.CreatedAt < endDate)
            .GroupBy(cl => cl.CreatedById)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync();

        var grns = await context.Grns.IgnoreQueryFilters().AsNoTracking()
            .Where(g => !g.DeletedAt.HasValue && g.CreatedAt >= startDate && g.CreatedAt < endDate)
            .GroupBy(g => g.CreatedById)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync();

        var adjustments = await context.StockAdjustments.IgnoreQueryFilters().AsNoTracking()
            .Where(sa => !sa.DeletedAt.HasValue && sa.CreatedAt >= startDate && sa.CreatedAt < endDate)
            .GroupBy(sa => sa.CreatedById)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync();

        var distributions = await context.DistributeMaterials.IgnoreQueryFilters().AsNoTracking()
            .Where(dm => !dm.DeletedAt.HasValue && dm.CreatedAt >= startDate && dm.CreatedAt < endDate)
            .GroupBy(dm => dm.CreatedById)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync();

        var swaps = await context.SwapRequests.IgnoreQueryFilters().AsNoTracking()
            .Where(sr => !sr.DeletedAt.HasValue && sr.ActionedById.HasValue
                && sr.CreatedAt >= startDate && sr.CreatedAt < endDate)
            .GroupBy(sr => sr.ActionedById!.Value)
            .Select(g => new { UserId = (Guid?)g.Key, Count = g.Count() })
            .ToListAsync();

        var putaways = await context.ShelfMaterialBatches.IgnoreQueryFilters().AsNoTracking()
            .Where(smb => !smb.DeletedAt.HasValue && smb.CreatedById.HasValue
                && smb.CreatedAt >= startDate && smb.CreatedAt < endDate)
            .GroupBy(smb => smb.CreatedById.Value)
            .Select(g => new { UserId = (Guid?)g.Key, Count = g.Count() })
            .ToListAsync();

        var allUserIds = checklists.Select(x => x.UserId)
            .Concat(grns.Select(x => x.UserId))
            .Concat(adjustments.Select(x => x.UserId))
            .Concat(distributions.Select(x => x.UserId))
            .Concat(swaps.Select(x => x.UserId))
            .Concat(putaways.Select(x => x.UserId))
            .Distinct().ToList();

        if (employeeId.HasValue)
            allUserIds = allUserIds.Where(id => id == employeeId.Value).ToList();

        var users = await context.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(u => allUserIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Department.Name })
            .ToListAsync();

        var checklistLookup = checklists.ToDictionary(x => x.UserId, x => x.Count);
        var grnLookup = grns.ToDictionary(x => x.UserId, x => x.Count);
        var adjLookup = adjustments.ToDictionary(x => x.UserId, x => x.Count);
        var distLookup = distributions.ToDictionary(x => x.UserId, x => x.Count);
        var swapLookup = swaps.ToDictionary(x => x.UserId, x => x.Count);
        var putawayLookup = putaways.ToDictionary(x => x.UserId, x => x.Count);

        var result = users
            .Select(u =>
            {
                var c = checklistLookup.GetValueOrDefault(u.Id, 0);
                var g = grnLookup.GetValueOrDefault(u.Id, 0);
                var a = adjLookup.GetValueOrDefault(u.Id, 0);
                var d = distLookup.GetValueOrDefault(u.Id, 0);
                var s = swapLookup.GetValueOrDefault(u.Id, 0);
                var p = putawayLookup.GetValueOrDefault(u.Id, 0);
                var total = c + g + a + d + s + p;

                return new WarehouseEmployeeActivityDto
                {
                    Employee = u.FirstName + " " + u.LastName,
                    Department = u.Name,
                    ChecklistsDone = c,
                    GrnsCreated = g,
                    StockAdjustments = a,
                    MaterialDistributions = d,
                    SwapsActioned = s,
                    PutawayActions = p,
                    TotalActions = total,
                };
            })
            .OrderByDescending(x => x.TotalActions)
            .Select((x, i) => { x.No = i + 1; return x; })
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<ArrivalLocationStatusDto>>> GetArrivalLocationStatus(
        Guid? warehouseId = null, string status = null, int agingThresholdDays = 3,
        Guid? departmentId = null)
    {
        var today = DateTime.UtcNow.Date;

        var query = context.DistributedRequisitionMaterials.IgnoreQueryFilters().AsNoTracking()
            .Where(drm => !drm.DeletedAt.HasValue
                && drm.WarehouseArrivalLocationId.HasValue);

        if (warehouseId.HasValue)
            query = query.Where(drm =>
                drm.WarehouseArrivalLocation.WarehouseId == warehouseId.Value);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<DistributedRequisitionMaterialStatus>(status, true, out var parsed))
            query = query.Where(drm => drm.Status == parsed);

        if (departmentId.HasValue)
            query = query.Where(drm => drm.Material.Departments.Any(md =>
                md.DepartmentId == departmentId.Value && !md.DeletedAt.HasValue));

        var data = await query
            .Select(drm => new
            {
                WarehouseName = drm.WarehouseArrivalLocation.Warehouse.Name,
                LocationName = drm.WarehouseArrivalLocation.Name,
                MaterialName = drm.Material.Name,
                BatchNo = drm.Material.Batches
                    .Where(mb => !mb.DeletedAt.HasValue)
                    .Select(mb => mb.BatchNumber)
                    .FirstOrDefault(),
                drm.Quantity,
                UomSymbol = drm.UoM.Symbol,
                drm.ArrivedAt,
                drm.Status,
                drm.WarehouseArrivalLocation.WarehouseId,
            })
            .ToListAsync();

        var result = data
            .Select(x =>
            {
                var daysInArrival = x.ArrivedAt.HasValue
                    ? (int)(today - x.ArrivedAt.Value.Date).TotalDays : 0;
                return new ArrivalLocationStatusDto
                {
                    Warehouse = x.WarehouseName,
                    ArrivalLocation = x.LocationName,
                    Material = x.MaterialName,
                    BatchNo = x.BatchNo,
                    Quantity = x.Quantity,
                    UOM = x.UomSymbol,
                    DaysInArrival = daysInArrival,
                    Status = x.Status.ToString(),
                    PriorityFlag = daysInArrival >= agingThresholdDays ? "HIGH" : "Normal",
                };
            })
            .OrderByDescending(x => x.DaysInArrival)
            .Select((x, i) => { x.No = i + 1; return x; })
            .ToList();

        return Result.Success(result);
    }

    // ---------------------------------------------------------------------
    // Production Dashboard KPI Widgets (KPI 3 - 12)
    // ---------------------------------------------------------------------

    /// <summary>
    /// KPI 3 - BMR Release Rate. The report specification names approved
    /// records "Issued"; intermediate QC/testing states remain pending.
    /// </summary>
    public async Task<Result<List<BmrReleaseRateDto>>> GetBmrReleaseRate(
        ProductionKpiFilter filter
    )
    {
        var query = context
            .BatchManufacturingRecords.IgnoreQueryFilters()
            .Where(b =>
                b.DeletedAt == null
                && b.ProductionScheduleProduct != null
                && b.ProductionScheduleProduct.Product != null
            );

        if (filter.DepartmentId.HasValue)
            query = query.Where(b =>
                b.ProductionScheduleProduct.Product.DepartmentId == filter.DepartmentId.Value
            );

        if (filter.StartDate.HasValue)
            query = query.Where(b => b.ManufacturingDate >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.AddDays(1);
            query = query.Where(b => b.ManufacturingDate < end);
        }

        var rows = await query
            .Select(b => new
            {
                b.ProductionScheduleProduct.Product.DepartmentId,
                Department = b.ProductionScheduleProduct.Product.Department.Name,
                b.Status,
            })
            .ToListAsync();

        var result = rows.GroupBy(row => new { row.DepartmentId, row.Department })
            .Select(group =>
            {
                var total = group.Count();

                // Both released states count as issued. Approved is the normal QA release
                // path; Available is a batch released after a QC OOS case closed favourably.
                // Counting only Approved would leave every OOS-released batch falling into
                // Pending below — which is computed by subtraction, so an unhandled status is
                // not merely uncounted, it is actively claimed to be pending. A batch that has
                // been released is not pending anything.
                var issued = group.Count(row =>
                    row.Status == BatchManufacturingStatus.Approved
                    || row.Status == BatchManufacturingStatus.Available);

                var rejected = group.Count(row => row.Status == BatchManufacturingStatus.Rejected);
                return new BmrReleaseRateDto
                {
                    DepartmentId = group.Key.DepartmentId,
                    Department = group.Key.Department,
                    TotalBmrs = total,
                    Pending = total - issued - rejected,
                    Issued = issued,
                    Rejected = rejected,
                    ReleaseRatePercentage = total == 0
                        ? 0
                        : Math.Round((decimal)issued / total * 100, 2),
                };
            })
            .OrderBy(row => row.Department)
            .ToList();

        return Result.Success(result);
    }

    /// <summary>
    /// KPI 4 - Yield Performance. Expected yield comes from the selected
    /// product packing per batch; actual quantity and gain/loss come from the
    /// completed final-packing record.
    /// </summary>
    public async Task<Result<List<YieldPerformanceDto>>> GetYieldPerformance(
        ProductionKpiFilter filter
    )
    {
        var query = context
            .FinalPackings.IgnoreQueryFilters()
            .Where(fp =>
                fp.DeletedAt == null
                && fp.ProductionScheduleProduct != null
                && fp.ProductionScheduleProduct.Product != null
                && fp.ProductPacking != null
            );

        if (filter.DepartmentId.HasValue)
            query = query.Where(fp =>
                fp.ProductionScheduleProduct.Product.DepartmentId == filter.DepartmentId.Value
            );

        if (filter.StartDate.HasValue)
            query = query.Where(fp => fp.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.AddDays(1);
            query = query.Where(fp => fp.CreatedAt < end);
        }

        var rows = await query
            .Select(fp => new
            {
                fp.ProductionScheduleProductId,
                fp.ProductionScheduleProduct.ProductId,
                Product = fp.ProductionScheduleProduct.Product.Name,
                fp.ProductionScheduleProduct.Product.DepartmentId,
                Department = fp.ProductionScheduleProduct.Product.Department.Name,
                ExpectedYield = fp.ProductPacking.ExpectedYield,
                fp.TotalQuantityPacked,
                fp.TotalGainOrLoss,
            })
            .ToListAsync();

        var result = rows.GroupBy(row => new
            {
                row.ProductId,
                row.Product,
                row.DepartmentId,
                row.Department,
            })
            .Select(group =>
            {
                var expected = group.Sum(row => row.ExpectedYield);
                var actual = group.Sum(row => row.TotalQuantityPacked);
                return new YieldPerformanceDto
                {
                    ProductId = group.Key.ProductId,
                    Product = group.Key.Product,
                    DepartmentId = group.Key.DepartmentId,
                    Department = group.Key.Department,
                    BatchCount = group.Select(row => row.ProductionScheduleProductId)
                        .Distinct()
                        .Count(),
                    ExpectedYield = expected,
                    ActualQuantityPacked = actual,
                    TotalGainOrLoss = group.Sum(row => row.TotalGainOrLoss),
                    VariancePercentage = expected == 0
                        ? 0
                        : Math.Round((actual - expected) / expected * 100, 2),
                };
            })
            .OrderBy(row => row.Product)
            .ToList();

        return Result.Success(result);
    }

    /// <summary>
    /// KPI 6 - Schedule Adherence (On-Time Completion Rate). Compares the
    /// actual completion time of each completed production activity against the
    /// scheduled end time of its schedule, grouped by department.
    /// </summary>
    public async Task<Result<List<ScheduleAdherenceDto>>> GetScheduleAdherence(
        ProductionKpiFilter filter
    )
    {
        var query = context
            .ProductionActivities.IgnoreQueryFilters()
            .Where(a =>
                a.DeletedAt == null
                && a.Status == ProductionStatus.Completed
                && a.CompletedAt.HasValue
            );

        if (filter.DepartmentId.HasValue)
            query = query.Where(a =>
                a.ProductionScheduleProduct.ProductionSchedule.DepartmentId
                == filter.DepartmentId.Value
            );

        if (filter.StartDate.HasValue)
        {
            var start = filter.StartDate.Value;
            query = query.Where(a => a.CompletedAt >= start);
        }

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.AddDays(1);
            query = query.Where(a => a.CompletedAt < end);
        }

        var rows = await query
            .Select(a => new
            {
                a.ProductionScheduleProduct.ProductionSchedule.DepartmentId,
                DepartmentName = a.ProductionScheduleProduct.ProductionSchedule.Department.Name,
                CompletedAt = a.CompletedAt.Value,
                a.ProductionScheduleProduct.ProductionSchedule.ScheduledEndTime,
            })
            .ToListAsync();

        var result = rows.GroupBy(r => new { r.DepartmentId, r.DepartmentName })
            .Select(g =>
            {
                var onTime = g.Count(x => x.CompletedAt <= x.ScheduledEndTime);
                var total = g.Count();
                return new ScheduleAdherenceDto
                {
                    DepartmentId = g.Key.DepartmentId,
                    Department = g.Key.DepartmentName,
                    OnTime = onTime,
                    Delayed = total - onTime,
                    TotalCompleted = total,
                    AdherencePercentage =
                        total == 0 ? 0 : Math.Round((decimal)onTime / total * 100, 2),
                };
            })
            .OrderBy(x => x.Department)
            .ToList();

        return Result.Success(result);
    }

    /// <summary>
    /// KPI 7 - Production Output Volume. Total quantity packed segmented by
    /// department and division.
    /// </summary>
    public async Task<Result<List<ProductionOutputVolumeDto>>> GetProductionOutputVolume(
        ProductionKpiFilter filter
    )
    {
        var query = context.FinalPackings.IgnoreQueryFilters().Where(fp => fp.DeletedAt == null);

        if (filter.DepartmentId.HasValue)
            query = query.Where(fp =>
                fp.ProductionScheduleProduct.Product.DepartmentId == filter.DepartmentId.Value
            );

        if (filter.StartDate.HasValue)
        {
            var start = filter.StartDate.Value;
            query = query.Where(fp => fp.CreatedAt >= start);
        }

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.AddDays(1);
            query = query.Where(fp => fp.CreatedAt < end);
        }

        var rows = await query
            .Select(fp => new
            {
                fp.ProductionScheduleProduct.Product.DepartmentId,
                DepartmentName = fp.ProductionScheduleProduct.Product.Department.Name,
                fp.ProductionScheduleProduct.Product.Division,
                fp.ProductionScheduleProduct.ProductId,
                fp.ProductionScheduleProduct.BatchNumber,
                Uom = fp.ProductionScheduleProduct.Product.BaseUoM.Symbol,
                fp.TotalQuantityPacked,
            })
            .ToListAsync();

        var result = rows.GroupBy(r => new
            {
                r.DepartmentId,
                r.DepartmentName,
                r.Division,
            })
            .Select(g => new ProductionOutputVolumeDto
            {
                DepartmentId = g.Key.DepartmentId,
                Department = g.Key.DepartmentName,
                Division = g.Key.Division,
                ProductCount = g.Select(x => x.ProductId).Distinct().Count(),
                TotalBatchCount = g.Select(x => x.BatchNumber)
                    .Where(b => b != null)
                    .Distinct()
                    .Count(),
                TotalQuantityPacked = g.Sum(x => x.TotalQuantityPacked),
                Uom = g.Select(x => x.Uom).FirstOrDefault(u => u != null),
            })
            .OrderBy(x => x.Department)
            .ThenBy(x => x.Division)
            .ToList();

        return Result.Success(result);
    }

    /// <summary>
    /// KPI 8 - ATR Testing Backlog. Batch Manufacturing Records awaiting QC
    /// action (New / Testing / TestTaken) grouped by department.
    /// </summary>
    public async Task<Result<List<AtrTestingBacklogDto>>> GetAtrTestingBacklog(
        ProductionKpiFilter filter
    )
    {
        var backlogStatuses = new[]
        {
            BatchManufacturingStatus.New,
            BatchManufacturingStatus.Testing,
            BatchManufacturingStatus.TestTaken,
        };

        var query = context
            .BatchManufacturingRecords.IgnoreQueryFilters()
            .Where(b => b.DeletedAt == null && backlogStatuses.Contains(b.Status));

        if (filter.DepartmentId.HasValue)
            query = query.Where(b =>
                b.ProductionScheduleProduct.ProductionSchedule.DepartmentId
                == filter.DepartmentId.Value
            );

        if (filter.StartDate.HasValue)
        {
            var start = filter.StartDate.Value;
            query = query.Where(b => b.CreatedAt >= start);
        }

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.AddDays(1);
            query = query.Where(b => b.CreatedAt < end);
        }

        var rows = await query
            .Select(b => new
            {
                b.ProductionScheduleProduct.ProductionSchedule.DepartmentId,
                DepartmentName = b.ProductionScheduleProduct.ProductionSchedule.Department.Name,
                b.Status,
                b.CreatedAt,
            })
            .ToListAsync();

        var now = DateTime.UtcNow;
        var result = rows.GroupBy(r => new { r.DepartmentId, r.DepartmentName })
            .Select(g => new AtrTestingBacklogDto
            {
                DepartmentId = g.Key.DepartmentId,
                Department = g.Key.DepartmentName,
                NewNotSampled = g.Count(x => x.Status == BatchManufacturingStatus.New),
                Testing = g.Count(x => x.Status == BatchManufacturingStatus.Testing),
                TestTaken = g.Count(x => x.Status == BatchManufacturingStatus.TestTaken),
                TotalBacklog = g.Count(),
                OldestWaitingDays = g.Any() ? (int)(now - g.Min(x => x.CreatedAt)).TotalDays : 0,
            })
            .OrderBy(x => x.Department)
            .ToList();

        return Result.Success(result);
    }

    /// <summary>
    /// KPI 9 - Stock Requisition Pending for Production. Stock transfer sources
    /// still open (InProgress / Approved), with rejects reported alongside,
    /// grouped by destination department.
    /// </summary>
    public async Task<Result<List<StockRequisitionPendingDto>>> GetStockRequisitionPending(
        ProductionKpiFilter filter
    )
    {
        var statuses = new[]
        {
            StockTransferStatus.InProgress,
            StockTransferStatus.Approved,
            StockTransferStatus.Rejected,
        };

        var query = context
            .StockTransferSources.IgnoreQueryFilters()
            .Where(s => s.DeletedAt == null && statuses.Contains(s.Status));

        if (filter.DepartmentId.HasValue)
            query = query.Where(s => s.ToDepartmentId == filter.DepartmentId.Value);

        if (filter.StartDate.HasValue)
        {
            var start = filter.StartDate.Value;
            query = query.Where(s => s.CreatedAt >= start);
        }

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.AddDays(1);
            query = query.Where(s => s.CreatedAt < end);
        }

        var rows = await query
            .Select(s => new
            {
                s.ToDepartmentId,
                DepartmentName = s.ToDepartment.Name,
                s.Status,
                s.CreatedAt,
            })
            .ToListAsync();

        var now = DateTime.UtcNow;
        var result = rows.GroupBy(r => new { r.ToDepartmentId, r.DepartmentName })
            .Select(g =>
            {
                var pending = g.Where(x =>
                        x.Status == StockTransferStatus.InProgress
                        || x.Status == StockTransferStatus.Approved
                    )
                    .ToList();
                return new StockRequisitionPendingDto
                {
                    DepartmentId = g.Key.ToDepartmentId,
                    Department = g.Key.DepartmentName,
                    InProgress = g.Count(x => x.Status == StockTransferStatus.InProgress),
                    Approved = g.Count(x => x.Status == StockTransferStatus.Approved),
                    Reject = g.Count(x => x.Status == StockTransferStatus.Rejected),
                    Total = g.Count(),
                    OldestPendingDays =
                        pending.Count != 0
                            ? (int)(now - pending.Min(x => x.CreatedAt)).TotalDays
                            : 0,
                };
            })
            .OrderBy(x => x.Department)
            .ToList();

        return Result.Success(result);
    }

    /// <summary>
    /// KPI 10 - FGTN Pending Approval. Finished Goods Transfer Notes awaiting
    /// approval, grouped by the source production department.
    /// </summary>
    public async Task<Result<List<FgtnPendingApprovalDto>>> GetFgtnPendingApproval(
        ProductionKpiFilter filter
    )
    {
        var query = context
            .FinishedGoodsTransferNotes.IgnoreQueryFilters()
            .Where(f => f.DeletedAt == null);

        if (filter.DepartmentId.HasValue)
            query = query.Where(f => f.FromWarehouse.DepartmentId == filter.DepartmentId.Value);

        if (filter.StartDate.HasValue)
        {
            var start = filter.StartDate.Value;
            query = query.Where(f => f.CreatedAt >= start);
        }

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.AddDays(1);
            query = query.Where(f => f.CreatedAt < end);
        }

        var rows = await query
            .Select(f => new
            {
                f.FromWarehouse.DepartmentId,
                DepartmentName = f.FromWarehouse.Department.Name,
                f.IsApproved,
                f.CreatedAt,
            })
            .ToListAsync();

        var now = DateTime.UtcNow;
        var result = rows.GroupBy(r => new { r.DepartmentId, r.DepartmentName })
            .Select(g =>
            {
                var pending = g.Where(x => !x.IsApproved).ToList();
                return new FgtnPendingApprovalDto
                {
                    DepartmentId = g.Key.DepartmentId,
                    FromDepartment = g.Key.DepartmentName,
                    PendingApproval = pending.Count,
                    Approved = g.Count(x => x.IsApproved),
                    AverageAgingDays =
                        pending.Count != 0
                            ? Math.Round(
                                (decimal)pending.Average(x => (now - x.CreatedAt).TotalDays),
                                2
                            )
                            : 0,
                };
            })
            .OrderBy(x => x.FromDepartment)
            .ToList();

        return Result.Success(result);
    }

    /// <summary>
    /// KPI 11 - Production Order Delivery Status. Order counts per status with
    /// their allocation, loaded and delivered figures.
    /// </summary>
    public async Task<
        Result<List<ProductionOrderDeliveryStatusDto>>
    > GetProductionOrderDeliveryStatus(ProductionKpiFilter filter, Guid? customerId)
    {
        var orderQuery = context
            .ProductionOrders.IgnoreQueryFilters()
            .Where(o => o.DeletedAt == null);

        if (customerId.HasValue)
            orderQuery = orderQuery.Where(o => o.CustomerId == customerId.Value);

        if (filter.StartDate.HasValue)
        {
            var start = filter.StartDate.Value;
            orderQuery = orderQuery.Where(o => o.CreatedAt >= start);
        }

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.AddDays(1);
            orderQuery = orderQuery.Where(o => o.CreatedAt < end);
        }

        var orderCounts = await orderQuery
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var allocationQuery = context
            .AllocateProductionOrders.IgnoreQueryFilters()
            .Where(a => a.DeletedAt == null);

        if (customerId.HasValue)
            allocationQuery = allocationQuery.Where(a =>
                a.ProductionOrder.CustomerId == customerId.Value
            );

        if (filter.StartDate.HasValue)
        {
            var start = filter.StartDate.Value;
            allocationQuery = allocationQuery.Where(a => a.ProductionOrder.CreatedAt >= start);
        }

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.AddDays(1);
            allocationQuery = allocationQuery.Where(a => a.ProductionOrder.CreatedAt < end);
        }

        var allocationCounts = await allocationQuery
            .GroupBy(a => a.ProductionOrder.Status)
            .Select(g => new
            {
                Status = g.Key,
                Allocated = g.Count(),
                Loaded = g.Count(x => x.Status == AllocateProductionOrderStatus.Loaded),
                Delivered = g.Count(x => x.Status == AllocateProductionOrderStatus.Delivered),
            })
            .ToListAsync();

        var statuses = new[]
        {
            ProductionOrderStatus.Pending,
            ProductionOrderStatus.PartialPackingReady,
            ProductionOrderStatus.FullPackingReady,
        };

        var result = statuses
            .Select(status =>
            {
                var alloc = allocationCounts.FirstOrDefault(a => a.Status == status);
                return new ProductionOrderDeliveryStatusDto
                {
                    Status = status,
                    StatusName = status.ToString(),
                    OrderCount = orderCounts.FirstOrDefault(o => o.Status == status)?.Count ?? 0,
                    AllocatedCount = alloc?.Allocated ?? 0,
                    Loaded = alloc?.Loaded ?? 0,
                    Delivered = alloc?.Delivered ?? 0,
                };
            })
            .ToList();

        return Result.Success(result);
    }

    /// <summary>
    /// KPI 12 - Material Return Rate. Compares quantity issued to production
    /// against quantity returned, grouped by production department.
    /// </summary>
    public async Task<Result<List<MaterialReturnRateDto>>> GetMaterialReturnRate(
        ProductionKpiFilter filter
    )
    {
        // Issued quantities are keyed on the destination (production) department.
        var issuedQuery = context
            .StockTransferSources.IgnoreQueryFilters()
            .Where(s => s.DeletedAt == null && s.Status == StockTransferStatus.Issued);

        if (filter.DepartmentId.HasValue)
            issuedQuery = issuedQuery.Where(s => s.ToDepartmentId == filter.DepartmentId.Value);

        if (filter.StartDate.HasValue)
        {
            var start = filter.StartDate.Value;
            issuedQuery = issuedQuery.Where(s => s.CreatedAt >= start);
        }

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.AddDays(1);
            issuedQuery = issuedQuery.Where(s => s.CreatedAt < end);
        }

        var issued = await issuedQuery
            .GroupBy(s => new { s.ToDepartmentId, DepartmentName = s.ToDepartment.Name })
            .Select(g => new
            {
                g.Key.ToDepartmentId,
                g.Key.DepartmentName,
                IssuedQuantity = g.Sum(x => x.Quantity),
            })
            .ToListAsync();

        // Returned quantities are keyed on the schedule's production department.
        var returnQuery = context
            .MaterialReturnNotes.IgnoreQueryFilters()
            .Where(r => r.DeletedAt == null);

        if (filter.DepartmentId.HasValue)
            returnQuery = returnQuery.Where(r =>
                r.ProductionScheduleProduct.ProductionSchedule.DepartmentId
                == filter.DepartmentId.Value
            );

        if (filter.StartDate.HasValue)
        {
            var start = filter.StartDate.Value;
            returnQuery = returnQuery.Where(r => r.CreatedAt >= start);
        }

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.AddDays(1);
            returnQuery = returnQuery.Where(r => r.CreatedAt < end);
        }

        var returns = await returnQuery
            .Select(r => new
            {
                r.ProductionScheduleProduct.ProductionSchedule.DepartmentId,
                DepartmentName = r.ProductionScheduleProduct.ProductionSchedule.Department.Name,
                PartialQuantity = r.PartialReturns.Sum(p => (decimal?)p.Quantity) ?? 0,
                FullQuantity =
                    r.FullReturns.Sum(f => (decimal?)f.MaterialBatchReservedQuantity.Quantity) ?? 0,
            })
            .ToListAsync();

        var returnGroups = returns
            .GroupBy(r => new { r.DepartmentId, r.DepartmentName })
            .Select(g => new
            {
                g.Key.DepartmentId,
                g.Key.DepartmentName,
                ReturnedQuantity = g.Sum(x => x.PartialQuantity + x.FullQuantity),
                NoteCount = g.Count(),
            })
            .ToList();

        var departmentIds = issued
            .Select(i => (Guid?)i.ToDepartmentId)
            .Union(returnGroups.Select(r => r.DepartmentId))
            .Distinct()
            .ToList();

        var result = departmentIds
            .Select(deptId =>
            {
                var iss = issued.FirstOrDefault(i => i.ToDepartmentId == deptId);
                var ret = returnGroups.FirstOrDefault(r => r.DepartmentId == deptId);
                var issuedQty = iss?.IssuedQuantity ?? 0;
                var returnedQty = ret?.ReturnedQuantity ?? 0;
                return new MaterialReturnRateDto
                {
                    DepartmentId = deptId,
                    Department = iss?.DepartmentName ?? ret?.DepartmentName,
                    TotalIssuedQuantity = issuedQty,
                    TotalReturnedQuantity = returnedQty,
                    ReturnRatePercentage =
                        issuedQty == 0 ? 0 : Math.Round(returnedQty / issuedQty * 100, 2),
                    ReturnNoteCount = ret?.NoteCount ?? 0,
                };
            })
            .OrderBy(x => x.Department)
            .ToList();

        return Result.Success(result);
    }

    // ---------------------------------------------------------------------
    // Production Dashboard KPI Widgets (KPI 6 - 12)
    // ---------------------------------------------------------------------

    public async Task<Result<IEnumerable<DockToStockTimeDto>>> GetDockToStockTime(
        WarehouseKpiFilterDto filter)
    {
        DateTime now = DateTime.UtcNow;
        DateTime? startDate = null;
        DateTime? endDate = null;

        if (filter.CustomStartDate.HasValue || filter.CustomEndDate.HasValue)
        {
            startDate = filter.CustomStartDate;
            endDate = filter.CustomEndDate;
        }
        else if (filter.DatePreset.HasValue)
        {
            switch (filter.DatePreset.Value)
            {
                case DateFilter.Today:
                    startDate = now.Date;
                    break;
                case DateFilter.ThisWeek:
                    startDate = now.Date.AddDays(-(int)now.DayOfWeek);
                    break;
                case DateFilter.ThisMonth:
                    startDate = now.Date.AddDays(1 - now.Day);
                    break;
            }
        }

        var periodLabel = GetPeriodLabel(filter, startDate, endDate);

        var drmQuery = context
            .DistributedRequisitionMaterials.AsNoTracking().IgnoreQueryFilters()
            .Where(drm =>
                drm.CheckedAt.HasValue
                && drm.GrnGeneratedAt.HasValue
                && !drm.DeletedAt.HasValue
            );

        if (filter.WarehouseId.HasValue)
        {
            var warehouseId = filter.WarehouseId.Value;
            drmQuery = drmQuery.Where(drm =>
                drm.DistributedRequisitionItems.Any(dri => dri.WarehouseId == warehouseId));
        }

        if (startDate.HasValue)
            drmQuery = drmQuery.Where(drm => drm.CheckedAt >= startDate.Value);

        if (endDate.HasValue)
            drmQuery = drmQuery.Where(drm => drm.CheckedAt <= endDate.Value);

        var records = await drmQuery
            .SelectMany(drm => drm.DistributedRequisitionItems)
            .Select(dri => new
            {
                WarehouseName = dri.Warehouse.Name,
                CheckedAt = dri.DistributedRequisitionMaterial.CheckedAt.Value,
                GrnGeneratedAt = dri.DistributedRequisitionMaterial.GrnGeneratedAt.Value
            })
            .ToListAsync();

        var grouped = records
            .GroupBy(r => r.WarehouseName)
            .Select(g =>
            {
                var hours = g
                    .Select(r => (r.GrnGeneratedAt - r.CheckedAt).TotalHours)
                    .OrderBy(h => h)
                    .ToList();

                double median;
                int count = hours.Count;

                if (count == 0)
                    median = 0;
                else if (count % 2 == 1)
                    median = hours[count / 2];
                else
                    median = (hours[count / 2 - 1] + hours[count / 2]) / 2.0;

                return new DockToStockTimeDto
                {
                    Warehouse = g.Key,
                    Period = periodLabel,
                    AverageHours = Math.Round(g.Average(r => (r.GrnGeneratedAt - r.CheckedAt).TotalHours), 2),
                    MedianHours = Math.Round(median, 2),
                    TotalRecords = count
                };
            })
            .OrderBy(d => d.Warehouse)
            .ToList();

        return Result.Success(grouped.AsEnumerable());
    }

    private static string GetPeriodLabel(
        WarehouseKpiFilterDto filter,
        DateTime? startDate,
        DateTime? endDate)
    {
        if (filter.DatePreset.HasValue)
        {
            return filter.DatePreset.Value switch
            {
                DateFilter.Today => "Today",
                DateFilter.ThisWeek => "This Week",
                DateFilter.ThisMonth => "This Month",
                _ => "All Time"
            };
        }

        if (startDate.HasValue && endDate.HasValue)
            return $"{startDate.Value:yyyy-MM-dd} to {endDate.Value:yyyy-MM-dd}";

        if (startDate.HasValue)
            return $"From {startDate.Value:yyyy-MM-dd}";

        return "All Time";
    }

    public async Task<Result<IEnumerable<StockTransferFulfilmentRateDto>>> GetStockTransferFulfilmentRate(
        WarehouseKpiFilterDto filter,
        Guid departmentId)
    {
        DateTime now = DateTime.UtcNow;
        DateTime? startDate = null;
        DateTime? endDate = null;

        if (filter.CustomStartDate.HasValue || filter.CustomEndDate.HasValue)
        {
            startDate = filter.CustomStartDate;
            endDate = filter.CustomEndDate;
        }
        else if (filter.DatePreset.HasValue)
        {
            switch (filter.DatePreset.Value)
            {
                case DateFilter.Today:
                    startDate = now.Date;
                    break;
                case DateFilter.ThisWeek:
                    startDate = now.Date.AddDays(-(int)now.DayOfWeek);
                    break;
                case DateFilter.ThisMonth:
                    startDate = now.Date.AddDays(1 - now.Day);
                    break;
            }
        }

        var baseQuery = context
            .StockTransferSources.AsNoTracking().IgnoreQueryFilters()
            .Where(sts => !sts.DeletedAt.HasValue);

        if (startDate.HasValue)
            baseQuery = baseQuery.Where(sts => sts.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            baseQuery = baseQuery.Where(sts => sts.CreatedAt <= endDate.Value);

        var outgoingQuery = baseQuery.Where(sts => sts.FromDepartmentId == departmentId);
        var incomingQuery = baseQuery.Where(sts => sts.ToDepartmentId == departmentId);

        if (filter.FromDepartmentId.HasValue)
        {
            outgoingQuery = outgoingQuery.Where(sts =>
                sts.FromDepartmentId == filter.FromDepartmentId.Value);
            incomingQuery = incomingQuery.Where(sts =>
                sts.FromDepartmentId == filter.FromDepartmentId.Value);
        }

        if (filter.ToDepartmentId.HasValue)
        {
            outgoingQuery = outgoingQuery.Where(sts =>
                sts.ToDepartmentId == filter.ToDepartmentId.Value);
            incomingQuery = incomingQuery.Where(sts =>
                sts.ToDepartmentId == filter.ToDepartmentId.Value);
        }

        var outgoingCounts = await outgoingQuery
            .GroupBy(sts => sts.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        var incomingCounts = await incomingQuery
            .GroupBy(sts => sts.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        var results = new List<StockTransferFulfilmentRateDto>();

        var outgoingLookup = outgoingCounts.ToDictionary(x => x.Key, x => x.Count);
        var incomingLookup = incomingCounts.ToDictionary(x => x.Key, x => x.Count);

        results.Add(BuildFulfilmentDto("Outgoing", outgoingLookup));
        results.Add(BuildFulfilmentDto("Incoming", incomingLookup));

        return Result.Success(results.AsEnumerable());
    }

    public async Task<Result<ReceivingPipelineSnapshotDto>> GetReceivingPipelineSnapshot(
        WarehouseKpiFilterDto filter)
    {
        var query = context
            .DistributedRequisitionMaterials.AsNoTracking().IgnoreQueryFilters()
            .Where(drm => !drm.DeletedAt.HasValue);

        if (filter.WarehouseId.HasValue)
            query = query.Where(drm =>
                drm.WarehouseArrivalLocation != null
                && drm.WarehouseArrivalLocation.WarehouseId == filter.WarehouseId.Value);

        var stageCounts = await query
            .GroupBy(drm => drm.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var stageLookup = stageCounts.ToDictionary(s => s.Status, s => s.Count);

        var pending = stageLookup.GetValueOrDefault(DistributedRequisitionMaterialStatus.Pending);
        var arrived = stageLookup.GetValueOrDefault(DistributedRequisitionMaterialStatus.Arrived);
        var checkedStatus = stageLookup.GetValueOrDefault(DistributedRequisitionMaterialStatus.Checked);
        var grnGenerated = stageLookup.GetValueOrDefault(DistributedRequisitionMaterialStatus.GrnGenerated);
        var distributedTotal = stageLookup.GetValueOrDefault(DistributedRequisitionMaterialStatus.Distributed);

        var assigned = 0;

        if (distributedTotal > 0)
        {
            assigned = await query
                .Where(drm => drm.Status == DistributedRequisitionMaterialStatus.Distributed)
                .CountAsync(drm =>
                    drm.CheckLists.Any(cl =>
                        cl.MaterialBatches.Any(mb => mb.ShelfMaterialBatches.Any())));
        }

        var distributed = distributedTotal - assigned;

        var total = pending + arrived + checkedStatus + grnGenerated + distributed + assigned;

        return Result.Success(new ReceivingPipelineSnapshotDto
        {
            Pending = pending,
            Arrived = arrived,
            Checked = checkedStatus,
            GrnGenerated = grnGenerated,
            Distributed = distributed,
            Assigned = assigned,
            Total = total
        });
    }

    private static StockTransferFulfilmentRateDto BuildFulfilmentDto(
        string direction,
        Dictionary<StockTransferStatus, int> lookup)
    {
        var pending = lookup.GetValueOrDefault(StockTransferStatus.InProgress);
        var approved = lookup.GetValueOrDefault(StockTransferStatus.Approved);
        var issued = lookup.GetValueOrDefault(StockTransferStatus.Issued);
        var total = pending + approved + issued
            + lookup.GetValueOrDefault(StockTransferStatus.Rejected);

        return new StockTransferFulfilmentRateDto
        {
            Direction = direction,
            TotalTransfers = total,
            Pending = pending,
            Approved = approved,
            Issued = issued,
            FulfilmentPercentage = total > 0
                ? Math.Round((decimal)issued / total * 100, 2)
                : 0
        };
    }

    public async Task<Result<IEnumerable<ExpiryRiskIndexDto>>> GetExpiryRiskIndex(
        WarehouseKpiFilterDto filter)
    {
        var today = DateTime.UtcNow.Date;
        var threshold30 = today.AddDays(30);
        var threshold60 = today.AddDays(60);
        var threshold90 = today.AddDays(90);

        var query = context
            .ShelfMaterialBatches.AsNoTracking().IgnoreQueryFilters()
            .Where(smb =>
                !smb.DeletedAt.HasValue
                && smb.Quantity > 0
                && smb.MaterialBatch.ExpiryDate.HasValue
                && !smb.MaterialBatch.DeletedAt.HasValue
                && !smb.MaterialBatch.Material.IsUnlimited
            );

        if (filter.WarehouseId.HasValue)
            query = query.Where(smb =>
                smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.WarehouseId == filter.WarehouseId.Value);

        var grouped = await query
            .GroupBy(smb => new
            {
                WarehouseName = smb.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.Warehouse.Name,
                WindowCode = smb.MaterialBatch.ExpiryDate <= threshold30 ? 0
                    : smb.MaterialBatch.ExpiryDate <= threshold60 ? 1
                    : smb.MaterialBatch.ExpiryDate <= threshold90 ? 2
                    : 3,
                UomSymbol = smb.UoM != null ? smb.UoM.Symbol : smb.MaterialBatch.UoM.Symbol
            })
            .Select(g => new
            {
                g.Key.WarehouseName,
                g.Key.WindowCode,
                g.Key.UomSymbol,
                BatchCount = g.Select(smb => smb.MaterialBatchId).Distinct().Count(),
                TotalQuantity = g.Sum(smb => smb.Quantity)
            })
            .ToListAsync();

        var windowLabels = new Dictionary<int, string>
        {
            [0] = "≤ 30 days",
            [1] = "31–60 days",
            [2] = "61–90 days",
            [3] = "> 90 days"
        };

        var result = grouped
            .Select(g => new ExpiryRiskIndexDto
            {
                Warehouse = g.WarehouseName,
                ExpiryWindow = windowLabels.GetValueOrDefault(g.WindowCode, "Unknown"),
                BatchCount = g.BatchCount,
                TotalQuantity = g.TotalQuantity.ToString("0.############################"),
                Uom = g.UomSymbol
            })
            .OrderBy(d => d.Warehouse)
            .ThenBy(d => d.ExpiryWindow)
            .ToList();

        if (filter.ExpiryWindow.HasValue)
        {
            var targetLabel = filter.ExpiryWindow.Value switch
            {
                ExpiryWindowFilter.Within30Days => "≤ 30 days",
                ExpiryWindowFilter.Within31To60Days => "31–60 days",
                ExpiryWindowFilter.Within61To90Days => "61–90 days",
                ExpiryWindowFilter.Over90Days => "> 90 days",
                _ => null
            };
            if (targetLabel != null)
                result = result.Where(r => r.ExpiryWindow == targetLabel).ToList();
        }

        return Result.Success(result.AsEnumerable());
    }

    public async Task<Result<SwapRequestActivityDto>> GetSwapRequestActivity(
        WarehouseKpiFilterDto filter,
        Guid departmentId)
    {
        DateTime now = DateTime.UtcNow;
        DateTime? startDate = null;
        DateTime? endDate = null;

        if (filter.CustomStartDate.HasValue || filter.CustomEndDate.HasValue)
        {
            startDate = filter.CustomStartDate;
            endDate = filter.CustomEndDate;
        }
        else if (filter.DatePreset.HasValue)
        {
            switch (filter.DatePreset.Value)
            {
                case DateFilter.Today:
                    startDate = now.Date;
                    break;
                case DateFilter.ThisWeek:
                    startDate = now.Date.AddDays(-(int)now.DayOfWeek);
                    break;
                case DateFilter.ThisMonth:
                    startDate = now.Date.AddDays(1 - now.Day);
                    break;
            }
        }

        var query = context
            .SwapRequests.AsNoTracking().IgnoreQueryFilters()
            .Where(sr => !sr.DeletedAt.HasValue);

        if (startDate.HasValue)
            query = query.Where(sr => sr.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(sr => sr.CreatedAt <= endDate.Value);

        if (filter.WarehouseId.HasValue)
        {
            var warehouseId = filter.WarehouseId.Value;
            query = query.Where(sr =>
                sr.FirstWarehouseId == warehouseId
                || sr.SecondWarehouseId == warehouseId);
        }

        var counts = await query
            .GroupBy(sr => sr.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var lookup = counts.ToDictionary(x => x.Status, x => x.Count);

        var pending = lookup.GetValueOrDefault(SwapRequestStatus.Pending);
        var approved = lookup.GetValueOrDefault(SwapRequestStatus.Approved);
        var rejected = lookup.GetValueOrDefault(SwapRequestStatus.Rejected);
        var total = pending + approved + rejected;

        var periodLabel = GetPeriodLabel(filter, startDate, endDate);

        return Result.Success(new SwapRequestActivityDto
        {
            Period = periodLabel,
            Pending = pending,
            Approved = approved,
            Rejected = rejected,
            Total = total
        });
    }
    

    public async Task<Result<IEnumerable<WarehouseCapacityUtilisationDto>>> GetWarehouseCapacityUtilisation(
        WarehouseKpiFilterDto filter, Guid? departmentId)
    {
        var query = context
            .Warehouses.IgnoreQueryFilters().AsNoTracking()
            .Where(w => !w.DeletedAt.HasValue);

        if (departmentId.HasValue)
            query = query.Where(w => w.DepartmentId == departmentId.Value);

        if (filter.WarehouseId.HasValue)
            query = query.Where(w => w.Id == filter.WarehouseId.Value);

        if (filter.WarehouseType.HasValue)
            query = query.Where(w => w.Type == filter.WarehouseType.Value);

        if (filter.Division.HasValue)
            query = query.Where(w => w.Division == filter.Division.Value);

        var raw = await query
            .Select(w => new
            {
                DepartmentName = w.Department.Name,
                w.Name,
                w.Type,
                TotalShelves = w
                    .Locations.SelectMany(l => l.Racks)
                    .SelectMany(r => r.Shelves)
                    .Count(),
                OccupiedShelves = w
                    .Locations.SelectMany(l => l.Racks)
                    .SelectMany(r => r.Shelves)
                    .Count(s => s.MaterialBatches.Any())
            })
            .ToListAsync();

        var result = raw
            .Select(r => new WarehouseCapacityUtilisationDto
            {
                Department = r.DepartmentName,
                Warehouse = r.Name,
                WarehouseType = r.Type,
                TotalShelves = r.TotalShelves,
                OccupiedShelves = r.OccupiedShelves,
                AvailableShelves = r.TotalShelves - r.OccupiedShelves,
                UtilisationPercentage = r.TotalShelves > 0
                    ? Math.Round((decimal)r.OccupiedShelves / r.TotalShelves * 100, 2)
                    : 0
            })
            .OrderBy(d => d.Warehouse)
            .ToList();

        return Result.Success(result.AsEnumerable());
    }

    public async Task<Result<IEnumerable<WarehouseDataFreshnessDto>>> GetWarehouseKpiFreshness(
        WarehouseKpiFilterDto filter, Guid? departmentId)
    {
        var warehouses = context.Warehouses.IgnoreQueryFilters().AsNoTracking()
            .Where(w => !w.DeletedAt.HasValue);
        if (departmentId.HasValue)
            warehouses = warehouses.Where(w => w.DepartmentId == departmentId.Value);
        if (filter.WarehouseId.HasValue)
            warehouses = warehouses.Where(w => w.Id == filter.WarehouseId.Value);
        if (filter.WarehouseType.HasValue)
            warehouses = warehouses.Where(w => w.Type == filter.WarehouseType.Value);
        if (filter.Division.HasValue)
            warehouses = warehouses.Where(w => w.Division == filter.Division.Value);

        var warehouseIds = warehouses.Select(w => w.Id);
        var inbound = context.DistributedRequisitionMaterials.IgnoreQueryFilters().AsNoTracking()
            .Where(drm => !drm.DeletedAt.HasValue &&
                drm.DistributedRequisitionItems.Any(dri => warehouseIds.Contains(dri.WarehouseId)));
        var receiving = inbound.Where(drm => drm.WarehouseArrivalLocationId.HasValue &&
            warehouseIds.Contains(drm.WarehouseArrivalLocation.WarehouseId));
        var dock = inbound.Where(drm => drm.CheckedAt.HasValue && drm.GrnGeneratedAt.HasValue);
        var shelfStock = context.ShelfMaterialBatches.IgnoreQueryFilters().AsNoTracking()
            .Where(smb => !smb.DeletedAt.HasValue && warehouseIds.Contains(
                smb.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId));
        var expiry = shelfStock.Where(smb => smb.Quantity > 0 &&
            smb.MaterialBatch.ExpiryDate.HasValue &&
            !smb.MaterialBatch.Material.IsUnlimited);
        var transfers = context.StockTransferSources.IgnoreQueryFilters().AsNoTracking()
            .Where(sts => !sts.DeletedAt.HasValue);
        if (departmentId.HasValue)
            transfers = transfers.Where(sts => sts.FromDepartmentId == departmentId.Value ||
                sts.ToDepartmentId == departmentId.Value);
        var materialDepartments = context.MaterialDepartments.IgnoreQueryFilters().AsNoTracking()
            .Where(md => !md.DeletedAt.HasValue);
        if (departmentId.HasValue)
            materialDepartments = materialDepartments.Where(md =>
                md.DepartmentId == departmentId.Value);
        var swaps = context.SwapRequests.IgnoreQueryFilters().AsNoTracking()
            .Where(sr => !sr.DeletedAt.HasValue &&
                (warehouseIds.Contains(sr.FirstWarehouseId) ||
                 warehouseIds.Contains(sr.SecondWarehouseId)));

        var sources = new List<WarehouseDataFreshnessDto>
        {
            await BuildFreshness("Warehouse Master", warehouses),
            await BuildFreshness("Dock-to-Stock", dock),
            await BuildFreshness("Stock Transfers", transfers),
            await BuildFreshness("Receiving Pipeline", receiving),
            await BuildFreshness("Expiry Risk", expiry),
            await BuildFreshness("Re-order Alerts", materialDepartments),
            await BuildFreshness("Swap Requests", swaps),
            await BuildFreshness("Material Movements", shelfStock)
        };

        return Result.Success(sources.AsEnumerable());
    }

    private static async Task<WarehouseDataFreshnessDto> BuildFreshness<T>(
        string source, IQueryable<T> query) where T : BaseEntity
    {
        var summary = await query
            .GroupBy(_ => 1)
            .Select(group => new
            {
                RecordCount = group.Count(),
                LastChangedAt = group.Max(item =>
                    (DateTime?)(item.UpdatedAt ?? item.CreatedAt))
            })
            .FirstOrDefaultAsync();

        return new WarehouseDataFreshnessDto
        {
            Source = source,
            RecordCount = summary?.RecordCount ?? 0,
            LastChangedAt = summary?.LastChangedAt
        };
    }
 
    public async Task<Result<IEnumerable<EmployeeHeadcountSnapshotDto>>> GetEmployeeHeadcountSnapshot(
        HrKpiFilterDto filter, Guid? departmentId)
    {
        var query = context.Employees.IgnoreQueryFilters().AsNoTracking()
            .Where(e => !e.DeletedAt.HasValue && e.DepartmentId.HasValue);

        if (departmentId.HasValue)
            query = query.Where(e => e.DepartmentId == departmentId.Value);

        if (filter.EmployeeType.HasValue)
            query = query.Where(e => e.Type == filter.EmployeeType.Value);

        if (filter.Status.HasValue)
            query = query.Where(e => e.Status == filter.Status.Value);

        if (departmentId.HasValue)
        {
            var deptName = await query
                .Select(e => e.Department.Name)
                .FirstOrDefaultAsync() ?? "Unknown";

            var permanentActive = await query
                .CountAsync(e => e.Type == EmployeeType.Permanent && e.Status == EmployeeStatus.Active);
            var casualActive = await query
                .CountAsync(e => e.Type == EmployeeType.Casual && e.Status == EmployeeStatus.Active);
            var permanentInactive = await query
                .CountAsync(e => e.Type == EmployeeType.Permanent && e.Status == EmployeeStatus.Inactive);
            var casualInactive = await query
                .CountAsync(e => e.Type == EmployeeType.Casual && e.Status == EmployeeStatus.Inactive);

            return Result.Success(new List<EmployeeHeadcountSnapshotDto>
            {
                new EmployeeHeadcountSnapshotDto
                {
                    Department = deptName,
                    PermanentActive = permanentActive,
                    CasualActive = casualActive,
                    PermanentInactive = permanentInactive,
                    CasualInactive = casualInactive,
                }
            }.AsEnumerable());
        }
        else
        {
            var groups = await query
                .GroupBy(e => e.Department.Name)
                .Select(g => new
                {
                    Department = g.Key,
                    PermanentActive = g.Count(e => e.Type == EmployeeType.Permanent && e.Status == EmployeeStatus.Active),
                    CasualActive = g.Count(e => e.Type == EmployeeType.Casual && e.Status == EmployeeStatus.Active),
                    PermanentInactive = g.Count(e => e.Type == EmployeeType.Permanent && e.Status == EmployeeStatus.Inactive),
                    CasualInactive = g.Count(e => e.Type == EmployeeType.Casual && e.Status == EmployeeStatus.Inactive),
                })
                .ToListAsync();

            var results = groups
                .OrderBy(g => g.Department)
                .Select(g => new EmployeeHeadcountSnapshotDto
                {
                    Department = g.Department,
                    PermanentActive = g.PermanentActive,
                    CasualActive = g.CasualActive,
                    PermanentInactive = g.PermanentInactive,
                    CasualInactive = g.CasualInactive,
                })
                .ToList();

            return Result.Success(results.AsEnumerable());
        }
    }

    public async Task<Result<IEnumerable<EmployeeGenderRatioDto>>> GetEmployeeGenderRatio(
        HrKpiFilterDto filter, Guid? departmentId)
    {
        var query = context.Employees.IgnoreQueryFilters().AsNoTracking()
            .Where(e => !e.DeletedAt.HasValue && e.DepartmentId.HasValue && e.Status == EmployeeStatus.Active);

        if (departmentId.HasValue)
            query = query.Where(e => e.DepartmentId == departmentId.Value);

        if (departmentId.HasValue)
        {
            var deptName = await query
                .Select(e => e.Department.Name)
                .FirstOrDefaultAsync() ?? "Unknown";

            var permMale = await query.CountAsync(e => e.Type == EmployeeType.Permanent && e.Gender == Gender.Male);
            var permFemale = await query.CountAsync(e => e.Type == EmployeeType.Permanent && e.Gender == Gender.Female);
            var casualMale = await query.CountAsync(e => e.Type == EmployeeType.Casual && e.Gender == Gender.Male);
            var casualFemale = await query.CountAsync(e => e.Type == EmployeeType.Casual && e.Gender == Gender.Female);

            var totalMale = permMale + casualMale;
            var totalFemale = permFemale + casualFemale;
            var ratio = totalFemale > 0
                ? $"{(decimal)totalMale / totalFemale:0.##}:1"
                : $"{totalMale}:0";

            return Result.Success(new List<EmployeeGenderRatioDto>
            {
                new EmployeeGenderRatioDto
                {
                    Department = deptName,
                    PermanentMale = permMale,
                    PermanentFemale = permFemale,
                    CasualMale = casualMale,
                    CasualFemale = casualFemale,
                    GenderRatio = ratio,
                }
            }.AsEnumerable());
        }
        else
        {
            var groups = await query
                .GroupBy(e => e.Department.Name)
                .Select(g => new
                {
                    Department = g.Key,
                    PermanentMale = g.Count(e => e.Type == EmployeeType.Permanent && e.Gender == Gender.Male),
                    PermanentFemale = g.Count(e => e.Type == EmployeeType.Permanent && e.Gender == Gender.Female),
                    CasualMale = g.Count(e => e.Type == EmployeeType.Casual && e.Gender == Gender.Male),
                    CasualFemale = g.Count(e => e.Type == EmployeeType.Casual && e.Gender == Gender.Female),
                })
                .ToListAsync();

            var results = groups
                .OrderBy(g => g.Department)
                .Select(g =>
                {
                    var totalMale = g.PermanentMale + g.CasualMale;
                    var totalFemale = g.PermanentFemale + g.CasualFemale;
                    var ratio = totalFemale > 0
                        ? $"{(decimal)totalMale / totalFemale:0.##}:1"
                        : $"{totalMale}:0";

                    return new EmployeeGenderRatioDto
                    {
                        Department = g.Department,
                        PermanentMale = g.PermanentMale,
                        PermanentFemale = g.PermanentFemale,
                        CasualMale = g.CasualMale,
                        CasualFemale = g.CasualFemale,
                        GenderRatio = ratio,
                    };
                })
                .ToList();

            return Result.Success(results.AsEnumerable());
        }
    }

    public async Task<Result<IEnumerable<LeaveRequestPipelineDto>>> GetLeaveRequestPipeline(
        HrKpiFilterDto filter, Guid? departmentId)
    {
        var query = context.LeaveRequests.IgnoreQueryFilters().AsNoTracking()
            .Include(lr => lr.Employee)
            .Where(lr => !lr.DeletedAt.HasValue);

        if (departmentId.HasValue)
            query = query.Where(lr => lr.Employee.DepartmentId == departmentId.Value);

        if (filter.StartDate.HasValue)
            query = query.Where(lr => lr.StartDate >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(lr => lr.EndDate <= filter.EndDate.Value);

        if (departmentId.HasValue)
        {
            var deptName = await query
                .Select(lr => lr.Employee.Department.Name)
                .FirstOrDefaultAsync() ?? "Unassigned Department";

            var groups = await query
                .GroupBy(lr => lr.RequestCategory)
                .Select(g => new
                {
                    Category = g.Key,
                    Pending = g.Count(lr => lr.LeaveStatus == LeaveStatus.Pending),
                    Approved = g.Count(lr => lr.LeaveStatus == LeaveStatus.Approved),
                    Rejected = g.Count(lr => lr.LeaveStatus == LeaveStatus.Rejected),
                    Expired = g.Count(lr => lr.LeaveStatus == LeaveStatus.Expired),
                })
                .ToListAsync();

            var results = groups
                .Select(g => new LeaveRequestPipelineDto
                {
                    Department = deptName,
                    Category = g.Category.ToString(),
                    Pending = g.Pending,
                    Approved = g.Approved,
                    Rejected = g.Rejected,
                    Expired = g.Expired,
                })
                .ToList();

            return Result.Success(results.AsEnumerable());
        }
        else
        {
            var groups = await query
                .GroupBy(lr => new { DeptName = lr.Employee.Department.Name, lr.RequestCategory })
                .Select(g => new
                {
                    Department = g.Key.DeptName,
                    Category = g.Key.RequestCategory,
                    Pending = g.Count(lr => lr.LeaveStatus == LeaveStatus.Pending),
                    Approved = g.Count(lr => lr.LeaveStatus == LeaveStatus.Approved),
                    Rejected = g.Count(lr => lr.LeaveStatus == LeaveStatus.Rejected),
                    Expired = g.Count(lr => lr.LeaveStatus == LeaveStatus.Expired),
                })
                .ToListAsync();

            var results = groups
                .Select(g => new LeaveRequestPipelineDto
                {
                    Department = g.Department ?? "Unassigned Department",
                    Category = g.Category.ToString(),
                    Pending = g.Pending,
                    Approved = g.Approved,
                    Rejected = g.Rejected,
                    Expired = g.Expired,
                })
                .OrderBy(r => r.Department)
                .ThenBy(r => r.Category)
                .ToList();

            return Result.Success(results.AsEnumerable());
        }
    }

   public async Task<Result<IEnumerable<OvertimeRequestActivityDto>>> GetOvertimeRequestActivity(
    HrKpiFilterDto filter, Guid? departmentId)
{
    var query = context.OvertimeRequests.IgnoreQueryFilters().AsNoTracking()
        .Where(o => !o.DeletedAt.HasValue);

    if (departmentId.HasValue)
        query = query.Where(o => o.DepartmentId == departmentId.Value);

    if (filter.StartDate.HasValue)
        query = query.Where(o => o.OvertimeDate >= filter.StartDate.Value);

    if (filter.EndDate.HasValue)
        query = query.Where(o => o.OvertimeDate <= filter.EndDate.Value);

    var rawData = await query
        .Select(o => new
        {
            o.Status,
            o.StartTime,
            o.EndTime,
            DeptName = o.Department.Name,
        })
        .ToListAsync();

    var computed = rawData.Select(d => new
    {
        d.Status,
        d.DeptName,
        TotalHours = ComputeOvertimeHours(d.StartTime, d.EndTime),
    }).ToList();

    var allStatuses = new[]
    {
        OvertimeStatus.Pending,
        OvertimeStatus.Approved,
        OvertimeStatus.Rejected,
        OvertimeStatus.Expired
    };

    if (departmentId.HasValue)
    {
        // Single department – always return all four statuses
        var deptName = computed.Select(d => d.DeptName).FirstOrDefault()
                       ?? "Unassigned Department";

        var groups = computed
            .GroupBy(d => d.Status)
            .ToDictionary(g => g.Key, g => new
            {
                Count = g.Count(),
                TotalHours = g.Sum(x => x.TotalHours)
            });

        var results = allStatuses.Select(s =>
        {
            groups.TryGetValue(s, out var match);

            return new OvertimeRequestActivityDto
            {
                Department = deptName,
                Status = s.ToString(),
                Count = match?.Count ?? 0,
                TotalHoursRequested = match?.TotalHours ?? 0,
                ApprovedHours = s == OvertimeStatus.Approved
                    ? (match?.TotalHours ?? 0)
                    : 0
            };
        }).ToList();

        return Result.Success(results.AsEnumerable());
    }
    else
    {
        // All departments – always return every status for every department that has data
        var groups = computed
            .GroupBy(d => new { d.DeptName, d.Status })
            .ToDictionary(
                g => (Dept: g.Key.DeptName, g.Key.Status),
                g => new
                {
                    Count = g.Count(),
                    TotalHours = g.Sum(x => x.TotalHours)
                });

        var departments = groups.Keys
            .Select(k => k.Dept)
            .Distinct()
            .DefaultIfEmpty("Unassigned Department");

        var results = departments
            .SelectMany(dept => allStatuses.Select(status =>
            {
                groups.TryGetValue((dept, status), out var match);

                return new OvertimeRequestActivityDto
                {
                    Department = dept ?? "Unassigned Department",
                    Status = status.ToString(),
                    Count = match?.Count ?? 0,
                    TotalHoursRequested = match?.TotalHours ?? 0,
                    ApprovedHours = status == OvertimeStatus.Approved
                        ? (match?.TotalHours ?? 0)
                        : 0
                };
            }))
            .OrderBy(r => r.Department)
            .ThenBy(r => r.Status)
            .ToList();

        return Result.Success(results.AsEnumerable());
    }
}
    public async Task<Result<IEnumerable<DailyAttendanceRateDto>>> GetDailyAttendanceRate(
        HrKpiFilterDto filter, Guid? departmentId)
    {
        var targetDate = filter.StartDate?.Date ?? DateTime.UtcNow.Date;

        var presentStaffNumbers = await context.AttendanceRecords
            .IgnoreQueryFilters().AsNoTracking()
            .Where(a => a.TimeStamp.Date == targetDate && a.WorkState == WorkState.CheckIn)
            .Select(a => a.EmployeeId)
            .Distinct()
            .ToListAsync();

        var employeeQuery = context.Employees.IgnoreQueryFilters().AsNoTracking()
            .Where(e => !e.DeletedAt.HasValue && e.Status == EmployeeStatus.Active);

        if (departmentId.HasValue)
            employeeQuery = employeeQuery.Where(e => e.DepartmentId == departmentId.Value);

        if (departmentId.HasValue)
        {
            var deptName = await employeeQuery
                .Select(e => e.Department.Name)
                .FirstOrDefaultAsync() ?? "Unassigned Department";

            var expectedTotal = await employeeQuery.CountAsync();
            var present = await employeeQuery
                .CountAsync(e => presentStaffNumbers.Contains(e.StaffNumber));
            var absent = expectedTotal - present;
            var rate = expectedTotal > 0 ? Math.Round((decimal)present / expectedTotal * 100, 2) : 0;

            return Result.Success(new List<DailyAttendanceRateDto>
            {
                new DailyAttendanceRateDto
                {
                    Department = deptName,
                    Present = present,
                    Absent = absent,
                    ExpectedTotal = expectedTotal,
                    AttendanceRatePercent = rate,
                }
            }.AsEnumerable());
        }
        else
        {
            var expectedByDept = await employeeQuery
                .GroupBy(e => e.Department.Name)
                .Select(g => new { Department = g.Key, Expected = g.Count() })
                .ToListAsync();

            var presentByDept = await employeeQuery
                .Where(e => presentStaffNumbers.Contains(e.StaffNumber))
                .GroupBy(e => e.Department.Name)
                .Select(g => new { Department = g.Key, Present = g.Count() })
                .ToListAsync();

            var results = expectedByDept
                .Select(e =>
                {
                    var present = presentByDept.FirstOrDefault(p => p.Department == e.Department)?.Present ?? 0;
                    return new DailyAttendanceRateDto
                    {
                        Department = e.Department ?? "Unassigned Department",
                        Present = present,
                        Absent = e.Expected - present,
                        ExpectedTotal = e.Expected,
                        AttendanceRatePercent = e.Expected > 0
                            ? Math.Round((decimal)present / e.Expected * 100, 2)
                            : 0,
                    };
                })
                .OrderBy(r => r.Department)
                .ToList();

            return Result.Success(results.AsEnumerable());
        }
    }

    public async Task<Result<IEnumerable<StaffRequisitionPipelineDto>>> GetStaffRequisitionPipeline(
        HrKpiFilterDto filter, Guid? departmentId)
    {
        var query = context.StaffRequisitions.IgnoreQueryFilters().AsNoTracking()
            .Where(sr => !sr.DeletedAt.HasValue);

        if (departmentId.HasValue)
            query = query.Where(sr => sr.DepartmentId == departmentId.Value);

        if (filter.StartDate.HasValue)
            query = query.Where(sr => sr.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(sr => sr.CreatedAt <= filter.EndDate.Value);

        var statuses = new[]
        {
            StaffRequisitionStatus.New,
            StaffRequisitionStatus.Pending,
            StaffRequisitionStatus.Approved,
            StaffRequisitionStatus.Rejected,
        };

        if (departmentId.HasValue)
        {
            var deptName = await query
                .Select(sr => sr.Department.Name)
                .FirstOrDefaultAsync() ?? "Unassigned Department";

            var groups = await query
                .GroupBy(sr => sr.StaffRequisitionStatus)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count(),
                    TotalPositions = g.Sum(sr => sr.StaffRequired),
                })
                .ToListAsync();

            var results = statuses
                .Select(s =>
                {
                    var match = groups.FirstOrDefault(g => g.Status == s);
                    return new StaffRequisitionPipelineDto
                    {
                        Department = deptName,
                        Status = s.ToString(),
                        Count = match?.Count ?? 0,
                        TotalPositionsRequired = match?.TotalPositions ?? 0,
                    };
                })
                .ToList();

            return Result.Success(results.AsEnumerable());
        }
        else
        {
            var groups = await query
                .GroupBy(sr => new { sr.Department.Name, sr.StaffRequisitionStatus })
                .Select(g => new
                {
                    Department = g.Key.Name,
                    Status = g.Key.StaffRequisitionStatus,
                    Count = g.Count(),
                    TotalPositions = g.Sum(sr => sr.StaffRequired),
                })
                .ToListAsync();

            var results = groups
                .Select(g => new StaffRequisitionPipelineDto
                {
                    Department = g.Department ?? "Unassigned Department",
                    Status = g.Status.ToString(),
                    Count = g.Count,
                    TotalPositionsRequired = g.TotalPositions,
                })
                .OrderBy(r => r.Department)
                .ThenBy(r => r.Status)
                .ToList();

            return Result.Success(results.AsEnumerable());
        }
    }

    public async Task<Result<IEnumerable<EmployeeGradeLevelDistributionDto>>> GetEmployeeGradeLevelDistribution(
        Guid? departmentId)
    {
        var query = context.Employees.IgnoreQueryFilters().AsNoTracking()
            .Where(e => !e.DeletedAt.HasValue && e.Status == EmployeeStatus.Active);

        if (departmentId.HasValue)
            query = query.Where(e => e.DepartmentId == departmentId.Value);

        if (departmentId.HasValue)
        {
            var deptName = await query
                .Select(e => e.Department.Name)
                .FirstOrDefaultAsync() ?? "Unassigned Department";

            var juniorStaff = await query.CountAsync(e => e.Level == EmployeeLevel.JuniorStaff);
            var seniorStaff = await query.CountAsync(e => e.Level == EmployeeLevel.SeniorStaff);
            var seniorMgmt = await query.CountAsync(e => e.Level == EmployeeLevel.SeniorManagement);

            return Result.Success(new List<EmployeeGradeLevelDistributionDto>
            {
                new EmployeeGradeLevelDistributionDto
                {
                    Department = deptName,
                    JuniorStaff = juniorStaff,
                    SeniorStaff = seniorStaff,
                    SeniorManagement = seniorMgmt,
                }
            }.AsEnumerable());
        }
        else
        {
            var groups = await query
                .GroupBy(e => e.Department.Name)
                .Select(g => new
                {
                    Department = g.Key,
                    JuniorStaff = g.Count(e => e.Level == EmployeeLevel.JuniorStaff),
                    SeniorStaff = g.Count(e => e.Level == EmployeeLevel.SeniorStaff),
                    SeniorManagement = g.Count(e => e.Level == EmployeeLevel.SeniorManagement),
                })
                .ToListAsync();

            var results = groups
                .Select(g => new EmployeeGradeLevelDistributionDto
                {
                    Department = g.Department ?? "Unassigned Department",
                    JuniorStaff = g.JuniorStaff,
                    SeniorStaff = g.SeniorStaff,
                    SeniorManagement = g.SeniorManagement,
                })
                .OrderBy(r => r.Department)
                .ToList();

            return Result.Success(results.AsEnumerable());
        }
    }

    public async Task<Result<IEnumerable<NewHiresThisPeriodDto>>> GetNewHiresThisPeriod(
        HrKpiFilterDto filter, Guid? departmentId)
    {
        var query = context.Employees.IgnoreQueryFilters().AsNoTracking()
            .Where(e => !e.DeletedAt.HasValue && e.Status == EmployeeStatus.New);

        if (departmentId.HasValue)
            query = query.Where(e => e.DepartmentId == departmentId.Value);

        if (filter.StartDate.HasValue)
            query = query.Where(e => e.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(e => e.CreatedAt <= filter.EndDate.Value);

        if (departmentId.HasValue)
        {
            var deptName = await query
                .Select(e => e.Department.Name)
                .FirstOrDefaultAsync() ?? "Unassigned Department";

            var permCount = await query.CountAsync(e => e.Type == EmployeeType.Permanent);
            var casualCount = await query.CountAsync(e => e.Type == EmployeeType.Casual);

            return Result.Success(new List<NewHiresThisPeriodDto>
            {
                new NewHiresThisPeriodDto
                {
                    Department = deptName,
                    PermanentNewHires = permCount,
                    CasualNewHires = casualCount,
                }
            }.AsEnumerable());
        }
        else
        {
            var groups = await query
                .GroupBy(e => e.Department.Name)
                .Select(g => new
                {
                    Department = g.Key,
                    PermanentNewHires = g.Count(e => e.Type == EmployeeType.Permanent),
                    CasualNewHires = g.Count(e => e.Type == EmployeeType.Casual),
                })
                .ToListAsync();

            var results = groups
                .Select(g => new NewHiresThisPeriodDto
                {
                    Department = g.Department ?? "Unassigned Department",
                    PermanentNewHires = g.PermanentNewHires,
                    CasualNewHires = g.CasualNewHires,
                })
                .OrderBy(r => r.Department)
                .ToList();

            return Result.Success(results.AsEnumerable());
        }
    }

    public async Task<Result<IEnumerable<EmployeeTurnoverRateDto>>> GetEmployeeTurnoverRate(
        HrKpiFilterDto filter, Guid? departmentId)
    {
        var inactiveStatuses = new[]
        {
            EmployeeInactiveStatus.Resignation,
            EmployeeInactiveStatus.Termination,
            EmployeeInactiveStatus.SummaryDismissed,
            EmployeeInactiveStatus.VacatedPost,
        };

        var baseQuery = context.Employees.IgnoreQueryFilters().AsNoTracking()
            .Where(e => !e.DeletedAt.HasValue);

        if (departmentId.HasValue)
            baseQuery = baseQuery.Where(e => e.DepartmentId == departmentId.Value);

        var startDate = filter.StartDate ?? DateTime.UtcNow.Date.AddMonths(-1);
        var endDate = filter.EndDate ?? DateTime.UtcNow.Date;

        if (departmentId.HasValue)
        {
            var deptName = await baseQuery
                .Select(e => e.Department.Name)
                .FirstOrDefaultAsync() ?? "Unassigned Department";

            var leavers = await baseQuery
                .CountAsync(e => e.InactiveStatus.HasValue
                    && inactiveStatuses.Contains(e.InactiveStatus.Value)
                    && e.ExitDate.HasValue
                    && e.ExitDate >= startDate
                    && e.ExitDate <= endDate);

            var startHeadcount = await baseQuery
                .CountAsync(e => e.CreatedAt < startDate
                    && (!e.ExitDate.HasValue || e.ExitDate >= startDate));

            var endHeadcount = await baseQuery
                .CountAsync(e => e.CreatedAt <= endDate
                    && (!e.ExitDate.HasValue || e.ExitDate > endDate));

            var avgHeadcount = (startHeadcount + endHeadcount) / 2m;
            var turnoverRate = avgHeadcount > 0 ? Math.Round(leavers / avgHeadcount * 100, 2) : 0;

            return Result.Success(new List<EmployeeTurnoverRateDto>
            {
                new EmployeeTurnoverRateDto
                {
                    Department = deptName,
                    Leavers = leavers,
                    AverageHeadcount = avgHeadcount,
                    TurnoverRatePercent = turnoverRate,
                }
            }.AsEnumerable());
        }
        else
        {
            var leaversByDept = await baseQuery
                .Where(e => e.InactiveStatus.HasValue
                    && inactiveStatuses.Contains(e.InactiveStatus.Value)
                    && e.ExitDate.HasValue
                    && e.ExitDate >= startDate
                    && e.ExitDate <= endDate)
                .GroupBy(e => e.Department.Name)
                .Select(g => new { Department = g.Key, Leavers = g.Count() })
                .ToListAsync();

            var startByDept = await baseQuery
                .Where(e => e.CreatedAt < startDate && (!e.ExitDate.HasValue || e.ExitDate >= startDate))
                .GroupBy(e => e.Department.Name)
                .Select(g => new { Department = g.Key, Count = g.Count() })
                .ToListAsync();

            var endByDept = await baseQuery
                .Where(e => e.CreatedAt <= endDate && (!e.ExitDate.HasValue || e.ExitDate > endDate))
                .GroupBy(e => e.Department.Name)
                .Select(g => new { Department = g.Key, Count = g.Count() })
                .ToListAsync();

            var allDepts = startByDept.Select(d => d.Department)
                .Union(endByDept.Select(d => d.Department))
                .Union(leaversByDept.Select(d => d.Department))
                .Distinct()
                .ToList();

            var results = allDepts
                .Select(dept =>
                {
                    var leavers = leaversByDept.FirstOrDefault(l => l.Department == dept)?.Leavers ?? 0;
                    var startCount = startByDept.FirstOrDefault(s => s.Department == dept)?.Count ?? 0;
                    var endCount = endByDept.FirstOrDefault(e => e.Department == dept)?.Count ?? 0;
                    var avgHeadcount = (startCount + endCount) / 2m;
                    var turnoverRate = avgHeadcount > 0 ? Math.Round(leavers / avgHeadcount * 100, 2) : 0;

                    return new EmployeeTurnoverRateDto
                    {
                        Department = dept ?? "Unassigned Department",
                        Leavers = leavers,
                        AverageHeadcount = avgHeadcount,
                        TurnoverRatePercent = turnoverRate,
                    };
                })
                .OrderBy(r => r.Department)
                .ToList();

            return Result.Success(results.AsEnumerable());
        }
    }

    public async Task<Result<IEnumerable<LeaveUtilisationRateDto>>> GetLeaveUtilisationRate(
        HrKpiFilterDto filter, Guid? departmentId)
    {
        var year = filter.Year ?? DateTime.UtcNow.Year;

        var employeeBase = context.Employees.IgnoreQueryFilters().AsNoTracking()
            .Where(e => !e.DeletedAt.HasValue);

        if (departmentId.HasValue)
            employeeBase = employeeBase.Where(e => e.DepartmentId == departmentId.Value);

        if (departmentId.HasValue)
        {
            var deptName = await employeeBase
                .Select(e => e.Department.Name)
                .FirstOrDefaultAsync() ?? "Unassigned Department";

            var staffDue = await employeeBase.CountAsync();

            var totalDaysAllowed = await employeeBase
                .SumAsync(e => e.Designation != null ? e.Designation.MaximumLeaveDays : 0);

            var daysUsedData = await context.LeaveRequests.IgnoreQueryFilters().AsNoTracking()
                .Where(lr => !lr.DeletedAt.HasValue
                    && lr.Approved
                    && lr.Employee.DepartmentId == departmentId.Value
                    && lr.StartDate.Year == year)
                .Select(lr => new { lr.StartDate, lr.EndDate })
                .ToListAsync();

            var totalDaysUsed = daysUsedData.Sum(lr => (lr.EndDate - lr.StartDate).Days);
            var utilisationRate = totalDaysAllowed > 0
                ? Math.Round((decimal)totalDaysUsed / totalDaysAllowed * 100, 2)
                : 0;

            return Result.Success(new List<LeaveUtilisationRateDto>
            {
                new LeaveUtilisationRateDto
                {
                    Department = deptName,
                    StaffDueForLeave = staffDue,
                    TotalDaysAllowed = totalDaysAllowed,
                    TotalDaysUsed = totalDaysUsed,
                    UtilisationPercent = utilisationRate,
                }
            }.AsEnumerable());
        }
        else
        {
            var staffAndAllowedByDept = await
                (from e in employeeBase
                 group e by e.Department.Name into g
                 select new
                 {
                     Department = g.Key,
                     StaffDue = g.Count(),
                     TotalAllowed = g.Sum(e => e.Designation != null ? e.Designation.MaximumLeaveDays : 0)
                 })
                .ToListAsync();

            var daysUsedByDept = await
                (from lr in context.LeaveRequests.IgnoreQueryFilters().AsNoTracking()
                 join e in context.Employees.IgnoreQueryFilters().AsNoTracking()
                     .Where(emp => !emp.DeletedAt.HasValue)
                     on lr.EmployeeId equals e.Id
                 where !lr.DeletedAt.HasValue
                     && lr.Approved
                     && lr.StartDate.Year == year
                 select new { DaysDiff = (lr.EndDate - lr.StartDate).Days, DeptName = e.Department.Name })
                .GroupBy(x => x.DeptName)
                .Select(g => new { Department = g.Key, Total = g.Sum(x => x.DaysDiff) })
                .ToListAsync();

            var allDepts = staffAndAllowedByDept.Select(d => d.Department)
                .Union(daysUsedByDept.Select(d => d.Department))
                .Distinct()
                .ToList();

            var results = allDepts
                .Select(dept =>
                {
                    var staffInfo = staffAndAllowedByDept.FirstOrDefault(s => s.Department == dept);
                    var staffDue = staffInfo?.StaffDue ?? 0;
                    var daysAllowed = staffInfo?.TotalAllowed ?? 0;
                    var daysUsed = daysUsedByDept.FirstOrDefault(d => d.Department == dept)?.Total ?? 0;
                    var utilisationRate = daysAllowed > 0
                        ? Math.Round((decimal)daysUsed / daysAllowed * 100, 2)
                        : 0;

                    return new LeaveUtilisationRateDto
                    {
                        Department = dept ?? "Unassigned Department",
                        StaffDueForLeave = staffDue,
                        TotalDaysAllowed = daysAllowed,
                        TotalDaysUsed = daysUsed,
                        UtilisationPercent = utilisationRate,
                    };
                })
                .OrderBy(r => r.Department)
                .ToList();

            return Result.Success(results.AsEnumerable());
        }
    }

    public async Task<Result<IEnumerable<ActiveDisciplinaryActionsDto>>> GetActiveDisciplinaryActions(
        Guid? departmentId)
    {
        var query = context.Employees.IgnoreQueryFilters().AsNoTracking()
            .Where(e => !e.DeletedAt.HasValue && e.ActiveStatus.HasValue);

        if (departmentId.HasValue)
            query = query.Where(e => e.DepartmentId == departmentId.Value);

        if (departmentId.HasValue)
        {
            var deptName = await query
                .Select(e => e.Department.Name)
                .FirstOrDefaultAsync() ?? "Unassigned Department";

            var underQuestion = await query.CountAsync(e => e.ActiveStatus == EmployeeActiveStatus.Question);
            var formalWarning = await query.CountAsync(e => e.ActiveStatus == EmployeeActiveStatus.Warning);
            var finalWarning = await query.CountAsync(e => e.ActiveStatus == EmployeeActiveStatus.FinalWarning);
            var suspended = await query.CountAsync(e => e.ActiveStatus == EmployeeActiveStatus.Suspension);

            return Result.Success(new List<ActiveDisciplinaryActionsDto>
            {
                new ActiveDisciplinaryActionsDto
                {
                    Department = deptName,
                    UnderQuestion = underQuestion,
                    FormalWarning = formalWarning,
                    FinalWarning = finalWarning,
                    Suspended = suspended,
                }
            }.AsEnumerable());
        }
        else
        {
            var groups = await query
                .GroupBy(e => e.Department.Name)
                .Select(g => new
                {
                    Department = g.Key,
                    UnderQuestion = g.Count(e => e.ActiveStatus == EmployeeActiveStatus.Question),
                    FormalWarning = g.Count(e => e.ActiveStatus == EmployeeActiveStatus.Warning),
                    FinalWarning = g.Count(e => e.ActiveStatus == EmployeeActiveStatus.FinalWarning),
                    Suspended = g.Count(e => e.ActiveStatus == EmployeeActiveStatus.Suspension),
                })
                .ToListAsync();

            var results = groups
                .Select(g => new ActiveDisciplinaryActionsDto
                {
                    Department = g.Department ?? "Unassigned Department",
                    UnderQuestion = g.UnderQuestion,
                    FormalWarning = g.FormalWarning,
                    FinalWarning = g.FinalWarning,
                    Suspended = g.Suspended,
                })
                .OrderBy(r => r.Department)
                .ToList();

            return Result.Success(results.AsEnumerable());
        }
    }

    public async Task<Result<List<EmployeeMasterListReportDto>>> GetEmployeeMasterList(
        EmployeeMasterListFilter filter)
    {
        var query = context.Employees
            .AsSplitQuery()
            .IgnoreAutoIncludes()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => !e.DeletedAt.HasValue)
            .Include(e => e.Department)
            .Include(e => e.Designation)
            .Include(e => e.ReportingManager)
            .AsQueryable();

        if (filter.DepartmentId.HasValue)
            query = query.Where(e => e.DepartmentId == filter.DepartmentId.Value);

        if (filter.EmployeeType.HasValue)
            query = query.Where(e => e.Type == filter.EmployeeType.Value);

        if (filter.GradeLevel.HasValue)
            query = query.Where(e => e.Level == filter.GradeLevel.Value);

        if (filter.Status.HasValue)
            query = query.Where(e => e.Status == filter.Status.Value);

        var result = await query
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Select(e => new EmployeeMasterListReportDto
            {
                StaffNumber = e.StaffNumber,
                FirstName = e.FirstName,
                LastName = e.LastName,
                Email = e.Email,
                PhoneNumber = e.PhoneNumber,
                Gender = e.Gender.ToString(),
                DateOfBirth = e.DateOfBirth,
                DateEmployed = e.DateEmployed,
                EmploymentType = e.Type.ToString(),
                GradeLevel = e.Level.ToString() ?? "N/A",
                Status = e.Status.ToString(),
                Department = e.Department != null ? e.Department.Name : "Unassigned",
                Designation = e.Designation != null ? e.Designation.Name : "N/A",
                ReportingManager = e.ReportingManager != null
                    ? e.ReportingManager.FirstName + " " + e.ReportingManager.LastName
                    : "N/A",
                Nationality = e.Nationality,
                Region = e.Region,
                MaritalStatus = e.MaritalStatus.ToString(),
                Religion = e.Religion.ToString(),
                BankAccountNumber = e.BankAccountNumber,
                SsnitNumber = e.SsnitNumber,
                GhanaCardNumber = e.GhanaCardNumber,
                AnnualLeaveEntitlement = e.AnnualLeaveDays,
            })
            .ToListAsync();

        var numbered = result.Select((e, idx) =>
        {
            e.No = idx + 1;
            return e;
        }).ToList();

        return Result.Success(numbered);
    }

    public async Task<Result<List<EmployeeDirectoryByDepartmentDto>>> GetEmployeeDirectoryByDepartment(
        EmployeeDirectoryFilter filter)
    {
        var query = context.Employees
            .AsSplitQuery()
            .IgnoreAutoIncludes()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => !e.DeletedAt.HasValue)
            .Include(e => e.Department)
            .Include(e => e.Designation)
            .Include(e => e.ReportingManager)
            .AsQueryable();

        if (filter.DepartmentId.HasValue)
            query = query.Where(e => e.DepartmentId == filter.DepartmentId.Value);

        if (filter.GradeLevel.HasValue)
            query = query.Where(e => e.Level == filter.GradeLevel.Value);

        if (filter.EmployeeType.HasValue)
            query = query.Where(e => e.Type == filter.EmployeeType.Value);

        var result = await query
            .OrderBy(e => e.Department != null ? e.Department.Name : "Unassigned")
            .ThenBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Select(e => new EmployeeDirectoryByDepartmentDto
            {
                Department = e.Department != null ? e.Department.Name : "Unassigned",
                StaffNumber = e.StaffNumber,
                EmployeeName = e.FirstName + " " + e.LastName,
                Designation = e.Designation != null ? e.Designation.Name : "N/A",
                GradeLevel = e.Level.ToString() ?? "N/A",
                EmploymentType = e.Type.ToString(),
                Status = e.Status == EmployeeStatus.Active ? "Active" : "Inactive",
                ReportingManager = e.ReportingManager != null
                    ? e.ReportingManager.FirstName + " " + e.ReportingManager.LastName
                    : "N/A",
                Email = e.Email,
                Phone = e.PhoneNumber,
            })
            .ToListAsync();

        var numbered = result.Select((e, idx) =>
        {
            e.No = idx + 1;
            return e;
        }).ToList();

        return Result.Success(numbered);
    }

    public async Task<Result<List<EmployeeDemographicsReportDto>>> GetEmployeeDemographics(
        EmployeeDemographicsFilter filter)
    {
        var query = context.Employees
            .IgnoreAutoIncludes()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => !e.DeletedAt.HasValue)
            .AsQueryable();

        if (filter.DepartmentId.HasValue)
            query = query.Where(e => e.DepartmentId == filter.DepartmentId.Value);

        var employees = await query
            .Select(e => new
            {
                e.Gender,
                e.DateOfBirth,
                e.MaritalStatus,
                e.Religion,
                e.Nationality,
                e.Type,
            })
            .ToListAsync();

        var grandTotal = employees.Count;
        if (grandTotal == 0)
            return Result.Success(new List<EmployeeDemographicsReportDto>());

        var result = new List<EmployeeDemographicsReportDto>();
        var no = 1;

        var genderGroups = employees
            .GroupBy(e => e.Gender.ToString())
            .Select(g => new
            {
                Dimension = "Gender",
                Category = g.Key,
                PermanentCount = g.Count(e => e.Type == EmployeeType.Permanent),
                CasualCount = g.Count(e => e.Type == EmployeeType.Casual),
            });

        foreach (var g in genderGroups)
        {
            var total = g.PermanentCount + g.CasualCount;
            result.Add(new EmployeeDemographicsReportDto
            {
                No = no++,
                Dimension = g.Dimension,
                Category = g.Category,
                PermanentCount = g.PermanentCount,
                CasualCount = g.CasualCount,
                TotalCount = total,
                Percentage = Math.Round((double)total / grandTotal * 100, 1),
            });
        }

        var ageGroups = employees
            .GroupBy(e =>
            {
                var age = DateTime.Today.Year - e.DateOfBirth.Year;
                if (e.DateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;
                return age switch
                {
                    >= 20 and <= 30 => "20-30",
                    >= 31 and <= 40 => "31-40",
                    >= 41 and <= 50 => "41-50",
                    >= 51 => "51+",
                    _ => "Under 20",
                };
            })
            .Select(g => new
            {
                Dimension = "Age Group",
                Category = g.Key,
                PermanentCount = g.Count(e => e.Type == EmployeeType.Permanent),
                CasualCount = g.Count(e => e.Type == EmployeeType.Casual),
            });

        foreach (var g in ageGroups)
        {
            var total = g.PermanentCount + g.CasualCount;
            result.Add(new EmployeeDemographicsReportDto
            {
                No = no++,
                Dimension = g.Dimension,
                Category = g.Category,
                PermanentCount = g.PermanentCount,
                CasualCount = g.CasualCount,
                TotalCount = total,
                Percentage = Math.Round((double)total / grandTotal * 100, 1),
            });
        }

        var maritalGroups = employees
            .GroupBy(e => e.MaritalStatus.ToString())
            .Select(g => new
            {
                Dimension = "Marital Status",
                Category = g.Key,
                PermanentCount = g.Count(e => e.Type == EmployeeType.Permanent),
                CasualCount = g.Count(e => e.Type == EmployeeType.Casual),
            });

        foreach (var g in maritalGroups)
        {
            var total = g.PermanentCount + g.CasualCount;
            result.Add(new EmployeeDemographicsReportDto
            {
                No = no++,
                Dimension = g.Dimension,
                Category = g.Category,
                PermanentCount = g.PermanentCount,
                CasualCount = g.CasualCount,
                TotalCount = total,
                Percentage = Math.Round((double)total / grandTotal * 100, 1),
            });
        }

        var religionGroups = employees
            .GroupBy(e => e.Religion.ToString())
            .Select(g => new
            {
                Dimension = "Religion",
                Category = g.Key,
                PermanentCount = g.Count(e => e.Type == EmployeeType.Permanent),
                CasualCount = g.Count(e => e.Type == EmployeeType.Casual),
            });

        foreach (var g in religionGroups)
        {
            var total = g.PermanentCount + g.CasualCount;
            result.Add(new EmployeeDemographicsReportDto
            {
                No = no++,
                Dimension = g.Dimension,
                Category = g.Category,
                PermanentCount = g.PermanentCount,
                CasualCount = g.CasualCount,
                TotalCount = total,
                Percentage = Math.Round((double)total / grandTotal * 100, 1),
            });
        }

        var nationalityGroups = employees
            .GroupBy(e => e.Nationality ?? "Unknown")
            .Select(g => new
            {
                Dimension = "Nationality",
                Category = g.Key,
                PermanentCount = g.Count(e => e.Type == EmployeeType.Permanent),
                CasualCount = g.Count(e => e.Type == EmployeeType.Casual),
            });

        foreach (var g in nationalityGroups)
        {
            var total = g.PermanentCount + g.CasualCount;
            result.Add(new EmployeeDemographicsReportDto
            {
                No = no++,
                Dimension = g.Dimension,
                Category = g.Category,
                PermanentCount = g.PermanentCount,
                CasualCount = g.CasualCount,
                TotalCount = total,
                Percentage = Math.Round((double)total / grandTotal * 100, 1),
            });
        }

        return Result.Success(result);
    }

    public async Task<Result<List<StaffGradeLevelReportDto>>> GetStaffGradeLevel(
        StaffGradeLevelFilter filter)
    {
        var query = context.Employees
            .AsSplitQuery()
            .IgnoreAutoIncludes()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => !e.DeletedAt.HasValue)
            .Include(e => e.Department)
            .AsQueryable();

        if (filter.DepartmentId.HasValue)
            query = query.Where(e => e.DepartmentId == filter.DepartmentId.Value);

        var employees = await query
            .Select(e => new
            {
                e.DepartmentId,
                DepartmentName = e.Department != null ? e.Department.Name : "Unassigned",
                e.Level,
                e.Gender,
            })
            .ToListAsync();

        var grouped = employees
            .GroupBy(e => new { e.DepartmentId, e.DepartmentName })
            .Select(g => new StaffGradeLevelReportDto
            {
                Department = g.Key.DepartmentName,
                SeniorMgtMale = g.Count(e => e.Level == EmployeeLevel.SeniorManagement && e.Gender == Gender.Male),
                SeniorMgtFemale = g.Count(e => e.Level == EmployeeLevel.SeniorManagement && e.Gender == Gender.Female),
                SeniorStaffMale = g.Count(e => e.Level == EmployeeLevel.SeniorStaff && e.Gender == Gender.Male),
                SeniorStaffFemale = g.Count(e => e.Level == EmployeeLevel.SeniorStaff && e.Gender == Gender.Female),
                JuniorStaffMale = g.Count(e => e.Level == EmployeeLevel.JuniorStaff && e.Gender == Gender.Male),
                JuniorStaffFemale = g.Count(e => e.Level == EmployeeLevel.JuniorStaff && e.Gender == Gender.Female),
            })
            .ToList();

        foreach (var dept in grouped)
        {
            dept.TotalMale = dept.SeniorMgtMale + dept.SeniorStaffMale + dept.JuniorStaffMale;
            dept.TotalFemale = dept.SeniorMgtFemale + dept.SeniorStaffFemale + dept.JuniorStaffFemale;
            dept.DepartmentalTotal = dept.TotalMale + dept.TotalFemale;
        }

        var numbered = grouped.Select((e, idx) =>
        {
            e.No = idx + 1;
            return e;
        }).ToList();

        return Result.Success(numbered);
    }

    private static int ComputeOvertimeHours(string startTime, string endTime)
    {
        if (!TimeOnly.TryParse(startTime, out var start) || !TimeOnly.TryParse(endTime, out var end))
            return 0;

        var duration = end.ToTimeSpan() - start.ToTimeSpan();
        return (int)duration.TotalHours;
    }

    public async Task<Result<List<LeaveRegisterReportDto>>> GetLeaveRegister(LeaveRegisterFilter filter)
    {
        var query = context.LeaveRequests
            .AsSplitQuery()
            .IgnoreAutoIncludes()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(lr => lr.Employee)
            .ThenInclude(e => e.Department)
            .Include(lr => lr.LeaveType)
            .Include(lr => lr.Approvals)
            .ThenInclude(a => a.ApprovedBy)
            .AsQueryable();

        if (filter.DepartmentId.HasValue)
            query = query.Where(lr => lr.Employee.DepartmentId == filter.DepartmentId.Value);

        if (filter.LeaveCategory.HasValue)
            query = query.Where(lr => lr.RequestCategory == filter.LeaveCategory.Value);

        if (filter.Status.HasValue)
            query = query.Where(lr => lr.LeaveStatus == filter.Status.Value);

        if (filter.StartDate.HasValue)
            query = query.Where(lr => lr.StartDate >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(lr => lr.EndDate <= filter.EndDate.Value);

        var leaveRequests = await query.ToListAsync();

        var result = leaveRequests.Select((lr, idx) => new LeaveRegisterReportDto
        {
            No = idx + 1,
            EmployeeName = lr.Employee != null
                ? lr.Employee.FirstName + " " + lr.Employee.LastName
                : "Unknown",
            StaffNumber = lr.Employee?.StaffNumber ?? "N/A",
            Department = lr.Employee?.Department != null
                ? lr.Employee.Department.Name
                : "Unassigned",
            LeaveCategory = lr.RequestCategory.ToString(),
            LeaveType = lr.LeaveType?.Name ?? "N/A",
            StartDate = lr.StartDate,
            EndDate = lr.EndDate,
            DurationDays = (lr.EndDate - lr.StartDate).Days + 1,
            PaidDays = lr.PaidDays,
            UnpaidDays = lr.UnpaidDays,
            Status = lr.LeaveStatus.ToString(),
            Justification = lr.Justification,
            ContactPerson = lr.ContactPerson,
            Destination = lr.Destination,
            ApprovedBy = lr.Approvals
                .Where(a => a.Status == ApprovalStatus.Approved)
                .Select(a => a.ApprovedBy != null
                    ? a.ApprovedBy.FirstName + " " + a.ApprovedBy.LastName
                    : null)
                .FirstOrDefault() ?? "N/A",
            DateApplied = lr.CreatedAt,
        }).ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<LeaveBalanceReportDto>>> GetLeaveBalance(LeaveBalanceFilter filter)
    {
        var year = filter.LeaveYear ?? DateTime.UtcNow.Year;

        var employees = context.Employees
            .AsSplitQuery()
            .IgnoreAutoIncludes()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Designation)
            .AsQueryable();

        if (filter.DepartmentId.HasValue)
            employees = employees.Where(e => e.DepartmentId == filter.DepartmentId.Value);

        if (filter.EmployeeId.HasValue)
            employees = employees.Where(e => e.Id == filter.EmployeeId.Value);

        var employeeList = await employees.ToListAsync();

        var employeeIds = employeeList.Select(e => e.Id).Distinct().ToList();

        var approvedLeaves = await context.LeaveRequests
            .AsSplitQuery()
            .IgnoreAutoIncludes()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(lr =>
                employeeIds.Contains(lr.EmployeeId)
                && lr.LeaveStatus == LeaveStatus.Approved
                && lr.StartDate.Year == year
            )
            .ToListAsync();

        var daysUsedByEmployee = approvedLeaves
            .GroupBy(lr => lr.EmployeeId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(lr => (lr.EndDate - lr.StartDate).Days + 1)
            );

        var result = employeeList.Select((emp, idx) =>
        {
            var daysAllowed = emp.Designation?.MaximumLeaveDays ?? 0;
            var daysUsed = daysUsedByEmployee.TryGetValue(emp.Id, out var used) ? used : 0;
            var daysRemaining = daysAllowed - daysUsed;
            var utilisation = daysAllowed > 0
                ? Math.Round((double)daysUsed / daysAllowed * 100, 1)
                : 0;

            return new LeaveBalanceReportDto
            {
                No = idx + 1,
                EmployeeName = emp.FirstName + " " + emp.LastName,
                StaffNumber = emp.StaffNumber ?? "N/A",
                Department = emp.Department != null ? emp.Department.Name : "Unassigned",
                LeaveYear = year,
                DaysAllowed = daysAllowed,
                DaysUsed = daysUsed,
                DaysRemaining = daysRemaining,
                Utilisation = utilisation,
            };
        }).ToList();

        return Result.Success(result);
    }

    public async Task<Result<List<LeaveApprovalAuditReportDto>>> GetLeaveApprovalAudit(LeaveApprovalAuditFilter filter)
    {
        var start = filter.StartDate.HasValue
            ? DateTime.SpecifyKind(filter.StartDate.Value, DateTimeKind.Utc)
            : DateTime.UtcNow.AddMonths(-1);
        var end = filter.EndDate.HasValue
            ? DateTime.SpecifyKind(filter.EndDate.Value, DateTimeKind.Utc)
            : DateTime.UtcNow;

        var query = context.LeaveRequests
            .AsSplitQuery()
            .IgnoreAutoIncludes()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(lr => lr.Employee)
            .ThenInclude(e => e.Department)
            .Include(lr => lr.Approvals)
            .ThenInclude(a => a.ApprovedBy)
            .Where(lr => lr.CreatedAt >= start && lr.CreatedAt <= end)
            .AsQueryable();

        if (filter.DepartmentId.HasValue)
            query = query.Where(lr => lr.Employee.DepartmentId == filter.DepartmentId.Value);

        if (filter.Status.HasValue)
            query = query.Where(lr => lr.LeaveStatus == filter.Status.Value);

        var leaveRequests = await query.ToListAsync();

        var result = new List<LeaveApprovalAuditReportDto>();
        var no = 1;

        foreach (var lr in leaveRequests)
        {
            var durationDays = (lr.EndDate - lr.StartDate).Days + 1;
            var employeeName = lr.Employee != null
                ? lr.Employee.FirstName + " " + lr.Employee.LastName
                : "Unknown";

            if (lr.Approvals == null || lr.Approvals.Count == 0)
            {
                result.Add(new LeaveApprovalAuditReportDto
                {
                    No = no++,
                    Employee = employeeName,
                    LeaveCategory = lr.RequestCategory.ToString(),
                    DurationDays = durationDays,
                    ApprovalStage = "No approvals",
                    Approver = "N/A",
                    ActionTaken = "Pending",
                    ActionDate = null,
                    Comments = null,
                    FinalStatus = lr.LeaveStatus.ToString(),
                });
                continue;
            }

            foreach (var approval in lr.Approvals.OrderBy(a => a.Order))
            {
                var approverName = approval.ApprovedBy != null
                    ? approval.ApprovedBy.FirstName + " " + approval.ApprovedBy.LastName
                    : "N/A";

                result.Add(new LeaveApprovalAuditReportDto
                {
                    No = no++,
                    Employee = employeeName,
                    LeaveCategory = lr.RequestCategory.ToString(),
                    DurationDays = durationDays,
                    ApprovalStage = $"Stage {approval.Order}",
                    Approver = approverName,
                    ActionTaken = approval.Status.ToString(),
                    ActionDate = approval.ApprovalTime,
                    Comments = approval.Comments,
                    FinalStatus = lr.LeaveStatus.ToString(),
                });
            }
        }

        return Result.Success(result);
    }

    // ── Report 14: Overtime Request Register ──────────────────────────────────

    public async Task<Result<List<OvertimeRequestRegisterReportDto>>> GetOvertimeRequestRegister(
        OvertimeRequestRegisterFilter filter)
    {
        var query = context.OvertimeRequests
            .AsSplitQuery()
            .IgnoreAutoIncludes()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(o => !o.DeletedAt.HasValue)
            .AsQueryable();

        if (filter.DepartmentId.HasValue)
            query = query.Where(o => o.DepartmentId == filter.DepartmentId.Value);

        if (filter.Status.HasValue)
            query = query.Where(o => o.Status == filter.Status.Value);

        if (filter.StartDate.HasValue)
        {
            var start = DateTime.SpecifyKind(filter.StartDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(o => o.OvertimeDate >= start);
        }

        if (filter.EndDate.HasValue)
        {
            var end = DateTime.SpecifyKind(filter.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(o => o.OvertimeDate <= end);
        }

        var result = await query
            .OrderBy(o => o.OvertimeDate)
            .Select(o => new OvertimeRequestRegisterReportDto
            {
                RequestCode = o.Code,
                Department = o.Department != null ? o.Department.Name : "Unassigned",
                Employees = string.Join(", ", o.Employees.Select(e => e.FirstName + " " + e.LastName)),
                OvertimeDate = o.OvertimeDate,
                StartTime = o.StartTime,
                EndTime = o.EndTime,
                TotalHours = o.TotalHours,
                Justification = o.Justification,
                Status = o.Status.ToString(),
                ApprovedBy = o.Approvals
                    .Where(a => a.Status == ApprovalStatus.Approved)
                    .Select(a => a.ApprovedBy != null
                        ? a.ApprovedBy.FirstName + " " + a.ApprovedBy.LastName
                        : null)
                    .FirstOrDefault() ?? "N/A",
                DateRequested = o.CreatedAt,
            })
            .ToListAsync();

        var numbered = result.Select((o, idx) =>
        {
            o.No = idx + 1;
            return o;
        }).ToList();

        return Result.Success(numbered);
    }

    // ── Report 15: Staff Requisition Register ─────────────────────────────────

    public async Task<Result<List<StaffRequisitionRegisterReportDto>>> GetStaffRequisitionRegister(
        StaffRequisitionRegisterFilter filter)
    {
        var query = context.StaffRequisitions
            .AsSplitQuery()
            .IgnoreAutoIncludes()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => !s.DeletedAt.HasValue)
            .AsQueryable();

        if (filter.DepartmentId.HasValue)
            query = query.Where(s => s.DepartmentId == filter.DepartmentId.Value);

        if (filter.Status.HasValue)
            query = query.Where(s => s.StaffRequisitionStatus == filter.Status.Value);

        if (filter.AppointmentType.HasValue)
            query = query.Where(s => s.AppointmentType == filter.AppointmentType.Value);

        if (filter.StartDate.HasValue)
        {
            var start = DateTime.SpecifyKind(filter.StartDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(s => s.CreatedAt >= start);
        }

        if (filter.EndDate.HasValue)
        {
            var end = DateTime.SpecifyKind(filter.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(s => s.CreatedAt <= end);
        }

        var result = await query
            .OrderBy(s => s.CreatedAt)
            .Select(s => new StaffRequisitionRegisterReportDto
            {
                Department = s.Department != null ? s.Department.Name : "Unassigned",
                Designation = s.Designation != null ? s.Designation.Name : "Unassigned",
                PositionsRequired = s.StaffRequired,
                AppointmentType = s.AppointmentType.ToString(),
                BudgetStatus = s.BudgetStatus.ToString(),
                RequestUrgency = s.RequestUrgency.ToString("dd MMM yyyy"),
                BusinessJustification = s.Justification,
                RequiredQualifications = s.Qualification,
                EducationRequirements = s.EducationalQualification,
                AdditionalRequirements = s.AdditionalRequirements,
                Status = s.StaffRequisitionStatus.ToString(),
                DateRequested = s.CreatedAt,
            })
            .ToListAsync();

        var numbered = result.Select((s, idx) =>
        {
            s.No = idx + 1;
            return s;
        }).ToList();

        return Result.Success(numbered);
    }

    // ── Report 16: Employee Suspension & Disciplinary Action ──────────────────

    public async Task<Result<List<EmployeeDisciplinaryReportDto>>> GetEmployeeDisciplinaryReport(
        EmployeeDisciplinaryFilter filter)
    {
        var today = DateTime.UtcNow;
        var query = context.Employees
            .AsSplitQuery()
            .IgnoreAutoIncludes()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => e.ActiveStatus.HasValue && !e.DeletedAt.HasValue)
            .AsQueryable();

        if (filter.DepartmentId.HasValue)
            query = query.Where(e => e.DepartmentId == filter.DepartmentId.Value);

        if (filter.DisciplinaryStatus.HasValue)
            query = query.Where(e => e.ActiveStatus == filter.DisciplinaryStatus.Value);

        var result = await query
            .OrderBy(e => e.Department!.Name)
            .ThenBy(e => e.LastName)
            .Select(e => new EmployeeDisciplinaryReportDto
            {
                EmployeeName = e.FirstName + " " + e.LastName,
                StaffNumber = e.StaffNumber,
                Department = e.Department != null ? e.Department.Name : "Unassigned",
                Designation = e.Designation != null ? e.Designation.Name : "Unassigned",
                DisciplinaryStatus = e.ActiveStatus!.Value.ToString(),
                SuspensionStartDate = e.SuspensionStartDate,
                SuspensionEndDate = e.SuspensionEndDate,
                ActiveStatus = e.Status.ToString(),
                DaysUnderAction = e.SuspensionStartDate.HasValue
                    ? ((e.SuspensionEndDate ?? today) - e.SuspensionStartDate.Value).Days + 1
                    : 0,
            })
            .ToListAsync();

        var numbered = result.Select((e, idx) =>
        {
            e.No = idx + 1;
            return e;
        }).ToList();

        return Result.Success(numbered);
    }

    // ── Report 17: Employee Exit & Separation ─────────────────────────────────

    public async Task<Result<List<EmployeeExitReportDto>>> GetEmployeeExitReport(
        EmployeeExitFilter filter)
    {
        var query = context.Employees
            .AsSplitQuery()
            .IgnoreAutoIncludes()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => e.InactiveStatus.HasValue && !e.DeletedAt.HasValue)
            .AsQueryable();

        if (filter.DepartmentId.HasValue)
            query = query.Where(e => e.DepartmentId == filter.DepartmentId.Value);

        if (filter.ExitReason.HasValue)
            query = query.Where(e => e.InactiveStatus == filter.ExitReason.Value);

        if (filter.StartDate.HasValue)
        {
            var start = DateTime.SpecifyKind(filter.StartDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(e => e.ExitDate.HasValue && e.ExitDate.Value >= start);
        }

        if (filter.EndDate.HasValue)
        {
            var end = DateTime.SpecifyKind(filter.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(e => e.ExitDate.HasValue && e.ExitDate.Value <= end);
        }

        var result = await query
            .OrderBy(e => e.ExitDate)
            .Select(e => new EmployeeExitReportDto
            {
                EmployeeName = e.FirstName + " " + e.LastName,
                StaffNumber = e.StaffNumber,
                Department = e.Department != null ? e.Department.Name : "Unassigned",
                Designation = e.Designation != null ? e.Designation.Name : "Unassigned",
                EmploymentType = e.Type.ToString(),
                GradeLevel = e.Level.ToString(),
                ExitReason = e.InactiveStatus!.Value.ToString(),
                ExitDate = e.ExitDate ?? e.UpdatedAt ?? e.CreatedAt,
                DateEmployed = e.DateEmployed,
                TenureYears = e.DateEmployed > DateTime.MinValue
                    ? (int)(((e.ExitDate ?? e.UpdatedAt ?? e.CreatedAt) - e.DateEmployed).TotalDays / 365.25)
                    : 0,
                NoticePeriodServed = e.ExitDate.HasValue
                    ? $"{Math.Max(0, (e.ExitDate.Value - e.DateEmployed).Days)} days"
                    : "N/A",
            })
            .ToListAsync();

        var numbered = result.Select((e, idx) =>
        {
            e.No = idx + 1;
            return e;
        }).ToList();

        return Result.Success(numbered);
    }

    // ── Report 18: Employee Anniversary & Birthday List ───────────────────────

    public async Task<Result<List<EmployeeAnniversaryBirthdayReportDto>>> GetEmployeeAnniversaryBirthdayReport(
        EmployeeAnniversaryBirthdayFilter filter)
    {
        var today = DateTime.UtcNow;
        var startDate = filter.StartDate ?? today;
        var endDate = filter.EndDate ?? today.AddMonths(3);

        var startMonth = startDate.Month;
        var endMonth = endDate.Month;
        var crossesYear = startMonth > endMonth;

        var needAnniversary = string.IsNullOrEmpty(filter.EventType) || filter.EventType == "Anniversary";
        var needBirthday = string.IsNullOrEmpty(filter.EventType) || filter.EventType == "Birthday";

        var employees = await context.Employees
            .AsSplitQuery()
            .IgnoreAutoIncludes()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => e.Status == EmployeeStatus.Active && !e.DeletedAt.HasValue
                && ((needAnniversary && (crossesYear
                    ? (e.DateEmployed.Month >= startMonth || e.DateEmployed.Month <= endMonth)
                    : (e.DateEmployed.Month >= startMonth && e.DateEmployed.Month <= endMonth)))
                || (needBirthday && (crossesYear
                    ? (e.DateOfBirth.Month >= startMonth || e.DateOfBirth.Month <= endMonth)
                    : (e.DateOfBirth.Month >= startMonth && e.DateOfBirth.Month <= endMonth)))))
            .Select(e => new
            {
                e.FirstName,
                e.LastName,
                e.StaffNumber,
                DepartmentName = e.Department != null ? e.Department.Name : "Unassigned",
                DesignationName = e.Designation != null ? e.Designation.Name : "Unassigned",
                e.DateEmployed,
                e.DateOfBirth,
            })
            .ToListAsync();

        var result = new List<EmployeeAnniversaryBirthdayReportDto>();

        foreach (var e in employees)
        {
            // Work Anniversary
            if (e.DateEmployed > DateTime.MinValue)
            {
                var anniversaryThisYear = DateTime.SpecifyKind(new DateTime(today.Year, e.DateEmployed.Month, e.DateEmployed.Day), DateTimeKind.Utc);
                if (anniversaryThisYear < today)
                    anniversaryThisYear = anniversaryThisYear.AddYears(1);

                if (anniversaryThisYear >= startDate && anniversaryThisYear <= endDate
                    && (string.IsNullOrEmpty(filter.EventType) || filter.EventType == "Anniversary"))
                {
                    var yearsOfService = today.Year - e.DateEmployed.Year;
                    if (today < e.DateEmployed.AddYears(yearsOfService))
                        yearsOfService--;

                    result.Add(new EmployeeAnniversaryBirthdayReportDto
                    {
                        EventType = "Work Anniversary",
                        EmployeeName = e.FirstName + " " + e.LastName,
                        StaffNumber = e.StaffNumber,
                        Department = e.DepartmentName,
                        Designation = e.DesignationName,
                        EventDate = anniversaryThisYear,
                        YearsOfService = yearsOfService,
                    });
                }
            }

            // Birthday
            if (e.DateOfBirth > DateTime.MinValue)
            {
                var birthdayThisYear = DateTime.SpecifyKind(new DateTime(today.Year, e.DateOfBirth.Month, e.DateOfBirth.Day), DateTimeKind.Utc);
                if (birthdayThisYear < today)
                    birthdayThisYear = birthdayThisYear.AddYears(1);

                if (birthdayThisYear >= startDate && birthdayThisYear <= endDate
                    && (string.IsNullOrEmpty(filter.EventType) || filter.EventType == "Birthday"))
                {
                    var age = today.Year - e.DateOfBirth.Year;
                    if (today < e.DateOfBirth.AddYears(age))
                        age--;

                    result.Add(new EmployeeAnniversaryBirthdayReportDto
                    {
                        EventType = "Birthday",
                        EmployeeName = e.FirstName + " " + e.LastName,
                        StaffNumber = e.StaffNumber,
                        Department = e.DepartmentName,
                        Designation = e.DesignationName,
                        EventDate = birthdayThisYear,
                        Age = age,
                    });
                }
            }
        }

        result = result.OrderBy(r => r.EventDate).ToList();
        for (var i = 0; i < result.Count; i++)
            result[i].No = i + 1;

        return Result.Success(result);
    }

    // ── Report 19: Shift Schedule Register ────────────────────────────────────

    public async Task<Result<List<ShiftScheduleRegisterReportDto>>> GetShiftScheduleRegister(
        ShiftScheduleRegisterFilter filter)
    {
        var query = context.ShiftAssignments
            .AsSplitQuery()
            .IgnoreAutoIncludes()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(sa => !sa.DeletedAt.HasValue)
            .AsQueryable();

        if (filter.DepartmentId.HasValue)
            query = query.Where(sa => sa.ShiftSchedules.DepartmentId == filter.DepartmentId.Value);

        if (filter.Status.HasValue)
            query = query.Where(sa => sa.ShiftSchedules.ScheduleStatus == filter.Status.Value);

        if (filter.StartDate.HasValue)
        {
            var start = DateTime.SpecifyKind(filter.StartDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(sa => sa.ScheduleDate >= start);
        }

        if (filter.EndDate.HasValue)
        {
            var end = DateTime.SpecifyKind(filter.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(sa => sa.ScheduleDate <= end);
        }

        var result = await query
            .OrderBy(sa => sa.ScheduleDate)
            .ThenBy(sa => sa.ShiftSchedules.ScheduleName)
            .Select(sa => new ShiftScheduleRegisterReportDto
            {
                ScheduleName = sa.ShiftSchedules.ScheduleName,
                Frequency = sa.ShiftSchedules.Frequency.ToString(),
                StartDate = sa.ShiftSchedules.StartDate,
                EndDate = sa.ShiftSchedules.EndDate,
                Status = sa.ShiftSchedules.ScheduleStatus.ToString(),
                Employee = sa.Employee.FirstName + " " + sa.Employee.LastName,
                ShiftType = sa.ShiftType.ShiftName,
                ShiftDate = sa.ScheduleDate,
                RotationType = sa.ShiftType.RotationType.ToString(),
            })
            .ToListAsync();

        var numbered = result.Select((sa, idx) =>
        {
            sa.No = idx + 1;
            return sa;
        }).ToList();

        return Result.Success(numbered);
    }
}
