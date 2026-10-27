using API.Filters;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
namespace API.Config.Swagger;
public sealed class ReportHistoryParameterFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!context.MethodInfo.IsDefined(typeof(ReportHistoryAttribute), true)) return;
        operation.Parameters.Add(new OpenApiParameter { Name = "asOfDate", In = ParameterLocation.Query, Required = false,
            Description = "Recorded historical state at this UTC instant. Today is capped at the current instant; pre-coverage dates return 409.", Schema = new OpenApiSchema { Type = "string", Format = "date-time", Nullable = true } });
        operation.Responses.TryAdd("409", new OpenApiResponse { Description = "Historical evidence unavailable for the selected date or report dependency." });
    }
}
