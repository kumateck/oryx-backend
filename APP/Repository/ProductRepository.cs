using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Products;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

 public class ProductRepository(ApplicationDbContext context, IMapper mapper) : IProductRepository
 {
     public async Task<Result<Guid>> CreateProduct(CreateProductRequest request, Guid userId)
     {
         var product = mapper.Map<Product>(request);
         product.CreatedById = userId;
         await context.Products.AddAsync(product); 
         await context.SaveChangesAsync();

         return product.Id;
     }
     
     public async Task<Result<ProductDto>> GetProduct(Guid productId) 
     { 
         var product = await context.Products
             .Include(p => p.BillOfMaterials)
             .FirstOrDefaultAsync(p => p.Id == productId);

         return product is null ? ProductErrors.NotFound(productId) : mapper.Map<ProductDto>(product);
     }

     public async Task<Result<Paginateable<IEnumerable<ProductDto>>>> GetProducts(int page, int pageSize, string searchQuery)
     {
         var query = context.Products
             .AsSplitQuery()
             .Include(p => p.BillOfMaterials)
             .Include(p => p.Category)
             .Include(p => p.UoM)
             .AsQueryable();

         if (!string.IsNullOrEmpty(searchQuery))
         {
             query = query.WhereSearch(searchQuery, f => f.Name);
         }

         return await PaginationHelper.GetPaginatedResultAsync(
             query,
             page,
             pageSize,
             mapper.Map<ProductDto>
         );
     }

     public async Task<Result> UpdateProduct(UpdateProductRequest request, Guid productId, Guid userId)
     {
         var existingProduct = await context.Products.FirstOrDefaultAsync(p => p.Id == productId);
         if (existingProduct is null)
         {
             return ProductErrors.NotFound(productId);
         }

         mapper.Map(request, existingProduct);
         existingProduct.LastUpdatedById = userId;

         context.Products.Update(existingProduct);
         await context.SaveChangesAsync();
         return Result.Success();
     }

     public async Task<Result> DeleteProduct(Guid productId, Guid userId)
     {
         var product = await context.Products.FirstOrDefaultAsync(p => p.Id == productId);
         if (product is null)
         {
             return ProductErrors.NotFound(productId);
         }

         product.DeletedAt = DateTime.UtcNow;
         product.LastDeletedById = userId;
         context.Products.Update(product);
         await context.SaveChangesAsync();
         return Result.Success();
     }
     
     public async Task<Result<Guid>> CreateBillOfMaterials(CreateProductBillOfMaterialRequest request, Guid productId) 
     { 
         var bom = mapper.Map<ProductBillOfMaterial>(request);
         
         await context.ProductBillOfMaterials.AddAsync(bom); 
         await context.SaveChangesAsync(); 
         return bom.Id;
     }
      
     public async Task<Result> UpdateBillOfMaterials(CreateProductBillOfMaterialRequest request, Guid bomId) 
     { 
         var existingBom = await context.ProductBillOfMaterials.FirstOrDefaultAsync(p => p.Id == bomId);
         
         if (existingBom is null)
         {
             return Error.NotFound("ProductBoM.NotFound", "Could not find bom for this product");
         }

         mapper.Map(request, existingBom);

         context.ProductBillOfMaterials.Update(existingBom);
         await context.SaveChangesAsync();
         return Result.Success();
     }

     public async Task<Result> DeleteBillOfMaterials(Guid bomId, Guid userId) 
     { 
         var bom = await context.ProductBillOfMaterials.FirstOrDefaultAsync(p => p.Id == bomId);
         
         if (bom is null)
         {
             return Error.NotFound("ProductBoM.NotFound", "Could not find bom for this product");
         }

         bom.DeletedAt = DateTime.UtcNow;
         bom.LastDeletedById = userId;
         context.ProductBillOfMaterials.Update(bom);
         await context.SaveChangesAsync();
         return Result.Success(); 
     }
}