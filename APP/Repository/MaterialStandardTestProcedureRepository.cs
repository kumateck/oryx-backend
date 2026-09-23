using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.MaterialStandardTestProcedures;
using DOMAIN.Entities.QualityRoutines;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class MaterialStandardTestProcedureRepository(ApplicationDbContext context, IMapper mapper)
    : IMaterialStandardTestProcedureRepository
{
    public async Task<Result<List<MaterialStpMappingDto>>> CreateMaterialStandardTestProcedure(
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
            .MaterialStandardTestProcedures.AsSplitQuery()
            .Include(stp => stp.Material)
            .Where(stp => stp.StpNumber == request.StpNumber)
            .ToListAsync();

        var mappings = new List<MaterialStpMappingDto>();

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
            mappings.Add(
                new MaterialStpMappingDto { MaterialId = material.Id, StpId = procedure.Id }
            );
        }
        await context.SaveChangesAsync();
        return mappings;
    }

    public async Task<
        Result<Paginateable<IEnumerable<MaterialStandardTestProcedureDto>>>
    > GetMaterialStandardTestProcedures(
        int page,
        int pageSize,
        string searchQuery,
        MaterialKind materialKind,
        bool unused,
        AnalysisType analysisType = AnalysisType.Chemical
    )
    {
        var query = context
            .MaterialStandardTestProcedures.AsSplitQuery()
            .Include(stp => stp.Material)
                .ThenInclude(m => m.MaterialCategory)
            .Where(m => m.Material.Kind == materialKind)
            .OrderBy(m => m.StpNumber)
            .AsQueryable();

        if (unused)
        {
            query = query.Where(stp => !context.MaterialAnalyticalRawData.Any(ard =>
                ard.MaterialStandardTestProcedure.MaterialId == stp.MaterialId
                && ard.AnalysisType == analysisType
            ));
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(
                searchQuery,
                stp => stp.StpNumber,
                stp => stp.Material.Name,
                stp => stp.Material.Code
            );
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

        var decodedStpNumber = Uri.UnescapeDataString(stpNumber);

        var procedures = await context
            .MaterialStandardTestProcedures.AsSplitQuery()
            .Include(stp => stp.Material)
            .Where(stp => stp.StpNumber == decodedStpNumber)
            .ToListAsync();

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
            query = query.WhereSearch(searchQuery, m => m.Name, m => m.Description, m => m.Code);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<MaterialDto>
        );
    }

    public async Task<Result<List<MaterialStpMappingDto>>> UpdateMaterialStandardTestProcedure(
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

        var stpNumber = procedure.StpNumber;
        var proceduresToUpdate = await context
            .MaterialStandardTestProcedures.Where(stp => stp.StpNumber == stpNumber)
            .ToListAsync();

        foreach (var p in proceduresToUpdate)
        {
            p.Description = request.Description;
            p.StpNumber = request.StpNumber;
        }

        context.MaterialStandardTestProcedures.UpdateRange(proceduresToUpdate);
        await context.SaveChangesAsync();

        return proceduresToUpdate
            .Select(p => new MaterialStpMappingDto { MaterialId = p.MaterialId, StpId = p.Id })
            .ToList();
    }

    public async Task<Result<List<MaterialStpMappingDto>>> AddRemoveMaterialsToStp(
        AddRemoveMaterialToStpRequest request
    )
    {
        var existingStps = await context
            .MaterialStandardTestProcedures.Include(stp => stp.Material)
            .Where(stp => stp.StpNumber == request.StpNumber)
            .ToListAsync();

        if (existingStps.Count == 0)
        {
            return Error.NotFound(
                "MaterialStandardTestProcedure.NotFound",
                $"No standard test procedure found with STP number '{request.StpNumber}'."
            );
        }

        var isPackageStp = existingStps.All(stp => stp.Material.Kind == MaterialKind.Package);
        var description = existingStps[0].Description;

        // Handle removals
        if (request.MaterialIdsToRemove is { Count: > 0 })
        {
            var stpsToRemove = existingStps
                .Where(stp => request.MaterialIdsToRemove.Contains(stp.MaterialId))
                .ToList();

            foreach (var stp in stpsToRemove)
            {
                var isLinkedToArd = await context.MaterialAnalyticalRawData.AnyAsync(ard =>
                    ard.StpId == stp.Id
                );
                if (isLinkedToArd)
                {
                    return Error.Conflict(
                        "MaterialStandardTestProcedure.LinkedToArd",
                        $"Cannot remove material '{stp.Material?.Name}' because it is linked to analytical raw data."
                    );
                }
            }

            context.MaterialStandardTestProcedures.RemoveRange(stpsToRemove);
            existingStps.RemoveAll(stp => request.MaterialIdsToRemove.Contains(stp.MaterialId));
        }

        // Handle additions
        if (request.MaterialIdsToAdd is { Count: > 0 })
        {
            var materialsToAdd = await context
                .Materials.Where(m => request.MaterialIdsToAdd.Contains(m.Id))
                .ToListAsync();

            if (materialsToAdd.Count != request.MaterialIdsToAdd.Count)
                return Error.Validation("Invalid.Material", "One or more materials are invalid.");

            switch (isPackageStp)
            {
                // Check if we are trying to add a non-package material to a package STP or vice-versa
                case true when materialsToAdd.Any(m => m.Kind != MaterialKind.Package):
                    return Error.Validation(
                        "MaterialStandardTestProcedure.Invalid",
                        "Cannot add raw materials to a packaging material STP."
                    );
                case false when materialsToAdd.Any(m => m.Kind == MaterialKind.Package):
                    return Error.Validation(
                        "MaterialStandardTestProcedure.Invalid",
                        "Cannot add packaging materials to a raw material STP."
                    );
                // Raw material constraint
                case false when (existingStps.Count + materialsToAdd.Count > 1):
                    return Error.Validation(
                        "MaterialStandardTestProcedure.Invalid",
                        "Raw materials can only have one material per STP number."
                    );
            }

            foreach (
                var procedure in from material in materialsToAdd
                where existingStps.All(stp => stp.MaterialId != material.Id)
                select new MaterialStandardTestProcedure
                {
                    StpNumber = request.StpNumber,
                    MaterialId = material.Id,
                    Description = description,
                }
            )
            {
                await context.MaterialStandardTestProcedures.AddAsync(procedure);
                existingStps.Add(procedure);
            }
        }

        await context.SaveChangesAsync();
        return existingStps
            .Select(p => new MaterialStpMappingDto { MaterialId = p.MaterialId, StpId = p.Id })
            .ToList();
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
