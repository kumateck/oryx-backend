using APP.Extensions;
using APP.Services.Forms;
using APP.Utils;
using DOMAIN.Entities.Forms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SHARED;
using SHARED.Services.Identity;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/pending-certificates/{responseId:guid}")]
public sealed class PendingCertificateController(PendingCertificateService service, ICurrentUserService actor) : ControllerBase
{
    [HttpPut("product", Name = "SavePendingProductCertificate")]
    [Authorize(PermissionKeys.CanEditProductCertificateOfAnalysis)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IResult> SaveProduct(Guid responseId, SavePendingCertificate request) =>
        Write(userId => service.Save(responseId, userId, TemplateRepairTestKind.Product, request));

    [HttpPost("product/review-request", Name = "RequestProductCertificateReview")]
    [Authorize(PermissionKeys.CanRequestProductCertificateReview)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IResult> RequestProductReview(Guid responseId, RequestCertificateChange request) =>
        Write(userId => service.Request(responseId, userId, TemplateRepairTestKind.Product, CertificateReturnKind.Review, request));

    [HttpPost("product/correction-request", Name = "RequestProductCertificateCorrection")]
    [Authorize(PermissionKeys.CanRequestProductCertificateCorrection)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IResult> RequestProductCorrection(Guid responseId, RequestCertificateChange request) =>
        Write(userId => service.Request(responseId, userId, TemplateRepairTestKind.Product, CertificateReturnKind.Correction, request));

    [HttpPut("raw-material", Name = "SavePendingRawMaterialCertificate")]
    [Authorize(PermissionKeys.CanEditRawMaterialCertificateOfAnalysis)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IResult> SaveRawMaterial(Guid responseId, SavePendingCertificate request) =>
        Write(userId => service.Save(responseId, userId, TemplateRepairTestKind.RawMaterial, request));

    [HttpPost("raw-material/review-request", Name = "RequestRawMaterialCertificateReview")]
    [Authorize(PermissionKeys.CanRequestRawMaterialCertificateReview)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IResult> RequestRawMaterialReview(Guid responseId, RequestCertificateChange request) =>
        Write(userId => service.Request(responseId, userId, TemplateRepairTestKind.RawMaterial, CertificateReturnKind.Review, request));

    [HttpPost("raw-material/correction-request", Name = "RequestRawMaterialCertificateCorrection")]
    [Authorize(PermissionKeys.CanRequestRawMaterialCertificateCorrection)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IResult> RequestRawMaterialCorrection(Guid responseId, RequestCertificateChange request) =>
        Write(userId => service.Request(responseId, userId, TemplateRepairTestKind.RawMaterial, CertificateReturnKind.Correction, request));

    [HttpPut("packaging-material", Name = "SavePendingPackagingMaterialCertificate")]
    [Authorize(PermissionKeys.CanEditPackagingMaterialCertificateOfAnalysis)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IResult> SavePackagingMaterial(Guid responseId, SavePendingCertificate request) =>
        Write(userId => service.Save(responseId, userId, TemplateRepairTestKind.PackagingMaterial, request));

    [HttpPost("packaging-material/review-request", Name = "RequestPackagingMaterialCertificateReview")]
    [Authorize(PermissionKeys.CanRequestPackagingMaterialCertificateReview)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IResult> RequestPackagingMaterialReview(Guid responseId, RequestCertificateChange request) =>
        Write(userId => service.Request(responseId, userId, TemplateRepairTestKind.PackagingMaterial, CertificateReturnKind.Review, request));

    [HttpPost("packaging-material/correction-request", Name = "RequestPackagingMaterialCertificateCorrection")]
    [Authorize(PermissionKeys.CanRequestPackagingMaterialCertificateCorrection)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IResult> RequestPackagingMaterialCorrection(Guid responseId, RequestCertificateChange request) =>
        Write(userId => service.Request(responseId, userId, TemplateRepairTestKind.PackagingMaterial, CertificateReturnKind.Correction, request));

    private async Task<IResult> Write(Func<Guid, Task<Result>> action)
    {
        if (actor.UserId is not { } userId) return TypedResults.Unauthorized();
        var result = await action(userId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
