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
    /// <summary>A list being prepared, as a draft or in review/approval. Only one at a time.</summary>
    private Task<bool> InPreparation() => context.ApprovedVendorLists.AnyAsync(item =>
        item.Status == ApprovedVendorListStatus.Draft || item.Status == ApprovedVendorListStatus.Pending);

    /// <summary>Starts the first list as a draft. Once a list is approved, changes start as a new revision.</summary>
    public Task<Result<Guid>> Create(SaveApprovedVendorListRequest request, Guid actorId) =>
        ReferenceMaintenanceTransaction.Run<Guid>(context, async () =>
    {
        var grants = await CurrentPermissionSet.Load(context, actorId);
        if (!grants.Contains(PermissionKeys.CanCreateApprovedVendorList)) return Refused(PermissionKeys.CanCreateApprovedVendorList);
        if (await InPreparation())
            return Error.Conflict("ApprovedVendorList.Pending", "A list is already being prepared. Continue, submit or discard it first.");
        if (await context.ApprovedVendorLists.AnyAsync(item => item.Status == ApprovedVendorListStatus.Approved))
            return Error.Conflict("ApprovedVendorList.Exists", "There is an approved list already. Create a new revision of it instead.");
        if (request.SupersedesRevision is { } supersedes && supersedes >= request.RevisionNumber)
            return Error.Validation("ApprovedVendorList.Revision", "The revision number must be higher than the revision it supersedes.");
        var lines = await DraftLines(request.Lines, actorId);
        if (lines.IsFailure) return lines.Errors.First();
        var list = new ApprovedVendorList
        {
            Id = Guid.NewGuid(), AnnexureNumber = request.AnnexureNumber.Trim(), RevisionNumber = request.RevisionNumber,
            SupersedesRevision = request.SupersedesRevision, Status = ApprovedVendorListStatus.Draft,
            CreatedById = actorId, CreatedAt = DateTime.UtcNow, Lines = lines.Value,
        };
        if (!(await CurrentPermissionSet.Load(context, actorId)).Contains(PermissionKeys.CanCreateApprovedVendorList))
            return Refused(PermissionKeys.CanCreateApprovedVendorList);
        context.ApprovedVendorLists.Add(list);
        await context.SaveChangesAsync();
        return list.Id;
    });

    /// <summary>
    /// Starts a new revision of the current approved list as a draft. The current list stays in force, and keeps
    /// restricting procurement, until the revision is approved.
    /// </summary>
    public Task<Result<Guid>> Revise(ReviseApprovedVendorListRequest request, Guid actorId) =>
        ReferenceMaintenanceTransaction.Run<Guid>(context, async () =>
    {
        var grants = await CurrentPermissionSet.Load(context, actorId);
        if (!grants.Contains(PermissionKeys.CanReviseApprovedVendorList)) return Refused(PermissionKeys.CanReviseApprovedVendorList);
        var current = await context.ApprovedVendorLists.FirstOrDefaultAsync(item => item.Status == ApprovedVendorListStatus.Approved);
        if (current is null)
            return Error.Conflict("ApprovedVendorList.NoCurrent", "There is no approved list to revise.");
        if (await InPreparation())
            return Error.Conflict("ApprovedVendorList.Pending", "A revision is already being prepared. Continue, submit or discard it first.");
        if (request.RevisionNumber <= current.RevisionNumber)
            return Error.Validation("ApprovedVendorList.Revision", $"The revision number must be higher than {current.RevisionNumber}.");
        var lines = await DraftLines(request.Lines, actorId);
        if (lines.IsFailure) return lines.Errors.First();
        var list = new ApprovedVendorList
        {
            Id = Guid.NewGuid(), AnnexureNumber = request.AnnexureNumber.Trim(), RevisionNumber = request.RevisionNumber,
            SupersedesRevision = current.RevisionNumber, PreviousRevisionId = current.Id,
            Status = ApprovedVendorListStatus.Draft, CreatedById = actorId, CreatedAt = DateTime.UtcNow, Lines = lines.Value,
        };
        if (!(await CurrentPermissionSet.Load(context, actorId)).Contains(PermissionKeys.CanReviseApprovedVendorList))
            return Refused(PermissionKeys.CanReviseApprovedVendorList);
        context.ApprovedVendorLists.Add(list);
        await context.SaveChangesAsync();
        return list.Id;
    });

    /// <summary>Saves a draft's header and lines, as often as needed. Nothing it names as new is created yet.</summary>
    public Task<Result> SaveDraft(Guid id, SaveApprovedVendorListDraftRequest request, Guid actorId) =>
        ReferenceMaintenanceTransaction.Run(context, async () =>
    {
        var list = await context.ApprovedVendorLists.Include(item => item.Lines).FirstOrDefaultAsync(item => item.Id == id);
        if (list is null) return NotFound;
        var grants = await CurrentPermissionSet.Load(context, actorId);
        if (!grants.Contains(PermissionKeys.CanEditApprovedVendorListDraft)) return Refused(PermissionKeys.CanEditApprovedVendorListDraft);
        var state = DraftState(list, request.DraftVersion);
        if (state is not null) return state;
        var header = await CheckHeader(list, request.RevisionNumber, request.SupersedesRevision);
        if (header is not null) return header;
        var lines = await DraftLines(request.Lines, actorId);
        if (lines.IsFailure) return lines.Errors.First();
        list.AnnexureNumber = request.AnnexureNumber.Trim();
        list.RevisionNumber = request.RevisionNumber;
        if (!list.PreviousRevisionId.HasValue) list.SupersedesRevision = request.SupersedesRevision;
        var saved = await ReplaceDraftLines(list, lines.Value, actorId, PermissionKeys.CanEditApprovedVendorListDraft);
        if (saved.IsFailure) return saved;
        return Result.Success();
    });

    /// <summary>
    /// Submits a draft: it becomes the fixed version and goes for review, then approval. Manufacturers and suppliers
    /// it names as new are created and mapped now, with the keys that needs.
    /// </summary>
    public Task<Result> Submit(Guid id, SubmitApprovedVendorListRequest request, Guid actorId) =>
        ReferenceMaintenanceTransaction.Run(context, async () =>
    {
        var grants = await CurrentPermissionSet.Load(context, actorId);
        if (!grants.Contains(PermissionKeys.CanSubmitApprovedVendorList)) return Refused(PermissionKeys.CanSubmitApprovedVendorList);
        var list = await context.ApprovedVendorLists.Include(item => item.Lines).FirstOrDefaultAsync(item => item.Id == id);
        if (list is null) return NotFound;
        var state = DraftState(list, request?.DraftVersion);
        if (state is not null) return state;
        var requested = list.Lines.Where(line => !line.DeletedAt.HasValue).OrderBy(line => line.Order).Select(AsRequest).ToList();
        if (requested.Count == 0)
            return Error.Validation("ApprovedVendorList.Lines", "Add at least one material before submitting.");
        var header = await CheckHeader(list, list.RevisionNumber, list.SupersedesRevision);
        if (header is not null) return header;
        if (!list.PreviousRevisionId.HasValue && await context.ApprovedVendorLists.AnyAsync(item => item.Status == ApprovedVendorListStatus.Approved))
            return Error.Conflict("ApprovedVendorList.Exists", "A list was approved since this draft started. Discard it and revise the approved list.");
        var lines = await BuildLines(requested, grants, actorId);
        if (lines.IsFailure) return lines.Errors.First();
        list.Status = ApprovedVendorListStatus.Pending;
        var saved = await ReplaceDraftLines(list, lines.Value, actorId, PermissionKeys.CanSubmitApprovedVendorList);
        if (saved.IsFailure) return saved;
        if (!(await CurrentPermissionSet.Load(context, actorId)).Contains(PermissionKeys.CanSubmitApprovedVendorList))
            return Refused(PermissionKeys.CanSubmitApprovedVendorList);
        // The review opens here (the list is always checked); approvals start when it is confirmed.
        await approvals.CreateInitialApprovalsAsync(ModelType, list.Id, actorId);
        return Result.Success();
    });

    private static Error? DraftState(ApprovedVendorList list, int? version)
    {
        if (list.Status != ApprovedVendorListStatus.Draft)
            return Error.Conflict("ApprovedVendorList.NotDraft", "This list has been submitted. Correct it through its review instead.");
        if (version != list.DraftVersion)
            return Error.Conflict("ApprovedVendorList.Stale", "The draft was saved by someone else since you opened it. Reload it and try again.");
        return null;
    }

    /// <summary>A revision's number must stay above the list it revises; a first list's above what it supersedes.</summary>
    private async Task<Error?> CheckHeader(ApprovedVendorList list, int revisionNumber, int? supersedes)
    {
        if (list.PreviousRevisionId.HasValue)
        {
            var previous = await context.ApprovedVendorLists.IgnoreQueryFilters().AsNoTracking().Where(item => item.Id == list.PreviousRevisionId)
                .Select(item => new { item.RevisionNumber, item.Status, item.DeletedAt }).FirstOrDefaultAsync();
            if (previous is null || previous.Status != ApprovedVendorListStatus.Approved ||
                (previous.DeletedAt.HasValue && await context.ApprovedVendorLists.AnyAsync(item => item.Status == ApprovedVendorListStatus.Approved)))
                return Error.Conflict("ApprovedVendorList.Current", "The list this draft revises is no longer the current list. Discard it and revise the current list.");
            if (revisionNumber <= previous.RevisionNumber)
                return Error.Validation("ApprovedVendorList.Revision", $"The revision number must be higher than {previous.RevisionNumber}.");
        }
        else if (supersedes is { } before && before >= revisionNumber)
            return Error.Validation("ApprovedVendorList.Revision", "The revision number must be higher than the revision it supersedes.");
        return null;
    }

    private async Task<Result> ReplaceDraftLines(ApprovedVendorList list, List<ApprovedVendorListLine> lines, Guid actorId, string key)
    {
        if (!(await CurrentPermissionSet.Load(context, actorId)).Contains(key)) return Refused(key);
        list.UpdatedAt = DateTime.UtcNow;
        list.LastUpdatedById = actorId;
        list.DraftVersion++;
        context.ApprovedVendorListLines.RemoveRange(list.Lines.Where(line => !line.DeletedAt.HasValue));
        await context.SaveChangesAsync();
        if (!(await CurrentPermissionSet.Load(context, actorId)).Contains(key)) return Refused(key);
        foreach (var line in lines) line.ApprovedVendorListId = list.Id;
        context.ApprovedVendorListLines.AddRange(lines);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    private static SaveApprovedVendorListLineRequest AsRequest(ApprovedVendorListLine line) => new()
    {
        MaterialId = line.MaterialId, ManufacturerId = line.ManufacturerId, SupplierId = line.SupplierId,
        NewManufacturer = line.ManufacturerId.HasValue ? null : new NewApprovedManufacturerRequest { Name = line.NewManufacturerName, Address = line.NewManufacturerAddress },
        NewSupplier = line.SupplierId.HasValue ? null
            : new NewApprovedSupplierRequest { Name = line.NewSupplierName, Address = line.NewSupplierAddress, Type = line.NewSupplierType ?? SupplierType.Local },
        QuantityPerPack = line.QuantityPerPack, ApprovalDate = line.ApprovalDate, ValidityDate = line.ValidityDate,
    };

    /// <summary>A draft's lines: checked as on submit, but new manufacturers and suppliers are only named, not created.</summary>
    private async Task<Result<List<ApprovedVendorListLine>>> DraftLines(List<SaveApprovedVendorListLineRequest> requested, Guid actorId)
    {
        requested ??= [];
        var materialIds = requested.Select(line => line.MaterialId).Distinct().ToList();
        var materials = await context.Materials.Where(item => materialIds.Contains(item.Id))
            .Select(item => new { item.Id, item.Kind, item.Name }).ToDictionaryAsync(item => item.Id);
        var lines = new List<ApprovedVendorListLine>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
            // A line stops being usable on the first day of its validity month, so that month must come after the approval month.
            if (ApprovedVendorRules.Month(row.ValidityDate) <= ApprovedVendorRules.Month(row.ApprovalDate))
                return Error.Validation("ApprovedVendorList.Dates", $"{at}: the validity month must be after the approval month.");
            if (row.ManufacturerId.HasValue == (row.NewManufacturer is not null))
                return Error.Validation("ApprovedVendorList.Manufacturer", $"{at}: choose a manufacturer or enter a new one.");
            if (row.SupplierId.HasValue == (row.NewSupplier is not null))
                return Error.Validation("ApprovedVendorList.Supplier", $"{at}: choose a supplier or enter a new one.");
            if (row.ManufacturerId is { } manufacturerId && !await context.Manufacturers.AnyAsync(item => item.Id == manufacturerId))
                return Error.Validation("ApprovedVendorList.Manufacturer", $"{at}: the manufacturer was not found.");
            if (row.SupplierId is { } supplierId && !await context.Suppliers.AnyAsync(item => item.Id == supplierId))
                return Error.Validation("ApprovedVendorList.Supplier", $"{at}: the supplier was not found.");
            var newManufacturer = row.NewManufacturer?.Name?.Trim();
            var newSupplier = row.NewSupplier?.Name?.Trim();
            if (row.NewManufacturer is not null && string.IsNullOrEmpty(newManufacturer))
                return Error.Validation("ApprovedVendorList.Manufacturer", $"{at}: enter the manufacturer's name.");
            if (row.NewSupplier is not null && string.IsNullOrEmpty(newSupplier))
                return Error.Validation("ApprovedVendorList.Supplier", $"{at}: enter the supplier's name.");
            if (!seen.Add($"{row.MaterialId}|{row.ManufacturerId?.ToString() ?? "new:" + newManufacturer}|{row.SupplierId?.ToString() ?? "new:" + newSupplier}"))
                return Error.Validation("ApprovedVendorList.Duplicate", $"{at}: this material, manufacturer and supplier are already on the list.");
            lines.Add(new ApprovedVendorListLine
            {
                Id = Guid.NewGuid(), Order = index + 1, MaterialId = row.MaterialId,
                ManufacturerId = row.ManufacturerId, NewManufacturerName = newManufacturer, NewManufacturerAddress = row.NewManufacturer?.Address?.Trim(),
                SupplierId = row.SupplierId, NewSupplierName = newSupplier, NewSupplierAddress = row.NewSupplier?.Address?.Trim(),
                NewSupplierType = row.NewSupplier?.Type, QuantityPerPack = row.QuantityPerPack,
                ApprovalDate = ApprovedVendorRules.Month(row.ApprovalDate), ValidityDate = ApprovedVendorRules.Month(row.ValidityDate),
                CreatedById = actorId, CreatedAt = DateTime.UtcNow,
            });
        }
        return lines;
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
    public Task<Result> Delete(Guid id, Guid actorId, string reason = null) => ReferenceMaintenanceTransaction.Run(context, async () =>
    {
        var grants = await CurrentPermissionSet.Load(context, actorId);
        if (!grants.Contains(PermissionKeys.CanDeleteApprovedVendorList)) return Refused(PermissionKeys.CanDeleteApprovedVendorList);
        var list = await context.ApprovedVendorLists.SingleOrDefaultAsync(item => item.Id == id);
        if (list is null) return NotFound;

        grants = await CurrentPermissionSet.Load(context, actorId);
        if (!grants.Contains(PermissionKeys.CanDeleteApprovedVendorList)) return Refused(PermissionKeys.CanDeleteApprovedVendorList);
        var why = reason?.Trim();
        if (list.Status == ApprovedVendorListStatus.Draft && string.IsNullOrEmpty(why))
            return Error.Validation("ApprovedVendorList.Reason", "Enter a reason for deleting the draft.");
        if (why?.Length > 1000)
            return Error.Validation("ApprovedVendorList.Reason", "Keep the reason under 1000 characters.");
        list.DeletedAt = DateTime.UtcNow;
        list.LastDeletedById = actorId;
        list.DeletionReason = why;
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
            // A line stops being usable on the first day of its validity month, so that month must come after the approval month.
            if (ApprovedVendorRules.Month(row.ValidityDate) <= ApprovedVendorRules.Month(row.ApprovalDate))
                return Error.Validation("ApprovedVendorList.Dates", $"{at}: the validity month must be after the approval month.");

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
