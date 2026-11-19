using APP.IRepository;
using APP.Services.Authorization;
using APP.Services.Reviews;
using APP.Utils;
using DOMAIN.Entities.ApprovedVendorLists;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Reviews;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.ApprovedVendorLists;

/// <summary>
/// QA's approved vendor list: the first list, corrections while it is in review, and new revisions of the
/// approved list. Saving opens the document review ("Checked by"); confirming it starts the approval
/// ("Approved by"), whose final approval supersedes the previous revision.
/// </summary>
public sealed partial class ApprovedVendorListService(ApplicationDbContext context, IApprovalRepository approvals)
{
    public const string ModelType = ReviewDocumentTypes.ApprovedVendorList;

    public async Task<List<ApprovedVendorListSummaryDto>> List()
    {
        var lists = await context.ApprovedVendorLists.AsNoTracking()
            .OrderByDescending(item => item.RevisionNumber).ThenByDescending(item => item.CreatedAt)
            .Select(item => new ApprovedVendorListSummaryDto
            {
                Id = item.Id, AnnexureNumber = item.AnnexureNumber, RevisionNumber = item.RevisionNumber,
                SupersedesRevision = item.SupersedesRevision, Status = item.Status, LineCount = item.Lines.Count,
                CreatedAt = item.CreatedAt, ApprovedAt = item.ApprovedAt,
                PreparedBy = item.CreatedBy == null ? null : (item.CreatedBy.FirstName + " " + item.CreatedBy.LastName).Trim(),
            }).ToListAsync();
        var pending = lists.Where(item => item.Status == ApprovedVendorListStatus.Pending).Select(item => item.Id).ToList();
        if (pending.Count == 0) return lists;
        var reviews = await context.DocumentReviews.AsNoTracking()
            .Where(item => item.ModelType == ModelType && pending.Contains(item.ModelId))
            .GroupBy(item => item.ModelId)
            .Select(group => group.OrderByDescending(item => item.Submission).First())
            .ToListAsync();
        foreach (var list in lists)
            list.ReviewStatus = OpenReviewStatus(reviews.FirstOrDefault(review => review.ModelId == list.Id));
        return lists;
    }

    public async Task<Result<ApprovedVendorListDto>> Get(Guid id)
    {
        var list = await context.ApprovedVendorLists.AsNoTracking()
            .Include(item => item.Lines).ThenInclude(line => line.Material)
            .Include(item => item.Lines).ThenInclude(line => line.Manufacturer)
            .Include(item => item.Lines).ThenInclude(line => line.Supplier)
            .Include(item => item.CreatedBy)
            .AsSplitQuery()
            .FirstOrDefaultAsync(item => item.Id == id);
        if (list is null) return NotFound;
        var today = DateTime.UtcNow.Date;
        var review = await DocumentReviewGate.LatestAsync(context, ModelType, id);
        var confirmed = await context.DocumentReviews.AsNoTracking()
            .Where(item => item.ModelType == ModelType && item.ModelId == id && item.Status == DocumentReviewStatus.Confirmed)
            .OrderByDescending(item => item.Submission).FirstOrDefaultAsync();
        var approver = await ReviewDocumentTypes.FinalApproverAsync(context, ModelType, id);
        var people = await Signatories([list.CreatedById, confirmed?.ReviewedById, approver.UserId]);
        return new ApprovedVendorListDto
        {
            Id = list.Id, AnnexureNumber = list.AnnexureNumber, RevisionNumber = list.RevisionNumber,
            SupersedesRevision = list.SupersedesRevision, Status = list.Status, LineCount = list.Lines.Count,
            CreatedAt = list.CreatedAt, ApprovedAt = list.ApprovedAt, SupersededAt = list.SupersededAt,
            PreviousRevisionId = list.PreviousRevisionId,
            PreparedBy = list.CreatedBy == null ? null : $"{list.CreatedBy.FirstName} {list.CreatedBy.LastName}".Trim(),
            ReviewStatus = list.Status == ApprovedVendorListStatus.Pending ? OpenReviewStatus(review) : null,
            ReviewVersion = review?.Version,
            PreparedBySignatory = Sign(people, list.CreatedById, list.CreatedAt),
            CheckedBy = Sign(people, confirmed?.ReviewedById, confirmed?.ReviewedAt),
            ApprovedBy = Sign(people, approver.UserId, approver.At),
            Lines = list.Lines.OrderBy(line => line.Order).Select(line => new ApprovedVendorListLineDto
            {
                Id = line.Id, Order = line.Order, MaterialId = line.MaterialId, MaterialCode = line.Material?.Code,
                MaterialName = line.Material?.Name, ManufacturerId = line.ManufacturerId,
                ManufacturerName = line.Manufacturer?.Name, ManufacturerAddress = line.Manufacturer?.Address,
                SupplierId = line.SupplierId, SupplierName = line.Supplier?.Name,
                QuantityPerPack = line.QuantityPerPack, ApprovalDate = line.ApprovalDate, ValidityDate = line.ValidityDate,
                Lapsed = ApprovedVendorRules.IsLapsed(line.ValidityDate, today),
            }).ToList(),
        };
    }

