using APP.Services.Authorization;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.StpRevisions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.StpRevisions;

/// <summary>Which STP test each specification test is performed by (step 4 of the STP plan).</summary>
public sealed class StpTestLinkService(ApplicationDbContext context)
{
    /// <summary>The specification a request names, with its kind, template and the material or product it is for.</summary>
    private sealed record Spec(Guid Id, bool Product, StpRevisionKind Kind, Guid FormId, Guid ItemId);

    private async Task<Spec> Load(bool product, Guid id) => product
        ? await context.ProductSpecifications.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new Spec(item.Id, true, StpRevisionKind.Product, item.FormId, item.ProductId)).FirstOrDefaultAsync()
        : await context.MaterialSpecifications.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new Spec(item.Id, false, item.Material.Kind == MaterialKind.Package ? StpRevisionKind.PackagingMaterial : StpRevisionKind.RawMaterial,
                item.FormId, item.MaterialId)).FirstOrDefaultAsync();

    /// <summary>The template's sections that are this specification's tests: shared ones and ones scoped to it.</summary>
    private IQueryable<DOMAIN.Entities.Forms.FormSection> Sections(Spec spec) => context.FormSections.AsNoTracking()
        .Where(section => section.FormId == spec.FormId && (spec.Product
            ? !section.MaterialSpecificationId.HasValue && (!section.ProductSpecificationId.HasValue || section.ProductSpecificationId == spec.Id)
            : !section.ProductSpecificationId.HasValue && (!section.MaterialSpecificationId.HasValue || section.MaterialSpecificationId == spec.Id)));

    private IQueryable<StpTestLink> Links(Spec spec) => spec.Product
        ? context.StpTestLinks.Where(item => item.ProductSpecificationId == spec.Id)
        : context.StpTestLinks.Where(item => item.MaterialSpecificationId == spec.Id);

    private async Task<List<string>> RecordedNumbers(Spec spec) => (spec.Product
            ? await context.ProductStandardTestProcedures.AsNoTracking().Where(item => item.ProductId == spec.ItemId).Select(item => item.StpNumber).ToListAsync()
            : await context.MaterialStandardTestProcedures.AsNoTracking().Where(item => item.MaterialId == spec.ItemId).Select(item => item.StpNumber).ToListAsync())
        .Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private async Task<Error?> Refuse(StpRevisionKind kind, bool write, Guid actorId)
    {
        var key = StpTestLinkPermissions.Key(kind, write);
        return (await CurrentPermissionSet.Load(context, actorId)).Contains(key) ? null
            : Error.Forbidden("StpTestLink.Permission", $"You need {key} for this.");
    }

    private static Error NotFound => Error.NotFound("StpTestLink.Specification", "Specification not found.");

    public async Task<Result<StpTestLinksDto>> Get(bool product, Guid specificationId, Guid actorId)
    {
        var spec = await Load(product, specificationId);
        if (spec is null) return NotFound;
        var refused = await Refuse(spec.Kind, false, actorId);
        if (refused is not null) return refused;
        var sections = await Sections(spec).OrderBy(item => item.Order).Select(item => new { item.Id, item.Name }).ToListAsync();
        var links = await Links(spec).AsNoTracking().ToListAsync();
        var numbers = await RecordedNumbers(spec);
        var approved = context.StpRevisions.AsNoTracking().Where(item => item.Kind == spec.Kind && item.Status == StpRevisionStatus.Approved);
        var linkedNumbers = links.Select(item => item.StpNumber).Distinct().ToList();
        var tests = await approved
            .Where(item => numbers.Count == 0 || numbers.Contains(item.StpNumber) || linkedNumbers.Contains(item.StpNumber))
            .SelectMany(item => item.Tests.Select(test => new { item.StpNumber, item.RevisionNumber, test.TestKey, test.Number, test.Title, test.Order }))
            .ToListAsync();
        string Canonical(string number) => numbers.FirstOrDefault(item => string.Equals(item, number, StringComparison.OrdinalIgnoreCase)) ?? number;
        return new StpTestLinksDto
        {
            Kind = spec.Kind, StpNumbers = numbers,
            Options = tests.Where(item => numbers.Count == 0 || numbers.Contains(Canonical(item.StpNumber), StringComparer.OrdinalIgnoreCase))
                .OrderBy(item => item.StpNumber).ThenBy(item => item.Order)
                .Select(item => new StpTestOptionDto { StpNumber = item.StpNumber, RevisionNumber = item.RevisionNumber, TestKey = item.TestKey, TestNumber = item.Number, TestTitle = item.Title })
                .ToList(),
            Tests = sections.Select(section =>
            {
                var link = links.FirstOrDefault(item => item.FormSectionId == section.Id);
                var test = link is null ? null : tests.FirstOrDefault(item => item.StpNumber == link.StpNumber && item.TestKey == link.TestKey);
                return new StpTestLinkDto
                {
                    FormSectionId = section.Id, SectionName = section.Name, StpNumber = link?.StpNumber, TestKey = link?.TestKey,
                    TestNumber = test?.Number, TestTitle = test?.Title, RevisionNumber = test?.RevisionNumber, Resolved = test is not null,
                };
            }).ToList(),
        };
    }

    /// <summary>Cites an STP test from a specification test, or clears the citation. The test must be in the STP's approved revision.</summary>
    public async Task<Result> Save(bool product, Guid specificationId, Guid formSectionId, SaveStpTestLinkRequest request, Guid actorId)
    {
        var spec = await Load(product, specificationId);
        if (spec is null) return NotFound;
        var refused = await Refuse(spec.Kind, true, actorId);
        if (refused is not null) return refused;
        if (!await Sections(spec).AnyAsync(item => item.Id == formSectionId))
            return Error.NotFound("StpTestLink.Section", "That test is not part of this specification.");
        var number = request?.StpNumber?.Trim();
        var key = request?.TestKey?.Trim();
        if (string.IsNullOrEmpty(number) != string.IsNullOrEmpty(key))
            return Error.Validation("StpTestLink.Test", "Choose both the STP and its test, or neither to clear the link.");
        if (!string.IsNullOrEmpty(number) && !await context.StpRevisions.AnyAsync(item => item.Kind == spec.Kind && item.StpNumber == number
                && item.Status == StpRevisionStatus.Approved && item.Tests.Any(test => test.TestKey == key)))
            return Error.Validation("StpTestLink.Test", $"{number} has no approved revision with that test.");
        var existing = await Links(spec).FirstOrDefaultAsync(item => item.FormSectionId == formSectionId);
        refused = await Refuse(spec.Kind, true, actorId);
        if (refused is not null) return refused;
        var now = DateTime.UtcNow;
        if (string.IsNullOrEmpty(number))
        {
            if (existing is null) return Result.Success();
            existing.DeletedAt = now;
            existing.LastDeletedById = actorId;
        }
        else if (existing is null)
            context.StpTestLinks.Add(new StpTestLink
            {
                Id = Guid.NewGuid(), FormSectionId = formSectionId, Kind = spec.Kind, StpNumber = number, TestKey = key,
                MaterialSpecificationId = product ? null : spec.Id, ProductSpecificationId = product ? spec.Id : null,
                CreatedById = actorId, CreatedAt = now,
            });
        else
        {
            existing.StpNumber = number;
            existing.TestKey = key;
            existing.UpdatedAt = now;
            existing.LastUpdatedById = actorId;
        }
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
