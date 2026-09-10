using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.RndAnalyticalMethods;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class RndAnalyticalMethodRepository(ApplicationDbContext context, IMapper mapper)
    : IRndAnalyticalMethodRepository
{
    private static Result<RndAnalyticalMethod> ValidateStatus(
        RndAnalyticalMethod method,
        params RndAnalyticalMethodStatus[] allowed
    )
    {
        if (!allowed.Contains(method.Status))
            return Error.Validation(
                "RndAnalyticalMethod.InvalidStatus",
                $"This action is not allowed while the method is {method.Status}."
            );

        return method;
    }

    private async Task<Result> ValidateSubjectAndProtocol(
        Guid? materialId,
        Guid? productId,
        Guid? validationProtocolFormId
    )
    {
        if (materialId.HasValue == productId.HasValue)
            return Error.Validation(
                "RndAnalyticalMethod.Subject",
                "Exactly one of MaterialId or ProductId must be set."
            );

        if (materialId.HasValue && !await context.Materials.AnyAsync(m => m.Id == materialId.Value))
            return Error.NotFound("RndAnalyticalMethod.MaterialNotFound", "Material not found.");

        if (productId.HasValue && !await context.Products.AnyAsync(p => p.Id == productId.Value))
            return Error.NotFound("RndAnalyticalMethod.ProductNotFound", "Product not found.");

        if (
            validationProtocolFormId.HasValue
            && !await context.Forms.AnyAsync(f => f.Id == validationProtocolFormId.Value)
        )
            return Error.NotFound("RndAnalyticalMethod.FormNotFound", "Validation protocol form not found.");

        return Result.Success();
    }

    public async Task<Result<Guid>> CreateMethod(
        Guid rndProjectId,
        CreateRndAnalyticalMethodRequest request,
        Guid userId
    )
    {
        var project = await context.RndProjects.FirstOrDefaultAsync(p => p.Id == rndProjectId);
        if (project is null)
            return Error.NotFound("RndAnalyticalMethod.ProjectNotFound", "R&D project not found.");

        var approvalGate = project.EnsureApprovedForProgression("R&D project");
        if (approvalGate.IsFailure)
            return approvalGate.Error;

        var validation = await ValidateSubjectAndProtocol(
            request.MaterialId,
            request.ProductId,
            request.ValidationProtocolFormId
        );
        if (validation.IsFailure)
            return Result.Failure<Guid>(validation.Errors);

        var method = new RndAnalyticalMethod
        {
            RndProjectId = rndProjectId,
            MaterialId = request.MaterialId,
            ProductId = request.ProductId,
            MethodName = request.MethodName,
            Description = request.Description,
            ValidationProtocolFormId = request.ValidationProtocolFormId,
            Status = RndAnalyticalMethodStatus.Draft,
            CreatedById = userId,
        };

        await context.RndAnalyticalMethods.AddAsync(method);
        await context.SaveChangesAsync();
        return method.Id;
    }

    public async Task<Result> UpdateMethod(Guid id, CreateRndAnalyticalMethodRequest request, Guid userId)
    {
        var method = await context.RndAnalyticalMethods.FirstOrDefaultAsync(m => m.Id == id);
        if (method is null)
            return Error.NotFound("RndAnalyticalMethod.NotFound", "Analytical method not found.");

        var statusCheck = ValidateStatus(method, RndAnalyticalMethodStatus.Draft);
        if (statusCheck.IsFailure)
            return statusCheck.Error;

        var validation = await ValidateSubjectAndProtocol(
            request.MaterialId,
            request.ProductId,
            request.ValidationProtocolFormId
        );
        if (validation.IsFailure)
            return validation;

        method.MaterialId = request.MaterialId;
        method.ProductId = request.ProductId;
        method.MethodName = request.MethodName;
        method.Description = request.Description;
        method.ValidationProtocolFormId = request.ValidationProtocolFormId;
        method.LastUpdatedById = userId;

        context.RndAnalyticalMethods.Update(method);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> UpdateStatus(Guid id, UpdateRndAnalyticalMethodStatusRequest request, Guid userId)
    {
        var method = await context.RndAnalyticalMethods.FirstOrDefaultAsync(m => m.Id == id);
        if (method is null)
            return Error.NotFound("RndAnalyticalMethod.NotFound", "Analytical method not found.");

        var validTransition = (method.Status, request.Status) switch
        {
            (RndAnalyticalMethodStatus.Draft, RndAnalyticalMethodStatus.UnderValidation) => true,
            (RndAnalyticalMethodStatus.UnderValidation, RndAnalyticalMethodStatus.Validated) => true,
            (RndAnalyticalMethodStatus.UnderValidation, RndAnalyticalMethodStatus.Draft) => true,
            _ => false,
        };

        if (!validTransition)
            return Error.Validation(
                "RndAnalyticalMethod.InvalidStatus",
                $"Cannot move an analytical method from {method.Status} to {request.Status}."
            );

        method.Status = request.Status;
        method.LastUpdatedById = userId;

        if (request.Status == RndAnalyticalMethodStatus.Validated)
        {
            method.ValidatedAt = DateTime.UtcNow;
            method.ValidatedById = userId;
        }

        context.RndAnalyticalMethods.Update(method);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    private IQueryable<RndAnalyticalMethod> MethodDetailQuery() =>
        context
            .RndAnalyticalMethods.AsSplitQuery()
            .Include(m => m.Material)
            .Include(m => m.Product)
            .Include(m => m.ValidatedBy);

    public async Task<Result<RndAnalyticalMethodDto>> GetMethod(Guid id)
    {
        var method = await MethodDetailQuery().FirstOrDefaultAsync(m => m.Id == id);
        return method is null
            ? Error.NotFound("RndAnalyticalMethod.NotFound", "Analytical method not found.")
            : mapper.Map<RndAnalyticalMethodDto>(method);
    }

    public async Task<Result<Paginateable<IEnumerable<RndAnalyticalMethodDto>>>> GetMethodsForProject(
        Guid rndProjectId,
        int page,
        int pageSize
    )
    {
        var query = MethodDetailQuery()
            .Where(m => m.RndProjectId == rndProjectId)
            .OrderByDescending(m => m.CreatedAt)
            .AsQueryable();

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<RndAnalyticalMethodDto>);
    }
}
