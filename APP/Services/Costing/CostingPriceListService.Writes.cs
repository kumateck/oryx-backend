using System.Data;
using APP.Services.Authorization;
using APP.Services.Reviews;
using APP.Utils;
using DOMAIN.Entities.Costing;
using DOMAIN.Entities.Reviews;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SHARED;

namespace APP.Services.Costing;

public sealed partial class CostingPriceListService
{
    private static Error Conflict => Error.Conflict("CostingPriceList.Stale", "The revision changed. Reload it before continuing.");
    private async Task<Error> Require(Guid actor, string key) => (await CurrentPermissionSet.Load(context, actor)).Contains(key)
        ? null : Error.Forbidden("CostingPriceList.Permission", $"You need {key} for this action.");

    private async Task<Result<Guid>> Write(Func<Task<Result<Guid>>> action)
    {
        await using var transaction = context.Database.IsRelational() ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        try
        {
            var result = await action();
            if (result.IsSuccess && transaction is not null) await transaction.CommitAsync();
            return result;
        }
        catch (DbUpdateConcurrencyException) { return Conflict; }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: "23505" or "40001" }) { return Conflict; }
        catch (PostgresException exception) when (exception.SqlState is "23505" or "40001") { return Conflict; }
    }

    private Error Header(PrepareCostingPriceListRequest request)
    {
        if (!Enum.IsDefined(request.Division) || string.IsNullOrWhiteSpace(request.DocumentNumber) || string.IsNullOrWhiteSpace(request.RevisionReason))
            return Error.Validation("CostingPriceList.Header", "Choose a division and enter the document number and revision reason.");
        if (request.RevisionNumber < 0 || request.EffectiveFrom == default || request.EffectiveFrom.Date < Now.Date)
            return Error.Validation("CostingPriceList.EffectiveDate", "Enter a revision number and an effective date of today or later.");
        return null;
    }

    public Task<Result<Guid>> Prepare(PrepareCostingPriceListRequest request, Guid actor, bool revise) => Write(async () =>
    {
        var key = revise ? PermissionKeys.CanReviseCostingPriceList : PermissionKeys.CanCreateCostingPriceList;
        var denied = await Require(actor, key); if (denied is not null) return denied;
        var problem = Header(request); if (problem is not null) return problem;
        if (await context.CostingPriceLists.AnyAsync(list => list.Division == request.Division
            && (list.Status == CostingPriceListStatus.Draft || list.Status == CostingPriceListStatus.Pending)))
            return Error.Conflict("CostingPriceList.Open", "A revision for this division is already being prepared.");
        var previous = await context.CostingPriceLists.AsNoTracking().Where(list => list.Division == request.Division && list.Status == CostingPriceListStatus.Approved)
            .OrderByDescending(list => list.RevisionNumber).FirstOrDefaultAsync();
        if (revise != (previous is not null) || request.PreviousRevisionId != previous?.Id)
            return Error.Conflict("CostingPriceList.Previous", "Prepare a new revision from the latest approved price list.");
        if (previous is not null && (request.RevisionNumber <= previous.RevisionNumber || request.EffectiveFrom.Date <= previous.EffectiveFrom.Date))
            return Error.Validation("CostingPriceList.Revision", "The revision number and effective date must be later than the preceding approved revision.");
        var list = new CostingPriceList
        {
            Id = Guid.NewGuid(), Division = request.Division, DocumentNumber = request.DocumentNumber.Trim(), RevisionNumber = request.RevisionNumber,
            PreviousRevisionId = previous?.Id, RevisionReason = request.RevisionReason.Trim(),
            EffectiveFrom = DateTime.SpecifyKind(request.EffectiveFrom.Date, DateTimeKind.Utc), Status = CostingPriceListStatus.Draft,
            Version = 1, CreatedAt = Now, CreatedById = actor,
        };
        if (previous is not null)
        {
            var source = await context.CostingPriceListLines.AsNoTracking().Where(line => line.CostingPriceListId == previous.Id).ToListAsync();
            list.Lines = source.Select(line => new CostingPriceListLine
            {
                Id = Guid.NewGuid(), ProductId = line.ProductId, ProductPackingId = line.ProductPackingId, PackingStyle = line.PackingStyle,
                ProductCode = line.ProductCode, ProductName = line.ProductName, DepartmentId = line.DepartmentId,
                Price = line.Price, PriceUoM = line.PriceUoM, CurrencyId = line.CurrencyId, CurrencyName = line.CurrencyName,
                CurrencySymbol = line.CurrencySymbol, CreatedAt = Now, CreatedById = actor,
            }).ToList();
        }
        else
        {
            // Pack styles are separate prices. Legacy product-level prices are unambiguous only for a single style.
            var packs = await context.ProductPackings.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
                .Where(pack => pack.DeletedAt == null && pack.Product.DeletedAt == null && pack.Product.Division == request.Division)
                .Select(pack => new { pack.Id, pack.Name, pack.ProductId, ProductName = pack.Product.Name, pack.Product.Code, pack.Product.DepartmentId,
                    Count = pack.Product.Packings.Count(style => style.DeletedAt == null),
                    Price = pack.Product.Prices.OrderByDescending(price => price.Date).Select(price => (decimal?)price.Price).FirstOrDefault(),
                    CurrencyId = pack.Product.Prices.OrderByDescending(price => price.Date).Select(price => price.CurrencyId).FirstOrDefault() }).ToListAsync();
            var baseId = await APP.Repository.CustomerPricingResolver.BaseCurrencyIdAsync(context);
            var labels = await APP.Repository.CustomerPricingResolver.CurrencyLabelsAsync(context);
            list.Lines = packs.Select(pack =>
            {
                var priced = pack.Count == 1 && pack.Price > 0 && (pack.CurrencyId ?? baseId).HasValue;
                var currency = priced ? APP.Repository.CustomerPricingResolver.Label(labels, pack.CurrencyId ?? baseId) : null;
                return new CostingPriceListLine { Id = Guid.NewGuid(), ProductId = pack.ProductId, ProductPackingId = pack.Id, PackingStyle = pack.Name,
                    ProductCode = pack.Code, ProductName = pack.ProductName, DepartmentId = pack.DepartmentId,
                    Price = priced ? pack.Price : null, PriceUoM = "pack", CurrencyId = currency?.Id, CurrencyName = currency?.Name,
                    CurrencySymbol = currency?.Symbol, CreatedAt = Now, CreatedById = actor };
            }).ToList();
        }
        denied = await Require(actor, key); if (denied is not null) return denied;
        context.CostingPriceLists.Add(list);
        await context.SaveChangesAsync();
        return list.Id;
    });

    private async Task<Result<CostingPriceList>> Editable(Guid id, int version)
    {
        var list = await context.CostingPriceLists.FirstOrDefaultAsync(item => item.Id == id);
        if (list is null) return Missing;
        if (list.Version != version) return Conflict;
        var review = await DocumentReviewGate.LatestAsync(context, ModelType, id);
        if (list.Status != CostingPriceListStatus.Draft && !(list.Status == CostingPriceListStatus.Pending
            && review is { Status: DocumentReviewStatus.AwaitingReview or DocumentReviewStatus.Returned }))
            return Error.Conflict("CostingPriceList.Locked", "Approved, rejected and approval-stage revisions cannot be edited. Prepare a new revision.");
        return list;
    }

    private void Changed(CostingPriceList list, Guid actor)
    { list.Version++; list.UpdatedAt = Now; list.LastUpdatedById = actor; }

    public Task<Result<Guid>> Update(Guid id, UpdateCostingPriceListRequest request, Guid actor) => Write(async () =>
    {
        var denied = await Require(actor, PermissionKeys.CanEditCostingPriceList); if (denied is not null) return denied;
        var loaded = await Editable(id, request.Version); if (loaded.IsFailure) return loaded.Error;
        var list = loaded.Value;
        var problem = Header(request); if (problem is not null) return problem;
        if (request.Division != list.Division || request.PreviousRevisionId != list.PreviousRevisionId)
            return Error.Validation("CostingPriceList.Scope", "The revision's division and predecessor cannot change.");
        if (list.PreviousRevisionId.HasValue)
        {
            var previous = await context.CostingPriceLists.AsNoTracking().SingleAsync(item => item.Id == list.PreviousRevisionId);
            if (request.RevisionNumber <= previous.RevisionNumber || request.EffectiveFrom.Date <= previous.EffectiveFrom.Date)
                return Error.Validation("CostingPriceList.Revision", "Use a later revision number and effective date than the preceding revision.");
        }
        list.DocumentNumber = request.DocumentNumber.Trim(); list.RevisionNumber = request.RevisionNumber;
        list.EffectiveFrom = DateTime.SpecifyKind(request.EffectiveFrom.Date, DateTimeKind.Utc); list.RevisionReason = request.RevisionReason.Trim();
        Changed(list, actor);
        denied = await Require(actor, PermissionKeys.CanEditCostingPriceList); if (denied is not null) return denied;
        await context.SaveChangesAsync();
        denied = await Require(actor, PermissionKeys.CanEditCostingPriceList); if (denied is not null) return denied;
        await DocumentReviewGate.NoteEditAsync(context, ModelType, id, actor);
        return id;
    });

    public Task<Result<Guid>> SaveLine(Guid id, SaveCostingPriceListLineRequest request, Guid actor) => Write(async () =>
    {
        var denied = await Require(actor, PermissionKeys.CanEditCostingPriceList); if (denied is not null) return denied;
        var loaded = await Editable(id, request.Version); if (loaded.IsFailure) return loaded.Error;
        var list = loaded.Value;
        var pack = await context.ProductPackings.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(item => item.Id == request.ProductPackingId && item.ProductId == request.ProductId && item.DeletedAt == null
                && item.Product.DeletedAt == null && item.Product.Division == list.Division)
            .Select(item => new { PackingStyle = item.Name, ProductName = item.Product.Name, item.Product.Code, item.Product.DepartmentId }).FirstOrDefaultAsync();
        if (pack is null) return Error.Validation("CostingPriceList.Packing", "Choose an active product and its own packing style from this division.");
        if (request.Price.HasValue && (request.Price <= 0 || !request.CurrencyId.HasValue))
            return Error.Validation("CostingPriceList.Price", "Enter a positive price per complete pack and choose its currency, or leave it unpriced.");
        var currency = request.Price.HasValue ? await context.Currencies.AsNoTracking().Where(item => item.Id == request.CurrencyId)
            .Select(item => new { item.Id, item.Name, item.Symbol }).FirstOrDefaultAsync() : null;
        if (request.Price.HasValue && currency is null) return Error.Validation("CostingPriceList.Currency", "Choose an active currency.");
        var line = await context.CostingPriceListLines.FirstOrDefaultAsync(item => item.CostingPriceListId == id
            && item.ProductId == request.ProductId && item.ProductPackingId == request.ProductPackingId);
        if (line is null) { line = new() { Id = Guid.NewGuid(), CostingPriceListId = id, ProductId = request.ProductId,
            ProductPackingId = request.ProductPackingId, CreatedAt = Now, CreatedById = actor }; context.CostingPriceListLines.Add(line); }
        line.ProductCode = pack.Code; line.ProductName = pack.ProductName; line.PackingStyle = pack.PackingStyle;
        line.DepartmentId = pack.DepartmentId; line.Price = request.Price; line.PriceUoM = "pack";
        line.CurrencyId = currency?.Id; line.CurrencyName = currency?.Name; line.CurrencySymbol = currency?.Symbol;
        line.UpdatedAt = Now; line.LastUpdatedById = actor; Changed(list, actor);
        denied = await Require(actor, PermissionKeys.CanEditCostingPriceList); if (denied is not null) return denied;
        await context.SaveChangesAsync();
        denied = await Require(actor, PermissionKeys.CanEditCostingPriceList); if (denied is not null) return denied;
        await DocumentReviewGate.NoteEditAsync(context, ModelType, id, actor);
        return id;
    });

    public Task<Result<Guid>> Submit(Guid id, SubmitCostingPriceListRequest request, Guid actor) => Write(async () =>
    {
        var denied = await Require(actor, PermissionKeys.CanSubmitCostingPriceList); if (denied is not null) return denied;
        var list = await context.CostingPriceLists.FirstOrDefaultAsync(item => item.Id == id);
        if (list is null) return Missing;
        if (list.Version != request.Version) return Conflict;
        if (list.Status != CostingPriceListStatus.Draft) return Error.Conflict("CostingPriceList.Status", "Only a draft can be submitted.");
        if (!await context.Approvals.AnyAsync(item => item.ItemType == ModelType && item.ApprovalStages.Any(stage => stage.Required)))
            return Error.Validation("CostingPriceList.Workflow", "Configure at least one required price-list approval stage before submitting.");
        if (list.EffectiveFrom.Date < Now.Date) return Error.Validation("CostingPriceList.EffectiveDate", "Update the effective date before submitting.");
        if (!await context.CostingPriceListLines.AnyAsync(line => line.CostingPriceListId == id && line.Price > 0))
            return Error.Validation("CostingPriceList.Lines", "Set at least one product packing-style price before submitting.");
        list.Status = CostingPriceListStatus.Pending; Changed(list, actor);
        denied = await Require(actor, PermissionKeys.CanSubmitCostingPriceList); if (denied is not null) return denied;
        await context.SaveChangesAsync();
        denied = await Require(actor, PermissionKeys.CanSubmitCostingPriceList); if (denied is not null) return denied;
        await approvals.CreateInitialApprovalsAsync(ModelType, id, actor);
        return id;
    });
}
