using APP.Extensions;
using APP.IRepository;
using APP.Services.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/file")]
[ApiController]
public class FileController(IFileRepository fileRepository, IBlobStorageService blobStorageService) : ControllerBase
{
    /// <summary>
    /// Uploads a file to be associated with a model.
    /// </summary>
    /// <param name="modelType">Type of the model to associate the file with.</param>
    /// <param name="modelId">ID of the model to associate the file with.</param>
    /// <param name="reference">A reference to the specific file or attachment.</param>
    /// <param name="file">The file to upload.</param>
    /// <returns>The ID of the uploaded file or an error message.</returns>
    [HttpPost("{modelType}/{modelId}/{reference}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize]
    public async Task<IResult> UploadFile(string modelType, Guid modelId, 
        string reference, [FromForm] IFormFile file)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await fileRepository.SaveBlobItem(modelType.ToLower(), modelId, reference, file, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes all attachments associated with a specific model.
    /// </summary>
    /// <param name="modelId">The ID of the model to delete attachments for.</param>
    /// <returns>204 No Content if successful, or an error message.</returns>
    [HttpDelete("{modelId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize]
    public async Task<IResult> DeleteAttachments(Guid modelId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await fileRepository.DeleteAttachment(modelId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific attachment based on its reference.
    /// </summary>
    /// <param name="modelId">The ID of the attachment.</param>
    /// <param name="reference">The reference of the attachment to delete.</param>
    /// <returns>204 No Content if successful, or an error message.</returns>
    [HttpDelete("{modelId}/{reference}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize]
    public async Task<IResult> DeleteSpecificAttachment(Guid modelId, string reference)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await fileRepository.DeleteAttachment(modelId, reference, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves an image (or any file) from blob storage by its model type, model ID, and reference.
    /// </summary>
    /// <param name="modelType">The type of the model (e.g., "Product", "User", etc.) where the file is associated.</param>
    /// <param name="modelId">The unique identifier of the model (e.g., product ID, user ID, etc.) to which the file is attached.</param>
    /// <param name="reference">A reference name for the specific file (e.g., file name, document ID, etc.).</param>
    /// <returns>Returns the image/file if found, or No Content if not.</returns>
    [HttpGet("{modelType}/{modelId}/{reference}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IFormFile))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IResult> GetImage(string modelType, Guid modelId, string reference)
    {
        try
        {
            var result = await blobStorageService.GetBlobAsync(modelType.ToLower(), $"{modelId}/{reference}");
            if (result.IsFailure) return TypedResults.NoContent();

            var (stream, contentType, name) = result.Value;
            stream.Seek(0, SeekOrigin.Begin);
            return TypedResults.File(stream, contentType, name);
        }
        catch (Exception)
        {
            return TypedResults.NoContent();
        }
    }
}
