using System.Security.Claims;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Permissions;
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
                IsActive = true,
                Children = g.Select(permission => new PermissionDetailDto
                {
                    Key = permission.Key,
                    Name = permission.Name,
                    Description = permission.Description,
                    SubModule = permission.SubModule,
                    Types = permission.Types
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

        return  rolePermissions
            .GroupBy(section => section.Module)
            .Select(group => new PermissionModuleDto
            {
                Module = group.Key,
                IsActive = group.Any(sec => sec.IsActive),
                Children = group.SelectMany(sec => sec.Children)
                    .GroupBy(child => child.Key)
                    .Select(childGroup => new PermissionDetailDto
                    {
                        Key = childGroup.Key,
                        Name = childGroup.First().Name,
                        Description = childGroup.First().Description,
                        SubModule = childGroup.First().SubModule,
                        HasOptions = childGroup.First().HasOptions,
                        Types = childGroup.SelectMany(child => child.Types).Distinct().ToList()
                    }).ToList()
            })
            .ToList();
    }
    
    public async Task<List<PermissionModuleDto>> GetPermissionByRole(Guid roleId)
    {
        // Get the list of permission keys for the role from the RoleClaims table
        var roleClaims = await context.RoleClaims
            .Where(item => item.RoleId == roleId && item.ClaimType == AppConstants.Permission)
            .ToListAsync();

        var roleClaimValues = roleClaims.Select(r => r.ClaimValue);
        
        var roleClaimIds = roleClaims.Select(r => r.Id);
        
        // Get all available permissions from PermissionUtils
        var allPermissions = PermissionUtils.GeneratePermissions().ToList();
        
        var filteredPermissions = allPermissions.Where(permission => roleClaimValues.Contains(permission.Key));
        
        // Fetch all required PermissionTypes in a single query
        var permissionTypesLookup = await context.PermissionTypes
            .Where(pt => roleClaimIds.Contains(pt.RoleClaimId))
            .GroupBy(pt => pt.Key)
            .ToDictionaryAsync(g => g.Key, g => g.Select(pt => pt.Type).ToList());
        
        // Group permissions by section and map to PermissionSectionDto structure
        return filteredPermissions
            .GroupBy(permission => permission.Module)
            .Select(g => new PermissionModuleDto
            {
                Module = g.Key,
                IsActive = g.Any(s => permissionTypesLookup.TryGetValue(s.Key, out var types ) && types.Count > 0),
                Children = g.Select(permission => new PermissionDetailDto
                {
                    Key = permission.Key,
                    Name = permission.Name,
                    Description = permission.Description,
                    SubModule = permission.SubModule,
                    Types = permissionTypesLookup.TryGetValue(permission.Key, out var types) ? types : []
                }).ToList()
            })
            .ToList();
        
        /*return allPermissions
            .GroupBy(permission => permission.Module)
            .Select(g => new PermissionModuleDto
            {
                Module = g.Key,
                Children = g.Select(permission => new PermissionDetailDto
                {
                    Key = permission.Key,
                    Name = permission.Name,
                    Description = permission.Description,
                    SubModule = permission.SubModule,
                    //Types = filteredPermissions.Select(p => p.Key).Contains(permission.Key) ? ["Access"] : []
                }).ToList()
            })
            .ToList();*/
    }
    
    public async Task<Result> UpdateRolePermissions(List<PermissionModuleDto> permissionModules, Guid roleId)
    {
        var role = await context.Roles.FirstOrDefaultAsync(item => item.Id == roleId);
        if (role == null) return RoleErrors.NotFound(roleId);
        
        if(permissionModules.Count == 0) 
            return Result.Success();
        
        // Extract permissions and their types from the provided sections
        var requestedPermissions = permissionModules
            .SelectMany(section => section.Children)
            .Select(permission => new
            {
                permission.Key,
                Types = permission.Types ?? []
            })
            .ToList();

        // Retrieve current role permissions
        var roleClaims = await context.RoleClaims
            .Where(rc => rc.RoleId == roleId && rc.ClaimType == AppConstants.Permission)
            .ToListAsync();

        var roleClaimIds = roleClaims.Select(r => r.Id).ToList();

        context.RoleClaims.RemoveRange(roleClaims);
        
        var permissionTypes = await context.PermissionTypes
            .Where(pt => roleClaimIds.Contains(pt.RoleClaimId))
            .ToListAsync();

        context.PermissionTypes.RemoveRange(permissionTypes);
        
        // Add new permissions and their types
        foreach (var permission in requestedPermissions)
        { 
            // Add the new role claim for the permission
            var claim = new Claim(AppConstants.Permission, permission.Key);
            await roleManager.AddClaimAsync(role, claim);

            // Retrieve the newly added role claim ID
            var newClaim = await context.RoleClaims
                .FirstOrDefaultAsync(rc => rc.RoleId == roleId && rc.ClaimValue == permission.Key);

            if (newClaim != null)
            {
                // Add associated types to PermissionType table
                await context.PermissionTypes.AddRangeAsync(permission.Types.Select(type => new PermissionType
                {
                    Key = permission.Key,
                    RoleClaimId = newClaim.Id,
                    Type = type
                }).ToList());
            }
        }
        
        // Clear the cache for users in the role
        var usersInRole = await userManager.GetUsersInRoleAsync(role.Name ?? "");
        foreach (var user in usersInRole)
        {
            cache.Remove($"UserId_{user.Id}_Permissions");
        }

        await context.SaveChangesAsync();
        return Result.Success();
    }
    
     public async Task<Result<List<MenuItem>>> GetFilteredMenu(Guid userId)
    {
        // Retrieve cached permissions
        var permissionsDict = await GetUserPermissions(userId);
        
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return UserErrors.NotFound(userId);

        // Filter menu based on permissions
        return FilterMenuItems(MenuConfig.MenuItems.Select(menuItem => menuItem.Clone()).ToList(), permissionsDict);
    }

    private List<MenuItem> FilterMenuItems(List<MenuItem> menuItems, Dictionary<string, List<string>> userPermissions)
    {
        if (menuItems == null || menuItems.Count == 0)
        {
            return [];
        }

        var result = new List<MenuItem>();

        foreach (var menu in menuItems)
        {
            if (menu.Children == null || menu.Children.Count == 0)
            {
                // Add the menu if it has no children and is visible
                if (IsMenuItemVisible(menu, userPermissions))
                {
                    result.Add(menu);
                }
            }
            else
            {
                // Filter children and add the parent menu if it's visible
                var validChildren = menu.Children
                    .Where(child => IsMenuItemVisible(child.Clone(), userPermissions))
                    .ToList();

                if (IsMenuItemVisible(menu, userPermissions) || validChildren.Count > 0)
                {
                    menu.Children = validChildren;
                    result.Add(menu);
                }
            }
        }

        return result;
    }

    private bool IsMenuItemVisible(MenuItem item, Dictionary<string, List<string>> userPermissions)
    {
        if (item.RequiredPermissionKey.Count == 0)
        {
            return true;
        }

        return item.RequiredPermissionKey.Any(permissionKey =>
        {
            var hasPermission = userPermissions.TryGetValue(permissionKey, out var types) && types.Count != 0;
            return hasPermission;
        });
    }
    
    private async Task<Dictionary<string, List<string>>> GetUserPermissions(Guid userId)
    {
        var cacheKey = $"UserId_{userId}_Permissions";
        if (cache.TryGetValue(cacheKey, out Dictionary<string, List<string>> permissionsDict)) return permissionsDict;

        // Load permissions from database
        var perms = (await GetAllPermissionForUser(userId)).Value;
        var dict = perms.SelectMany(i => i.Children)
            .ToDictionary(i => i.Key, i => i.Types);
        return dict;
    }
}