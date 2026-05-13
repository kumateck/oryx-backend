using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.ProductStandardTestProcedures;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ProductStandardTestProcedureRepository(ApplicationDbContext context, IMapper mapper)
    : IProductStandardTestProcedureRepository
{
    public async Task<Result<List<ProductStpMappingDto>>> CreateProductStandardTestProcedure(
        CreateProductStandardTestProcedureRequest request
    )
    {
        if (request.ProductIds == null || request.ProductIds.Count == 0)
            return Error.Validation("Invalid.Products", "At least one product is required.");

        var products = await context
            .Products.Where(m => request.ProductIds.Contains(m.Id))
            .ToListAsync();

        if (products.Count != request.ProductIds.Count)
            return Error.Validation("Invalid.Product", "One or more products are invalid.");

        // Fetch existing STPs for this STP number
        var existingStps = await context
            .ProductStandardTestProcedures.Where(stp => stp.StpNumber == request.StpNumber)
            .ToListAsync();

        var mappings = new List<ProductStpMappingDto>();

        foreach (var product in products)
        {
            // check if product already has this STP number
            var alreadyExistsForProduct = existingStps.Any(stp => stp.ProductId == product.Id);

            if (alreadyExistsForProduct)
            {
                return Error.Validation(
                    "ProductStandardTestProcedure.Exists",
                    $"Product '{product.Name}' already has this STP number."
                );
            }

            var procedure = new ProductStandardTestProcedure
            {
                StpNumber = request.StpNumber,
                ProductId = product.Id,
                Description = request.Description,
            };

            await context.ProductStandardTestProcedures.AddAsync(procedure);
            mappings.Add(new ProductStpMappingDto { ProductId = product.Id, StpId = procedure.Id });
        }

        await context.SaveChangesAsync();
        return mappings;
    }

    public async Task<
        Result<Paginateable<IEnumerable<ProductStandardTestProcedureDto>>>
    > GetProductStandardTestProcedures(int page, int pageSize, string searchQuery)
    {
        var query = context
            .ProductStandardTestProcedures.AsQueryable()
            .IgnoreQueryFilters()
            .Where(stp => !stp.DeletedAt.HasValue)
            .Include(stp => stp.Product)
            .AsSplitQuery();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, stp => stp.StpNumber);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            entity =>
                mapper.Map<ProductStandardTestProcedureDto>(
                    entity,
                    opts =>
                        opts.Items[AppConstants.ModelType] = nameof(ProductStandardTestProcedure)
                )
        );
    }

    public async Task<Result<ProductStandardTestProcedureDto>> GetProductStandardTestProcedure(
        Guid id
    )
    {
        var procedure = await context
            .ProductStandardTestProcedures.AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(stp => stp.Product)
            .FirstOrDefaultAsync(stp => stp.Id == id && !stp.DeletedAt.HasValue);

        return procedure is null
            ? Error.NotFound(
                "ProductStandardTestProcedure.NotFound",
                "Product Standard test procedure not found"
            )
            : mapper.Map<ProductStandardTestProcedureDto>(
                procedure,
                opts =>
                {
                    opts.Items[AppConstants.ModelType] = nameof(ProductStandardTestProcedure);
                }
            );
    }

    public async Task<
        Result<ProductStandardTestProcedureDto>
    > GetProductStandardTestProcedureByProduct(Guid id)
    {
        var procedure = await context
            .ProductStandardTestProcedures.AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(stp => stp.Product)
            .FirstOrDefaultAsync(stp => stp.ProductId == id && !stp.DeletedAt.HasValue);

        return procedure is null
            ? Error.NotFound(
                "ProductStandardTestProcedure.NotFound",
                "Product Standard test procedure not found"
            )
            : mapper.Map<ProductStandardTestProcedureDto>(
                procedure,
                opts =>
                {
                    opts.Items[AppConstants.ModelType] = nameof(ProductStandardTestProcedure);
                }
            );
    }

    public async Task<
        Result<Paginateable<IEnumerable<ProductListDto>>>
    > GetProductsNotUsedInStandardTestProcedure(int page, int pageSize, string searchQuery)
    {
        var query = context
            .Products.AsSplitQuery()
            .Where(p =>
                !context.ProductStandardTestProcedures.Any(s =>
                    s.ProductId == p.Id && !s.DeletedAt.HasValue
                )
            )
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, f => f.Name);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<ProductListDto>
        );
    }

    public async Task<Result<List<ProductStpMappingDto>>> UpdateProductStandardTestProcedure(
        Guid id,
        UpdateProductStandardTestProcedureRequest request
    )
    {
        var procedure = await context.ProductStandardTestProcedures.FirstOrDefaultAsync(stp =>
            stp.Id == id
        );

        if (procedure is null)
        {
            return Error.NotFound(
                "ProductStandardTestProcedure.NotFound",
                "Product Standard test procedure not found"
            );
        }

        var stpNumber = procedure.StpNumber;
        var proceduresToUpdate = await context
            .ProductStandardTestProcedures.Where(stp => stp.StpNumber == stpNumber)
            .ToListAsync();

        foreach (var p in proceduresToUpdate)
        {
            p.Description = request.Description;
            p.StpNumber = request.StpNumber;
        }

        context.ProductStandardTestProcedures.UpdateRange(proceduresToUpdate);
        await context.SaveChangesAsync();

        return proceduresToUpdate
            .Select(p => new ProductStpMappingDto { ProductId = p.ProductId, StpId = p.Id })
            .ToList();
    }

    public async Task<
        Result<Paginateable<IEnumerable<ProductStandardTestProcedureDto>>>
    > GetProductStandardTestProceduresNotLinkedToArd(int page, int pageSize, string searchQuery)
    {
        var query = context
            .ProductStandardTestProcedures.AsSplitQuery()
            .Include(stp => stp.Product)
            .Where(stp => !context.ProductAnalyticalRawData.Any(ard => ard.StpId == stp.Id))
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, stp => stp.StpNumber);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            entity =>
                mapper.Map<ProductStandardTestProcedureDto>(
                    entity,
                    opts =>
                        opts.Items[AppConstants.ModelType] = nameof(ProductStandardTestProcedure)
                )
        );
    }

    public async Task<Result> DeleteProductStandardTestProcedure(Guid id, Guid userId)
    {
        var procedure = await context
            .ProductStandardTestProcedures.IgnoreQueryFilters()
            .Where(stp => !stp.DeletedAt.HasValue)
            .Include(stp => stp.Product)
            .FirstOrDefaultAsync(stp => stp.Id == id);
        if (procedure is null)
        {
            return Error.NotFound(
                "ProductStandardTestProcedure.NotFound",
                "Product Standard test procedure not found"
            );
        }

        var isLinkedToArd = await context.ProductAnalyticalRawData.AnyAsync(ard => ard.StpId == id);
        if (isLinkedToArd)
        {
            return Error.Conflict(
                "ProductStandardTestProcedure.LinkedToArd",
                $"Cannot delete standard test procedure because it is linked to analytical raw data for product {procedure.Product.Name}"
            );
        }

        procedure.DeletedAt = DateTime.UtcNow;
        procedure.LastDeletedById = userId;

        context.ProductStandardTestProcedures.Update(procedure);
        await context.SaveChangesAsync();

        return Result.Success();
    }
}
