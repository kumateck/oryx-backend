using System.Security.Claims;
using APP.Utils;
using DOMAIN.Entities.MaterialStandardTestProcedures;
using DOMAIN.Entities.ProductStandardTestProcedures;
using DOMAIN.Entities.StpDocuments;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.StpDocuments;

public sealed class StpDocumentAccessService(
    ApplicationDbContext context,
    IAuthorizationService authorizationService) : IStpDocumentAccessService
{
    public async Task<bool> CanAccessOwner(ClaimsPrincipal user, string ownerType, bool write)
    {
        string[] policies = ownerType switch
        {
            nameof(ProductStandardTestProcedure) => write
                ? [PermissionKeys.CanEditProductStp]
                : [PermissionKeys.CanViewProductStps],
            nameof(MaterialStandardTestProcedure) => write
                ? [PermissionKeys.CanEditRawMaterialStp, PermissionKeys.CanEditPackagingMaterialStp]
                : [PermissionKeys.CanViewRawMaterialStps, PermissionKeys.CanViewPackagingMaterialStps],
            _ => []
        };

        foreach (var policy in policies)
        {
            if ((await authorizationService.AuthorizeAsync(user, policy)).Succeeded)
                return true;
        }

        return false;
    }

    public async Task<bool> CanAccessDocument(ClaimsPrincipal user, Guid documentId, bool write)
    {
        var ownerType = await context.StpDocuments
            .Where(document => document.Id == documentId)
            .Select(document => document.OwnerType)
            .FirstOrDefaultAsync();
        return ownerType != null && await CanAccessOwner(user, ownerType, write);
    }

    public async Task<bool> CanOpenEditor(ClaimsPrincipal user, Guid documentId)
    {
        var document = await context.StpDocuments
            .Where(item => item.Id == documentId)
            .Select(item => new { item.OwnerType, item.Status })
            .FirstOrDefaultAsync();
        if (document == null) return false;

        var write = document.Status == StpDocumentStatus.Draft;
        return await CanAccessOwner(user, document.OwnerType, write);
    }
}
