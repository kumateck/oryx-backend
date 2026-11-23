using System.Linq.Expressions;
using APP.Extensions;
using APP.IRepository;
using APP.Services.Reviews;
using APP.Utils;
using DOMAIN.Entities.Costing;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Reviews;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Costing;

public sealed partial class CostingPriceListService(ApplicationDbContext context, IApprovalRepository approvals, TimeProvider clock = null)
{
    public const string ModelType = "CostingPriceList";
    private DateTime Now => (clock ?? TimeProvider.System).GetUtcNow().UtcDateTime;
    private static Error Missing => Error.NotFound("CostingPriceList.NotFound", "Price-list revision not found.");

    public async Task<Paginateable<IEnumerable<CostingPackingOptionDto>>> Options(Division division, int page, int pageSize, string search)
    {
        var query = context.ProductPackings.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(pack => pack.DeletedAt == null && pack.Product.DeletedAt == null && pack.Product.Division == division);
        if (!string.IsNullOrWhiteSpace(search)) query = query.WhereSearch(search, pack => pack.Name, pack => pack.Product.Name, pack => pack.Product.Code);
        var rows = query.OrderBy(pack => pack.Product.Name).ThenBy(pack => pack.Name).ThenBy(pack => pack.Id).Select(pack => new CostingPackingOptionDto
        { ProductId = pack.ProductId, ProductPackingId = pack.Id, ProductCode = pack.Product.Code, ProductName = pack.Product.Name, PackingStyle = pack.Name });
        return await PaginationHelper.GetPaginatedResultAsync(rows, Math.Max(1, page), Math.Clamp(pageSize, 1, 100), row => row);
    }

    private static readonly Expression<Func<CostingPriceList, CostingPriceListSummaryDto>> Summary = list => new()
    {
        Id = list.Id, Division = list.Division, DocumentNumber = list.DocumentNumber, RevisionNumber = list.RevisionNumber,
        PreviousRevisionId = list.PreviousRevisionId, SupersedesRevision = list.PreviousRevisionId.HasValue ? list.PreviousRevision.RevisionNumber : null,
        EffectiveFrom = list.EffectiveFrom, RevisionReason = list.RevisionReason, Status = list.Status, Version = list.Version,
        LineCount = list.Lines.Count, CreatedAt = list.CreatedAt, ApprovedAt = list.ApprovedAt,
        PreparedBy = list.CreatedBy == null ? null : (list.CreatedBy.FirstName + " " + list.CreatedBy.LastName).Trim(),
    };

    public async Task<CostingPriceListPageDto> List(Division division, int page, int pageSize, string search)
    {
        var query = context.CostingPriceLists.AsNoTracking().Where(list => list.Division == division);
        if (!string.IsNullOrWhiteSpace(search)) query = query.WhereSearch(search, list => list.DocumentNumber, list => list.RevisionReason);
        var result = await PaginationHelper.GetPaginatedResultAsync(query.OrderByDescending(list => list.RevisionNumber).Select(Summary),
            Math.Max(1, page), Math.Clamp(pageSize, 1, 100), row => row);
        var rows = result.Data.ToList();
        var now = Now;
        var current = await EffectiveRevision(context, division, now).Select(list => (Guid?)list.Id).FirstOrDefaultAsync();
        var ids = rows.Where(row => row.Status == CostingPriceListStatus.Pending).Select(row => row.Id).ToList();
        var reviews = await context.DocumentReviews.AsNoTracking().Where(review => review.ModelType == ModelType && ids.Contains(review.ModelId))
            .OrderByDescending(review => review.Submission).Select(review => new { review.ModelId, review.Status }).ToListAsync();
        foreach (var row in rows)
        {
            row.ReviewStatus = reviews.FirstOrDefault(review => review.ModelId == row.Id)?.Status;
            row.Stage = Stage(row, current, now);
        }
        var latestApproved = await context.CostingPriceLists.AsNoTracking()
            .Where(list => list.Division == division && list.Status == CostingPriceListStatus.Approved)
            .OrderByDescending(list => list.RevisionNumber).Select(Summary).FirstOrDefaultAsync();
        if (latestApproved is not null) latestApproved.Stage = Stage(latestApproved, current, now);
        return new CostingPriceListPageDto
        {
            Data = rows, PageIndex = result.PageIndex, PageCount = result.PageCount, TotalRecordCount = result.TotalRecordCount,
            NumberOfPagesToShow = result.NumberOfPagesToShow, StartPageIndex = result.StartPageIndex, StopPageIndex = result.StopPageIndex,
            LatestApproved = latestApproved,
            OpenRevisionId = await context.CostingPriceLists.Where(list => list.Division == division &&
                (list.Status == CostingPriceListStatus.Draft || list.Status == CostingPriceListStatus.Pending)).Select(list => (Guid?)list.Id).FirstOrDefaultAsync(),
            NextRevisionNumber = (await context.CostingPriceLists.Where(list => list.Division == division).MaxAsync(list => (int?)list.RevisionNumber) ?? -1) + 1,
        };
    }

