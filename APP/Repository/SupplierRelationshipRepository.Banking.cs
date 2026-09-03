using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class SupplierRelationshipRepository
{
    public async Task<Result<List<SupplierBankDetailDto>>> GetBankDetails(Guid supplierId)
    {
        var supplier = await RequireSupplier(supplierId);
        if (!supplier.IsSuccess) return supplier.Error;
        var items = await context.SupplierBankDetails.AsNoTracking().Include(item => item.Currency)
            .Where(item => item.SupplierId == supplierId).OrderBy(item => item.BankName).ToListAsync();
        return items.Select(ToBankDto).ToList();
    }

    public async Task<Result<Guid>> CreateBankDetail(
        Guid supplierId, SupplierBankDetailRequest request, Guid userId)
    {
        var supplier = await RequireSupplier(supplierId);
        if (!supplier.IsSuccess) return supplier.Error;
        if (!await context.Currencies.AnyAsync(item => item.Id == request.CurrencyId))
            return Error.NotFound("Currency.NotFound", "Currency not found.");
        if (await context.SupplierBankDetails.AnyAsync(item => item.SupplierId == supplierId
                && item.AccountNumber == request.AccountNumber && item.CurrencyId == request.CurrencyId))
            return Error.Conflict("SupplierBankDetail.Duplicate", "This account is already registered.");
        var entity = new SupplierBankDetail { SupplierId = supplierId, CreatedById = userId };
        Apply(entity, request);
        await context.SupplierBankDetails.AddAsync(entity);
        await context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<Result> UpdateBankDetail(
        Guid supplierId, Guid id, SupplierBankDetailRequest request, Guid userId)
    {
        var entity = await context.SupplierBankDetails.FirstOrDefaultAsync(item =>
            item.Id == id && item.SupplierId == supplierId);
        if (entity is null)
            return Error.NotFound("SupplierBankDetail.NotFound", "Supplier bank detail not found.");
        if (!await context.Currencies.AnyAsync(item => item.Id == request.CurrencyId))
            return Error.NotFound("Currency.NotFound", "Currency not found.");
        if (await context.SupplierBankDetails.AnyAsync(item => item.Id != id
                && item.SupplierId == supplierId && item.AccountNumber == request.AccountNumber
                && item.CurrencyId == request.CurrencyId))
            return Error.Conflict("SupplierBankDetail.Duplicate", "This account is already registered.");
        Apply(entity, request);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteBankDetail(Guid supplierId, Guid id, Guid userId)
    {
        var entity = await context.SupplierBankDetails.FirstOrDefaultAsync(item =>
            item.Id == id && item.SupplierId == supplierId);
        if (entity is null)
            return Error.NotFound("SupplierBankDetail.NotFound", "Supplier bank detail not found.");
        entity.DeletedAt = DateTime.UtcNow;
        entity.LastDeletedById = userId;
        
        await context.SaveChangesAsync();
        return Result.Success();
    }

    private static void Apply(SupplierBankDetail entity, SupplierBankDetailRequest request)
    {
        entity.BankName = request.BankName.Trim();
        entity.AccountNumber = request.AccountNumber.Trim();
        entity.AccountName = request.AccountName.Trim();
        entity.SwiftCode = request.SwiftCode?.Trim();
        entity.Iban = request.Iban?.Trim();
        entity.BranchAddress = request.BranchAddress?.Trim();
        entity.CurrencyId = request.CurrencyId;
    }

    private static SupplierBankDetailDto ToBankDto(SupplierBankDetail item) => new()
    {
        Id = item.Id, CreatedAt = item.CreatedAt, SupplierId = item.SupplierId,
        BankName = item.BankName, AccountNumber = item.AccountNumber, AccountName = item.AccountName,
        SwiftCode = item.SwiftCode, Iban = item.Iban, BranchAddress = item.BranchAddress,
        Currency = new CurrencyDto
        {
            Id = item.Currency.Id, Name = item.Currency.Name, Symbol = item.Currency.Symbol,
            Description = item.Currency.Description, IsBaseCurrency = item.Currency.IsBaseCurrency,
        }
    };
}
