using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.QualityRoutines;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class WaterQualityRepository(ApplicationDbContext context)
    : IWaterQualityRepository
{
    public async Task<Result<Guid>> ActivatePeriod(
        ActivateWaterQualityPeriodRequest request, Guid actorId)
    {
        if (request.ValidFrom == default || request.ValidUntil == default ||
            request.ValidFrom >= request.ValidUntil)
            return Error.Validation("WaterQualityPeriod.Dates",
                "A water quality period requires a valid start and end.");
        var certificate = await context.RoutineCertificates
            .Include(item => item.RoutineSample)
            .Include(item => item.RoutineExecution)
            .FirstOrDefaultAsync(item => item.Id == request.RoutineCertificateId);
        if (certificate?.RoutineSample is null ||
            certificate.RoutineExecution.Type != RoutineType.Water)
            return Error.Validation("WaterQualityPeriod.Certificate",
                "Select an approved water-sample certificate.");
        if (certificate.RoutineExecution.Status != RoutineStatus.Approved)
            return Error.Conflict("WaterQualityPeriod.Approval",
                "The water routine requires final approval before coverage begins.");
        if (await context.WaterQualityPeriods.AnyAsync(item =>
            item.RoutineCertificateId == certificate.Id))
            return Error.Conflict("WaterQualityPeriod.Exists",
                "This water certificate already has a quality period.");
        if (request.ValidFrom < certificate.IssuedAt &&
            string.IsNullOrWhiteSpace(request.RetrospectiveReason))
            return Error.Validation("WaterQualityPeriod.RetrospectiveReason",
                "Retrospective coverage requires a QA reason.");
        var period = new WaterQualityPeriod
        {
            Id = Guid.NewGuid(), RoutineCertificateId = certificate.Id,
            RoutineSampleId = certificate.RoutineSample.Id,
            SamplingPoint = certificate.RoutineSample.SamplingPoint,
            ValidFrom = request.ValidFrom, ValidUntil = request.ValidUntil,
            RetrospectiveReason = request.RetrospectiveReason?.Trim(),
            Status = WaterQualityPeriodStatus.Active,
            ApprovedById = actorId, ApprovedAt = DateTime.UtcNow,
            CreatedById = actorId
        };
        context.WaterQualityPeriods.Add(period);
        context.RoutineAuditEvents.Add(new RoutineAuditEvent
        {
            Id = Guid.NewGuid(), RoutineExecutionId = certificate.RoutineExecutionId,
            ActorId = actorId, OccurredAt = DateTime.UtcNow,
            Action = "WaterQualityPeriodActivated",
            Detail = $"{period.SamplingPoint}: {period.ValidFrom:O} to {period.ValidUntil:O}",
            CreatedById = actorId
        });
        await context.SaveChangesAsync();
        return period.Id;
    }

    public async Task<Result<Guid>> RecordUse(
        RecordWaterUseRequest request, Guid actorId)
    {
        if (request.UsedAt == default)
            return Error.Validation("WaterUse.Date", "Actual water-use time is required.");
        if (request.BatchManufacturingRecordId.HasValue ==
            request.RndTrialBatchId.HasValue)
            return Error.Validation("WaterUse.Subject",
                "Link water use to exactly one production or R&D batch.");
        if (!request.BatchManufacturingRecordId.HasValue &&
            request.ProductionActivityStepId.HasValue)
            return Error.Validation("WaterUse.Step",
                "A production activity step requires a production batch.");
        var period = await context.WaterQualityPeriods
            .FirstOrDefaultAsync(item => item.Id == request.WaterQualityPeriodId);
        if (period is null)
            return Error.NotFound("WaterUse.Period", "Water quality period was not found.");
        if (period.Status != WaterQualityPeriodStatus.Active ||
            request.UsedAt < period.ValidFrom || request.UsedAt >= period.ValidUntil)
            return Error.Conflict("WaterUse.Coverage",
                "The selected point has no active approved coverage at the use time.");
        if (request.BatchManufacturingRecordId.HasValue)
        {
            var batch = await context.BatchManufacturingRecords
                .Where(item => item.Id == request.BatchManufacturingRecordId)
                .Select(item => new { item.ProductionScheduleProductId })
                .FirstOrDefaultAsync();
            if (batch is null)
                return Error.NotFound("WaterUse.Batch", "Production batch was not found.");
            if (request.ProductionActivityStepId.HasValue &&
                !await context.ProductionActivitySteps.AnyAsync(item =>
                    item.Id == request.ProductionActivityStepId &&
                    item.ProductionActivity.ProductionScheduleProductId ==
                        batch.ProductionScheduleProductId))
                return Error.Validation("WaterUse.Step",
                    "Production activity step does not belong to the batch product run.");
        }
        else if (!await context.RndTrialBatches.AnyAsync(item =>
            item.Id == request.RndTrialBatchId))
            return Error.NotFound("WaterUse.RndBatch", "R&D batch was not found.");
        var record = new WaterUseRecord
        {
            Id = Guid.NewGuid(), WaterQualityPeriodId = period.Id,
            SamplingPoint = period.SamplingPoint, UsedAt = request.UsedAt,
            BatchManufacturingRecordId = request.BatchManufacturingRecordId,
            ProductionActivityStepId = request.ProductionActivityStepId,
            RndTrialBatchId = request.RndTrialBatchId,
            Status = WaterUseStatus.Recorded, CreatedById = actorId
        };
        context.WaterUseRecords.Add(record);
        await context.SaveChangesAsync();
        return record.Id;
    }

    public async Task<Result<int>> HoldPeriod(Guid id,
        HoldWaterQualityPeriodRequest request, Guid actorId)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return Error.Validation("WaterQualityPeriod.HoldReason",
                "A QA hold reason is required.");
        var period = await context.WaterQualityPeriods
            .Include(item => item.RoutineCertificate)
            .Include(item => item.UseRecords)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (period is null)
            return Error.NotFound("WaterQualityPeriod", "Water quality period was not found.");
        if (period.Status == WaterQualityPeriodStatus.Held)
            return Error.Conflict("WaterQualityPeriod.Held", "This period is already held.");
        period.Status = WaterQualityPeriodStatus.Held;
        period.HoldReason = request.Reason.Trim();
        period.HeldById = actorId;
        period.HeldAt = DateTime.UtcNow;
        foreach (var use in period.UseRecords) use.Status = WaterUseStatus.Held;
        context.RoutineAuditEvents.Add(new RoutineAuditEvent
        {
            Id = Guid.NewGuid(),
            RoutineExecutionId = period.RoutineCertificate.RoutineExecutionId,
            ActorId = actorId, OccurredAt = DateTime.UtcNow,
            Action = "WaterQualityPeriodHeld",
            Detail = $"{period.HoldReason}; affected uses: {period.UseRecords.Count}",
            CreatedById = actorId
        });
        await context.SaveChangesAsync();
        return period.UseRecords.Count;
    }

    public async Task<Result<List<WaterQualityPeriodDto>>> ListPeriods()
    {
        var periods = await context.WaterQualityPeriods
            .Include(item => item.UseRecords)
            .OrderByDescending(item => item.ValidFrom).ToListAsync();
        return periods.Select(item => new WaterQualityPeriodDto
        {
            Id = item.Id, RoutineCertificateId = item.RoutineCertificateId,
            RoutineSampleId = item.RoutineSampleId,
            SamplingPoint = item.SamplingPoint,
            ValidFrom = item.ValidFrom, ValidUntil = item.ValidUntil,
            Status = item.Status, RetrospectiveReason = item.RetrospectiveReason,
            ApprovedAt = item.ApprovedAt, HoldReason = item.HoldReason,
            HeldAt = item.HeldAt, UseRecordCount = item.UseRecords.Count
        }).ToList();
    }

    public async Task<Result<Paginateable<IEnumerable<EligibleWaterCertificateDto>>>>
        ListEligibleCertificates(int page, int pageSize, string searchQuery)
    {
        var query = context.RoutineCertificates
            .Include(item => item.RoutineSample)
            .Include(item => item.RoutineExecution)
            .Where(item => item.RoutineSample != null &&
                item.RoutineExecution.Type == RoutineType.Water &&
                item.RoutineExecution.Status == RoutineStatus.Approved &&
                !context.WaterQualityPeriods.Any(period =>
                    period.RoutineCertificateId == item.Id))
            .OrderByDescending(item => item.IssuedAt)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
            query = query.WhereSearch(searchQuery, item => item.CertificateCode);

        var paginatedResult = await PaginationHelper.GetPaginatedResultAsync(
            query, page, pageSize);
        var certificates = await paginatedResult.Data.ToListAsync();

        return new Paginateable<IEnumerable<EligibleWaterCertificateDto>>
        {
            Data = certificates.Select(item => new EligibleWaterCertificateDto
            {
                Id = item.Id, CertificateCode = item.CertificateCode,
                SamplingPoint = item.RoutineSample.SamplingPoint,
                IssuedAt = item.IssuedAt
            }),
            PageIndex = page, PageCount = paginatedResult.PageCount,
            TotalRecordCount = paginatedResult.TotalRecordCount,
            StartPageIndex = paginatedResult.StartPageIndex,
            StopPageIndex = paginatedResult.StopPageIndex
        };
    }
}