    internal static IQueryable<CostingPriceList> EffectiveRevision(ApplicationDbContext db, Division division, DateTime asOf) =>
        db.CostingPriceLists.AsNoTracking().Where(list => list.Division == division && list.Status == CostingPriceListStatus.Approved
            && list.EffectiveFrom <= asOf && list.ApprovedAt.HasValue && list.ApprovedAt <= asOf)
            .OrderByDescending(list => list.EffectiveFrom).ThenByDescending(list => list.RevisionNumber);

    private static string Stage(CostingPriceListSummaryDto row, Guid? current, DateTime now) => row.Status switch
    {
        CostingPriceListStatus.Draft => "Draft",
        CostingPriceListStatus.Rejected => "Rejected",
        CostingPriceListStatus.Approved when row.Id == current => "Current",
        CostingPriceListStatus.Approved when row.EffectiveFrom > now => "Scheduled",
        CostingPriceListStatus.Approved => "Superseded",
        _ when row.ReviewStatus == DocumentReviewStatus.Returned => "Returned for correction",
        _ when row.ReviewStatus == DocumentReviewStatus.AwaitingReview => "Awaiting check",
        _ => "Awaiting approval",
    };

    public async Task<Result<CostingPriceListDetailDto>> Get(Guid id)
    {
        var row = await context.CostingPriceLists.AsNoTracking().Where(list => list.Id == id).Select(Summary).FirstOrDefaultAsync();
        if (row is null) return Missing;
        var now = Now;
        var current = await EffectiveRevision(context, row.Division, now).Select(list => (Guid?)list.Id).FirstOrDefaultAsync();
        var review = await DocumentReviewGate.LatestAsync(context, ModelType, id);
        row.ReviewStatus = review?.Status;
        var prepared = await context.CostingPriceLists.Where(list => list.Id == id).Select(list => list.CreatedById).SingleAsync();
        var confirmed = await context.DocumentReviews.AsNoTracking().Where(item => item.ModelType == ModelType && item.ModelId == id
            && item.Status == DocumentReviewStatus.Confirmed).OrderByDescending(item => item.Submission)
            .Select(item => new { item.ReviewedById, item.ReviewedAt }).FirstOrDefaultAsync();
        var approved = await ReviewDocumentTypes.FinalApproverAsync(context, ModelType, id);
        return new CostingPriceListDetailDto
        {
            Id = row.Id, Division = row.Division, DocumentNumber = row.DocumentNumber, RevisionNumber = row.RevisionNumber,
            PreviousRevisionId = row.PreviousRevisionId, SupersedesRevision = row.SupersedesRevision,
            EffectiveFrom = row.EffectiveFrom, RevisionReason = row.RevisionReason, Status = row.Status, Version = row.Version,
            LineCount = row.LineCount, CreatedAt = row.CreatedAt, ApprovedAt = row.ApprovedAt, PreparedBy = row.PreparedBy,
            ReviewStatus = row.ReviewStatus, Stage = Stage(row, current, now),
            PreparedBySignatory = await Sign(prepared, row.CreatedAt),
            CheckedBy = await Sign(confirmed?.ReviewedById, confirmed?.ReviewedAt),
            ApprovedBy = await Sign(approved.UserId, approved.At),
        };
    }

