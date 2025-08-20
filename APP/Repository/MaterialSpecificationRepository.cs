using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.MaterialSpecifications;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class MaterialSpecificationRepository(ApplicationDbContext context, IMapper mapper) : IMaterialSpecificationRepository
{
    public async Task<Result<Guid>> CreateMaterialSpecification(CreateMaterialSpecificationRequest request)
    {
        var isLinked = await context.MaterialSpecifications.AnyAsync(m => m.Id == request.MaterialId);
        if (isLinked)
        {
            return Error.Conflict("MaterialSpecification.AlreadyLinked", "Material specification already linked");
        }

        if (request.DueDate < DateTime.UtcNow)
        {
            return Error.Validation("MaterialSpecification.DueDate", "Due date must be greater than current date");
        }
        
        var materialSpec = mapper.Map<MaterialSpecification>(request);
        await context.MaterialSpecifications.AddAsync(materialSpec);
        
        await context.SaveChangesAsync();
        return materialSpec.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<MaterialSpecificationDto>>>> GetMaterialSpecifications(int page, int pageSize, string searchQuery, MaterialKind materialKind)
    {
        var query = context.MaterialSpecifications
            .AsSplitQuery()
            .Include(ms => ms.Material)
            .Include(ms => ms.Form)
            .Include(ms => ms.CreatedBy)
            .Include(ms => ms.Response)
            .Where(ms => ms.Material.Kind == materialKind)
            .AsQueryable();

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize,
            mapper.Map<MaterialSpecificationDto>);
    }

    public async Task<Result<MaterialSpecificationDto>> GetMaterialSpecification(Guid id)
    {
        var materialSpec = await context.MaterialSpecifications
            .AsSplitQuery()
            .Include(ms => ms.Material)
            .Include(ms => ms.Form)
            .ThenInclude(ps => ps.Sections)
            .ThenInclude(ps => ps.Fields)
            .ThenInclude(ps => ps.Question)
            .Include(ps => ps.Form)
            .ThenInclude(ps => ps.Sections)
            .ThenInclude(ps => ps.Instrument)
            .Include(ms => ms.CreatedBy)
            .Include(m => m.Response)
            .ThenInclude(r => r.FormResponses)
            .ThenInclude(r => r.FormField)
            .FirstOrDefaultAsync(ps => ps.Id == id);
        
        return materialSpec is null ? 
            Error.NotFound("MaterialSpecification.NotFound", "Material specification not found")
            : mapper.Map<MaterialSpecificationDto>(materialSpec);
    }

    public async Task<Result<MaterialSpecificationDto>> GetMaterialSpecificationByMaterial(Guid materialId)
    {
        var materialSpec = await context.MaterialSpecifications
            .AsSplitQuery()
            .Include(ms => ms.Material)
            .Include(ms => ms.Form)
            .ThenInclude(ps => ps.Sections)
            .ThenInclude(ps => ps.Fields)
            .ThenInclude(ps => ps.Question)
            .Include(ps => ps.Form)
            .ThenInclude(ps => ps.Sections)
            .ThenInclude(ps => ps.Instrument)
            .Include(ms => ms.CreatedBy)
            .Include(m => m.Response)
            .ThenInclude(r => r.FormResponses)
            .ThenInclude(r => r.FormField)
            .FirstOrDefaultAsync(ps => ps.MaterialId == materialId);
        return materialSpec is null ? 
            Error.NotFound("MaterialSpecification.NotFound", "Material specification not found")
            : mapper.Map<MaterialSpecificationDto>(materialSpec);
    }

    public async Task<Result> UpdateMaterialSpecification(Guid id, CreateMaterialSpecificationRequest request)
    {
        var materialSpec = await context.MaterialSpecifications.FirstOrDefaultAsync(ps => ps.Id == id);
        
        if (materialSpec is null)
        {
            return Error.NotFound("MaterialSpecification.NotFound", "Material specification not found");
        }
        
        mapper.Map(request, materialSpec);
        
        context.MaterialSpecifications.Update(materialSpec);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteMaterialSpecification(Guid id, Guid userId)
    {
        var materialSpec = await context.MaterialSpecifications.FirstOrDefaultAsync(ps => ps.Id == id);
        
        if (materialSpec is null)
        {
            return Error.NotFound("MaterialSpecification.NotFound", "Material specification not found");
        }
        
        materialSpec.LastDeletedById = userId;
        materialSpec.DeletedAt = DateTime.UtcNow;
        
        context.MaterialSpecifications.Update(materialSpec);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}