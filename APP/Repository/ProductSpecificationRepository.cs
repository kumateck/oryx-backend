using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.ProductSpecifications;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ProductSpecificationRepository(ApplicationDbContext context, IMapper mapper)
    : IProductSpecificationRepository
{
    public async Task<Result<List<ProductSpecificationMappingDto>>> CreateProductSpecification(
        CreateProductSpecificationRequest request
    )
    {
        if (request.ProductIds == null || request.ProductIds.Count == 0)
            return Error.Validation("Invalid.Products", "At least one product is required.");

        var products = await context
            .Products.Where(p => request.ProductIds.Contains(p.Id))
            .ToListAsync();

        if (products.Count != request.ProductIds.Count)
            return Error.Validation("Invalid.Product", "One or more products are invalid.");

        if (request.DueDate < DateTime.UtcNow)
        {
            return Error.Validation(
                "ProductSpecification.DueDate",
                "Due date must be greater than current date"
            );
        }

        // Fetch existing specifications for this spec number
        var existingSpecs = await context
            .ProductSpecifications.Where(ps =>
                ps.SpecificationNumber == request.SpecificationNumber
            )
            .ToListAsync();

        var mappings = new List<ProductSpecificationMappingDto>();

        foreach (var product in products)
        {
            var alreadyExistsForProduct = existingSpecs.Any(ps => ps.ProductId == product.Id);

            if (alreadyExistsForProduct)
            {
                return Error.Validation(
                    "ProductSpecification.Exists",
                    $"Product '{product.Name}' already has this specification number."
                );
            }

            var productSpec = mapper.Map<ProductSpecification>(request);
            productSpec.ProductId = product.Id;
            await context.ProductSpecifications.AddAsync(productSpec);
            mappings.Add(
                new ProductSpecificationMappingDto
                {
                    ProductId = product.Id,
                    SpecificationId = productSpec.Id,
                }
            );
        }

        await context.SaveChangesAsync();
        return mappings;
    }

    public async Task<
        Result<Paginateable<IEnumerable<ProductSpecificationDto>>>
    > GetProductSpecifications(int page, int pageSize, string searchQuery, bool? isVerified = null)
    {
        var query = context
            .ProductSpecifications.AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(ps => ps.Form)
            .Include(ps => ps.Product)
            .Include(ps => ps.CreatedBy)
            .Where(ps => !ps.DeletedAt.HasValue)
            .AsQueryable();

        if (isVerified.HasValue)
        {
            query = query.Where(p => p.IsVerified == isVerified.Value);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<ProductSpecificationDto>
        );
    }

    public async Task<Result<ProductSpecificationDto>> GetProductSpecification(Guid id)
    {
        var productSpec = await context
            .ProductSpecifications.AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(ps => ps.Product)
            .Include(ps => ps.Form)
                .ThenInclude(ps => ps.Sections.OrderBy(s => s.Order))
                    .ThenInclude(ps => ps.Fields)
                        .ThenInclude(ps => ps.Question)
            .Include(ps => ps.Form)
                .ThenInclude(ps => ps.Sections.OrderBy(s => s.Order))
                    .ThenInclude(ps => ps.Instrument)
            .Include(ps => ps.CreatedBy)
            .Include(m => m.Response)
                .ThenInclude(r => r.FormResponses)
                    .ThenInclude(r => r.FormField)
            .Include(ps => ps.FormSections)
            .Where(ps => !ps.DeletedAt.HasValue)
            .FirstOrDefaultAsync(ps => ps.Id == id);

        return productSpec is null
            ? Error.NotFound("ProductSpecification.NotFound", "Product specification not found")
            : mapper.Map<ProductSpecificationDto>(productSpec);
    }

    public async Task<Result<List<ProductListDto>>> GetProductsNotLinkedToSpecification()
    {
        var products = await context
            .Products.IgnoreQueryFilters()
            .Where(ps =>
                !ps.DeletedAt.HasValue
                && !context.ProductSpecifications.Any(m => m.ProductId == ps.Id)
            )
            .ToListAsync();

        return mapper.Map<List<ProductListDto>>(products);
    }

    public async Task<Result<ProductSpecificationDto>> GetProductSpecificationByProduct(
        Guid productId
    )
    {
        var productSpec = await context
            .ProductSpecifications.IgnoreQueryFilters()
            .Include(ps => ps.Product)
            .Include(ps => ps.Form)
            .Include(ps => ps.CreatedBy)
            .Include(ps => ps.FormSections)
            .Where(ps => !ps.DeletedAt.HasValue)
            .FirstOrDefaultAsync(ps => ps.ProductId == productId);

        return productSpec is null
            ? Error.NotFound("ProductSpecification.NotFound", "Product specification not found")
            : mapper.Map<ProductSpecificationDto>(productSpec);
    }

    public async Task<Result<List<ProductSpecificationDto>>> GetProductSpecificationByProductId(
        Guid productId
    )
    {
        var productSpec = await context
            .ProductSpecifications.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(ps => ps.Product)
            .Include(ps => ps.Form)
                .ThenInclude(ps => ps.Sections.OrderBy(s => s.Order))
                    .ThenInclude(ps => ps.Fields)
                        .ThenInclude(ps => ps.Question)
            .Include(ps => ps.Form)
                .ThenInclude(ps => ps.Sections.OrderBy(s => s.Order))
                    .ThenInclude(ps => ps.Instrument)
            .Include(ps => ps.CreatedBy)
            .Include(m => m.Response)
                .ThenInclude(r => r.FormResponses)
                    .ThenInclude(r => r.FormField)
            .Include(ps => ps.FormSections)
            .Where(ps => ps.ProductId == productId && !ps.DeletedAt.HasValue)
            .ToListAsync();

        return mapper.Map<List<ProductSpecificationDto>>(productSpec);
    }

    public async Task<
        Result<List<ProductSpecificationDto>>
    > GetProductSpecificationBySpecificationNumber(string specificationNumber)
    {
        if (string.IsNullOrWhiteSpace(specificationNumber))
            return Error.Validation("Invalid.SpecificationNumber", "Invalid specification number.");

        var specs = await context
            .ProductSpecifications.AsSplitQuery()
            .Include(ps => ps.Product)
            .Where(ps => ps.SpecificationNumber == specificationNumber)
            .ToListAsync();

        if (specs.Count == 0)
        {
            return Error.NotFound(
                "ProductSpecification.NotFound",
                "Product specification not found."
            );
        }

        return mapper.Map<List<ProductSpecificationDto>>(specs);
    }

    public async Task<Result<List<ProductSpecificationMappingDto>>> UpdateProductSpecification(
        Guid id,
        UpdateProductSpecificationRequest request
    )
    {
        var productSpec = await context.ProductSpecifications.FirstOrDefaultAsync(ps =>
            ps.Id == id
        );

        if (productSpec is null)
        {
            return Error.NotFound(
                "ProductSpecification.NotFound",
                "Product specification not found"
            );
        }

        var specificationNumber = productSpec.SpecificationNumber;
        var specsToUpdate = await context
            .ProductSpecifications.Where(ps => ps.SpecificationNumber == specificationNumber)
            .ToListAsync();

        foreach (var spec in specsToUpdate)
        {
            var oldSpecNumber = spec.SpecificationNumber;
            mapper.Map(request, spec);

            if (oldSpecNumber != spec.SpecificationNumber)
            {
                var ards = await context
                    .ProductAnalyticalRawData.Where(ad =>
                        ad.ProductStandardTestProcedure.ProductId == spec.ProductId
                    )
                    .ToListAsync();

                foreach (var ard in ards)
                {
                    ard.SpecNumber = spec.SpecificationNumber;
                }
                context.ProductAnalyticalRawData.UpdateRange(ards);
            }
        }

        context.ProductSpecifications.UpdateRange(specsToUpdate);
        await context.SaveChangesAsync();

        return specsToUpdate
            .Select(s => new ProductSpecificationMappingDto
            {
                ProductId = s.ProductId,
                SpecificationId = s.Id,
            })
            .ToList();
    }

    public async Task<
        Result<List<ProductSpecificationMappingDto>>
    > AddRemoveProductsToSpecification(AddRemoveProductToSpecificationRequest request)
    {
        var existingSpecs = await context
            .ProductSpecifications.Include(ps => ps.Product)
            .Where(ps => ps.SpecificationNumber == request.SpecificationNumber)
            .ToListAsync();

        if (existingSpecs.Count == 0)
        {
            return Error.NotFound(
                "ProductSpecification.NotFound",
                $"No specification found with specification number '{request.SpecificationNumber}'."
            );
        }

        var templateSpec = existingSpecs[0];

        // Handle removals
        if (request.ProductIdsToRemove is { Count: > 0 })
        {
            var specsToRemove = existingSpecs
                .Where(ps => request.ProductIdsToRemove.Contains(ps.ProductId))
                .ToList();

            foreach (var spec in specsToRemove)
            {
                var linkedArd = await context.ProductAnalyticalRawData.AnyAsync(ard =>
                    ard.SpecNumber == spec.SpecificationNumber
                    && ard.ProductStandardTestProcedure.ProductId == spec.ProductId
                    && ard.DeletedAt == null
                );

                if (linkedArd)
                {
                    return Error.Conflict(
                        "ProductSpecification.LinkedToArd",
                        $"Cannot remove product '{spec.Product?.Name}' because it is linked to analytical raw data."
                    );
                }
            }

            context.ProductSpecifications.RemoveRange(specsToRemove);
            existingSpecs.RemoveAll(ps => request.ProductIdsToRemove.Contains(ps.ProductId));
        }

        // Handle additions
        if (request.ProductIdsToAdd is { Count: > 0 })
        {
            var productsToAdd = await context
                .Products.Where(p => request.ProductIdsToAdd.Contains(p.Id))
                .ToListAsync();

            if (productsToAdd.Count != request.ProductIdsToAdd.Count)
                return Error.Validation("Invalid.Product", "One or more products are invalid.");

            foreach (
                var spec in from product in productsToAdd
                where existingSpecs.All(ps => ps.ProductId != product.Id)
                select new ProductSpecification
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
                    ProductId = product.Id,
                    TestStage = templateSpec.TestStage,
                    ResponseId = null,
                }
            )
            {
                await context.ProductSpecifications.AddAsync(spec);
                existingSpecs.Add(spec);
            }
        }

        await context.SaveChangesAsync();
        return existingSpecs
            .Select(s => new ProductSpecificationMappingDto
            {
                ProductId = s.ProductId,
                SpecificationId = s.Id,
            })
            .ToList();
    }

    public async Task<Result> DeleteProductSpecification(Guid id, Guid userId)
    {
        var productSpec = await context
            .ProductSpecifications.Include(productSpecification => productSpecification.Product)
            .FirstOrDefaultAsync(ps => ps.Id == id);

        if (productSpec is null)
        {
            return Error.NotFound(
                "ProductSpecification.NotFound",
                "Product specification not found"
            );
        }

        var linkedArd = await context
            .ProductAnalyticalRawData.Include(ard => ard.ProductStandardTestProcedure.Product)
            .FirstOrDefaultAsync(ard =>
                ard.SpecNumber == productSpec.SpecificationNumber
                && ard.ProductStandardTestProcedure.ProductId == productSpec.ProductId
                && ard.DeletedAt == null
            );

        if (linkedArd is not null)
        {
            return Error.Conflict(
                "ProductSpecification.LinkedToArd",
                $"Cannot delete specification '{productSpec.SpecificationNumber}' for '{linkedArd.ProductStandardTestProcedure.Product.Name}' as it is linked to an ARD."
            );
        }

        productSpec.LastDeletedById = userId;
        productSpec.DeletedAt = DateTime.UtcNow;

        context.ProductSpecifications.Update(productSpec);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
