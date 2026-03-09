using APP.IRepository;
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
                await context.MaterialAnalyticalRawData.FirstOrDefaultAsync(x =>
                    x.Id == request.ModelId
                ),
            VerifiableEntity.ProductAnalyticalRawData =>
                await context.ProductAnalyticalRawData.FirstOrDefaultAsync(x =>
                    x.Id == request.ModelId
                ),
            _ => null,
        };

        if (entity == null)
        {
            return Error.NotFound(
                "Entity.NotFound",
                $"{request.ModelType} with ID {request.ModelId} was not found."
            );
        }

        entity.IsVerified = true;
        entity.VerifiedAt = DateTime.UtcNow;
        entity.VerifiedById = userId;

        await context.SaveChangesAsync();
        return Result.Success();
    }
}
