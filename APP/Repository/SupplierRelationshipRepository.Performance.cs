using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class SupplierRelationshipRepository
{
    public async Task<Result<List<SupplierComplianceDueDto>>> GetRequalificationDue(
        int withinDays, DateTime? asOf = null)
    {
        if (withinDays < 0)
            return Error.Validation("Supplier.RequalificationDays", "Within-days must be zero or greater.");
        var date = (asOf ?? DateTime.UtcNow).Date;
        var cutoff = date.AddDays(withinDays);
        return await context.Suppliers.AsNoTracking()
            .Where(item => item.Status == SupplierStatus.Approved
                && item.RequalificationDueDate.HasValue
                && item.RequalificationDueDate.Value.Date <= cutoff)
            .OrderBy(item => item.RequalificationDueDate)
            .Select(item => new SupplierComplianceDueDto
            {
                SupplierId = item.Id, SupplierName = item.Name,
                DueDate = item.RequalificationDueDate!.Value,
                DaysUntilDue = (item.RequalificationDueDate.Value.Date - date).Days,
            }).ToListAsync();
    }

    public async Task<Result<SupplierPerformanceDto>> ComputePerformance(
        Guid supplierId, ComputeSupplierPerformanceRequest request)
    {
        var supplier = await RequireSupplier(supplierId);
        if (!supplier.IsSuccess) return supplier.Error;
        var period = ValidatePerformanceRequest(request);
        if (!period.IsSuccess) return period.Error;
        var start = request.PeriodStart.Date;
        var endExclusive = request.PeriodEnd.Date.AddDays(1);

        var deliveries = await context.ShipmentDocuments.AsNoTracking()
            .Where(document => document.ArrivedAt >= start && document.ArrivedAt < endExclusive)
            .SelectMany(document => document.ShipmentInvoice.Items
                .Where(item => item.PurchaseOrder.SupplierId == supplierId)
                .Select(item => new
                {
                    ShipmentId = document.Id,
                    item.PurchaseOrderId,
                    document.ArrivedAt,
                    item.PurchaseOrder.ExpectedDeliveryDate,
                }))
            .Distinct().ToListAsync();
        var evaluable = deliveries.Where(item => item.ExpectedDeliveryDate.HasValue).ToList();
        var onTime = evaluable.Count(item => item.ArrivedAt!.Value.Date
            <= item.ExpectedDeliveryDate!.Value.Date);

        var batches = await context.MaterialBatches.AsNoTracking()
            .Where(batch => batch.Checklist.SupplierId == supplierId
                && batch.DateReceived >= start && batch.DateReceived < endExclusive)
            .Select(batch => new { batch.Status, batch.DateRejected }).ToListAsync();
        var rejected = batches.Count(item => item.Status == BatchStatus.Rejected
            || item.DateRejected.HasValue);

        var onTimeRate = Rate(onTime, evaluable.Count);
        var rejectRate = Rate(rejected, batches.Count);
        var warnings = new List<string>();
        if (deliveries.Count != evaluable.Count)
            warnings.Add($"{deliveries.Count - evaluable.Count} delivery record(s) lacked an expected delivery date and were excluded.");
        if (evaluable.Count == 0)
            warnings.Add("No deliveries with expected dates were available for this period.");
        if (batches.Count == 0)
            warnings.Add("No supplier-linked material batches were available for this period.");

        return new SupplierPerformanceDto
        {
            SupplierId = supplierId, PeriodStart = start, PeriodEnd = request.PeriodEnd.Date,
            EvaluatedDeliveries = evaluable.Count, OnTimeDeliveries = onTime,
            EvaluatedBatches = batches.Count, RejectedBatches = rejected,
            OnTimeDeliveryRate = onTimeRate, QualityRejectRate = rejectRate,
            Score = decimal.Clamp(onTimeRate * request.OnTimeDeliveryWeight
                + (100m - rejectRate) * request.QualityWeight, 0m, 100m),
            DataQualityWarnings = warnings
        };
    }

    public async Task<Result<Guid>> PersistPerformance(
        Guid supplierId, ComputeSupplierPerformanceRequest request, Guid userId)
    {
        var computed = await ComputePerformance(supplierId, request);
        if (!computed.IsSuccess) return computed.Error;
        if (await context.SupplierPerformanceRecords.AnyAsync(item => item.SupplierId == supplierId
                && item.PeriodStart == computed.Value.PeriodStart
                && item.PeriodEnd == computed.Value.PeriodEnd))
            return Error.Conflict("SupplierPerformance.Duplicate", "This evaluation period is already preserved.");
        var record = new SupplierPerformanceRecord
        {
            SupplierId = supplierId, PeriodStart = computed.Value.PeriodStart,
            PeriodEnd = computed.Value.PeriodEnd,
            OnTimeDeliveryRate = computed.Value.OnTimeDeliveryRate,
            QualityRejectRate = computed.Value.QualityRejectRate,
            Score = computed.Value.Score, CreatedById = userId,
        };
        await context.SupplierPerformanceRecords.AddAsync(record);
        await context.SaveChangesAsync();
        return record.Id;
    }

    public async Task<Result<List<SupplierPerformanceDto>>> GetPerformanceRecords(Guid supplierId)
    {
        var supplier = await RequireSupplier(supplierId);
        if (!supplier.IsSuccess) return supplier.Error;
        return await context.SupplierPerformanceRecords.AsNoTracking()
            .Where(item => item.SupplierId == supplierId)
            .OrderByDescending(item => item.PeriodEnd)
            .Select(item => new SupplierPerformanceDto
            {
                Id = item.Id, CreatedAt = item.CreatedAt, SupplierId = item.SupplierId,
                PeriodStart = item.PeriodStart, PeriodEnd = item.PeriodEnd,
                OnTimeDeliveryRate = item.OnTimeDeliveryRate,
                QualityRejectRate = item.QualityRejectRate, Score = item.Score,
            }).ToListAsync();
    }

    private static Result ValidatePerformanceRequest(ComputeSupplierPerformanceRequest request)
    {
        var period = ValidatePeriod(request.PeriodStart, request.PeriodEnd);
        if (!period.IsSuccess) return period;
        return request.OnTimeDeliveryWeight + request.QualityWeight != 1m
            ? Error.Validation("SupplierPerformance.Weights", "Performance weights must total 1.")
            : Result.Success();
    }

    private static decimal Rate(int numerator, int denominator)
        => denominator == 0 ? 0m : decimal.Round(numerator * 100m / denominator, 2);
}
