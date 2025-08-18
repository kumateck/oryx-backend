using APP.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Configurations;
using SHARED.Requests;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/configuration")]
[ApiController]
public class ConfigurationController(IConfigurationRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new configuration.
    /// </summary>
    /// <param name="request">The configuration creation request.</param>
    /// <returns>Returns the ID of the newly created configuration.</returns>
    [HttpPost]
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateConfiguration([FromBody] CreateConfigurationRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreateConfiguration(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a configuration by ID.
    /// </summary>
    /// <param name="configurationId">The ID of the configuration to retrieve.</param>
    /// <returns>Returns the detailed information of the configuration.</returns>
    [HttpGet("{configurationId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ConfigurationDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetConfiguration(Guid configurationId)
    {
        var result = await repository.GetConfiguration(configurationId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a configuration by modelType.
    /// </summary>
    /// <param name="modelType">The modeltype of the configuration to retrieve.</param>
    /// <returns>Returns the detailed information of the configuration.</returns>
    [HttpGet("by-model-type/{modelType}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ConfigurationDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetConfiguration(string modelType)
    {
        var result = await repository.GetConfiguration(modelType);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a list of configurations.
    /// </summary>
    /// <param name="page">Page number of the pagination.</param>
    /// <param name="pageSize">Number of configurations per page.</param>
    /// <param name="searchQuery">Search query to filter configurations.</param>
    /// <returns>A paginated list of configurations.</returns>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ConfigurationDto>>))]
    public async Task<IResult> GetConfigurations([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetConfigurations(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific configuration by ID.
    /// </summary>
    /// <param name="request">The update request containing the new configuration data.</param>
    /// <param name="configurationId">The ID of the configuration to update.</param>
    /// <returns>Returns success if the update was successful; otherwise returns an error.</returns>
    [HttpPut("{configurationId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateConfiguration([FromBody] CreateConfigurationRequest request, Guid configurationId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateConfiguration(request, configurationId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Deletes a configuration by ID.
    /// </summary>
    /// <param name="configurationId">The ID of the configuration to delete.</param>
    /// <returns>Returns success if the configuration was successfully deleted.</returns>
    [HttpDelete("{configurationId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteConfiguration(Guid configurationId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteConfiguration(configurationId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets a list of all Naming Types.
    /// </summary>
    /// <remarks>
    /// This endpoint returns a list of all available question validation types along with their integer values.
    /// The `value` in the response is what should be passed to the POST Question API when specifying a naming type.
    /// </remarks>
    /// <returns>A list of Naming Types with their corresponding value and name.</returns>
    /// <response code="200">Returns the list of namning types</response>
    [HttpGet("naming-types")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<TypeResponse>))]
    public IResult GetQuestionValidationTypes()
    {
        var types = Enum.GetValues(typeof(NamingType))
            .Cast<NamingType>()
            .Select(qt => new TypeResponse
            {
                Value = (int)qt,
                Name = qt.ToString()
            })
            .ToList();

        return TypedResults.Ok(types);
    }

    /// <summary>
    /// Retrieves the count of items using a particular code config
    /// </summary>
    /// <param name="modelType">The model type of which the count is need </param>
    /// <param name="prefix">The prefix of the particular model </param>
    /// <returns>Returns the count or usage of the configuration.</returns>
    [HttpGet("{modelType}/count")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(int))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetConfiguration([FromRoute ]string modelType, [FromQuery] string prefix)
    {
        var result = await repository.GetCountForCodeConfiguration(modelType, prefix);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
