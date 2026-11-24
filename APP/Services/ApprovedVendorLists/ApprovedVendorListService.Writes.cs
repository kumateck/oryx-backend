using System.Data;
using APP.Services.Authorization;
using APP.Services.Reviews;
using APP.Services.Transactions;
using APP.Utils;
using DOMAIN.Entities.ApprovedVendorLists;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.Reviews;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.ApprovedVendorLists;

public sealed partial class ApprovedVendorListService
{
    /// <summary>The first list. Once a list is approved, changes are made as a new revision.</summary>
    public async Task<Result<Guid>> Create(SaveApprovedVendorListRequest request, Guid actorId)
    {
        await using var transaction = await Begin();
        var grants = await CurrentPermissionSet.Load(context, actorId);
        if (!grants.Contains(PermissionKeys.CanCreateApprovedVendorList)) return Refused(PermissionKeys.CanCreateApprovedVendorList);
        if (await context.ApprovedVendorLists.AnyAsync(item => item.Status == ApprovedVendorListStatus.Pending))
            return Error.Conflict("ApprovedVendorList.Pending", "A list is already being prepared. Finish or correct it first.");
        if (await context.ApprovedVendorLists.AnyAsync(item => item.Status == ApprovedVendorListStatus.Approved))
            return Error.Conflict("ApprovedVendorList.Exists", "There is an approved list already. Create a new revision of it instead.");
        if (request.SupersedesRevision is { } supersedes && supersedes >= request.RevisionNumber)
            return Error.Validation("ApprovedVendorList.Revision", "The revision number must be higher than the revision it supersedes.");
        var list = new ApprovedVendorList
        {
            Id = Guid.NewGuid(), AnnexureNumber = request.AnnexureNumber.Trim(), RevisionNumber = request.RevisionNumber,
            SupersedesRevision = request.SupersedesRevision, Status = ApprovedVendorListStatus.Pending,
            CreatedById = actorId, CreatedAt = DateTime.UtcNow,
        };
        var lines = await BuildLines(request.Lines, grants, actorId);
        if (lines.IsFailure) return lines.Errors.First();
        list.Lines = lines.Value;
        context.ApprovedVendorLists.Add(list);
        await context.SaveChangesAsync();
        // The review opens here (the list is always checked); approvals start when it is confirmed.
        await approvals.CreateInitialApprovalsAsync(ModelType, list.Id, actorId);
        if (transaction is not null) await transaction.CommitAsync();
        return list.Id;
    }

    /// <summary>A new revision of the current approved list. It supersedes that list when it is approved.</summary>
    public async Task<Result<Guid>> Revise(ReviseApprovedVendorListRequest request, Guid actorId)
    {
        await using var transaction = await Begin();
        var grants = await CurrentPermissionSet.Load(context, actorId);
        if (!grants.Contains(PermissionKeys.CanReviseApprovedVendorList)) return Refused(PermissionKeys.CanReviseApprovedVendorList);
        var current = await context.ApprovedVendorLists.FirstOrDefaultAsync(item => item.Status == ApprovedVendorListStatus.Approved);
        if (current is null)
            return Error.Conflict("ApprovedVendorList.NoCurrent", "There is no approved list to revise.");
        if (await context.ApprovedVendorLists.AnyAsync(item => item.Status == ApprovedVendorListStatus.Pending))
            return Error.Conflict("ApprovedVendorList.Pending", "A revision is already being prepared. Finish or correct it first.");
        if (request.RevisionNumber <= current.RevisionNumber)
            return Error.Validation("ApprovedVendorList.Revision", $"The revision number must be higher than {current.RevisionNumber}.");
        var list = new ApprovedVendorList
        {
            Id = Guid.NewGuid(), AnnexureNumber = request.AnnexureNumber.Trim(), RevisionNumber = request.RevisionNumber,
            SupersedesRevision = current.RevisionNumber, PreviousRevisionId = current.Id,
            Status = ApprovedVendorListStatus.Pending, CreatedById = actorId, CreatedAt = DateTime.UtcNow,
        };
        var lines = await BuildLines(request.Lines, grants, actorId);
        if (lines.IsFailure) return lines.Errors.First();
        list.Lines = lines.Value;
        context.ApprovedVendorLists.Add(list);
        await context.SaveChangesAsync();
        await approvals.CreateInitialApprovalsAsync(ModelType, list.Id, actorId);
        if (transaction is not null) await transaction.CommitAsync();
        return list.Id;
    }

