using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
using DOMAIN.Entities.Materials;

namespace APP.Repository;

public class MaterialRepository(ApplicationDbContext context, IMapper mapper) : IMaterialRepository
{
        // Create Material
        public async Task<Result<Guid>> CreateMaterial(CreateMaterialRequest request, Guid userId)
        {
            var material = mapper.Map<Material>(request);
            material.CreatedById = userId;
            await context.Materials.AddAsync(material);
            await context.SaveChangesAsync();

            return material.Id;
        }

        // Get Material by ID
        public async Task<Result<MaterialDto>> GetMaterial(Guid materialId)
        {
            var material = await context.Materials
                .Include(m => m.MaterialCategory)  // Include category if needed
                .FirstOrDefaultAsync(m => m.Id == materialId);

            return material is null
                ? MaterialErrors.NotFound(materialId)
                : mapper.Map<MaterialDto>(material);
        }

        // Get paginated list of Materials
        public async Task<Result<Paginateable<IEnumerable<MaterialDto>>>> GetMaterials(int page, int pageSize, string searchQuery)
        {
            var query = context.Materials
                .AsSplitQuery()
                .Include(m => m.MaterialCategory)
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

        // Update Material
        public async Task<Result> UpdateMaterial(CreateMaterialRequest request, Guid materialId, Guid userId)
        {
            var existingMaterial = await context.Materials.FirstOrDefaultAsync(m => m.Id == materialId);
            if (existingMaterial is null)
            {
                return MaterialErrors.NotFound(materialId);
            }

            mapper.Map(request, existingMaterial);
            existingMaterial.LastUpdatedById = userId;

            context.Materials.Update(existingMaterial);
            await context.SaveChangesAsync();
            return Result.Success();
        }

        // Delete Material (soft delete)
        public async Task<Result> DeleteMaterial(Guid materialId, Guid userId)
        {
            var material = await context.Materials.FirstOrDefaultAsync(m => m.Id == materialId);
            if (material is null)
            {
                return MaterialErrors.NotFound(materialId);
            }

            material.DeletedAt = DateTime.UtcNow;
            material.LastDeletedById = userId;

            context.Materials.Update(material);
            await context.SaveChangesAsync();
            return Result.Success();
        } 
}

