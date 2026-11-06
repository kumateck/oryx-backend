using APP.Extensions;
using APP.Services.Materials;
using APP.Utils;
using DOMAIN.Entities.Materials.WhitePlacebo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SHARED;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/white-placebo")]
[Authorize(PermissionKeys.CanViewWhitePlacebo)]
public sealed class WhitePlaceboController(WhitePlaceboService service) : ControllerBase
{
    [HttpGet("workspace", Name = "GetWhitePlaceboWorkspace")]
    [ProducesResponseType(typeof(WhitePlaceboWorkspaceDto), StatusCodes.Status200OK)]
    public async Task<IResult> Workspace() => Reply(await service.Workspace());

    [HttpPut("recipe", Name = "SaveWhitePlaceboRecipe")]
    [Authorize(PermissionKeys.CanEditWhitePlaceboRecipe)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IResult> SaveRecipe([FromBody] SaveWhitePlaceboRecipe body) => Write(() => service.SaveRecipe(body));

    [HttpGet(Name = "ListWhitePlaceboPreparations")]
    [ProducesResponseType(typeof(WhitePlaceboPreparationPageDto), StatusCodes.Status200OK)]
    public async Task<IResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string search = "")
        => Reply(await service.List(page, pageSize, search));

    [HttpGet("{id:guid}", Name = "GetWhitePlaceboPreparation")]
    [ProducesResponseType(typeof(WhitePlaceboPreparationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> Get(Guid id) => Reply(await service.Get(id));

    [HttpPost(Name = "CreateWhitePlaceboPreparation")]
    [Authorize(PermissionKeys.CanRequestWhitePlaceboIngredients)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IResult> Create([FromBody] CreateWhitePlaceboPreparation body) => Concurrent(async () => Reply(await service.Create(body)));

    [HttpPost("{id:guid}/issue", Name = "IssueWhitePlaceboIngredients")]
    [Authorize(PermissionKeys.CanIssueWhitePlaceboIngredients)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IResult> Issue(Guid id) => Write(() => service.Issue(id));

    [HttpPost("{id:guid}/output", Name = "RecordWhitePlaceboOutput")]
    [Authorize(PermissionKeys.CanRecordWhitePlaceboOutput)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IResult> Output(Guid id, [FromBody] RecordWhitePlaceboOutput body) => Write(() => service.RecordOutput(id, body));

    private static IResult Reply<T>(Result<T> result) => result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    private static Task<IResult> Write(Func<Task<Result>> action) => Concurrent(async () => {
        var result = await action();
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    });
    private static async Task<IResult> Concurrent(Func<Task<IResult>> action)
    {
        try { return await action(); }
        catch (DbUpdateConcurrencyException) { return Changed(); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: "40001" or "23505" }) { return Changed(); }
        catch (PostgresException error) when (error.SqlState is "40001" or "23505") { return Changed(); }
    }
    private static IResult Changed() => TypedResults.Problem(statusCode: 409,
        detail: "The recipe, preparation or stock changed during this action. Reload the saved record before trying again.");
}
