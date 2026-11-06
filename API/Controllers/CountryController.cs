using API.Authorization;
using APP.Extensions;
using APP.IRepository;
using DOMAIN.Entities.Countries;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/countries")]
public class CountryController(ICountryRepository repository) : ControllerBase
{

    /// <summary>
    /// Returns a list of countries.
    /// </summary>
    [HttpGet]
    [SignedInUser("The country list, a reference lookup.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CountryDto>))]
    public async Task<IResult> GetCountries()
    {
        var result = await repository.GetCountries();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}