    private async Task<CostingPriceListSignatoryDto> Sign(Guid? userId, DateTime? at)
    {
        if (!userId.HasValue) return null;
        var person = await context.Users.IgnoreQueryFilters().AsNoTracking().Where(user => user.Id == userId)
            .Select(user => new CostingPriceListSignatoryDto { Name = (user.FirstName + " " + user.LastName).Trim(), Signature = user.Signature, SignedAt = at })
            .FirstOrDefaultAsync();
        if (person is not null) person.Designation = await context.Employees.IgnoreQueryFilters().AsNoTracking()
            .Where(employee => employee.UserId == userId && employee.Designation != null).Select(employee => employee.Designation.Name).FirstOrDefaultAsync();
        return person;
    }

    internal IQueryable<CostingPriceListLineDto> LineQuery(Guid id, string search)
    {
        var lines = context.CostingPriceListLines.AsNoTracking().Where(line => line.CostingPriceListId == id);
        if (!string.IsNullOrWhiteSpace(search)) lines = lines.WhereSearch(search, line => line.ProductName, line => line.ProductCode, line => line.PackingStyle);
        return lines.OrderBy(line => line.ProductName).ThenBy(line => line.PackingStyle).ThenBy(line => line.Id).Select(line => new CostingPriceListLineDto
        {
            Id = line.Id, ProductId = line.ProductId, ProductPackingId = line.ProductPackingId, PackingStyle = line.PackingStyle,
            ProductCode = line.ProductCode, ProductName = line.ProductName, Price = line.Price, PriceUoM = line.PriceUoM,
            CurrencyId = line.CurrencyId, CurrencyName = line.CurrencyName, CurrencySymbol = line.CurrencySymbol,
            PreviousPrice = context.CostingPriceListLines.Where(previous => previous.CostingPriceListId == line.CostingPriceList.PreviousRevisionId
                && previous.ProductId == line.ProductId && previous.ProductPackingId == line.ProductPackingId).Select(previous => previous.Price).FirstOrDefault(),
            PreviousCurrencySymbol = context.CostingPriceListLines.Where(previous => previous.CostingPriceListId == line.CostingPriceList.PreviousRevisionId
                && previous.ProductId == line.ProductId && previous.ProductPackingId == line.ProductPackingId).Select(previous => previous.CurrencySymbol).FirstOrDefault(),
            PreviousPriceUoM = context.CostingPriceListLines.Where(previous => previous.CostingPriceListId == line.CostingPriceList.PreviousRevisionId
                && previous.ProductId == line.ProductId && previous.ProductPackingId == line.ProductPackingId).Select(previous => previous.PriceUoM).FirstOrDefault(),
            PreviousCurrencyId = context.CostingPriceListLines.Where(previous => previous.CostingPriceListId == line.CostingPriceList.PreviousRevisionId
                && previous.ProductId == line.ProductId && previous.ProductPackingId == line.ProductPackingId).Select(previous => previous.CurrencyId).FirstOrDefault(),
        });
    }

    public async Task<Result<Paginateable<IEnumerable<CostingPriceListLineDto>>>> Lines(Guid id, int page, int pageSize, string search)
    {
        if (!await context.CostingPriceLists.AnyAsync(list => list.Id == id)) return Missing;
        return await PaginationHelper.GetPaginatedResultAsync(LineQuery(id, search), Math.Max(1, page), Math.Clamp(pageSize, 1, 100), row => row);
    }

    public async Task<Result<CostingPriceListPrintDto>> Print(Guid id)
    {
        var revision = await Get(id);
        if (revision.IsFailure) return revision.Error;
        return new CostingPriceListPrintDto { Revision = revision.Value, Lines = await LineQuery(id, null).ToListAsync() };
    }
}
