using DOMAIN.Entities.Users.Request;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API.Config.Swagger;

/// <summary>Swashbuckle 6.4 does not reflect JsonRequired; null remains a valid explicit choice.</summary>
public sealed class SwitchDepartmentRequestSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(SwitchDepartmentRequest)) schema.Required.Add("departmentId");
    }
}
