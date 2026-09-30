using System.Data;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SHARED;

namespace APP.Repository;

public partial class SupplierRelationshipRepository
{
    public async Task<Result<List<SupplierPricingAgreementDto>>> GetPricingAgreements(Guid supplierId)
    {
        var supplier = await RequireSupplier(supplierId);
        if (!supplier.IsSuccess) return supplier.Error;
        var items = await PricingQuery().Where(item => item.SupplierId == supplierId)
            .Where(item => item.Status == SupplierPricingAgreementStatus.Approved
                && item.ChangeKind != SupplierPricingAgreementChangeKind.Archive)
            .OrderByDescending(item => item.EffectiveFrom).ToListAsync();
        return items.Select(ToPricingDto).ToList();
    }

    public async Task<Result<SupplierPricingAgreementDto>> GetPricingAgreementProposal(Guid supplierId, Guid id,
        Guid userId, List<Guid> roleIds)
    {
        var item = await PricingQuery().Include(item => item.Approvals)
            .FirstOrDefaultAsync(item => item.Id == id && item.SupplierId == supplierId);
        if (item is not null && item.CreatedById != userId && !item.Approvals.Any(stage =>
                stage.UserId == userId || stage.RoleId.HasValue && roleIds.Contains(stage.RoleId.Value)))
            return Error.Forbidden("SupplierPricingAgreement.Unauthorized", "You are not assigned to this proposal.");
        if (item is null)
            return Error.NotFound("SupplierPricingAgreement.NotFound", "Pricing agreement proposal not found.");
        var dto = ToPricingDto(item);
        if (item.ReplacesAgreementId.HasValue)
        {
            var prior = await PricingQuery().FirstOrDefaultAsync(agreement =>
                agreement.Id == item.ReplacesAgreementId.Value && agreement.SupplierId == supplierId);
            if (prior is not null)
            {
                dto.PriorAgreedPrice = prior.AgreedPrice;
                dto.PriorPriceUoM = prior.PriceUoM;
                dto.PriorCurrencySymbol = prior.Currency?.Symbol;
            }
        }
        return dto;
    }

    public async Task<Result<Guid>> CreatePricingAgreement(
        Guid supplierId, SupplierPricingAgreementRequest request, Guid userId)
    {
        return await SubmitPricingChange(supplierId, request, userId, null,
            SupplierPricingAgreementChangeKind.Create);
    }

    public async Task<Result> UpdatePricingAgreement(
        Guid supplierId, Guid id, SupplierPricingAgreementRequest request, Guid userId)
    {
        var entity = await context.SupplierPricingAgreements.FirstOrDefaultAsync(item =>
            item.Id == id && item.SupplierId == supplierId);
        if (entity is null)
            return Error.NotFound("SupplierPricingAgreement.NotFound", "Pricing agreement not found.");
        if (entity.Status != SupplierPricingAgreementStatus.Approved)
            return Error.Conflict("SupplierPricingAgreement.Status", "Only an approved agreement can be revised.");
        if (request.EffectiveFrom.Date < entity.EffectiveFrom.Date)
            return Error.Validation("SupplierPricingAgreement.Dates", "A revision cannot begin before the current agreement.");
        var submitted = await SubmitPricingChange(supplierId, request, userId, id,
            SupplierPricingAgreementChangeKind.Replace);
        return submitted.IsSuccess ? Result.Success() : Result.Failure(submitted.Errors);
    }

    public async Task<Result> DeletePricingAgreement(Guid supplierId, Guid id, Guid userId)
    {
        var entity = await context.SupplierPricingAgreements.FirstOrDefaultAsync(item =>
            item.Id == id && item.SupplierId == supplierId);
        if (entity is null)
            return Error.NotFound("SupplierPricingAgreement.NotFound", "Pricing agreement not found.");
        if (entity.Status != SupplierPricingAgreementStatus.Approved)
            return Error.Conflict("SupplierPricingAgreement.Status", "Only an approved agreement can be archived.");
        var request = new SupplierPricingAgreementRequest {
            MaterialId = entity.MaterialId, UoMId = entity.UoMId,
            AgreedPrice = entity.AgreedPrice, PriceUoM = entity.PriceUoM,
            CurrencyId = entity.CurrencyId, EffectiveFrom = entity.EffectiveFrom,
            EffectiveTo = entity.EffectiveTo, Notes = entity.Notes,
        };
        var submitted = await SubmitPricingChange(supplierId, request, userId, id,
            SupplierPricingAgreementChangeKind.Archive);
        return submitted.IsSuccess ? Result.Success() : Result.Failure(submitted.Errors);
    }

