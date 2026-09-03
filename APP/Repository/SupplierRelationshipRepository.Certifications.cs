using DOMAIN.Entities.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class SupplierRelationshipRepository
{
    public async Task<Result<List<SupplierCertificationDto>>> GetCertifications(Guid supplierId)
    {
        var supplier = await RequireSupplier(supplierId);
        if (!supplier.IsSuccess) return supplier.Error;
        var items = await context.SupplierCertifications.AsNoTracking().Include(item => item.Supplier)
            .Where(item => item.SupplierId == supplierId)
            .OrderBy(item => item.ExpiryDate)
            .ToListAsync();
        return items.Select(ToCertificationDto).ToList();
    }

    public async Task<Result<Guid>> CreateCertification(
        Guid supplierId, SupplierCertificationRequest request, Guid userId)
    {
        var supplier = await RequireSupplier(supplierId);
        if (!supplier.IsSuccess) return supplier.Error;
        var validation = ValidateCertification(request);
        if (!validation.IsSuccess) return validation.Error;
        if (request.AttachmentId.HasValue
            && !await context.Attachments.AnyAsync(item => item.Id == request.AttachmentId))
            return Error.NotFound("Attachment.NotFound", "Certificate attachment not found.");
        if (await context.SupplierCertifications.AnyAsync(item =>
                item.SupplierId == supplierId && item.CertificateNumber == request.CertificateNumber))
            return Error.Conflict("SupplierCertification.Duplicate", "Certificate number already exists for this supplier.");

        var entity = new SupplierCertification { SupplierId = supplierId, CreatedById = userId };
        Apply(entity, request);
        await context.SupplierCertifications.AddAsync(entity);
        await context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<Result> UpdateCertification(
        Guid supplierId, Guid id, SupplierCertificationRequest request, Guid userId)
    {
        var entity = await context.SupplierCertifications.FirstOrDefaultAsync(item =>
            item.Id == id && item.SupplierId == supplierId);
        if (entity is null)
            return Error.NotFound("SupplierCertification.NotFound", "Supplier certification not found.");
        var validation = ValidateCertification(request);
        if (!validation.IsSuccess) return validation;
        if (request.AttachmentId.HasValue
            && !await context.Attachments.AnyAsync(item => item.Id == request.AttachmentId))
            return Error.NotFound("Attachment.NotFound", "Certificate attachment not found.");
        if (await context.SupplierCertifications.AnyAsync(item => item.Id != id
                && item.SupplierId == supplierId && item.CertificateNumber == request.CertificateNumber))
            return Error.Conflict("SupplierCertification.Duplicate", "Certificate number already exists for this supplier.");
        Apply(entity, request);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteCertification(Guid supplierId, Guid id, Guid userId)
    {
        var entity = await context.SupplierCertifications.FirstOrDefaultAsync(item =>
            item.Id == id && item.SupplierId == supplierId);
        if (entity is null)
            return Error.NotFound("SupplierCertification.NotFound", "Supplier certification not found.");
        entity.DeletedAt = DateTime.UtcNow;
        entity.LastDeletedById = userId;
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<List<SupplierCertificationDto>>> GetExpiringCertifications(
        int withinDays, DateTime? asOf = null)
    {
        if (withinDays < 0)
            return Error.Validation("SupplierCertification.Days", "Within-days must be zero or greater.");
        var start = (asOf ?? DateTime.UtcNow).Date;
        var end = start.AddDays(withinDays);
        var items = await context.SupplierCertifications.AsNoTracking().Include(item => item.Supplier)
            .Where(item => item.ExpiryDate.Date >= start && item.ExpiryDate.Date <= end)
            .OrderBy(item => item.ExpiryDate)
            .ToListAsync();
        return items.Select(ToCertificationDto).ToList();
    }

    private static Result ValidateCertification(SupplierCertificationRequest request)
        => request.ExpiryDate.Date < request.IssueDate.Date
            ? Error.Validation("SupplierCertification.Dates", "Expiry date cannot be before issue date.")
            : Result.Success();

    private static void Apply(SupplierCertification entity, SupplierCertificationRequest request)
    {
        entity.CertificationType = request.CertificationType;
        entity.CertificateNumber = request.CertificateNumber.Trim();
        entity.IssuingBody = request.IssuingBody.Trim();
        entity.IssueDate = request.IssueDate;
        entity.ExpiryDate = request.ExpiryDate;
        entity.AttachmentId = request.AttachmentId;
    }

    private static SupplierCertificationDto ToCertificationDto(SupplierCertification item) => new()
    {
        Id = item.Id, CreatedAt = item.CreatedAt, SupplierId = item.SupplierId,
        SupplierName = item.Supplier.Name, CertificationType = item.CertificationType,
        CertificateNumber = item.CertificateNumber, IssuingBody = item.IssuingBody,
        IssueDate = item.IssueDate, ExpiryDate = item.ExpiryDate, AttachmentId = item.AttachmentId,
    };
}
