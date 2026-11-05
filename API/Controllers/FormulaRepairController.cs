using APP.Extensions;
using APP.Services.Formulas;
using APP.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SHARED;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/formula-repairs")]
[Authorize]
public sealed class FormulaRepairController(QuestionFormulaRepairService service) : ControllerBase
{
    [HttpGet("questions/{questionId:guid}", Name = "GetFormulaRepairContext")]
    [ProducesResponseType(typeof(FormulaRepairContextDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IResult> Context(Guid questionId) => Run(() => service.Context(questionId));

    [HttpPost("questions/{questionId:guid}/preview", Name = "PreviewFormulaRepair")]
    [ProducesResponseType(typeof(FormulaRepairPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IResult> Preview(Guid questionId, FormulaRepairCandidate request) => Run(() => service.Preview(questionId, request));

    [HttpPost("questions/{questionId:guid}/apply", Name = "ApplyFormulaRepair")]
    [ProducesResponseType(typeof(FormulaRepairResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IResult> Apply(Guid questionId, ApplyFormulaRepairRequest request) => Run(() => service.Apply(questionId, request));

    [HttpGet("questions/{questionId:guid}/history", Name = "GetFormulaRepairHistory")]
    [ProducesResponseType(typeof(List<FormulaRepairHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IResult> History(Guid questionId) => Run(() => service.History(questionId));

    [HttpPost("tests/{atrId:guid}/fields/{formFieldId:guid}/evaluate", Name = "EvaluateRepairedFormula")]
    [Authorize(PermissionKeys.CanExecuteFormulaResponse)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IResult> Evaluate(Guid atrId, Guid formFieldId, EvaluateRepairedFormulaRequest request) =>
        Run(() => service.Evaluate(new(atrId, formFieldId), request.ExpectedConfigurationHash));

    private static async Task<IResult> Run<T>(Func<Task<Result<T>>> action)
    {
        try { var result = await action(); return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails(); }
        catch (DbUpdateConcurrencyException) { return Changed(); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: "40001" or "40P01" or "55P03" or "23505" }) { return Changed(); }
        catch (PostgresException error) when (error.SqlState is "40001" or "40P01" or "55P03" or "23505") { return Changed(); }
        catch (Exception error) when (error is ArgumentException or System.Text.Json.JsonException or FormatException)
        { return Result.Failure(Error.Validation("FormulaRepair.Configuration", "The formula configuration is invalid. Correct it and preview again.")).ToProblemDetails(); }
    }
    private static IResult Changed() => Result.Failure(Error.Conflict("FormulaRepair.ConcurrentChange",
        "The question, permissions or test changed. Reload the repair preview.")).ToProblemDetails();
}

public sealed record EvaluateRepairedFormulaRequest(string ExpectedConfigurationHash);
