using APP.IRepository;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.QualityRoutines;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class MicrobialRequirementRepository(ApplicationDbContext context)
    : IMicrobialRequirementRepository
{
    public async Task<Result<Guid>> Create(CreateMicrobialRequirementRequest request, Guid actorId)
    {
        if (!Enum.IsDefined(request.Subject) || string.IsNullOrWhiteSpace(request.Reason))
            return Error.Validation("MicrobialRequirement", "Valid subject and reason are required.");
        if (request.Subject == RequirementSubject.Material)
        {
            if (!request.MaterialId.HasValue || request.ProductId.HasValue ||
                request.Stage.HasValue)
                return Error.Validation("MicrobialRequirement.Material",
                    "Material requirement needs one material and no product stage.");
            if (!await context.Materials.AnyAsync(item => item.Id == request.MaterialId))
                return Error.NotFound("MicrobialRequirement.Material",
                    "Material was not found.");
        }
        else
        {
            if (!request.ProductId.HasValue || request.MaterialId.HasValue ||
                request.Stage != TestStage.Finished)
                return Error.Validation("MicrobialRequirement.Product",
                    "Product microbial applicability is for the Finished stage only.");
            if (!await context.Products.AnyAsync(item => item.Id == request.ProductId))
                return Error.NotFound("MicrobialRequirement.Product",
                    "Product was not found.");
        }
        if (await context.MicrobialRequirements.AnyAsync(item =>
            !item.IsVerified && item.Subject == request.Subject &&
            item.MaterialId == request.MaterialId && item.ProductId == request.ProductId &&
            item.Stage == request.Stage))
            return Error.Conflict("MicrobialRequirement.Pending",
                "A pending configuration already exists for this material or product stage.");
        var requirement = new MicrobialRequirement
        {
            Id = Guid.NewGuid(), Subject = request.Subject,
            MaterialId = request.MaterialId, ProductId = request.ProductId,
            Stage = request.Stage, Required = request.Required,
            Reason = request.Reason.Trim(), CreatedById = actorId
        };
        context.MicrobialRequirements.Add(requirement);
        await context.SaveChangesAsync();
        return requirement.Id;
    }

    public async Task<Result<List<MicrobialRequirementDto>>> List(
        Guid? materialId, Guid? productId)
    {
        if (materialId.HasValue && productId.HasValue)
            return Error.Validation("MicrobialRequirement.Filter",
                "Filter by material or product, not both.");
        var query = context.MicrobialRequirements
            .Include(item => item.Material).Include(item => item.Product).AsQueryable();
        if (materialId.HasValue) query = query.Where(item => item.MaterialId == materialId);
        if (productId.HasValue) query = query.Where(item => item.ProductId == productId);
        var items = await query.OrderByDescending(item => item.VerifiedAt)
            .ThenByDescending(item => item.CreatedAt).ToListAsync();
        return items.Select(item => new MicrobialRequirementDto
        {
            Id = item.Id, Subject = item.Subject, MaterialId = item.MaterialId,
            ProductId = item.ProductId, SubjectName = item.Material?.Name ?? item.Product?.Name,
            Stage = item.Stage,
            Required = item.Required, Reason = item.Reason,
            IsVerified = item.IsVerified, VerifiedAt = item.VerifiedAt
        }).ToList();
    }
}
