using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Departments.Request;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class DepartmentRepository(ApplicationDbContext context, IMapper mapper) : IDepartmentRepository
{
    public async Task<Result<Guid>> CreateDepartment(CreateDepartmentRequest request, Guid userId)
    {
        var department = mapper.Map<Department>(request);
        department.CreatedById = userId;
        department.CreatedAt = DateTime.UtcNow;
        await context.Departments.AddAsync(department);

        if (department.Type == DepartmentType.Production)
        {
            var productionWarehouse = new Warehouse
            {
                Id = Guid.NewGuid(),
                Name = $"{department.Name} Production Floor",
                Description = $"The {department.Name} production materials",
                CreatedById = userId,
                DepartmentId = department.Id,
                CreatedAt = DateTime.UtcNow,
                Type = WarehouseType.Production
            };
        
            await context.Warehouses.AddAsync(productionWarehouse);
        
            department.Warehouses.Add(productionWarehouse);

            var packagedMaterialWarehouse = new Warehouse
            {
                Id = Guid.NewGuid(),
                Name = $"{department.Name} Package Warehouse",
                Description = $"The {department.Name} packaged materials storage warehouse",
                CreatedById = userId,
                DepartmentId = department.Id,
                CreatedAt = DateTime.UtcNow,
                Type = WarehouseType.PackagedStorage,
                MaterialKind = MaterialKind.Package

            };
            
            await context.Warehouses.AddAsync(packagedMaterialWarehouse);
            
            department.Warehouses.Add(packagedMaterialWarehouse);
            
            var rawMaterialWarehouse = new Warehouse
            {
                Id = Guid.NewGuid(),
                Name = $"{department.Name} Raw Warehouse",
                Description = $"The {department.Name} raw materials storage warehouse",
                CreatedById = userId,
                DepartmentId = department.Id,
                CreatedAt = DateTime.UtcNow,
                Type = WarehouseType.RawMaterialStorage,
                MaterialKind = MaterialKind.Raw
            };
        
            await context.Warehouses.AddAsync(rawMaterialWarehouse);
        
            department.Warehouses.Add(rawMaterialWarehouse);
            
            var finishedGoodsWarehouse = new Warehouse
            {
                Id = Guid.NewGuid(),
                Name = $"{department.Name} Finished Goods Warehouse",
                Description = $"The {department.Name} finished goods warehouse",
                CreatedById = userId,
                DepartmentId = department.Id,
                CreatedAt = DateTime.UtcNow,
                Type = WarehouseType.FinishedGoodsStorage
            };
            
            await context.Warehouses.AddAsync(finishedGoodsWarehouse);
            
            department.Warehouses.Add(finishedGoodsWarehouse);
        }
        
        await context.SaveChangesAsync();

        return department.Id;
    }

    public async Task<Result<DepartmentDto>> GetDepartment(Guid departmentId)
    {
        var department = await context.Departments
            .Include(d => d.Warehouses)
            .FirstOrDefaultAsync(d => d.Id == departmentId);

        return department is null
            ? Error.NotFound("Department.NotFound", "Department not found")
            : mapper.Map<DepartmentDto>(department);
    }
    
    public async Task<Result<Paginateable<IEnumerable<DepartmentDto>>>> GetDepartments(int page, int pageSize, string searchQuery, DepartmentType? type)
    {
        var query = context.Departments
            .AsSplitQuery()
            .Include(d => d.Warehouses)
            .AsQueryable();
        
        if (type.HasValue)
        {
            query = query.Where(d => d.Type == type);
        }

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, d => d.Name, d => d.Description);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<DepartmentDto>
        );
    }
    
    public async Task<Result> UpdateDepartment(CreateDepartmentRequest request, Guid departmentId, Guid userId)
    {
        var existingDepartment = await context.Departments
            .Include(d => d.Warehouses)
            .FirstOrDefaultAsync(d => d.Id == departmentId);
        if (existingDepartment is null)
        {
            return Error.NotFound("Department.NotFound", "Department not found");
        }
        
        context.Warehouses.RemoveRange(existingDepartment.Warehouses);
        mapper.Map(request, existingDepartment);
        context.Departments.Update(existingDepartment);
        
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // Delete Department (soft delete)
    public async Task<Result> DeleteDepartment(Guid departmentId, Guid userId)
    {
        var department = await context.Departments.FirstOrDefaultAsync(d => d.Id == departmentId);
        if (department is null)
        {
            return Error.NotFound("Department.NotFound", "Department not found");
        }

        department.DeletedAt = DateTime.UtcNow;
        department.LastDeletedById = userId;

        context.Departments.Update(department);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
