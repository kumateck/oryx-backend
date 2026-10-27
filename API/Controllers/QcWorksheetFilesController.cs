using APP.Extensions;
using APP.Services.Authorization;
using APP.Services.Storage;
using APP.Utils;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/qc/worksheets/instances/{id:guid}/files")]
public sealed class QcWorksheetFilesController(ApplicationDbContext context,
    IBlobStorageService storage) : ControllerBase
{
    [HttpPost("{reference}")]
    [RequestSizeLimit(17825792)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IResult> Upload(Guid id, string reference, IFormFile file)
    {
        if (!Guid.TryParse(HttpContext.Items["Sub"] as string, out var actor)) return TypedResults.Unauthorized();
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        var instance = await context.QcWorksheetInstances.SingleOrDefaultAsync(item => item.Id == id);
        if (instance is null) return TypedResults.NotFound();
        var permission = instance.AnalysisType == DOMAIN.Entities.QcWorksheets.SpecificationAnalysisType.Chemical
            ? QcWorksheetPermissionKeys.CanEnterChemicalWorksheetResult : QcWorksheetPermissionKeys.CanEnterMicrobialWorksheetResult;
        var grants = await CurrentPermissionSet.Load(context, actor);
        if (!grants.Contains(permission) || instance.AssignedToId != actor) return TypedResults.Forbid();
        if (instance.Status != DOMAIN.Entities.QcWorksheets.WorksheetInstanceStatus.InProgress)
            return TypedResults.Conflict();
        if (!System.Text.RegularExpressions.Regex.IsMatch(reference, "^[A-Za-z0-9._-]{1,200}$") || file.Length is <= 0 or > 16777216)
            return TypedResults.BadRequest();
        if (await context.Attachments.AnyAsync(item => item.ModelId == id && item.Reference == reference)) return TypedResults.Conflict();
        // Lock the instance through the upload: a concurrent signature cannot capture a
        // reference whose contents are still changing. References are append-only.
        instance.UpdatedAt = DateTime.UtcNow;
        instance.LastUpdatedById = actor;
        context.Attachments.Add(new DOMAIN.Entities.Attachments.Attachment
        {
            ModelId = id, ModelType = "QcWorksheetInstance", Reference = reference,
            Name = Path.GetFileName(file.FileName), CreatedById = actor
        });
        await context.SaveChangesAsync();
        try
        {
            var result = await storage.UploadBlobAsync("qcworksheetinstance", file, $"{id}/{reference}");
            if (result.IsFailure) return result.ToProblemDetails();
            var currentGrants = await CurrentPermissionSet.Load(context, actor);
            if (!currentGrants.Contains(permission)) return TypedResults.Forbid();
            if (transaction is not null) await transaction.CommitAsync();
            return TypedResults.NoContent();
        }
        catch (Exception)
        {
            return SHARED.Result.Failure(SHARED.Error.Failure("Worksheet.UploadFailed", "Evidence could not be stored. Refresh the worksheet before retrying.")).ToProblemDetails();
        }
    }

    [HttpGet("{reference}")]
    public async Task<IResult> Read(Guid id, string reference)
    {
        if (!Guid.TryParse(HttpContext.Items["Sub"] as string, out var actor)) return TypedResults.Unauthorized();
        var grants = await CurrentPermissionSet.Load(context, actor);
        if (!grants.Contains(QcWorksheetPermissionKeys.CanViewQcTestRequests)) return TypedResults.Forbid();
        if (!await context.QcWorksheetInstances.AnyAsync(item => item.Id == id) ||
            !await context.Attachments.AnyAsync(item => item.ModelId == id && item.ModelType == "QcWorksheetInstance" && item.Reference == reference) ||
            !System.Text.RegularExpressions.Regex.IsMatch(reference, "^[A-Za-z0-9._-]{1,200}$")) return TypedResults.NotFound();
        var result = await storage.GetBlobAsync("qcworksheetinstance", $"{id}/{reference}");
        if (result.IsFailure) return result.ToProblemDetails();
        var (stream, contentType, name) = result.Value;
        return Results.File(stream, contentType, name);
    }
}
