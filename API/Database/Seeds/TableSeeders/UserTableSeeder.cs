using System.Diagnostics;
using System.Security.Claims;
using IdentityModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using APP.Extensions;
using APP.Utils;
using DOMAIN.Context;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;

namespace API.Database.Seeds.TableSeeders;

public class UserTableSeeders : ISeeder
{
    public void Handle(IServiceScope scope)
    {
        var dbContext = scope.ServiceProvider.GetService<OryxContext>();
        var userManager = scope.ServiceProvider.GetService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetService<RoleManager<Role>>();
        
        if (userManager == null || dbContext == null || roleManager == null) return;

        SeedRoles(roleManager, dbContext);
        SeedUsers(userManager, dbContext);
    }
    
    private static void SeedUsers
        (UserManager<User> userManager, OryxContext dbContext)
    {
        var defaultUser = dbContext.Users.IgnoreQueryFilters()
            .FirstOrDefault(item => item.Email == "anthonygyan@gmail.com");
        
        if (defaultUser != null) return;
        
        var user = new User
        {
            UserName = "anthonygyan@gmail.com",
            Email = "anthonygyan@gmail.com",
            FirstName = "Tony",
            LastName = "Gyan",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            OrganizationName = AppConstants.DefaultTenantId,
            CreatedAt = DateTime.Now
        };

        var result = userManager.CreateAsync
            (user, "Pass123$1").Result;
        
        if (!result.Succeeded) return;
        {
            var createdUser = dbContext.Users.IgnoreQueryFilters()
                .FirstOrDefault(item => item.Email == "anthonygyan@gmail.com");

            if (createdUser == null) return;

            createdUser.OrganizationName = AppConstants.DefaultTenantId;
            dbContext.Users.Update(createdUser);
            dbContext.SaveChanges();
            
            var roleResult = userManager.AddToRolesAsync(user, new List<string> { RoleUtils.AppRoleSuper, RoleUtils.AppRoleAdmin }).Result;

            if (!roleResult.Succeeded) return;

            _ = userManager.AddClaimsAsync(user, new[]
            {
                new Claim(JwtClaimTypes.Name, "Tony"),
                new Claim(JwtClaimTypes.GivenName, "Gyan"),
                new Claim(JwtClaimTypes.FamilyName, "Admin")
            }).Result;
        }
        //Console.WriteLine(claimResult);
    }
    
    
    private static void SeedRoles
        (RoleManager<Role> roleManager, OryxContext dbContext)
    {
        foreach (var roleName in RoleUtils.AppRoles())
        {
            var role = dbContext.Roles.IgnoreQueryFilters().FirstOrDefault(item =>
                item.Name == roleName);
            if (role != null)
            {
                 if(role.Name == RoleUtils.AppRoleSuper)
                    //AddPermissionsToRole(roleManager, role);
                 Debug.WriteLine("roles being seeded");
            }
            else
            {
                var newRole = new Role
                {
                    Name = roleName,
                    NormalizedName = roleName.Capitalize(),
                    DisplayName = roleName.RemoveCharacter('.').Capitalize()
                };

                var result = roleManager.CreateAsync(newRole).Result;

                if (!result.Succeeded)
                {
                    continue;
                }
                    
                //if(newRole.Name == RoleUtils.AppRoleSuper)
                     //AddPermissionsToRole(roleManager, newRole);
            }
        }
    }

    // private static void AddPermissionsToRole(RoleManager<Role> roleManager, Role newRole)
    // {
    //     foreach (var permission in PermissionUtils.GetAllPermissions())
    //     {
    //         if (roleManager.GetClaimsAsync(newRole).Result.Select(item => item.Value).Contains(permission)) continue;
    //         _ = roleManager.AddClaimAsync(newRole, new Claim(AppConstants.Permission, permission)).Result;
    //     }
    // }
}