    /// <summary>Corrects a list while it is in review (awaiting review or returned). The edit is recorded on the review.</summary>
    public async Task<Result> Update(Guid id, SaveApprovedVendorListRequest request, Guid actorId)
    {
        await using var transaction = await Begin();
        var grants = await CurrentPermissionSet.Load(context, actorId);
        if (!grants.Contains(PermissionKeys.CanEditApprovedVendorList)) return Refused(PermissionKeys.CanEditApprovedVendorList);
        var list = await context.ApprovedVendorLists.Include(item => item.Lines).FirstOrDefaultAsync(item => item.Id == id);
        if (list is null) return NotFound;
        var review = await DocumentReviewGate.LatestAsync(context, ModelType, id);
        if (list.Status != ApprovedVendorListStatus.Pending
            || review is not { Status: DocumentReviewStatus.AwaitingReview or DocumentReviewStatus.Returned })
            return Error.Conflict("ApprovedVendorList.NotInReview", "Only a list in review can be corrected. Revise the approved list instead.");
        if (request.ReviewVersion is { } version && version != review.Version)
            return Error.Conflict("ApprovedVendorList.Stale", "The list changed since you opened it. Reload it and try again.");
        if (list.PreviousRevisionId.HasValue)
        {
            var previous = await context.ApprovedVendorLists.IgnoreQueryFilters().AsNoTracking().Where(item => item.Id == list.PreviousRevisionId)
                .Select(item => item.RevisionNumber).FirstAsync();
            if (request.RevisionNumber <= previous)
                return Error.Validation("ApprovedVendorList.Revision", $"The revision number must be higher than {previous}.");
        }
        else
        {
            if (request.SupersedesRevision is { } supersedes && supersedes >= request.RevisionNumber)
                return Error.Validation("ApprovedVendorList.Revision", "The revision number must be higher than the revision it supersedes.");
            list.SupersedesRevision = request.SupersedesRevision;
        }
        var lines = await BuildLines(request.Lines, grants, actorId);
        if (lines.IsFailure) return lines.Errors.First();
        list.AnnexureNumber = request.AnnexureNumber.Trim();
        list.RevisionNumber = request.RevisionNumber;
        list.UpdatedAt = DateTime.UtcNow;
        list.LastUpdatedById = actorId;
        context.ApprovedVendorListLines.RemoveRange(list.Lines);
        await context.SaveChangesAsync();
        foreach (var line in lines.Value) line.ApprovedVendorListId = list.Id;
        context.ApprovedVendorListLines.AddRange(lines.Value);
        await context.SaveChangesAsync();
        await DocumentReviewGate.NoteEditAsync(context, ModelType, id, actorId);
        if (transaction is not null) await transaction.CommitAsync();
        return Result.Success();
    }

    /// <summary>Removes the selected revision from use while retaining its document and revision history.</summary>
    public Task<Result> Delete(Guid id, Guid actorId) => ReferenceMaintenanceTransaction.Run(context, async () =>
    {
        var grants = await CurrentPermissionSet.Load(context, actorId);
        if (!grants.Contains(PermissionKeys.CanDeleteApprovedVendorList)) return Refused(PermissionKeys.CanDeleteApprovedVendorList);
        var list = await context.ApprovedVendorLists.SingleOrDefaultAsync(item => item.Id == id);
        if (list is null) return NotFound;

        grants = await CurrentPermissionSet.Load(context, actorId);
        if (!grants.Contains(PermissionKeys.CanDeleteApprovedVendorList)) return Refused(PermissionKeys.CanDeleteApprovedVendorList);
        list.DeletedAt = DateTime.UtcNow;
        list.LastDeletedById = actorId;
        await context.SaveChangesAsync();
        return Result.Success();
    });

    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> Begin() =>
        context.Database.IsRelational() ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;

    private static Error Refused(string key) =>
        Error.Forbidden("ApprovedVendorList.Permission", $"You need {key} for this.");

