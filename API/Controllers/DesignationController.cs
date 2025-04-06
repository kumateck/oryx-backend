using APP.Extensions;
using APP.IRepository;
using DOMAIN.Entities.Designations;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class DesignationController(IDesignationRepository repository): ControllerBase
{

    public async Task<IResult> CreateDesignation([FromBody] CreateDesignationRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.CreateDesignation(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}