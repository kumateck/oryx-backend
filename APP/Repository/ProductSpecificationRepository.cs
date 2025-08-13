using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.ProductSpecifications;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ProductSpecificationRepository(ApplicationDbContext context, IMapper mapper) : IProductSpecificationRepository
{
    public async Task<Result<Guid>> CreateProductSpecification(CreateProductSpecificationRequest request)
    {
        if (request.DueDate < DateTime.UtcNow)
        {
            return Error.Validation("MaterialSpecification.DueDate", "Due date must be greater than current date");
        }
        
        var productSpec = mapper.Map<ProductSpecification>(request);
        await context.AddAsync(productSpec);
       
        await context.SaveChangesAsync();
        return productSpec.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<ProductSpecificationDto>>>> GetProductSpecifications(int page, int pageSize, string searchQuery)
    {
        var query = context.ProductSpecifications
            .IgnoreQueryFilters()
            .Include(ps => ps.Form)
            .Include(ps => ps.Product)
            .Include(ps => ps.CreatedBy)
            .Where(ps => !ps.DeletedAt.HasValue)
            .AsQueryable();

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<ProductSpecificationDto>);
    }

    public async Task<Result<ProductSpecificationDto>> GetProductSpecification(Guid id)
    {

        var productSpec = await context.ProductSpecifications
                .IgnoreQueryFilters()
                .Include(ps => ps.Product)
                .Include(ps => ps.Form)
                .Include(ps => ps.CreatedBy)
                .FirstOrDefaultAsync(ps => ps.Id == id);

        return productSpec is null ? Error.NotFound("ProductSpecification.NotFound", "Product specification not found")
            : mapper.Map<ProductSpecificationDto>(productSpec);
    }
    
    public async Task<Result<ProductSpecificationDto>> GetProductSpecificationByProduct(Guid productId)
    {

        var productSpec = await context.ProductSpecifications
            .IgnoreQueryFilters()
            .Include(ps => ps.Product)
            .Include(ps => ps.Form)
            .Include(ps => ps.CreatedBy)
            .FirstOrDefaultAsync(ps => ps.ProductId == productId);

        return productSpec is null ? Error.NotFound("ProductSpecification.NotFound", "Product specification not found")
            : mapper.Map<ProductSpecificationDto>(productSpec);
            
    }

    public async Task<Result> UpdateProductSpecification(Guid id, CreateProductSpecificationRequest request)
    {
        var productSpec = await context.ProductSpecifications.FirstOrDefaultAsync(ps => ps.Id == id);
        
        if (productSpec is null)
        {
            return Error.NotFound("ProductSpecification.NotFound", "Product specification not found");
        }
        
        mapper.Map(request, productSpec);
        
        context.ProductSpecifications.Update(productSpec);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteProductSpecification(Guid id, Guid userId)
    {
        var productSpec = await context.ProductSpecifications.FirstOrDefaultAsync(ps => ps.Id == id);
        
        if (productSpec is null)
        {
            return Error.NotFound("ProductSpecification.NotFound", "Product specification not found");
        }
        
        productSpec.LastDeletedById = userId;
        productSpec.DeletedAt = DateTime.UtcNow;
        
        context.ProductSpecifications.Update(productSpec);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}