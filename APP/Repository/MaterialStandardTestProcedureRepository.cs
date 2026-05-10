using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.MaterialStandardTestProcedures;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class MaterialStandardTestProcedureRepository(ApplicationDbContext context, IMapper mapper)
    : IMaterialStandardTestProcedureRepository
{
    public async Task<Result> CreateMaterialStandardTestProcedure(
        CreateMaterialStandardTestProcedureRequest request
    )
    {
        if (request.MaterialIds == null || request.MaterialIds.Count == 0)
            return Error.Validation("Invalid.Materials", "At least one material is required.");

        var materials = await context
            .Materials.Where(m => request.MaterialIds.Contains(m.Id))
            .ToListAsync();

        if (materials.Count != request.MaterialIds.Count)
            return Error.Validation("Invalid.Material", "One or more materials are invalid.");

        // Fetch existing STPs for this STP number
        var existingStps = await context
            .MaterialStandardTestProcedures.Include(stp => stp.Material)
            .Where(stp => stp.StpNumber == request.StpNumber)
            .ToListAsync();

        //  if STP already used by a raw material → block everything
        if (existingStps.Any(stp => stp.Material.Kind != MaterialKind.Package))
        {
            return Error.Validation(
                "MaterialStandardTestProcedure.Exists",
                "This STP number is already assigned to a raw material."
            );
        }

        // cannot assign non-packaging materials if STP already exists
        if (existingStps.Count != 0 && materials.Any(m => m.Kind != MaterialKind.Package))
        {
            return Error.Validation(
                "MaterialStandardTestProcedure.Invalid",
                "Raw materials can only have one material per STP number."
            );
        }

        foreach (var material in materials)
        {
            // packaging materials → only once per material
            var alreadyExistsForMaterial = existingStps.Any(stp => stp.MaterialId == material.Id);

            if (alreadyExistsForMaterial)
            {
                return Error.Validation(
                    "MaterialStandardTestProcedure.Exists",
                    $"Material '{material.Name}' already has this STP number."
                );
            }

            var procedure = new MaterialStandardTestProcedure
            {
                StpNumber = request.StpNumber,
                MaterialId = material.Id,
                Description = request.Description,
            };

            await context.MaterialStandardTestProcedures.AddAsync(procedure);
        }
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<
        Result<Paginateable<IEnumerable<MaterialStandardTestProcedureDto>>>
    > GetMaterialStandardTestProcedures(
        int page,
        int pageSize,
        string searchQuery,
        MaterialKind materialKind,
        bool unused
    )
    {
        var query = context
            .MaterialStandardTestProcedures.AsSplitQuery()
            .Include(stp => stp.Material)
                .ThenInclude(m => m.MaterialCategory)
            .Where(m => m.Material.Kind == materialKind)
            .AsQueryable();

        if (unused)
        {
            var usedStpIds = await context
                .MaterialAnalyticalRawData.Select(item => item.StpId)
                .ToListAsync();
            query = query.Where(stp => !usedStpIds.Contains(stp.Id));
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, stp => stp.StpNumber);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            entity =>
                mapper.Map<MaterialStandardTestProcedureDto>(
                    entity,
                    opts =>
                    {
                        opts.Items[AppConstants.ModelType] = nameof(MaterialStandardTestProcedure);
                    }
                )
        );
    }

    public async Task<Result<MaterialStandardTestProcedureDto>> GetMaterialStandardTestProcedure(
        Guid id
    )
    {
        var procedure = await context
            .MaterialStandardTestProcedures.AsSplitQuery()
            .Include(stp => stp.Material)
            .FirstOrDefaultAsync(stp => stp.Id == id);

        return procedure is null
            ? Error.NotFound(
                "MaterialStandardTestProcedure.NotFound",
                "Material Standard test procedure not found"
            )
            : mapper.Map<MaterialStandardTestProcedureDto>(
                procedure,
                opts =>
                {
                    opts.Items[AppConstants.ModelType] = nameof(MaterialStandardTestProcedure);
                }
            );
    }

    public async Task<
        Result<MaterialStandardTestProcedureDto>
    > GetMaterialStandardTestProcedureByMaterial(Guid id)
    {
        var procedure = await context
            .MaterialStandardTestProcedures.AsSplitQuery()
            .Include(stp => stp.Material)
            .FirstOrDefaultAsync(stp => stp.MaterialId == id);

        return procedure is null
            ? Error.NotFound(
                "MaterialStandardTestProcedure.NotFound",
                "Material Standard test procedure not found"
            )
            : mapper.Map<MaterialStandardTestProcedureDto>(
                procedure,
                opts =>
                {
                    opts.Items[AppConstants.ModelType] = nameof(MaterialStandardTestProcedure);
                }
            );
    }

    public async Task<
        Result<List<MaterialStandardTestProcedureDto>>
    > GetMaterialStandardTestProcedureByStpNumber(string stpNumber)
    {
        if (string.IsNullOrWhiteSpace(stpNumber))
            return Error.Validation("Invalid.StpNumber", "Invalid STP number.");

        var procedures = await context
            .MaterialStandardTestProcedures.AsSplitQuery()
            .Include(stp => stp.Material)
            .Where(stp => stp.StpNumber == stpNumber)
            .ToListAsync();

        var materialStp = await context.MaterialSpecifications.FirstOrDefaultAsync(m =>
            m.MaterialId == procedures[0].MaterialId
        );

        if (procedures.Count == 0)
        {
            return Error.NotFound(
                "MaterialStandardTestProcedure.NotFound",
                "Material Standard Test Procedure not found."
            );
        }

        var result = mapper.Map<List<MaterialStandardTestProcedureDto>>(
            procedures,
            opts => opts.Items[AppConstants.ModelType] = nameof(MaterialStandardTestProcedure)
        );

        return result;
    }

    public async Task<
        Result<Paginateable<IEnumerable<MaterialDto>>>
    > GetMaterialsNotUsedInStandardTestProcedure(
        int page,
        int pageSize,
        string searchQuery,
        MaterialKind kind
    )
    {
        var query = context
            .Materials.AsSplitQuery()
            .Include(m => m.MaterialCategory)
            .Where(m =>
                m.Kind == kind
                && !context.MaterialStandardTestProcedures.Any(stp => stp.MaterialId == m.Id)
            )
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, m => m.Name, m => m.Description);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<MaterialDto>
        );
    }

    public async Task<Result> UpdateMaterialStandardTestProcedure(
        Guid id,
        CreateMaterialStandardTestProcedureRequest request
    )
    {
        var procedure = await context.MaterialStandardTestProcedures.FirstOrDefaultAsync(stp =>
            stp.Id == id
        );

        if (procedure is null)
        {
            return Error.NotFound(
                "MaterialStandardTestProcedure.NotFound",
                "Material Standard test procedure not found"
            );
        }

        mapper.Map(request, procedure);

        context.MaterialStandardTestProcedures.Update(procedure);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> DeleteMaterialStandardTestProcedure(Guid id, Guid userId)
    {
        var procedure = await context.MaterialStandardTestProcedures.FirstOrDefaultAsync(stp =>
            stp.Id == id
        );
        if (procedure is null)
        {
            return Error.NotFound(
                "MaterialStandardTestProcedure.NotFound",
                "Material Standard test procedure not found"
            );
        }

        procedure.DeletedAt = DateTime.UtcNow;
        procedure.LastDeletedById = userId;

        context.MaterialStandardTestProcedures.Update(procedure);
        await context.SaveChangesAsync();

        return Result.Success();
    }
}
