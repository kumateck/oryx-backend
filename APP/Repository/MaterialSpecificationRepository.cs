using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.MaterialSpecifications;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SHARED;
using SHARED.Requests;

namespace APP.Repository;

public class MaterialSpecificationRepository(ApplicationDbContext context, IMapper mapper)
    : IMaterialSpecificationRepository
{
    public async Task<Result<List<MaterialSpecificationMappingDto>>> CreateMaterialSpecification(
        CreateMaterialSpecificationRequest request
    )
    {
        if (request.MaterialIds == null || request.MaterialIds.Count == 0)
            return Error.Validation("Invalid.Materials", "At least one material is required.");

        var materials = await context
            .Materials.Where(m => request.MaterialIds.Contains(m.Id))
            .ToListAsync();

        if (materials.Count != request.MaterialIds.Count)
            return Error.Validation("Invalid.Material", "One or more materials are invalid.");

        if (request.DueDate < DateTime.UtcNow)
        {
            return Error.Validation(
                "MaterialSpecification.DueDate",
                "Due date must be greater than current date"
            );
        }

        // Fetch existing specifications for this spec number
        var existingSpecs = await context
            .MaterialSpecifications.AsSplitQuery()
            .Include(ms => ms.Material)
            .Where(ms => ms.SpecificationNumber == request.SpecificationNumber)
            .ToListAsync();

        var mappings = new List<MaterialSpecificationMappingDto>();

        foreach (var material in materials)
        {
            var alreadyExistsForMaterial = existingSpecs.Any(ms => ms.MaterialId == material.Id);

            if (alreadyExistsForMaterial)
            {
                return Error.Validation(
                    "MaterialSpecification.Exists",
                    $"Material '{material.Name}' already has this specification number."
                );
            }

            var materialSpec = mapper.Map<MaterialSpecification>(request);
            materialSpec.Reference = SpecificationReferenceHelper.FormatReference(request.Reference);
            materialSpec.MaterialId = material.Id;
            await context.MaterialSpecifications.AddAsync(materialSpec);
            mappings.Add(
                new MaterialSpecificationMappingDto
                {
                    MaterialId = material.Id,
                    SpecificationId = materialSpec.Id,
                }
            );
        }

        await context.SaveChangesAsync();
        return mappings;
    }

    public async Task<
        Result<Paginateable<IEnumerable<MaterialSpecificationDto>>>
    > GetMaterialSpecifications(
        int page,
        int pageSize,
        string searchQuery,
        MaterialKind materialKind,
        bool? isVerified = null
    )
    {
        var query = context
            .MaterialSpecifications.AsSplitQuery()
            .Include(ms => ms.Material)
            .Include(ms => ms.Form)
            .Include(ms => ms.CreatedBy)
            .Include(ms => ms.Response)
            .Where(ms => ms.Material.Kind == materialKind)
            .OrderBy(ms => ms.SpecificationNumber)
            .AsQueryable();

        if (isVerified.HasValue)
        {
            query = query.Where(p => p.IsVerified == isVerified.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(
                searchQuery,
                q => q.SpecificationNumber,
                q => q.Description,
                q => q.Material.Name,
                q => q.Material.Code
            );
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<MaterialSpecificationDto>
        );
    }

    public async Task<Result<MaterialSpecificationDto>> GetMaterialSpecification(Guid id)
    {
        var materialSpec = await context
            .MaterialSpecifications.IgnoreAutoIncludes()
            .IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(ms => ms.Material)
            .Include(ms => ms.Form)
                .ThenInclude(ps => ps.Sections.OrderBy(s => s.Order))
                    .ThenInclude(ps => ps.Fields)
                        .ThenInclude(ps => ps.Question)
                            .ThenInclude(q => q.Options)
            .Include(ps => ps.Form)
                .ThenInclude(ps => ps.Sections.OrderBy(s => s.Order))
                    .ThenInclude(ps => ps.Instrument)
                        .ThenInclude(ps => ps.QcEquipmentCategory)
            .Include(ms => ms.CreatedBy)
            .Include(m => m.Response)
                .ThenInclude(r => r.FormResponses)
                    .ThenInclude(r => r.FormField)
            .Include(ms => ms.FormSections)
            .FirstOrDefaultAsync(ps => ps.Id == id);

        return Result.Success(mapper.Map<MaterialSpecificationDto>(materialSpec));
    }

    public async Task<
        Result<Paginateable<IEnumerable<MaterialDto>>>
    > GetMaterialsNotLinkedToSpecification(
        int page,
        int pageSize,
        string searchQuery,
        MaterialKind? materialKind
    )
    {
        var materials = context
            .Materials.IgnoreQueryFilters()
            .Where(ps =>
                !ps.DeletedAt.HasValue
                && !context.MaterialSpecifications.Any(m => m.MaterialId == ps.Id)
            )
            .AsQueryable();

        if (materialKind.HasValue)
        {
            materials = materials.Where(ps => ps.Kind == materialKind.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            materials = materials.WhereSearch(searchQuery, ps => ps.Name, ps => ps.Code);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            materials,
            page,
            pageSize,
            mapper.Map<MaterialDto>
        );
    }

    public async Task<Result<MaterialSpecificationDto>> GetMaterialSpecificationByMaterial(
        Guid materialId
    )
    {
        var materialSpec = await context
            .MaterialSpecifications.IgnoreAutoIncludes()
            .AsSplitQuery()
            .Include(ms => ms.Material)
            .Include(ms => ms.Form)
                .ThenInclude(ps => ps.Sections.OrderBy(s => s.Order))
                    .ThenInclude(ps => ps.Fields)
                        .ThenInclude(ps => ps.Question)
                            .ThenInclude(q => q.Options)
            .Include(ps => ps.Form)
                .ThenInclude(ps => ps.Sections.OrderBy(s => s.Order))
                    .ThenInclude(ps => ps.Instrument)
                        .ThenInclude(ps => ps.QcEquipmentCategory)
            .Include(ms => ms.CreatedBy)
            .Include(m => m.Response)
                .ThenInclude(r => r.FormResponses)
                    .ThenInclude(r => r.FormField)
            .Include(ms => ms.FormSections)
            .FirstOrDefaultAsync(ps => ps.MaterialId == materialId);

        return Result.Success(mapper.Map<MaterialSpecificationDto>(materialSpec));
    }

    public async Task<
        Result<List<MaterialSpecificationDto>>
    > GetMaterialSpecificationBySpecificationNumber(string specificationNumber)
    {
        if (string.IsNullOrWhiteSpace(specificationNumber))
            return Error.Validation("Invalid.SpecificationNumber", "Invalid specification number.");

        var specs = await context
            .MaterialSpecifications.AsSplitQuery()
            .Include(ms => ms.Material)
            .Where(ms => ms.SpecificationNumber == specificationNumber)
            .ToListAsync();

        if (specs.Count == 0)
        {
            return Error.NotFound(
                "MaterialSpecification.NotFound",
                "Material specification not found."
            );
        }

        return mapper.Map<List<MaterialSpecificationDto>>(specs);
    }

    public async Task<Result<List<MaterialSpecificationMappingDto>>> UpdateMaterialSpecification(
        Guid id,
        UpdateMaterialSpecificationRequest request
    )
    {
        var materialSpec = await context.MaterialSpecifications.FirstOrDefaultAsync(ps =>
            ps.Id == id
        );

        if (materialSpec is null)
        {
            return Error.NotFound(
                "MaterialSpecification.NotFound",
                "Material specification not found"
            );
        }

        var specificationNumber = materialSpec.SpecificationNumber;
        var specsToUpdate = await context
            .MaterialSpecifications.Where(ms => ms.SpecificationNumber == specificationNumber)
            .ToListAsync();

        foreach (var spec in specsToUpdate)
        {
            var oldSpecNumber = spec.SpecificationNumber;
            mapper.Map(request, spec);
            spec.Reference = SpecificationReferenceHelper.FormatReference(request.Reference);

            if (oldSpecNumber != spec.SpecificationNumber)
            {
                var ards = await context
                    .MaterialAnalyticalRawData.Where(ad =>
                        ad.MaterialStandardTestProcedure.MaterialId == spec.MaterialId
                    )
                    .ToListAsync();

                foreach (var ard in ards)
                {
                    ard.SpecNumber = spec.SpecificationNumber;
                }
                context.MaterialAnalyticalRawData.UpdateRange(ards);
            }
        }

        context.MaterialSpecifications.UpdateRange(specsToUpdate);
        await context.SaveChangesAsync();

        return specsToUpdate
            .Select(s => new MaterialSpecificationMappingDto
            {
                MaterialId = s.MaterialId,
                SpecificationId = s.Id,
            })
            .ToList();
    }

    public async Task<
        Result<List<MaterialSpecificationMappingDto>>
    > AddRemoveMaterialsToSpecification(AddRemoveMaterialToSpecificationRequest request)
    {
        var existingSpecs = await context
            .MaterialSpecifications.Include(ms => ms.Material)
            .Where(ms => ms.SpecificationNumber == request.SpecificationNumber)
            .ToListAsync();

        if (existingSpecs.Count == 0)
        {
            return Error.NotFound(
                "MaterialSpecification.NotFound",
                $"No specification found with specification number '{request.SpecificationNumber}'."
            );
        }

        var templateSpec = existingSpecs[0];

        // Handle removals
        if (request.MaterialIdsToRemove is { Count: > 0 })
        {
            var specsToRemove = existingSpecs
                .Where(ms => request.MaterialIdsToRemove.Contains(ms.MaterialId))
                .ToList();

            foreach (var spec in specsToRemove)
            {
                var linkedArd = await context.MaterialAnalyticalRawData.AnyAsync(ard =>
                    ard.SpecNumber == spec.SpecificationNumber
                    && ard.MaterialStandardTestProcedure.MaterialId == spec.MaterialId
                    && ard.DeletedAt == null
                );

                if (linkedArd)
                {
                    return Error.Conflict(
                        "MaterialSpecification.LinkedToArd",
                        $"Cannot remove material '{spec.Material?.Name}' because it is linked to analytical raw data."
                    );
                }
            }

            context.MaterialSpecifications.RemoveRange(specsToRemove);
            existingSpecs.RemoveAll(ms => request.MaterialIdsToRemove.Contains(ms.MaterialId));
        }

        // Handle additions
        if (request.MaterialIdsToAdd is { Count: > 0 })
        {
            var materialsToAdd = await context
                .Materials.Where(m => request.MaterialIdsToAdd.Contains(m.Id))
                .ToListAsync();

            if (materialsToAdd.Count != request.MaterialIdsToAdd.Count)
                return Error.Validation("Invalid.Material", "One or more materials are invalid.");

            foreach (
                var spec in from material in materialsToAdd
                where existingSpecs.All(ms => ms.MaterialId != material.Id)
                select new MaterialSpecification
                {
                    SpecificationNumber = templateSpec.SpecificationNumber,
                    RevisionNumber = templateSpec.RevisionNumber,
                    SupersedesNumber = templateSpec.SupersedesNumber,
                    EffectiveDate = templateSpec.EffectiveDate,
                    ReviewDate = templateSpec.ReviewDate,
                    FormId = templateSpec.FormId,
                    DueDate = templateSpec.DueDate,
                    Description = templateSpec.Description,
                    UserId = templateSpec.UserId,
                    MaterialId = material.Id,
                    ResponseId = null,
                }
            )
            {
                await context.MaterialSpecifications.AddAsync(spec);
                existingSpecs.Add(spec);
            }
        }

        await context.SaveChangesAsync();
        return existingSpecs
            .Select(s => new MaterialSpecificationMappingDto
            {
                MaterialId = s.MaterialId,
                SpecificationId = s.Id,
            })
            .ToList();
    }

    public async Task<Result> DeleteMaterialSpecification(Guid id, Guid userId)
    {
        var materialSpec = await context
            .MaterialSpecifications.Include(materialSpecification => materialSpecification.Material)
            .FirstOrDefaultAsync(ps => ps.Id == id);

        if (materialSpec is null)
        {
            return Error.NotFound(
                "MaterialSpecification.NotFound",
                "Material specification not found"
            );
        }

        var linkedArd = await context
            .MaterialAnalyticalRawData.Include(ard => ard.MaterialStandardTestProcedure.Material)
            .FirstOrDefaultAsync(ard =>
                ard.SpecNumber == materialSpec.SpecificationNumber
                && ard.MaterialStandardTestProcedure.MaterialId == materialSpec.MaterialId
                && ard.DeletedAt == null
            );

        if (linkedArd is not null)
        {
            return Error.Conflict(
                "MaterialSpecification.LinkedToArd",
                $"Cannot delete specification '{materialSpec.SpecificationNumber}' for '{linkedArd.MaterialStandardTestProcedure.Material.Name}' as it is linked to an ARD."
            );
        }
        materialSpec.LastDeletedById = userId;
        materialSpec.DeletedAt = DateTime.UtcNow;

        context.MaterialSpecifications.Update(materialSpec);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ImportMaterialSpecificationsFromCsv(
        IFormFile file,
        MaterialKind kind,
        Guid userId
    )
    {
        if (file == null || file.Length == 0)
            return UploadErrors.EmptyFile;

        var specs = new List<MaterialSpecification>();

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        using var reader = new StreamReader(stream);

        // Read header row
        var headerLine = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(headerLine))
            return UploadErrors.WorksheetNotFound;

        var headerParts = headerLine.Split(',', StringSplitOptions.TrimEntries);
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < headerParts.Length; i++)
            headers[headerParts[i]] = i;

        // Required headers
        var requiredHeaders = new[]
        {
            "Name",
            "Code",
            "Specification",
            "Revision Date",
            "Effective Date",
            "Revision",
            "Supersedes",
        };

        foreach (var header in requiredHeaders)
        {
            if (!headers.ContainsKey(header))
                return UploadErrors.MissingRequiredHeader(header);
        }

        // Lookups
        var materials = await context
            .Materials.AsNoTracking()
            .Where(m => m.Kind == kind)
            .ToDictionaryAsync(m => m.Code.ToLower(), m => m.Id);

        var existingNumbers = await context
            .MaterialSpecifications.IgnoreQueryFilters()
            .Select(m => m.SpecificationNumber)
            .ToHashSetAsync();

        while (await reader.ReadLineAsync() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(',', StringSplitOptions.TrimEntries);

            string Get(string header)
            {
                var index = headers[header];
                return index < parts.Length ? parts[index].Trim() : "";
            }

            var code = Get("Code").ToLower();
            if (!materials.TryGetValue(code, out var materialId))
                continue;

            var specNumber = Get("Specification");
            if (string.IsNullOrWhiteSpace(specNumber) || existingNumbers.Contains(specNumber))
                continue;

            var spec = new MaterialSpecification
            {
                SpecificationNumber = specNumber,
                RevisionNumber = Get("Revision"),
                SupersedesNumber = Get("Supersedes"),
                Description = Get("Name"),

                EffectiveDate = DateTime.TryParse(Get("Effective Date"), out var eff)
                    ? DateTime.SpecifyKind(eff, DateTimeKind.Utc)
                    : DateTime.UtcNow,

                ReviewDate = DateTime.TryParse(Get("Revision Date"), out var rev)
                    ? DateTime.SpecifyKind(rev, DateTimeKind.Utc)
                    : DateTime.UtcNow,

                DueDate = DateTime.UtcNow, // adjust if required

                MaterialId = materialId,

                // Fill defaults
                UserId = userId,
                FormId = Guid.Parse("019a7e06-a806-741f-b47a-2c228a675ca1"),
                ResponseId = null,
                FormSections = [],
            };

            specs.Add(spec);
            existingNumbers.Add(specNumber);
        }

        if (specs.Count != 0)
        {
            await context.MaterialSpecifications.AddRangeAsync(specs);
            await context.SaveChangesAsync();
        }

        return Result.Success();
    }
}
