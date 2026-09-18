using APP.IRepository;
using AutoMapper;
using DOMAIN.Entities.QcWorksheets;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository.QcWorksheets;

/// <summary>
/// Water validity windows and the uses booked against them.
/// <para>
/// Three rules govern everything here.
/// </para>
/// <para>
/// <b>A window is never authored.</b> There is no create endpoint. A window appears at
/// <see cref="WaterQualityPeriodStatus.PendingActivation"/> when a Water certificate is issued,
/// and the only things a person does to one afterwards are activate it, hold it, and book uses
/// against it.
/// </para>
/// <para>
/// <b>Activation is a decision, not a formality.</b> It takes a stated reason, and it resolves
/// <see cref="WaterQualityPeriod.ValidUntil"/> from the point's live schedule at that moment
/// rather than from a fixed duration. Nothing activates itself.
/// </para>
/// <para>
/// <b>A hold cascades.</b> Withdrawing a window flags every use underneath it for Quality Impact
/// Assessment in the same transaction — nothing depends on anyone remembering to go and look.
/// </para>
/// </summary>
public class WaterQualityRepository(ApplicationDbContext context, IMapper mapper)
    : IWaterQualityRepository
{
    // -----------------------------------------------------------------------
    // Reads
    // -----------------------------------------------------------------------

    public async Task<Result<List<WaterQualityPeriodSummaryDto>>> GetPeriods(
        WaterQualityPeriodStatus? status, Guid? samplingPointId)
    {
        var query = context.QcWaterQualityPeriods
            .AsNoTracking()
            .Include(item => item.CreatedBy)
            .Include(item => item.ActivatedBy)
            .Include(item => item.HeldBy)
            .Include(item => item.SamplingPoint)
            .Include(item => item.TestRequestSubject)
                .ThenInclude(subject => subject.TestRequest)
            .Include(item => item.UseRecords)
            .AsQueryable();

        if (status.HasValue) query = query.Where(item => item.Status == status.Value);
        if (samplingPointId.HasValue) query = query.Where(item => item.SamplingPointId == samplingPointId.Value);

        var periods = await query
            .OrderByDescending(item => item.ValidFrom)
            .AsSplitQuery()
            .ToListAsync();

        return Result.Success(periods.Select(period => ToSummaryDto(period)).ToList());
    }

    public async Task<Result<WaterQualityPeriodDetailDto>> GetPeriod(Guid id)
    {
        var period = await LoadDetail(id);

        return period is null
            ? Result.Failure<WaterQualityPeriodDetailDto>(QcWorksheetErrors.WaterQualityPeriodNotFound(id))
            : Result.Success(ToDetailDto(period));
    }

    // -----------------------------------------------------------------------
    // Activate
    // -----------------------------------------------------------------------

    /// <summary>
    /// PendingActivation to Active.
    /// <para>
    /// Deliberately not automatic on certificate issuance, and deliberately reasoned. The window
    /// being activated already started days ago — at the sample's collection time, because that is
    /// what <see cref="WaterQualityPeriod.ValidFrom"/> always is — so activating it tells
    /// production it may rely on water it has already been consuming throughout incubation.
    /// Somebody confirms that is justified and says why.
    /// </para>
    /// <para>
    /// <see cref="WaterQualityPeriod.ValidUntil"/> is resolved here, from the point's next
    /// scheduled test <i>as it stands at this moment</i> — not a fixed offset from ValidFrom. That
    /// is the locked dynamic-validity rule: if the next round slips, this window runs out and is
    /// flagged Expired rather than quietly continuing to cover production.
    /// </para>
    /// </summary>
    public async Task<Result<WaterQualityPeriodDetailDto>> Activate(
        Guid id, ActivateWaterQualityPeriodRequest request, Guid userId)
    {
        var period = await context.QcWaterQualityPeriods
            .Include(item => item.SamplingPoint)
            .SingleOrDefaultAsync(item => item.Id == id);

        if (period is null)
            return Result.Failure<WaterQualityPeriodDetailDto>(
                QcWorksheetErrors.WaterQualityPeriodNotFound(id));

        if (period.Status != WaterQualityPeriodStatus.PendingActivation)
            return Result.Failure<WaterQualityPeriodDetailDto>(
                QcWorksheetErrors.ActivateRequiresPendingActivation(period.Status));

        // Enforced here rather than by a NOT NULL column, exactly as TestRequest's unscheduled
        // reason is: the column is legitimately empty for the whole PendingActivation phase.
        if (string.IsNullOrWhiteSpace(request?.RetrospectiveReason))
            return Result.Failure<WaterQualityPeriodDetailDto>(
                QcWorksheetErrors.RetrospectiveReasonRequired);

        var validUntil = await ResolveValidUntil(period.SamplingPointId);

        if (!validUntil.HasValue)
            return Result.Failure<WaterQualityPeriodDetailDto>(
                QcWorksheetErrors.NoMonitoringProgramForValidUntil(period.SamplingPoint?.Code));

        period.Status = WaterQualityPeriodStatus.Active;
        period.ValidUntil = validUntil.Value;
        period.RetrospectiveReason = request.RetrospectiveReason.Trim();
        period.ActivatedById = userId;
        period.ActivatedAt = DateTime.UtcNow;
        period.UpdatedAt = DateTime.UtcNow;
        period.LastUpdatedById = userId;

        await context.SaveChangesAsync();

        return await GetPeriod(id);
    }

    // -----------------------------------------------------------------------
    // Hold
    // -----------------------------------------------------------------------

    /// <summary>
    /// Active to Held, cascading to every use booked underneath.
    /// <para>
    /// The cascade is the point of the action. A held window means water that production relied on
    /// may not have been fit, and the uses are what say which batches and steps that touched. The
    /// flag is all this milestone models — the Quality Impact Assessment investigation itself is a
    /// QA process, not a QC data question, and is deliberately modelled nowhere in this design.
    /// </para>
    /// </summary>
    public async Task<Result<WaterQualityPeriodDetailDto>> Hold(
        Guid id, HoldWaterQualityPeriodRequest request, Guid userId)
    {
        var period = await context.QcWaterQualityPeriods
            .Include(item => item.UseRecords)
            .SingleOrDefaultAsync(item => item.Id == id);

        if (period is null)
            return Result.Failure<WaterQualityPeriodDetailDto>(
                QcWorksheetErrors.WaterQualityPeriodNotFound(id));

        if (period.Status != WaterQualityPeriodStatus.Active)
            return Result.Failure<WaterQualityPeriodDetailDto>(
                QcWorksheetErrors.HoldRequiresActive(period.Status));

        if (string.IsNullOrWhiteSpace(request?.HoldReason))
            return Result.Failure<WaterQualityPeriodDetailDto>(QcWorksheetErrors.HoldReasonRequired);

        var now = DateTime.UtcNow;

        period.Status = WaterQualityPeriodStatus.Held;
        period.HoldReason = request.HoldReason.Trim();
        period.HeldById = userId;
        period.HeldAt = now;
        period.UpdatedAt = now;
        period.LastUpdatedById = userId;

        foreach (var record in period.UseRecords.Where(
                     item => item.Status == WaterUseRecordStatus.Recorded))
        {
            record.Status = WaterUseRecordStatus.Held;
            record.UpdatedAt = now;
            record.LastUpdatedById = userId;
        }

        await context.SaveChangesAsync();

        return await GetPeriod(id);
    }

    // -----------------------------------------------------------------------
    // Record use
    // -----------------------------------------------------------------------

    /// <summary>
    /// Books one consumption against a window.
    /// <para>
    /// Manual entry. Automatic capture from production consumption stays deferred
    /// (deferred-and-next-steps.md), so nothing in the production path calls this.
    /// </para>
    /// </summary>
    public async Task<Result<WaterUseRecordDto>> RecordUse(RecordWaterUseRequest request, Guid userId)
    {
        var period = await context.QcWaterQualityPeriods
            .SingleOrDefaultAsync(item => item.Id == request.WaterQualityPeriodId);

        if (period is null)
            return Result.Failure<WaterUseRecordDto>(
                QcWorksheetErrors.WaterQualityPeriodNotFound(request.WaterQualityPeriodId));

        // Active only. A use against PendingActivation scaffolding, or against a window already
        // withdrawn or run out, asserts coverage that was never granted.
        if (period.Status != WaterQualityPeriodStatus.Active)
            return Result.Failure<WaterUseRecordDto>(
                QcWorksheetErrors.WaterUseRequiresActivePeriod(period.Status));

        if (request.BatchManufacturingRecordId.HasValue
            && !await context.BatchManufacturingRecords.AnyAsync(
                item => item.Id == request.BatchManufacturingRecordId.Value))
            return Result.Failure<WaterUseRecordDto>(
                QcWorksheetErrors.BatchManufacturingRecordNotFound(request.BatchManufacturingRecordId.Value));

        if (request.ProductionActivityStepId.HasValue
            && !await context.ProductionActivitySteps.AnyAsync(
                item => item.Id == request.ProductionActivityStepId.Value))
            return Result.Failure<WaterUseRecordDto>(Error.Validation(
                "QcWaterUseRecord.ProductionActivityStepNotFound",
                $"The production activity step with the Id: {request.ProductionActivityStepId.Value} "
                + "was not found"));

        var record = new WaterUseRecord
        {
            Id = Guid.NewGuid(),
            WaterQualityPeriodId = period.Id,
            UsedAt = request.UsedAt,
            BatchManufacturingRecordId = request.BatchManufacturingRecordId,
            ProductionActivityStepId = request.ProductionActivityStepId,
            RecordedById = userId,
            Status = WaterUseRecordStatus.Recorded,
            CreatedAt = DateTime.UtcNow,
            CreatedById = userId
        };

        context.QcWaterUseRecords.Add(record);
        await context.SaveChangesAsync();

        var saved = await context.QcWaterUseRecords
            .AsNoTracking()
            .Include(item => item.RecordedBy)
            .Include(item => item.BatchManufacturingRecord)
            .SingleAsync(item => item.Id == record.Id);

        return Result.Success(ToUseRecordDto(saved));
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// The point's next scheduled test, taken from its Active monitoring programs as they stand
    /// right now.
    /// <para>
    /// The earliest wins when a point carries more than one program — a point scheduled weekly for
    /// Microbial and monthly for Chemical is next tested in a week, and claiming a month of
    /// coverage would outrun the next result that could contradict it.
    /// </para>
    /// </summary>
    private async Task<DateTime?> ResolveValidUntil(Guid samplingPointId)
    {
        var dueDates = await context.QcMonitoringPrograms
            .Where(item => item.SamplingPointId == samplingPointId)
            .Where(item => item.Status == MonitoringProgramStatus.Active)
            .Select(item => item.NextDueDate)
            .ToListAsync();

        return dueDates.Count == 0 ? null : dueDates.Min();
    }

    private async Task<WaterQualityPeriod> LoadDetail(Guid id) =>
        await context.QcWaterQualityPeriods
            .AsNoTracking()
            .Include(item => item.CreatedBy)
            .Include(item => item.ActivatedBy)
            .Include(item => item.HeldBy)
            .Include(item => item.SamplingPoint)
            .Include(item => item.TestRequestSubject)
                .ThenInclude(subject => subject.TestRequest)
            .Include(item => item.UseRecords)
                .ThenInclude(record => record.RecordedBy)
            .Include(item => item.UseRecords)
                .ThenInclude(record => record.BatchManufacturingRecord)
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.Id == id);

    private void FillSummary(WaterQualityPeriodSummaryDto target, WaterQualityPeriod period)
    {
        target.Id = period.Id;
        target.SamplingPointId = period.SamplingPointId;
        target.SamplingPointCode = period.SamplingPoint?.Code;
        target.SamplingPointName = period.SamplingPoint?.Name;
        target.TestRequestSubjectId = period.TestRequestSubjectId;
        target.TestRequestId = period.TestRequestSubject?.TestRequestId ?? Guid.Empty;
        target.TestRequestArNumber = period.TestRequestSubject?.TestRequest?.ArNumber;
        target.ValidFrom = period.ValidFrom;
        target.ValidUntil = period.ValidUntil;
        target.Status = period.Status;
        target.RetrospectiveReason = period.RetrospectiveReason;
        target.ActivatedBy = mapper.Map<UserDto>(period.ActivatedBy);
        target.ActivatedAt = period.ActivatedAt;
        target.HoldReason = period.HoldReason;
        target.HeldBy = mapper.Map<UserDto>(period.HeldBy);
        target.HeldAt = period.HeldAt;
        target.UseRecordCount = period.UseRecords.Count;
        target.HeldUseRecordCount = period.UseRecords.Count(
            item => item.Status == WaterUseRecordStatus.Held);
        target.CreatedAt = period.CreatedAt;
        target.CreatedBy = mapper.Map<UserDto>(period.CreatedBy);
    }

    private WaterQualityPeriodSummaryDto ToSummaryDto(WaterQualityPeriod period)
    {
        var dto = new WaterQualityPeriodSummaryDto();
        FillSummary(dto, period);
        return dto;
    }

    private WaterQualityPeriodDetailDto ToDetailDto(WaterQualityPeriod period)
    {
        var dto = new WaterQualityPeriodDetailDto
        {
            UseRecords = period.UseRecords
                .OrderByDescending(record => record.UsedAt)
                .Select(ToUseRecordDto)
                .ToList()
        };

        FillSummary(dto, period);
        return dto;
    }

    private WaterUseRecordDto ToUseRecordDto(WaterUseRecord record) => new()
    {
        Id = record.Id,
        WaterQualityPeriodId = record.WaterQualityPeriodId,
        UsedAt = record.UsedAt,
        BatchManufacturingRecordId = record.BatchManufacturingRecordId,
        BatchNumber = record.BatchManufacturingRecord?.BatchNumber,
        ProductionActivityStepId = record.ProductionActivityStepId,
        RecordedBy = mapper.Map<UserDto>(record.RecordedBy),
        Status = record.Status,
        CreatedAt = record.CreatedAt,
        CreatedBy = mapper.Map<UserDto>(record.CreatedBy)
    };
}