    /// <summary>The manufacturers mapped to a raw material, each with the suppliers mapped to supply it from them.</summary>
    public async Task<Result<List<ApprovedVendorMappingDto>>> Mappings(Guid materialId)
    {
        if (!await context.Materials.AnyAsync(item => item.Id == materialId && item.Kind == MaterialKind.Raw))
            return Error.NotFound("ApprovedVendorList.Material", "Raw material not found.");
        var manufacturers = await context.ManufacturerMaterials.AsNoTracking()
            .Where(item => item.MaterialId == materialId && item.Manufacturer.DeletedAt == null)
            .Select(item => new { item.ManufacturerId, item.Manufacturer.Name, item.Manufacturer.Address, item.QuantityPerPackOption })
            .ToListAsync();
        var suppliers = await context.SupplierManufacturers.AsNoTracking()
            .Where(item => item.MaterialId == materialId && item.Supplier.DeletedAt == null && item.Manufacturer.DeletedAt == null)
            .Select(item => new { item.ManufacturerId, item.Manufacturer.Name, item.Manufacturer.Address, item.SupplierId, SupplierName = item.Supplier.Name })
            .ToListAsync();
        var result = manufacturers.GroupBy(item => item.ManufacturerId).Select(group => new ApprovedVendorMappingDto
        {
            ManufacturerId = group.Key, ManufacturerName = group.First().Name, ManufacturerAddress = group.First().Address,
            QuantityPerPack = group.Select(item => item.QuantityPerPackOption)
                .FirstOrDefault(option => option is { Type: DOMAIN.Entities.Procurement.Manufacturers.QuantityType.Single } && option.Values.Count == 1)?.Values[0],
        }).ToList();
        // A supplier mapping to a manufacturer that has no material mapping of its own still names a manufacturer of the material.
        foreach (var orphan in suppliers.Where(item => result.All(entry => entry.ManufacturerId != item.ManufacturerId))
                     .GroupBy(item => item.ManufacturerId))
            result.Add(new ApprovedVendorMappingDto { ManufacturerId = orphan.Key, ManufacturerName = orphan.First().Name, ManufacturerAddress = orphan.First().Address });
        foreach (var entry in result)
            entry.Suppliers = suppliers.Where(item => item.ManufacturerId == entry.ManufacturerId)
                .GroupBy(item => item.SupplierId)
                .Select(group => new ApprovedVendorMappedSupplierDto { SupplierId = group.Key, SupplierName = group.First().SupplierName })
                .OrderBy(item => item.SupplierName).ToList();
        return result.OrderBy(item => item.ManufacturerName).ToList();
    }

    public Task<List<ApprovedVendorEligibilityDto>> Eligibility(ApprovedVendorEligibilityRequest request) =>
        ApprovedVendorRules.Eligibility(context, request?.MaterialIds ?? [], DateTime.UtcNow.Date);

    private static Error NotFound => Error.NotFound("ApprovedVendorList.NotFound", "Approved vendor list not found.");

    private static DocumentReviewStatus? OpenReviewStatus(DocumentReview review) =>
        review is { Status: DocumentReviewStatus.AwaitingReview or DocumentReviewStatus.Returned } ? review.Status : null;

    private async Task<Dictionary<Guid, ApprovedVendorListSignatoryDto>> Signatories(IEnumerable<Guid?> ids)
    {
        var wanted = ids.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var users = await context.Users.IgnoreQueryFilters().AsNoTracking().Where(user => wanted.Contains(user.Id))
            .Select(user => new { user.Id, Name = (user.FirstName + " " + user.LastName).Trim(), user.Signature })
            .ToListAsync();
        var designations = await context.Employees.IgnoreQueryFilters().AsNoTracking()
            .Where(employee => employee.UserId.HasValue && wanted.Contains(employee.UserId.Value) && employee.Designation != null)
            .Select(employee => new { UserId = employee.UserId!.Value, employee.Designation.Name })
            .ToListAsync();
        return users.ToDictionary(user => user.Id, user => new ApprovedVendorListSignatoryDto
        {
            UserId = user.Id, Name = user.Name, Signature = user.Signature,
            Designation = designations.FirstOrDefault(item => item.UserId == user.Id)?.Name,
        });
    }

    private static ApprovedVendorListSignatoryDto Sign(Dictionary<Guid, ApprovedVendorListSignatoryDto> people, Guid? id, DateTime? at)
    {
        if (!id.HasValue || !people.TryGetValue(id.Value, out var person)) return null;
        return new ApprovedVendorListSignatoryDto
            { UserId = person.UserId, Name = person.Name, Designation = person.Designation, Signature = person.Signature, SignedAt = at };
    }
}
