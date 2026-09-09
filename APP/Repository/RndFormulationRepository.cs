using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.RndFormulations;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class RndFormulationRepository(ApplicationDbContext context, IMapper mapper) : IRndFormulationRepository
{
    private static Result<RndFormulation> ValidateStatus(
        RndFormulation formulation,
        params RndFormulationStatus[] allowed
    )
    {
        if (!allowed.Contains(formulation.Status))
            return Error.Validation(
                "RndFormulation.InvalidStatus",
                $"This action is not allowed while the formulation is {formulation.Status}."
            );

        return formulation;
    }

    private async Task<Result<List<RndFormulationItem>>> BuildItems(
        List<CreateRndFormulationItemRequest> requestItems
    )
    {
        var materialIds = requestItems.Select(i => i.MaterialId).ToList();
        var existingMaterialIds = await context
            .Materials.Where(m => materialIds.Contains(m.Id))
            .Select(m => m.Id)
            .ToListAsync();
        var missing = materialIds.Except(existingMaterialIds).ToList();
        if (missing.Count != 0)
            return Error.NotFound("RndFormulation.MaterialNotFound", "One or more materials were not found.");

        var items = requestItems
            .Select(item => new RndFormulationItem
            {
                MaterialId = item.MaterialId,
                MaterialTypeId = item.MaterialTypeId,
                Grade = item.Grade,
                CasNumber = item.CasNumber,
                Order = item.Order,
                IsSubstitutable = item.IsSubstitutable,
                BaseQuantity = item.BaseQuantity,
                BaseUoMId = item.BaseUoMId,
                PrescribedQuantity = item.PrescribedQuantity,
                Percentage = item.Percentage,
                Substitutes = item
                    .SubstituteMaterialIds.Select(substituteId => new RndFormulationItemSubstitute
                    {
                        SubstituteMaterialId = substituteId,
                    })
                    .ToList(),
            })
            .ToList();

        return items;
    }

    public async Task<Result<Guid>> CreateFormulation(
        Guid rndProjectId,
        CreateRndFormulationRequest request,
        Guid userId
    )
    {
        var project = await context.RndProjects.FirstOrDefaultAsync(p => p.Id == rndProjectId);
        if (project is null)
            return Error.NotFound("RndFormulation.ProjectNotFound", "R&D project not found.");

        var approvalGate = project.EnsureApprovedForProgression("R&D project");
        if (approvalGate.IsFailure)
            return approvalGate.Error;

        var itemsResult = await BuildItems(request.Items);
        if (itemsResult.IsFailure)
            return Result.Failure<Guid>(itemsResult.Errors);

        var formulation = new RndFormulation
        {
            RndProjectId = rndProjectId,
            Version = 1,
            Status = RndFormulationStatus.Draft,
            Items = itemsResult.Value,
            CreatedById = userId,
        };

        await context.RndFormulations.AddAsync(formulation);
        await context.SaveChangesAsync();
        return formulation.Id;
    }

    public async Task<Result<Guid>> CreateNewVersion(
        Guid previousFormulationId,
        CreateRndFormulationRequest request,
        Guid userId
    )
    {
        var previous = await context.RndFormulations.FirstOrDefaultAsync(f => f.Id == previousFormulationId);
        if (previous is null)
            return Error.NotFound("RndFormulation.NotFound", "Formulation not found.");

        var project = await context.RndProjects.FirstOrDefaultAsync(p => p.Id == previous.RndProjectId);
        if (project is null)
            return Error.NotFound("RndFormulation.ProjectNotFound", "R&D project not found.");

        var approvalGate = project.EnsureApprovedForProgression("R&D project");
        if (approvalGate.IsFailure)
            return approvalGate.Error;

        var itemsResult = await BuildItems(request.Items);
        if (itemsResult.IsFailure)
            return Result.Failure<Guid>(itemsResult.Errors);

        var newVersion = new RndFormulation
        {
            RndProjectId = previous.RndProjectId,
            Version = previous.Version + 1,
            Status = RndFormulationStatus.Draft,
            Items = itemsResult.Value,
            CreatedById = userId,
        };

        previous.Status = RndFormulationStatus.Superseded;
        previous.LastUpdatedById = userId;
        context.RndFormulations.Update(previous);

        await context.RndFormulations.AddAsync(newVersion);
        await context.SaveChangesAsync();
        return newVersion.Id;
    }

    public async Task<Result> UpdateFormulation(Guid id, CreateRndFormulationRequest request, Guid userId)
    {
        var formulation = await context
            .RndFormulations.Include(f => f.Items)
                .ThenInclude(i => i.Substitutes)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (formulation is null)
            return Error.NotFound("RndFormulation.NotFound", "Formulation not found.");

        var statusCheck = ValidateStatus(formulation, RndFormulationStatus.Draft);
        if (statusCheck.IsFailure)
            return statusCheck.Error;

        var itemsResult = await BuildItems(request.Items);
        if (itemsResult.IsFailure)
            return Result.Failure(itemsResult.Errors);

        context.RndFormulationItems.RemoveRange(formulation.Items);
        formulation.Items = itemsResult.Value;
        formulation.LastUpdatedById = userId;

        context.RndFormulations.Update(formulation);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> UpdateStatus(Guid id, RndFormulationStatus status, Guid userId)
    {
        var formulation = await context.RndFormulations.FirstOrDefaultAsync(f => f.Id == id);
        if (formulation is null)
            return Error.NotFound("RndFormulation.NotFound", "Formulation not found.");

        var validTransition = (formulation.Status, status) switch
        {
            (RndFormulationStatus.Draft, RndFormulationStatus.InReview) => true,
            (RndFormulationStatus.InReview, RndFormulationStatus.Approved) => true,
            (RndFormulationStatus.InReview, RndFormulationStatus.Draft) => true,
            _ => false,
        };

        if (!validTransition)
            return Error.Validation(
                "RndFormulation.InvalidStatus",
                $"Cannot move a formulation from {formulation.Status} to {status}."
            );

        formulation.Status = status;
        formulation.LastUpdatedById = userId;

        context.RndFormulations.Update(formulation);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    private IQueryable<RndFormulation> FormulationDetailQuery() =>
        context
            .RndFormulations.AsSplitQuery()
            .Include(f => f.Items)
                .ThenInclude(i => i.Material)
            .Include(f => f.Items)
                .ThenInclude(i => i.MaterialType)
            .Include(f => f.Items)
                .ThenInclude(i => i.BaseUoM)
            .Include(f => f.Items)
                .ThenInclude(i => i.Substitutes);

    private RndFormulationDto MapFormulation(RndFormulation formulation)
    {
        var dto = mapper.Map<RndFormulationDto>(formulation);
        dto.Items = formulation
            .Items.Select(item => new RndFormulationItemDto
            {
                Id = item.Id,
                Material = mapper.Map<CollectionItemDto>(item.Material),
                MaterialType = mapper.Map<CollectionItemDto>(item.MaterialType),
                Grade = item.Grade,
                CasNumber = item.CasNumber,
                Order = item.Order,
                IsSubstitutable = item.IsSubstitutable,
                BaseQuantity = item.BaseQuantity,
                BaseUoM = mapper.Map<CollectionItemDto>(item.BaseUoM),
                PrescribedQuantity = item.PrescribedQuantity,
                Percentage = item.Percentage,
                SubstituteMaterialIds = item.Substitutes.Select(s => s.SubstituteMaterialId).ToList(),
            })
            .ToList();
        return dto;
    }

    public async Task<Result<RndFormulationDto>> GetFormulation(Guid id)
    {
        var formulation = await FormulationDetailQuery().FirstOrDefaultAsync(f => f.Id == id);
        return formulation is null
            ? Error.NotFound("RndFormulation.NotFound", "Formulation not found.")
            : MapFormulation(formulation);
    }

    public async Task<Result<Paginateable<IEnumerable<RndFormulationDto>>>> GetFormulationsForProject(
        Guid rndProjectId,
        int page,
        int pageSize
    )
    {
        var query = FormulationDetailQuery()
            .Where(f => f.RndProjectId == rndProjectId)
            .OrderByDescending(f => f.Version)
            .AsQueryable();

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, MapFormulation);
    }
}
