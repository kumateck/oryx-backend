using APP.Extensions;
using Microsoft.AspNetCore.Http;
using SHARED;
using Xunit;

namespace APP.Tests.FullProcedures;

public sealed class ForbiddenResultTests
{
    [Fact]
    public void Forbidden_domain_error_maps_to_http_403()
    {
        var result = Result.Failure(Error.Forbidden(
            "TemplateArea.AccessDenied", "Area assignment is required."));

        var problem = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result.ToProblemDetails());

        Assert.Equal(StatusCodes.Status403Forbidden, problem.StatusCode);
    }
}
