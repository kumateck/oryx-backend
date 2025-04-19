using System.Security.Claims;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SHARED;

namespace APP.Repository;

public class PermissionRepository(ApplicationDbContext context, UserManager<User> userManager, RoleManager<Role> roleManager, IMemoryCache cache) 
    : IPermissionRepository
{
    public IEnumerable<PermissionModuleDto> GetAllPermissionInSystem()
    {
        return PermissionUtils.GeneratePermissions()
            .GroupBy(permission => permission.Module)
            .Select(g => new PermissionModuleDto
            {
                Module = g.Key,
                Children = g.Select(permission => new PermissionDetailDto
                {
                    Key = permission.Key,
                    Name = permission.Name,
                    Description = permission.Description,
                    SubModule = permission.SubModule
                }).ToList()
            })
            .ToList();
    }

    public IEnumerable<PermissionDto> GetAllPermissions()
    {
        return PermissionUtils.GeneratePermissions();
    }

    public async Task<Result<List<PermissionModuleDto>>> GetAllPermissionForUser(Guid userId)
    {
        var user = await context.Users.FirstOrDefaultAsync(item => item.Id == userId);
        if (user == null) return UserErrors.NotFound(userId);
        var userRoles = await context.UserRoles.Where(item => item.UserId == userId).ToListAsync();

        var rolePermissions = new List<PermissionModuleDto>();
        foreach (var userRole in userRoles)
        {
            rolePermissions.AddRange(await GetPermissionByRole(userRole.RoleId));
        }

        return rolePermissions;
    }
    
    public async Task<List<PermissionModuleDto>> GetPermissionByRole(Guid roleId)
    {
        // Get the list of permission keys for the role from the RoleClaims table
        var roleClaims = await context.RoleClaims
            .Where(item => item.RoleId == roleId && item.ClaimType == AppConstants.Permission)
            .ToListAsync();

        var roleClaimValues = roleClaims.Select(r => r.ClaimValue);
        
        // Get all available permissions from PermissionUtils
        var allPermissions = PermissionUtils.GeneratePermissions().ToList();
        
        var filteredPermissions = allPermissions.Where(permission => roleClaimValues.Contains(permission.Key));
        
        // Group permissions by section and map to PermissionSectionDto structure
        return filteredPermissions
            .GroupBy(permission => permission.Module)
            .Select(g => new PermissionModuleDto
            {
                Module = g.Key,
                Children = g.Select(permission => new PermissionDetailDto
                {
                    Key = permission.Key,
                    Name = permission.Name,
                    Description = permission.Description,
                    SubModule = permission.SubModule
                }).ToList()
            })
            .ToList();
    }
    
    public async Task<Result> UpdateRolePermissions(List<string> permissions, Guid roleId)
    {
        var role = await context.Roles.FirstOrDefaultAsync(item => item.Id == roleId);
        if (role == null) return RoleErrors.NotFound(roleId);

        // Retrieve current role permissions
        var roleClaims = await context.RoleClaims
            .Where(rc => rc.RoleId == roleId && rc.ClaimType == AppConstants.Permission)
            .ToListAsync();
        
        context.RoleClaims.RemoveRange(roleClaims);
            
        // Add new permissions and their types
        foreach (var permission in permissions)
        { 
            // Add the new role claim for the permission
            var claim = new Claim(AppConstants.Permission, permission);
            await roleManager.AddClaimAsync(role, claim);

            // Clear the cache for users in the role
            var usersInRole = await userManager.GetUsersInRoleAsync(role.Name ?? "");
            foreach (var user in usersInRole)
            {
                cache.Remove($"UserId_{user.Id}_Permissions");
            }
        }
        await context.SaveChangesAsync();
        return Result.Success();
    }
}