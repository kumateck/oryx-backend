using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Reports;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Warehouses;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class ReportRepository
{
    /// <summary>The production monthly report, the monthly schedule summary and the batch value report read this data.</summary>
    private static readonly string[] ProductionMonthlyReports =
    [
        PermissionKeys.CanViewProductionMonthlyReport,
        PermissionKeys.CanViewProductionScheduleSummaryReport,
        PermissionKeys.CanViewProductionBatchValueReport,
    ];

    /// <summary>
    /// Resolves the department a report may cover: production users only their own; others any production
    /// department, or all when none is chosen. Null error means allowed.
    /// </summary>
    private async Task<(Error? Error, Guid? DepartmentId, string? DepartmentName, Guid? ActorDepartment, DepartmentType? ActorType)> ReportDepartment(
        Guid userId, Guid? requested, CancellationToken cancellationToken)
    {
        var actor = await context.Users.IgnoreAutoIncludes().AsNoTracking()
            .Where(user => user.Id == userId && !user.IsDisabled)
            .Select(user => new { user.DepartmentId, Type = (DepartmentType?)user.Department.Type })
            .SingleOrDefaultAsync(cancellationToken);
        if (actor?.Type is null)
            return (Error.Forbidden("Report.DepartmentScope", "An assigned department is required."), null, null, null, null);
        var departmentId = requested;
        if (actor.Type == DepartmentType.Production)
        {
            if (departmentId.HasValue && departmentId != actor.DepartmentId)
                return (Error.Forbidden("Report.DepartmentScope", "Production users can only report on their own department."), null, null, null, null);
            departmentId = actor.DepartmentId;
        }
        if (!departmentId.HasValue) return (null, null, null, actor.DepartmentId, actor.Type);
        // Archived departments still have history.
        var department = await context.Departments.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(item => item.Id == departmentId && item.Type == DepartmentType.Production)
            .Select(item => new { item.Id, item.Name }).SingleOrDefaultAsync(cancellationToken);
        return department is null
            ? (Error.NotFound("Report.Department", "The production department was not found."), null, null, null, null)
            : (null, department.Id, department.Name, actor.DepartmentId, actor.Type);
    }

    /// <summary>Refuses a result when the actor's grants or department changed while the report was read.</summary>
    private async Task<Error?> StillAllowed(Guid userId, IReadOnlyCollection<string> keys, Guid? department, DepartmentType? type,
        CancellationToken cancellationToken)
    {
        if (!keys.Any((await CurrentPermissionSet.Load(context, userId)).Contains))
            return Error.Forbidden("Report.Permission", "Access to this report was removed.");
        var current = await context.Users.IgnoreAutoIncludes().AsNoTracking()
            .Where(user => user.Id == userId && !user.IsDisabled)
            .Select(user => new { user.DepartmentId, Type = (DepartmentType?)user.Department.Type })
            .SingleOrDefaultAsync(cancellationToken);
        return current is null || current.DepartmentId != department || current.Type != type
            ? Error.Forbidden("Report.DepartmentScope", "Your department changed. Reload before generating the report.") : null;
    }

    public async Task<Result<ProductionMonthlyReportDto>> GetProductionMonthlyReport(
        ProductionMonthlyFilter filter, Guid userId, CancellationToken cancellationToken = default)
    {
        if (!ProductionMonthlyReports.Any((await CurrentPermissionSet.Load(context, userId)).Contains))
            return Error.Forbidden("Report.Permission", "A production monthly report key is required.");
        if (filter.Year is not (>= 2000 and <= 2200) || filter.Month is not (>= 1 and <= 12) || filter.DepartmentId == Guid.Empty)
            return Error.Validation("Report.Scope", "Choose a month.");
        var scope = await ReportDepartment(userId, filter.DepartmentId, cancellationToken);
        if (scope.Error is not null) return scope.Error;

        var start = new DateTime(filter.Year.Value, filter.Month.Value, 1, 0, 0, 0, DateTimeKind.Utc);
        var endExclusive = start.AddMonths(1);
        var query = context.ProductionScheduleProducts.IgnoreAutoIncludes().AsNoTracking()
            .Where(item => !item.Cancelled && !item.ProductionSchedule.DeletedAt.HasValue
                && item.ProductionSchedule.ScheduledStartTime >= start && item.ProductionSchedule.ScheduledStartTime < endExclusive);
        if (scope.DepartmentId.HasValue) query = query.Where(item => item.Product.DepartmentId == scope.DepartmentId);
        var batches = await query.Select(item => new
        {
            item.Id, ScheduleCode = item.ProductionSchedule.Code, ScheduleStart = item.ProductionSchedule.ScheduledStartTime,
            item.ProductId, ProductCode = item.Product.Code, ProductName = item.Product.Name,
            item.Product.DepartmentId, DepartmentName = item.Product.Department.Name,
            MarketId = item.MarketTypeId, MarketName = item.MarketType.Name, PlannedBatch = item.BatchNumber, item.BatchSize,
            item.ProductPackingId, PackingStyle = item.ProductPacking.Name,
            PacksPerShipper = (int?)item.ProductPacking.PackPerShipper, ExpectedYield = (decimal?)item.ProductPacking.ExpectedYield,
        }).ToListAsync(cancellationToken);
        var ids = batches.Select(item => item.Id).ToList();
        var productIds = batches.Select(item => item.ProductId).Distinct().ToList();

        // The issued BMR's batch number, as printed on the batch record.
        var issued = (await context.BatchManufacturingRecords.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
                .Where(record => ids.Contains(record.ProductionScheduleProductId) && record.IssuedDate.HasValue && !record.DeletedAt.HasValue)
                .Select(record => new { record.ProductionScheduleProductId, record.BatchNumber, record.IssuedDate }).ToListAsync(cancellationToken))
            .GroupBy(record => record.ProductionScheduleProductId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(record => record.IssuedDate).First().BatchNumber);
        var packings = (await context.FinalPackings.IgnoreAutoIncludes().AsNoTracking()
                .Where(packing => ids.Contains(packing.ProductionScheduleProductId))
                .Select(packing => new { packing.ProductionScheduleProductId, packing.CreatedAt, packing.TotalQuantityPacked, packing.RetainedSamples,
                    packing.NumberOfBottlesPerShipper, packing.NUmberOfFullShipperPacked, packing.LeftOver }).ToListAsync(cancellationToken))
            .GroupBy(packing => packing.ProductionScheduleProductId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(packing => packing.CreatedAt).First());
        var transfers = (await context.FinishedGoodsTransferNotes.IgnoreAutoIncludes().AsNoTracking()
                .Where(note => ids.Contains(note.BatchManufacturingRecord.ProductionScheduleProductId))
                .Select(note => new { note.BatchManufacturingRecord.ProductionScheduleProductId, note.TotalQuantity, Unit = note.UoM.Symbol })
                .ToListAsync(cancellationToken))
            .GroupBy(note => note.ProductionScheduleProductId).ToDictionary(group => group.Key, group => group.ToList());
        var prices = (await context.Products.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
                .Where(product => productIds.Contains(product.Id))
                .Select(product => new { product.Id, Prices = product.Prices.Select(price => new { price.Price, price.PriceUoM, price.Date, price.CurrencyId }).ToList() })
                .ToListAsync(cancellationToken))
            .ToDictionary(product => product.Id, product => product.Prices);
        var currencyIds = prices.Values.SelectMany(list => list).Select(price => price.CurrencyId).OfType<Guid>().Distinct().ToList();
        var currencies = await context.Currencies.IgnoreQueryFilters().AsNoTracking().Where(currency => currencyIds.Contains(currency.Id))
            .ToDictionaryAsync(currency => currency.Id, currency => currency.Symbol ?? currency.Name, cancellationToken);

        var rows = batches.Select(batch =>
        {
            var share = batch.BatchSize == BatchSize.Half ? 0.5m : 1m;
            packings.TryGetValue(batch.Id, out var packing);
            var notes = transfers.TryGetValue(batch.Id, out var list) ? list : [];
            var units = notes.Select(note => note.Unit).Distinct().ToList();
            var price = (prices.TryGetValue(batch.ProductId, out var history) ? history : [])
                .Where(entry => entry.Date <= batch.ScheduleStart).OrderByDescending(entry => entry.Date).FirstOrDefault();
            return new ProductionMonthlyRowDto
            {
                ScheduleProductId = batch.Id, ScheduleCode = batch.ScheduleCode, ScheduleStart = batch.ScheduleStart,
                ProductId = batch.ProductId, ProductCode = batch.ProductCode, ProductName = batch.ProductName,
                DepartmentId = batch.DepartmentId, DepartmentName = batch.DepartmentName,
                MarketId = batch.MarketId, MarketName = batch.MarketName,
                BatchNumber = issued.TryGetValue(batch.Id, out var number) && !string.IsNullOrWhiteSpace(number) ? number : batch.PlannedBatch,
                BatchSize = batch.BatchSize, BatchShare = share, ProductPackingId = batch.ProductPackingId, PackingStyle = batch.PackingStyle,
                PacksPerShipper = batch.PacksPerShipper is > 0 ? batch.PacksPerShipper : null,
                ExpectedYield = batch.ExpectedYield is > 0 ? decimal.Round(batch.ExpectedYield.Value * share, 0, MidpointRounding.AwayFromZero) : null,
                Packed = packing is not null, ActualYield = packing?.TotalQuantityPacked, RetainedSamples = packing?.RetainedSamples,
                BottlesPerShipper = packing?.NumberOfBottlesPerShipper, FullShippers = packing?.NUmberOfFullShipperPacked, LeftOver = packing?.LeftOver,
                TransferNotes = notes.Count,
                TransferredToWarehouse = notes.Count > 0 && units.Count == 1 ? notes.Sum(note => note.TotalQuantity) : null,
                TransferUnit = units.Count == 1 ? units[0] : null,
                UnitPrice = price?.Price, PriceUnit = price?.PriceUoM,
                Currency = price?.CurrencyId is Guid currency && currencies.TryGetValue(currency, out var symbol) ? symbol : null,
            };
        }).OrderBy(row => row.ScheduleStart).ThenBy(row => row.ProductName, StringComparer.Ordinal)
            .ThenBy(row => row.BatchNumber, StringComparer.Ordinal).ThenBy(row => row.ScheduleProductId).ToList();

        var refused = await StillAllowed(userId, ProductionMonthlyReports, scope.ActorDepartment, scope.ActorType, cancellationToken);
        if (refused is not null) return refused;
        cancellationToken.ThrowIfCancellationRequested();
        return new ProductionMonthlyReportDto { Year = filter.Year.Value, Month = filter.Month.Value, DepartmentId = scope.DepartmentId,
            DepartmentName = scope.DepartmentName, GeneratedAt = DateTime.UtcNow, Rows = rows };
    }

    public async Task<Result<StockIssuedReportDto>> GetStockIssuedReport(
        StockIssuedFilter filter, Guid userId, CancellationToken cancellationToken = default)
    {
        string[] keys = [PermissionKeys.CanViewStockIssuedReport];
        if (!keys.Any((await CurrentPermissionSet.Load(context, userId)).Contains))
            return Error.Forbidden("Report.Permission", "Stock issued report access is required.");
        if (filter.DepartmentId == Guid.Empty || filter.ProductId == Guid.Empty
            || filter.StartDate.HasValue != filter.EndDate.HasValue || filter.StartDate > filter.EndDate || filter.EndDate == DateOnly.MaxValue)
            return Error.Validation("Report.Scope", "Choose either both issue dates or neither.");
        var scope = await ReportDepartment(userId, filter.DepartmentId, cancellationToken);
        if (scope.Error is not null) return scope.Error;
        var batch = string.IsNullOrWhiteSpace(filter.BatchNumber) ? null : filter.BatchNumber.Trim().ToUpperInvariant();
        DateTime? start = filter.StartDate?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        DateTime? endExclusive = filter.EndDate?.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        // A stock requisition issue writes one bin card line per material batch taken from the store.
        var cards = from card in context.BinCardInformation.IgnoreQueryFilters().AsNoTracking()
            join requisition in context.Requisitions.IgnoreQueryFilters().AsNoTracking() on card.RequisitionId equals requisition.Id
            where card.QuantityIssued > 0 && !card.DeletedAt.HasValue && requisition.RequisitionType == RequisitionType.Stock
                && requisition.ProductionScheduleProductId != null
            select new { card, requisition };
        if (scope.DepartmentId.HasValue) cards = cards.Where(item => item.requisition.ProductionScheduleProduct.Product.DepartmentId == scope.DepartmentId);
        if (filter.ProductId.HasValue) cards = cards.Where(item => item.requisition.ProductionScheduleProduct.ProductId == filter.ProductId);
        var requisitionRows = await cards.Select(item => new
        {
            item.card.Id, IssuedAt = item.card.CreatedAt, item.requisition.Code, RequisitionId = item.requisition.Id,
            ProductId = (Guid?)item.requisition.ProductionScheduleProduct.ProductId,
            ProductCode = item.requisition.ProductionScheduleProduct.Product.Code, ProductName = item.requisition.ProductionScheduleProduct.Product.Name,
            DepartmentId = item.requisition.ProductionScheduleProduct.Product.DepartmentId,
            DepartmentName = item.requisition.ProductionScheduleProduct.Product.Department.Name,
            PlannedBatch = item.requisition.ProductionScheduleProduct.BatchNumber, RecordedBatch = item.card.ProductBatchNumber,
            MaterialId = (Guid?)item.card.MaterialBatch.MaterialId, MaterialCode = item.card.MaterialBatch.Material.Code,
            MaterialName = item.card.MaterialBatch.Material.Name, MaterialKind = (DOMAIN.Entities.Materials.MaterialKind?)item.card.MaterialBatch.Material.Kind,
            MaterialBatchNumber = item.card.MaterialBatch.BatchNumber, item.card.ArNumber, Quantity = item.card.QuantityIssued,
            Unit = item.card.UoM.Symbol, WarehouseName = item.card.Warehouse.Name,
        }).ToListAsync(cancellationToken);
        var requisitionIds = requisitionRows.Select(row => row.RequisitionId).Distinct().ToList();
        var issuers = (await context.StockRequisitionIssues.IgnoreQueryFilters().AsNoTracking()
                .Where(issue => requisitionIds.Contains(issue.RequisitionId))
                .Select(issue => new { issue.RequisitionId, issue.IssuedAt, issue.IssuedBy.FirstName, issue.IssuedBy.LastName, issue.IssuedBy.UserName })
                .ToListAsync(cancellationToken))
            .GroupBy(issue => issue.RequisitionId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(issue => issue.IssuedAt).Select(issue => (issue.IssuedAt,
                Name: $"{issue.FirstName} {issue.LastName}".Trim() is { Length: > 0 } name ? name : issue.UserName)).First());

        var extra = context.ProductionExtraPackings.IgnoreQueryFilters().AsNoTracking().Where(item => item.IssuedAt.HasValue && !item.DeletedAt.HasValue);
        if (scope.DepartmentId.HasValue) extra = extra.Where(item => item.ProductionScheduleProduct.Product.DepartmentId == scope.DepartmentId);
        if (filter.ProductId.HasValue) extra = extra.Where(item => item.ProductionScheduleProduct.ProductId == filter.ProductId);
        if (start.HasValue) extra = extra.Where(item => item.IssuedAt >= start && item.IssuedAt < endExclusive);
        var extraRows = await extra.Select(item => new
        {
            item.Id, IssuedAt = item.IssuedAt!.Value, item.IssuedBy.FirstName, item.IssuedBy.LastName, item.IssuedBy.UserName,
            ProductId = (Guid?)item.ProductionScheduleProduct.ProductId, ProductCode = item.ProductionScheduleProduct.Product.Code,
            ProductName = item.ProductionScheduleProduct.Product.Name, item.ProductionScheduleProduct.Product.DepartmentId,
            DepartmentName = item.ProductionScheduleProduct.Product.Department.Name, PlannedBatch = item.ProductionScheduleProduct.BatchNumber,
            item.MaterialId, MaterialCode = item.Material.Code, MaterialName = item.Material.Name,
            MaterialKind = (DOMAIN.Entities.Materials.MaterialKind?)item.Material.Kind, item.Quantity, Unit = item.UoM.Symbol,
        }).ToListAsync(cancellationToken);

        var rows = requisitionRows.Select(row => new StockIssuedRowDto
            {
                // The issue record holds when and by whom the requisition was issued; older data falls back to the bin card line.
                Id = row.Id, Source = StockIssueSource.Requisition,
                IssuedAt = issuers.TryGetValue(row.RequisitionId, out var issuer) ? issuer.IssuedAt : row.IssuedAt,
                IssuedByName = issuers.ContainsKey(row.RequisitionId) ? issuer.Name : null, RequisitionCode = row.Code,
                ProductId = row.ProductId, ProductCode = row.ProductCode, ProductName = row.ProductName,
                DepartmentId = row.DepartmentId, DepartmentName = row.DepartmentName,
                BatchNumber = string.IsNullOrWhiteSpace(row.RecordedBatch) ? row.PlannedBatch : row.RecordedBatch,
                MaterialId = row.MaterialId, MaterialCode = row.MaterialCode, MaterialName = row.MaterialName, MaterialKind = row.MaterialKind,
                MaterialBatchNumber = row.MaterialBatchNumber, ArNumber = row.ArNumber, Quantity = row.Quantity, Unit = row.Unit,
                WarehouseName = row.WarehouseName,
            })
            .Concat(extraRows.Select(row => new StockIssuedRowDto
            {
                Id = row.Id, Source = StockIssueSource.ExtraPacking, IssuedAt = row.IssuedAt,
                IssuedByName = $"{row.FirstName} {row.LastName}".Trim() is { Length: > 0 } name ? name : row.UserName,
                ProductId = row.ProductId, ProductCode = row.ProductCode, ProductName = row.ProductName,
                DepartmentId = row.DepartmentId, DepartmentName = row.DepartmentName, BatchNumber = row.PlannedBatch,
                MaterialId = row.MaterialId, MaterialCode = row.MaterialCode, MaterialName = row.MaterialName, MaterialKind = row.MaterialKind,
                Quantity = row.Quantity, Unit = row.Unit,
            }))
            .Where(row => batch is null || string.Equals(row.BatchNumber?.Trim(), batch, StringComparison.OrdinalIgnoreCase))
            .Where(row => !start.HasValue || (row.IssuedAt >= start && row.IssuedAt < endExclusive))
            .OrderBy(row => row.IssuedAt).ThenBy(row => row.BatchNumber, StringComparer.Ordinal)
            .ThenBy(row => row.MaterialCode, StringComparer.Ordinal).ThenBy(row => row.MaterialBatchNumber, StringComparer.Ordinal)
            .ThenBy(row => row.Id).ToList();

        var refused = await StillAllowed(userId, keys, scope.ActorDepartment, scope.ActorType, cancellationToken);
        if (refused is not null) return refused;
        cancellationToken.ThrowIfCancellationRequested();
        return new StockIssuedReportDto { DepartmentId = scope.DepartmentId, DepartmentName = scope.DepartmentName, ProductId = filter.ProductId,
            BatchNumber = filter.BatchNumber?.Trim(), StartDate = filter.StartDate, EndDate = filter.EndDate, GeneratedAt = DateTime.UtcNow, Rows = rows };
    }
}
