using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class RoleRepository(ApplicationDbContext context, IMapper mapper, UserManager<User> userManager, RoleManager<Role> roleManager , IPermissionRepository permissionRepository)
    : IRoleRepository
{
    public async Task<Result<List<RoleDto>>> GetRoles()
    {
        var roles =  await context.Roles.ToListAsync();
        return roles.Select(mapper.Map<RoleDto>).ToList();
    }
    
    public async Task<Result<Paginateable<IEnumerable<RoleDto>>>> GetRoles(int page, int pageSize, string searchQuery)
    {
        var roles =  context.Roles.AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            roles = roles.WhereSearch(searchQuery, q => q.Name, q => q.DisplayName);
        }
        
        return await PaginationHelper.GetPaginatedResultAsync(
            roles,
            page,
            pageSize,
            mapper.Map<RoleDto>
        );
    }

    public async Task<Result<Paginateable<List<RolePermissionDto>>>> GetRolesWithPermissions(int page, int pageSize, 
        string searchQuery)
    {
        var result = await GetRoles(page, pageSize, searchQuery);
        var roles = mapper.Map<Paginateable<List<RolePermissionDto>>>(result.Value);
        foreach (var role in roles.Data)
        {
            role.Permissions = await permissionRepository.GetPermissionByRole(role.Id);
        }

        return roles;
    }

    public async Task<Result<RolePermissionDto>> GetRole(Guid id)
    {
        var role = mapper.Map<RolePermissionDto>(await context.Roles.FirstOrDefaultAsync(item => item.Id == id));
        role.Permissions = await permissionRepository.GetPermissionByRole(role.Id);
        return role;
    }

    public async Task<Result> CreateRole(CreateRoleRequest request, Guid userId)
    {
        var newRole = new Role
        {
            Name = request.Name,
            DisplayName = request.Name.Capitalize(),
            CreatedById = userId,
            CreatedAt = DateTime.UtcNow
        };
        
        if (!await IsValidRoleName(request.Name))
            return RoleErrors.InvalidRoleName(request.Name);
        
        var result = await roleManager.CreateAsync(newRole);
            
        if (!result.Succeeded)
        {
            return Error.Failure("Role.Create", $"{result.Errors.First()}");
        }

        await permissionRepository.UpdateRolePermissions(request.Permissions, newRole.Id);
        return Result.Success();
    }

    public async Task<Result> UpdateRole(UpdateRoleRequest request, Guid id, Guid userid)
    {
        var role =await context.Roles.FirstOrDefaultAsync(r => r.Id == id);
        if (role == null) return RoleErrors.NotFound(id);

        role.Name = request.Name;
        role.DisplayName = request.DisplayName;
        role.NormalizedName = request.Name.Normalize();
        role.Type = request.Type;
        role.LastUpdatedById = userid;
        context.Roles.Update(role);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<dynamic>> CheckRole(Guid id)
    {
        var role = await context.Roles.FirstOrDefaultAsync(item => item.Id == id);
        if (role is null) RoleErrors.NotFound(id);
        
        var usersWithRole = await userManager.GetUsersInRoleAsync(role.Name);
        return new { HasUsers = usersWithRole.Count != 0 };
    }
    
    public async Task<Result> DeleteRole(Guid id, Guid userId)
    {
        var role = await context.Roles.FirstOrDefaultAsync(item => item.Id == id);
        if (role == null) return RoleErrors.NotFound(id);

        role.DeletedAt = DateTime.Now;
        role.LastUpdatedById = userId;
        context.Roles.Update(role);
        await context.SaveChangesAsync();

        return Result.Success();
    }
    
    private async Task<bool> IsValidRoleName(string roleName)
    {
        var role = await context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        return role == null;
    }
}