    /// <summary>
    /// Checks the lines and resolves their manufacturers and suppliers: a new name creates the record, and a
    /// manufacturer or supplier not yet mapped to the material is mapped to it. Each needs the existing key for
    /// that change, checked against the grants loaded for this write.
    /// </summary>
    private async Task<Result<List<ApprovedVendorListLine>>> BuildLines(List<SaveApprovedVendorListLineRequest> requested,
        HashSet<string> grants, Guid actorId)
    {
        if (requested is null || requested.Count == 0)
            return Error.Validation("ApprovedVendorList.Lines", "Add at least one material.");
        var materialIds = requested.Select(line => line.MaterialId).Distinct().ToList();
        var materials = await context.Materials.Where(item => materialIds.Contains(item.Id))
            .Select(item => new { item.Id, item.Kind, item.Name }).ToDictionaryAsync(item => item.Id);
        var newManufacturers = new Dictionary<string, Manufacturer>(StringComparer.OrdinalIgnoreCase);
        var newSuppliers = new Dictionary<string, Supplier>(StringComparer.OrdinalIgnoreCase);
        var lines = new List<ApprovedVendorListLine>();
        for (var index = 0; index < requested.Count; index++)
        {
            var row = requested[index];
            var at = $"Line {index + 1}";
            if (!materials.TryGetValue(row.MaterialId, out var material))
                return Error.Validation("ApprovedVendorList.Material", $"{at}: choose a material.");
            if (material.Kind != MaterialKind.Raw)
                return Error.Validation("ApprovedVendorList.Material", $"{at}: {material.Name} is not a raw material.");
            if (row.QuantityPerPack <= 0)
                return Error.Validation("ApprovedVendorList.Quantity", $"{at}: enter the quantity per pack in kg.");
            if (row.ApprovalDate == default || row.ValidityDate == default)
                return Error.Validation("ApprovedVendorList.Dates", $"{at}: enter the approval and validity months.");
            if (ApprovedVendorRules.Month(row.ValidityDate) < ApprovedVendorRules.Month(row.ApprovalDate))
                return Error.Validation("ApprovedVendorList.Dates", $"{at}: the validity month cannot be before the approval month.");

            var manufacturer = await ResolveManufacturer(row, at, newManufacturers, grants, actorId);
            if (manufacturer.IsFailure) return manufacturer.Errors.First();
            var supplier = await ResolveSupplier(row, at, newSuppliers, grants, actorId);
            if (supplier.IsFailure) return supplier.Errors.First();
            if (lines.Any(line => line.MaterialId == row.MaterialId && line.ManufacturerId == manufacturer.Value.Id && line.SupplierId == supplier.Value.Id))
                return Error.Validation("ApprovedVendorList.Duplicate", $"{at}: this material, manufacturer and supplier are already on the list.");

            var mapped = await MapToMaterial(row, manufacturer.Value, supplier.Value, at, grants, actorId);
            if (mapped.IsFailure) return mapped.Errors.First();
            lines.Add(new ApprovedVendorListLine
            {
                Id = Guid.NewGuid(), Order = index + 1, MaterialId = row.MaterialId,
                ManufacturerId = manufacturer.Value.Id, SupplierId = supplier.Value.Id, QuantityPerPack = row.QuantityPerPack,
                ApprovalDate = ApprovedVendorRules.Month(row.ApprovalDate), ValidityDate = ApprovedVendorRules.Month(row.ValidityDate),
                CreatedById = actorId, CreatedAt = DateTime.UtcNow,
            });
        }
        return lines;
    }

    private async Task<Result<Manufacturer>> ResolveManufacturer(SaveApprovedVendorListLineRequest row, string at,
        Dictionary<string, Manufacturer> created, HashSet<string> grants, Guid actorId)
    {
        if (row.ManufacturerId.HasValue == (row.NewManufacturer is not null))
            return Error.Validation("ApprovedVendorList.Manufacturer", $"{at}: choose a manufacturer or enter a new one.");
        if (row.ManufacturerId is { } id)
        {
            var existing = await context.Manufacturers.FirstOrDefaultAsync(item => item.Id == id);
            return existing is null ? Error.Validation("ApprovedVendorList.Manufacturer", $"{at}: the manufacturer was not found.") : existing;
        }
        var name = row.NewManufacturer.Name?.Trim();
        if (string.IsNullOrEmpty(name)) return Error.Validation("ApprovedVendorList.Manufacturer", $"{at}: enter the manufacturer's name.");
        if (created.TryGetValue(name, out var again)) return again;
        if (!grants.Contains(PermissionKeys.CanCreateManufacturer)) return Refused(PermissionKeys.CanCreateManufacturer);
        if (await context.Manufacturers.AnyAsync(item => item.Name.ToLower() == name.ToLower()))
            return Error.Validation("ApprovedVendorList.Manufacturer", $"{at}: a manufacturer named {name} already exists. Choose it from the list.");
        var manufacturer = new Manufacturer
        {
            Id = Guid.NewGuid(), Name = name, Address = row.NewManufacturer.Address?.Trim(),
            CreatedById = actorId, CreatedAt = DateTime.UtcNow,
        };
        context.Manufacturers.Add(manufacturer);
        created[name] = manufacturer;
        return manufacturer;
    }