    private async Task<Result<Guid>> SubmitPricingChange(Guid supplierId,
        SupplierPricingAgreementRequest request, Guid userId, Guid? targetId,
        SupplierPricingAgreementChangeKind kind)
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        var validation = await ValidatePricing(supplierId, targetId, request);
        if (!validation.IsSuccess) return validation.Error;
        if (targetId.HasValue && await context.SupplierPricingAgreements.AnyAsync(item =>
                item.ReplacesAgreementId == targetId && item.Status == SupplierPricingAgreementStatus.Pending))
            return Error.Conflict("SupplierPricingAgreement.Pending", "An agreement change is already awaiting approval.");
        var proposal = new SupplierPricingAgreement {
            Id = Guid.NewGuid(), SupplierId = supplierId, CreatedById = userId,
            CreatedAt = DateTime.UtcNow, Status = SupplierPricingAgreementStatus.Pending,
            ChangeKind = kind, ReplacesAgreementId = targetId,
        };
        Apply(proposal, request);
        await context.SupplierPricingAgreements.AddAsync(proposal);
        await context.SaveChangesAsync();
        await approvalRepository.CreateInitialApprovalsAsync(nameof(SupplierPricingAgreement), proposal.Id);
        if (transaction is not null) await transaction.CommitAsync();
        return proposal.Id;
    }

    public async Task<Result<SupplierPricingAgreementDto>> GetActivePricingAgreement(
        Guid supplierId, Guid materialId, Guid uomId, DateTime asOf)
    {
        var matches = await PricingQuery().Where(item => item.SupplierId == supplierId
                && item.Status == SupplierPricingAgreementStatus.Approved
                && item.ChangeKind != SupplierPricingAgreementChangeKind.Archive
                && item.MaterialId == materialId && item.UoMId == uomId
                && item.EffectiveFrom.Date <= asOf.Date
                && (!item.EffectiveTo.HasValue || item.EffectiveTo.Value.Date >= asOf.Date))
            .OrderByDescending(item => item.EffectiveFrom).Take(2).ToListAsync();
        switch (matches.Count)
        {
            case 0:
                return Error.NotFound("SupplierPricingAgreement.NotFound", "No active pricing agreement was found.");
            case > 1:
                logger.LogError(
                    "Ambiguous supplier pricing agreements for supplier {SupplierId}, material {MaterialId}, UoM {UoMId} at {AsOf}",
                    supplierId, materialId, uomId, asOf);
                return Error.Conflict(
                    "SupplierPricingAgreement.Ambiguous", "Multiple active pricing agreements require correction.");
            default:
                return ToPricingDto(matches[0]);
        }
    }

    private async Task<Result> ValidatePricing(
        Guid supplierId, Guid? existingId, SupplierPricingAgreementRequest request)
    {
        var supplier = await RequireSupplier(supplierId);
        if (!supplier.IsSuccess) return supplier.Error;
        if (request.AgreedPrice <= 0)
            return Error.Validation("SupplierPricingAgreement.Price", "Agreed price must be greater than zero.");
        if (request.EffectiveTo.HasValue && request.EffectiveTo.Value.Date < request.EffectiveFrom.Date)
            return Error.Validation("SupplierPricingAgreement.Dates", "Effective-to cannot be before effective-from.");
        if (!await context.Materials.AnyAsync(item => item.Id == request.MaterialId))
            return Error.NotFound("Material.NotFound", "Material not found.");
        if (!await context.UnitOfMeasures.AnyAsync(item => item.Id == request.UoMId))
            return Error.NotFound("UnitOfMeasure.NotFound", "Unit of measure not found.");
        if (!await context.SupplierManufacturers.AnyAsync(item =>
                item.SupplierId == supplierId && item.MaterialId == request.MaterialId
                && item.UoMId == request.UoMId))
            return Error.Validation("SupplierPricingAgreement.Association",
                "Material and unit are not associated with this supplier.");
        if (!await context.Currencies.AnyAsync(item => item.Id == request.CurrencyId))
            return Error.NotFound("Currency.NotFound", "Currency not found.");
        var overlaps = await context.SupplierPricingAgreements.AnyAsync(item =>
            item.SupplierId == supplierId && item.MaterialId == request.MaterialId
            && item.Status != SupplierPricingAgreementStatus.Rejected
            && item.ChangeKind != SupplierPricingAgreementChangeKind.Archive
            && item.UoMId == request.UoMId && (!existingId.HasValue || item.Id != existingId)
            && (!item.EffectiveTo.HasValue || item.EffectiveTo.Value.Date >= request.EffectiveFrom.Date)
            && (!request.EffectiveTo.HasValue || item.EffectiveFrom.Date <= request.EffectiveTo.Value.Date));
        return overlaps
            ? Error.Conflict("SupplierPricingAgreement.Overlap", "An overlapping pricing agreement already exists.")
            : Result.Success();
    }

    private IQueryable<SupplierPricingAgreement> PricingQuery() => context.SupplierPricingAgreements
        .AsNoTracking().Include(item => item.Material).Include(item => item.UoM).Include(item => item.Currency);

    private static void Apply(SupplierPricingAgreement entity, SupplierPricingAgreementRequest request)
    {
        entity.MaterialId = request.MaterialId; entity.UoMId = request.UoMId;
        entity.AgreedPrice = request.AgreedPrice; entity.PriceUoM = request.PriceUoM.Trim();
        entity.CurrencyId = request.CurrencyId; entity.EffectiveFrom = request.EffectiveFrom;
        entity.EffectiveTo = request.EffectiveTo; entity.Notes = request.Notes?.Trim();
    }

    private static SupplierPricingAgreementDto ToPricingDto(SupplierPricingAgreement item) => new()
    {
        Id = item.Id, CreatedAt = item.CreatedAt, SupplierId = item.SupplierId,
        Status = item.Status, ChangeKind = item.ChangeKind,
        ReplacesAgreementId = item.ReplacesAgreementId,
        MaterialId = item.MaterialId, MaterialName = item.Material.Name,
        UoMId = item.UoMId, UoMName = item.UoM.Name, AgreedPrice = item.AgreedPrice,
        PriceUoM = item.PriceUoM, EffectiveFrom = item.EffectiveFrom,
        EffectiveTo = item.EffectiveTo, Notes = item.Notes,
        Currency = new CurrencyDto { Id = item.Currency.Id, Name = item.Currency.Name,
            Symbol = item.Currency.Symbol, Description = item.Currency.Description,
            IsBaseCurrency = item.Currency.IsBaseCurrency },
    };
}
