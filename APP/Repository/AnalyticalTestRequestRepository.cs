using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Products.Equipments;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class AnalyticalTestRequestRepository(ApplicationDbContext context, IMapper mapper)
    : IAnalyticalTestRequestRepository
{
    public async Task<Result<Guid>> CreateAnalyticalTestRequest(CreateAnalyticalTestRequest request)
    {
        if (
            await context
                .AnalyticalTestRequests.IgnoreQueryFilters()
                .AnyAsync(a =>
                    a.BatchManufacturingRecordId == request.BatchManufacturingRecordId
                    && a.ProductionScheduleProductId == request.ProductionScheduleProductId
                    && a.Stage == request.Stage
                )
        )
            return Error.Validation("Atr", $"This atr at stage {request.Stage} already exists");

        var test = mapper.Map<AnalyticalTestRequest>(request);
        await context.AddAsync(test);
        await context.SaveChangesAsync();
        return test.Id;
    }

    public async Task<
        Result<Paginateable<IEnumerable<AnalyticalTestRequestDto>>>
    > GetAnalyticalTestRequests(
        int page,
        int pageSize,
        string searchQuery,
        AnalyticalTestStatus? status
    )
    {
        var query = context
            .AnalyticalTestRequests.AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(p => p.ProductionScheduleProduct)
                .ThenInclude(s => s.Product)
            .Include(p => p.ProductionScheduleProduct)
                .ThenInclude(s => s.ProductionSchedule)
            .Include(s => s.ProductionActivityStep)
            .Include(s => s.BatchManufacturingRecord)
            .Include(s => s.Assignees)
                .ThenInclude(a => a.User)
            .Include(s => s.CreatedBy)
            .Include(s => s.IssuedBy)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(
                searchQuery,
                q => q.ArNumber,
                q => q.Filled,
                q => q.SampledQuantity
            );
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        var result = await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            entity =>
            {
                var dto = mapper.Map<AnalyticalTestRequestDto>(entity);
                dto.Assignees = entity
                    .Assignees.Select(a => new UserDto
                    {
                        Id = a.UserId,
                        FirstName = a.User.FirstName,
                        LastName = a.User.LastName,
                    })
                    .ToList();
                return dto;
            }
        );

        return Result.Success(result);
    }

    public async Task<Result<AnalyticalTestRequestDto>> GetAnalyticalTestRequest(Guid id)
    {
        var test = await context
            .AnalyticalTestRequests.AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(p => p.ProductionScheduleProduct)
                .ThenInclude(s => s.Product)
            .Include(p => p.ProductionScheduleProduct)
                .ThenInclude(s => s.ProductionSchedule)
            .Include(s => s.ProductionActivityStep)
            .Include(s => s.BatchManufacturingRecord)
            .Include(s => s.CreatedBy)
            .Include(s => s.SampledBy)
            .Include(s => s.ReleasedBy)
            .Include(s => s.AcknowledgedBy)
            .Include(s => s.TestedBy)
            .Include(s => s.IssuedBy)
            .FirstOrDefaultAsync(atr => atr.Id == id);
        return test is null
            ? Error.NotFound("ATR.NotFound", "Analytical test request not found")
            : mapper.Map<AnalyticalTestRequestDto>(test);
    }

    public async Task<Result> UpdateAnalyticalTestRequest(
        Guid id,
        CreateAnalyticalTestRequest request
    )
    {
        var test = await context.AnalyticalTestRequests.FirstOrDefaultAsync(atr => atr.Id == id);

        if (test is null)
        {
            return Error.NotFound("ATR.NotFound", "Analytical test request not found");
        }

        mapper.Map(request, test);
        context.AnalyticalTestRequests.Update(test);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> UpdateAnalyticalTestRequest(
        Guid id,
        UpdateAnalyticalTestRequest request,
        Guid userId
    )
    {
        var test = await context.AnalyticalTestRequests.FirstOrDefaultAsync(atr => atr.Id == id);

        if (test is null)
        {
            return Error.NotFound("ATR.NotFound", "Analytical test request not found");
        }

        if (request.Status == AnalyticalTestStatus.Acknowledged)
        {
            test.AcknowledgedAt = DateTime.UtcNow;
            test.Status = request.Status;
            test.AcknowledgedById = userId;
            test.ArNumber = request.ArNumber;
        }
        else if (request.Status == AnalyticalTestStatus.Sampled)
        {
            test.SampledAt = DateTime.UtcNow;
            test.NumberOfContainers = request.NumberOfContainers;
            test.Status = request.Status;
            test.SampledById = userId;
            test.SampledQuantity = request.SampledQuantity;
        }
        else if (request.Status == AnalyticalTestStatus.Testing)
        {
            test.Status = request.Status;
            test.TestedById = userId;
            test.TestedAt = DateTime.UtcNow;
        }
        else if (request.Status == AnalyticalTestStatus.Released)
        {
            test.ReleasedAt = DateTime.UtcNow;
            test.ReleasedById = userId;
            var activityStep = await context.ProductionActivitySteps.FirstOrDefaultAsync(p =>
                p.Id == test.ProductionActivityStepId
            );
            if (activityStep is not null)
            {
                activityStep.Status = ProductionStatus.Completed;
                context.ProductionActivitySteps.Update(activityStep);
            }
        }

        context.AnalyticalTestRequests.Update(test);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> AssignAnalyticalTestRequest(
        Guid id,
        AssignAnalyticalTestRequest request
    )
    {
        var test = await context
            .AnalyticalTestRequests.AsSplitQuery()
            .Include(a => a.Assignees)
            .FirstOrDefaultAsync(atr => atr.Id == id);

        if (test is null)
        {
            return Error.NotFound("ATR.NotFound", "Analytical test request not found");
        }

        test.Assignees.Clear();
        test.Assignees.AddRange(
            request.UserIds.Select(userId => new AnalyticalTestRequestAssignee
            {
                AnalyticalTestRequestId = id,
                UserId = userId,
            })
        );

        test.Status = AnalyticalTestStatus.Assigned;
        test.AssignedAt = DateTime.UtcNow;

        context.AnalyticalTestRequests.Update(test);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ReassignAnalyticalTestRequest(
        Guid id,
        AssignAnalyticalTestRequest request
    )
    {
        return await AssignAnalyticalTestRequest(id, request);
    }

    public async Task<Result> RequestRetest(Guid id, Guid userId)
    {
        var test = await context.AnalyticalTestRequests.FirstOrDefaultAsync(atr => atr.Id == id);
        if (test is null)
        {
            return Error.NotFound("ATR.NotFound", "Analytical test request not found");
        }

        test.Status = AnalyticalTestStatus.Testing;
        test.LastUpdatedById = userId;
        test.UpdatedAt = DateTime.UtcNow;

        context.AnalyticalTestRequests.Update(test);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> CompleteRetest(Guid id, DateTime extendedExpiryDate, Guid userId)
    {
        var test = await context.AnalyticalTestRequests.FirstOrDefaultAsync(atr => atr.Id == id);
        if (test is null)
        {
            return Error.NotFound("ATR.NotFound", "Analytical test request not found");
        }

        test.ExpiryDate = extendedExpiryDate;
        test.Status = AnalyticalTestStatus.Released;
        test.LastUpdatedById = userId;
        test.UpdatedAt = DateTime.UtcNow;

        context.AnalyticalTestRequests.Update(test);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteAnalyticalTestRequest(Guid id, Guid userId)
    {
        var test = await context.AnalyticalTestRequests.FirstOrDefaultAsync(atr => atr.Id == id);

        if (test is null)
        {
            return Error.NotFound("ATR.NotFound", "Analytical test request not found");
        }

        test.LastDeletedById = userId;
        test.DeletedAt = DateTime.UtcNow;

        context.AnalyticalTestRequests.Update(test);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<AnalyticalTestRequestDto>> GetAnalyticalTestRequestByActivityStep(
        Guid activityStepId
    )
    {
        var analyticalTest = await context.AnalyticalTestRequests.FirstOrDefaultAsync(atr =>
            atr.ProductionActivityStepId == activityStepId
        );
        if (analyticalTest is null)
            return Error.NotFound("ATR.NotFound", "Analytical test request not found");
        return mapper.Map<AnalyticalTestRequestDto>(analyticalTest);
    }

    // Create QC Equipment
    public async Task<Result<Guid>> CreateQcEquipment(CreateQcEquipment request, Guid userId)
    {
        var equipment = mapper.Map<QcEquipment>(request);
        equipment.CreatedById = userId;

        await context.QcEquipments.AddAsync(equipment);
        await context.SaveChangesAsync();

        return equipment.Id;
    }

    // Get QC Equipment by ID
    public async Task<Result<QcEquipmentDto>> GetQcEquipment(Guid equipmentId)
    {
        var equipment = await context
            .QcEquipments.AsSplitQuery()
            .Include(e => e.QcEquipmentCategory)
            .FirstOrDefaultAsync(e => e.Id == equipmentId);

        return equipment is null
            ? Error.NotFound("QcEquipment.NotFound", "QC Equipment with this Id not found")
            : mapper.Map<QcEquipmentDto>(equipment);
    }

    // Get paginated QC Equipments
    public async Task<Result<Paginateable<IEnumerable<QcEquipmentDto>>>> GetQcEquipments(
        int page,
        int pageSize,
        string searchQuery
    )
    {
        var query = context
            .QcEquipments.AsSplitQuery()
            .Include(e => e.QcEquipmentCategory)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(
                searchQuery,
                e => e.Name,
                e => e.SerialNumber,
                e => e.Make,
                e => e.Model
            );
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<QcEquipmentDto>
        );
    }

    // Get all QC Equipments
    public async Task<Result<List<QcEquipmentDto>>> GetQcEquipments()
    {
        return mapper.Map<List<QcEquipmentDto>>(
            await context
                .QcEquipments.AsSplitQuery()
                .Include(e => e.QcEquipmentCategory)
                .ToListAsync()
        );
    }

    // Update QC Equipment
    public async Task<Result> UpdateQcEquipment(
        CreateQcEquipment request,
        Guid equipmentId,
        Guid userId
    )
    {
        var existingEquipment = await context.QcEquipments.FirstOrDefaultAsync(e =>
            e.Id == equipmentId
        );
        if (existingEquipment is null)
        {
            return Error.NotFound("QcEquipment.NotFound", "QC Equipment with this Id not found");
        }

        mapper.Map(request, existingEquipment);
        existingEquipment.LastUpdatedById = userId;

        context.QcEquipments.Update(existingEquipment);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // Delete QC Equipment (soft delete)
    public async Task<Result> DeleteQcEquipment(Guid equipmentId, Guid userId)
    {
        var equipment = await context.QcEquipments.FirstOrDefaultAsync(e => e.Id == equipmentId);
        if (equipment is null)
        {
            return Error.NotFound("QcEquipment.NotFound", "QC Equipment with this Id not found");
        }

        equipment.DeletedAt = DateTime.UtcNow;
        equipment.LastDeletedById = userId;

        context.QcEquipments.Update(equipment);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