    private async Task<Result<Supplier>> ResolveSupplier(SaveApprovedVendorListLineRequest row, string at,
        Dictionary<string, Supplier> created, HashSet<string> grants, Guid actorId)
    {
        if (row.SupplierId.HasValue == (row.NewSupplier is not null))
            return Error.Validation("ApprovedVendorList.Supplier", $"{at}: choose a supplier or enter a new one.");
        if (row.SupplierId is { } id)
        {
            var existing = await context.Suppliers.FirstOrDefaultAsync(item => item.Id == id);
            return existing is null ? Error.Validation("ApprovedVendorList.Supplier", $"{at}: the supplier was not found.") : existing;
        }
        var name = row.NewSupplier.Name?.Trim();
        if (string.IsNullOrEmpty(name)) return Error.Validation("ApprovedVendorList.Supplier", $"{at}: enter the supplier's name.");
        if (created.TryGetValue(name, out var again)) return again;
        if (!grants.Contains(PermissionKeys.CanCreateVendor)) return Refused(PermissionKeys.CanCreateVendor);
        if (await context.Suppliers.AnyAsync(item => item.Name.ToLower() == name.ToLower()))
            return Error.Validation("ApprovedVendorList.Supplier", $"{at}: a supplier named {name} already exists. Choose it from the list.");
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(), Name = name, Address = row.NewSupplier.Address?.Trim(), Type = row.NewSupplier.Type,
            Status = SupplierStatus.New, CreatedById = actorId, CreatedAt = DateTime.UtcNow,
        };
        context.Suppliers.Add(supplier);
        created[name] = supplier;
        return supplier;
    }

    /// <summary>Adds the material mappings a line needs that do not exist yet.</summary>
    private async Task<Result> MapToMaterial(SaveApprovedVendorListLineRequest row, Manufacturer manufacturer, Supplier supplier,
        string at, HashSet<string> grants, Guid actorId)
    {
        var manufacturerIsNew = row.NewManufacturer is not null;
        var supplierIsNew = row.NewSupplier is not null;
        var manufacturerMapped = context.ManufacturerMaterials.Local.Any(item => item.ManufacturerId == manufacturer.Id && item.MaterialId == row.MaterialId && item.DeletedAt == null)
            || (!manufacturerIsNew && await context.ManufacturerMaterials.AnyAsync(item => item.ManufacturerId == manufacturer.Id && item.MaterialId == row.MaterialId));
        if (!manufacturerMapped)
        {
            var key = manufacturerIsNew ? PermissionKeys.CanCreateManufacturer : PermissionKeys.CanUpdateManufacturerDetails;
            if (!grants.Contains(key)) return Error.Forbidden("ApprovedVendorList.Permission",
                $"{at}: {manufacturer.Name} is not mapped to this material yet. Mapping it needs {key}.");
            context.ManufacturerMaterials.Add(new ManufacturerMaterial
            {
                Id = Guid.NewGuid(), ManufacturerId = manufacturer.Id, MaterialId = row.MaterialId, QuantityType = QuantityType.Single,
                QuantityPerPackOption = new QuantityPerPackOption { Type = QuantityType.Single, Values = [row.QuantityPerPack] },
                CreatedById = actorId, CreatedAt = DateTime.UtcNow,
            });
        }
        var supplierMapped = context.SupplierManufacturers.Local.Any(item => item.SupplierId == supplier.Id && item.ManufacturerId == manufacturer.Id && item.MaterialId == row.MaterialId && item.DeletedAt == null)
            || (!supplierIsNew && !manufacturerIsNew && await context.SupplierManufacturers.AnyAsync(item =>
                item.SupplierId == supplier.Id && item.ManufacturerId == manufacturer.Id && item.MaterialId == row.MaterialId));
        if (!supplierMapped)
        {
            var key = supplierIsNew ? PermissionKeys.CanCreateVendor : PermissionKeys.CanUpdateVendorDetails;
            if (!grants.Contains(key)) return Error.Forbidden("ApprovedVendorList.Permission",
                $"{at}: {supplier.Name} is not mapped to supply this material from {manufacturer.Name} yet. Mapping it needs {key}.");
            context.SupplierManufacturers.Add(new SupplierManufacturer
            {
                Id = Guid.NewGuid(), SupplierId = supplier.Id, ManufacturerId = manufacturer.Id, MaterialId = row.MaterialId,
                CreatedById = actorId, CreatedAt = DateTime.UtcNow,
            });
        }
        return Result.Success();
    }
}
