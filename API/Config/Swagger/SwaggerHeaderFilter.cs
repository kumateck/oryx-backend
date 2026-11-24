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

        if (context.MethodInfo.DeclaringType == typeof(API.Controllers.ApprovalController)
            && context.MethodInfo.Name == nameof(API.Controllers.ApprovalController.ApproveItem))
            operation.Parameters.Add(new OpenApiParameter {
                Name = "If-Match", In = ParameterLocation.Header, Required = false,
                Description = "Pending product COA reporting evidence token returned by the response detail. Required when approving configured assay reporting.",
                Schema = new OpenApiSchema { Type = "string" }
            });
        operation.Parameters.Add(moduleHeader);
        operation.Parameters.Add(subModuleHeader);
    }
}