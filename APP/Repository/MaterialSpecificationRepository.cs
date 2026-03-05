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
    public async Task<Result<Guid>> CreateMaterialSpecification(
        CreateMaterialSpecificationRequest request
    )
    {
        var isLinked = await context.MaterialSpecifications.AnyAsync(m =>
            m.Id == request.MaterialId
        );
        if (isLinked)
        {
            return Error.Conflict(
                "MaterialSpecification.AlreadyLinked",
                "Material specification already linked"
            );
        }

        if (request.DueDate < DateTime.UtcNow)
        {
            return Error.Validation(
                "MaterialSpecification.DueDate",
                "Due date must be greater than current date"
            );
        }

        var materialSpec = mapper.Map<MaterialSpecification>(request);
        await context.MaterialSpecifications.AddAsync(materialSpec);

        await context.SaveChangesAsync();
        return materialSpec.Id;
    }

    public async Task<
        Result<Paginateable<IEnumerable<MaterialSpecificationDto>>>
    > GetMaterialSpecifications(
        int page,
        int pageSize,
        string searchQuery,
        MaterialKind materialKind
    )
    {
        var query = context
            .MaterialSpecifications.AsSplitQuery()
            .Include(ms => ms.Material)
            .Include(ms => ms.Form)
            .Include(ms => ms.CreatedBy)
            .Include(ms => ms.Response)
            .Where(ms => ms.Material.Kind == materialKind)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(
                searchQuery,
                q => q.SpecificationNumber,
                q => q.Description,
                q => q.Material.Name
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
            .Include(ms => ms.CreatedBy)
            .Include(m => m.Response)
                .ThenInclude(r => r.FormResponses)
                    .ThenInclude(r => r.FormField)
            .Include(ms => ms.FormSections)
            .FirstOrDefaultAsync(ps => ps.Id == id);

        return Result.Success(mapper.Map<MaterialSpecificationDto>(materialSpec));
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
            .Include(ms => ms.CreatedBy)
            .Include(m => m.Response)
                .ThenInclude(r => r.FormResponses)
                    .ThenInclude(r => r.FormField)
            .Include(ms => ms.FormSections)
            .FirstOrDefaultAsync(ps => ps.MaterialId == materialId);

        return Result.Success(mapper.Map<MaterialSpecificationDto>(materialSpec));
    }

    public async Task<Result> UpdateMaterialSpecification(
        Guid id,
        CreateMaterialSpecificationRequest request
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

        mapper.Map(request, materialSpec);

        context.MaterialSpecifications.Update(materialSpec);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteMaterialSpecification(Guid id, Guid userId)
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
