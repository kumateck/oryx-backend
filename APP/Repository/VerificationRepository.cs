using APP.IRepository;
using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Base;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
using SHARED.Requests;

namespace APP.Repository;

public class VerificationRepository(ApplicationDbContext context) : IVerificationRepository
{
    public async Task<Result> VerifyEntity(VerifyRequest request, Guid userId)
    {
        // Each kind of document is verified under its own permission, checked from current data before the change.
        var required = await RequiredKey(request);
        if (required is null)
            return Error.NotFound("Entity.NotFound", $"{request.ModelType} with ID {request.ModelId} was not found.");
        var actor = await CurrentPermissionSet.Load(context, userId);
        if (AccessGuard.Missing(actor, required, "verify this document") is { } refused) return refused;

        IVerifiable entity = request.ModelType switch
        {
            VerifiableEntity.Product => await context.Products.FirstOrDefaultAsync(x =>
                x.Id == request.ModelId
            ),
            VerifiableEntity.MaterialSpecification =>
                await context.MaterialSpecifications.FirstOrDefaultAsync(x =>
                    x.Id == request.ModelId
                ),
            VerifiableEntity.ProductSpecification =>
                await context.ProductSpecifications.FirstOrDefaultAsync(x =>
                    x.Id == request.ModelId
                ),
            VerifiableEntity.MaterialAnalyticalRawData =>
                await context.MaterialAnalyticalRawData.Include(x => x.CoaItems)
                    .FirstOrDefaultAsync(x => x.Id == request.ModelId),
            VerifiableEntity.ProductAnalyticalRawData =>
                await context.ProductAnalyticalRawData.Include(x => x.CoaItems)
                    .FirstOrDefaultAsync(x => x.Id == request.ModelId),
            VerifiableEntity.MicrobialRequirement => await context.MicrobialRequirements.FirstOrDefaultAsync(x =>
                x.Id == request.ModelId),
            VerifiableEntity.RoutineArd => await context.RoutineArds.Include(x => x.CoaItems)
                .FirstOrDefaultAsync(x => x.Id == request.ModelId),
            _ => null,
        };

        if (entity == null)
        {
            return Error.NotFound(
                "Entity.NotFound",
                $"{request.ModelType} with ID {request.ModelId} was not found."
            );
        }

        if (request.ModelType == VerifiableEntity.MicrobialRequirement &&
            (entity as DOMAIN.Entities.QualityRoutines.MicrobialRequirement)?.CreatedById == userId)
            return Error.Validation("MicrobialRequirement.SeparationOfDuties",
                "The author cannot verify their own microbial applicability change.");

        var hasReportableItems = entity switch
        {
            DOMAIN.Entities.MaterialARD.MaterialAnalyticalRawData material =>
                material.CoaItems.Count > 0,
            DOMAIN.Entities.ProductAnalyticalRawData.ProductAnalyticalRawData product =>
                product.CoaItems.Count > 0,
            DOMAIN.Entities.QualityRoutines.RoutineArd routine =>
                routine.CoaItems.Any(item => item.IncludeOnCoa),
            _ => true
        };
        if (!hasReportableItems)
            return Error.Conflict("Ard.CoaItems",
                "An ARD requires controlled reportable COA items before verification.");

        entity.IsVerified = true;
        entity.VerifiedAt = DateTime.UtcNow;
        entity.VerifiedById = userId;

        await context.SaveChangesAsync();
        return Result.Success();
    }

    /// <summary>The permission that verifies this record, or null when it does not exist.</summary>
    private async Task<string> RequiredKey(VerifyRequest request)
    {
        MaterialKind? kind;
        switch (request.ModelType)
        {
            case VerifiableEntity.Product: return PermissionKeys.CanVerifyProduct;
            case VerifiableEntity.ProductSpecification: return PermissionKeys.CanVerifyProductSpecification;
            case VerifiableEntity.ProductAnalyticalRawData: return PermissionKeys.CanVerifyProductArd;
            case VerifiableEntity.MicrobialRequirement: return PermissionKeys.CanManageMicrobialRequirements;
            case VerifiableEntity.RoutineArd: return PermissionKeys.CanManageRoutineSetup;
            case VerifiableEntity.MaterialSpecification:
                kind = await context.MaterialSpecifications.Where(x => x.Id == request.ModelId).Select(x => (MaterialKind?)x.Material.Kind).FirstOrDefaultAsync();
                return kind is null ? null : kind == MaterialKind.Raw ? PermissionKeys.CanVerifyRawMaterialSpecification : PermissionKeys.CanVerifyPackagingMaterialSpecification;
            case VerifiableEntity.MaterialAnalyticalRawData:
                kind = await context.MaterialAnalyticalRawData.Where(x => x.Id == request.ModelId)
                    .Select(x => (MaterialKind?)x.MaterialStandardTestProcedure.Material.Kind).FirstOrDefaultAsync();
                return kind is null ? null : kind == MaterialKind.Raw ? PermissionKeys.CanVerifyRawMaterialArd : PermissionKeys.CanVerifyPackagingMaterialArd;
            default: return null;
        }
    }
}
