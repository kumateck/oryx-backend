using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Equipments;
using DOMAIN.Entities.Routes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/product")]
[ApiController]
[Authorize]
public class ProductController(IProductRepository repository) : ControllerBase
{
    // Product CRUD operations (existing)

    /// <summary>
    /// Creates a new product.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.CreateProduct(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific product by its ID.
    /// </summary>
    [HttpGet("{productId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProduct(Guid productId)
    {
        var result = await repository.GetProduct(productId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of products.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(Paginateable<IEnumerable<ProductListDto>>)
    )]
    public async Task<IResult> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] Division? division = null,
        [FromQuery] string category = null,
        [FromQuery] bool? isVerified = null
    )
    {
        var result = await repository.GetProducts(
            page,
            pageSize,
            searchQuery,
            departmentId,
            division,
            category,
            isVerified
        );
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a list of product categories.
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ProductCategory>))]
    public async Task<IResult> GetProductCategories()
    {
        var result = await repository.GetProductCategories();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific product by its ID.
    /// </summary>
    [HttpPut("{productId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateProduct(
        [FromBody] UpdateProductRequest request,
        Guid productId
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.UpdateProduct(request, productId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific product package description by its ID.
    /// </summary>
    [HttpPut("package-description/{productId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateProductPackage(
        [FromBody] UpdateProductPackageDescriptionRequest request,
        Guid productId
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.UpdateProductPackageDescription(
            request,
            productId,
            Guid.Parse(userId)
        );
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific product by its ID.
    /// </summary>
    [HttpDelete("{productId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteProduct(Guid productId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.DeleteProduct(productId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the active bom for the product.
    /// </summary>
    [HttpGet("{productId}/bom")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductBillOfMaterialDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetBillOfMaterial(Guid productId)
    {
        var result = await repository.GetBillOfMaterialByProductId(productId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Creates a new route for a product.
    /// </summary>
    [HttpPost("{productId}/routes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateRoute(
        [FromBody] List<CreateRouteRequest> request,
        Guid productId
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.CreateRoute(request, productId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific route by its ID.
    /// </summary>
    [HttpGet("routes/{routeId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RouteDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetRoute(Guid routeId)
    {
        var result = await repository.GetRoute(routeId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of routes.
    /// </summary>
    [HttpGet("{productId}/routes")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<RouteDto>))]
    public async Task<IResult> GetRoutes(Guid productId)
    {
        var result = await repository.GetRoutes(productId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific route by its ID.
    /// </summary>
    [HttpDelete("routes/{routeId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteRoute(Guid routeId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.DeleteRoute(routeId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Creates a new product package.
    /// </summary>
    [HttpPost("{productId}/packages")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateProductPackage(
        [FromBody] List<CreateProductPackageRequest> request,
        Guid productId
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.CreateProductPackage(request, productId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Creates a new product package.
    /// </summary>
    [HttpPost("{productId}/packing")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateProductPacking(
        [FromBody] List<CreateProductPacking> request,
        Guid productId
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.CreateProductPacking(request, productId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves packings lists for a product
    /// </summary>
    [HttpGet("{productId}/packing")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ProductPackingDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProductPackings([FromRoute] Guid productId)
    {
        var result = await repository.GetProductPackings(productId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific product package by its ID.
    /// </summary>
    [HttpGet("packages/{productPackageId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductPackageDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProductPackage(Guid productPackageId)
    {
        var result = await repository.GetProductPackage(productPackageId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of product packages.
    /// </summary>
    [HttpGet("{productId}/packages")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ProductPackageDto>))]
    public async Task<IResult> GetProductPackages(Guid productId)
    {
        var result = await repository.GetProductPackages(productId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific product package by its ID.
    /// </summary>
    [HttpPut("packages/{productPackageId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateProductPackage(
        [FromBody] CreateProductPackageRequest request,
        Guid productPackageId
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.UpdateProductPackage(
            request,
            productPackageId,
            Guid.Parse(userId)
        );
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific product package by its ID.
    /// </summary>
    [HttpDelete("packages/{productPackageId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteProductPackage(Guid productPackageId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.DeleteProductPackage(productPackageId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Creates a new finished product.
    /// </summary>
    [HttpPost("{productId}/finished")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateFinishedProduct(
        [FromBody] List<CreateFinishedProductRequest> request,
        Guid productId
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.CreateFinishedProduct(request, productId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific Bill of Material.
    /// </summary>
    /// <param name="productId">The ID of the Product for which the bom should be archived.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPut("{productId}/bom/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> ArchiveBillOfMaterial(Guid productId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.ArchiveBillOfMaterial(productId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Creates a new equipment.
    /// </summary>
    [HttpPost("equipment")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateEquipment([FromBody] CreateEquipmentRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.CreateEquipment(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves specific equipment by its ID.
    /// </summary>
    [HttpGet("equipment/{equipmentId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EquipmentDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetEquipment(Guid equipmentId)
    {
        var result = await repository.GetEquipment(equipmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of equipment.
    /// </summary>
    [HttpGet("equipment")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(Paginateable<IEnumerable<EquipmentDto>>)
    )]
    public async Task<IResult> GetEquipments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null
    )
    {
        var result = await repository.GetEquipments(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a list of all equipment.
    /// </summary>
    [HttpGet("equipment/all")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<EquipmentDto>))]
    public async Task<IResult> GetAllEquipments()
    {
        var result = await repository.GetEquipments();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates specific equipment by its ID.
    /// </summary>
    [HttpPut("equipment/{equipmentId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateEquipment(
        [FromBody] CreateEquipmentRequest request,
        Guid equipmentId
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.UpdateEquipment(request, equipmentId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes specific equipment by its ID.
    /// </summary>
    [HttpDelete("equipment/{equipmentId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteEquipment(Guid equipmentId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.DeleteEquipment(equipmentId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Imports products from an Excel file.
    /// </summary>
    /// <param name="file">The uploaded Excel file containing product data.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> UploadProducts(IFormFile file)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.ImportProductsFromExcel(file);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Imports product bill of materials (BOM) from an Excel file.
    /// </summary>
    /// <param name="file">The uploaded Excel file containing BOM data.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("bom/upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> UploadProductBom(IFormFile file)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.ImportProductBomFromExcel(file);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Imports product packaging information from an Excel file.
    /// </summary>
    /// <param name="file">The uploaded Excel file containing package data.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("packages/upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> UploadProductPackages(IFormFile file)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.ImportProductPackagesFromExcel(file);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Imports product stock from an Excel file.
    /// </summary>
    /// <param name="file">The uploaded Excel file containing materials.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("upload/stock")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> UploadProductStock(IFormFile file)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.ImportProductStockFromExcel(file);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Imports equipment from an Excel file.
    /// </summary>
    /// <param name="file">The uploaded Excel file containing materials.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("upload/equipment")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> UploadEquipment(IFormFile file)
    {
        var result = await repository.ImportEquipmentFromExcel(file);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
