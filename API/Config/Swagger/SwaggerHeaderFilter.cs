using APP.Utils;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.Warehouses;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API.Config.Swagger;

public class SwaggerHeaderFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();

        var moduleHeader = new OpenApiParameter
        {
            Name = AppConstants.Module,
            In = ParameterLocation.Header,
            Description = "The module this request falls under",
            Required = false,
            Example = new OpenApiString(nameof(Warehouse))
        };
        
        var subModuleHeader = new OpenApiParameter
        {
            Name = AppConstants.SubModule,
            In = ParameterLocation.Header,
            Description = "The sub module this request falls under",
            Required = false,
            Example = new OpenApiString(nameof(PurchaseOrder))
        };

        operation.Parameters.Add(moduleHeader);
        operation.Parameters.Add(subModuleHeader);
    }
}