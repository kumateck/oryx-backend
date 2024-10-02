using System.Diagnostics;
using System.Security.Claims;
using IdentityModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using APP.Extensions;
using APP.Utils;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;

namespace API.Database.Seeds.TableSeeders;

public class UserTableSeeders : ISeeder
{
    public void Handle(IServiceScope scope)
    {
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetService<RoleManager<Role>>();
        
        if (userManager == null || dbContext == null || roleManager == null) return;

        SeedRoles(roleManager, dbContext);
        SeedUsers(userManager, dbContext);
    }
    
    private static void SeedUsers
        (UserManager<User> userManager, ApplicationDbContext dbContext)
    {
        var defaultUser = dbContext.Users.IgnoreQueryFilters()
            .FirstOrDefault(item => item.Email == "dkadusei@kumateck.com");
        
        if (defaultUser != null) return;
        
        var user = new User
        {
            UserName = "dkadusei@kumateck.com",
            Email = "dkadusei@kumateck.com",
            FirstName = "Des",
            LastName = "Kumateck",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            OrganizationName = AppConstants.DefaultTenantId,
            CreatedAt = DateTime.UtcNow
        };

        var result = userManager.CreateAsync
            (user, "Pass123$1").Result;
        
        if (!result.Succeeded) return;
        {
            var createdUser = dbContext.Users.IgnoreQueryFilters()
                .FirstOrDefault(item => item.Email == "dkadusei@kumateck.com");

            if (createdUser == null) return;

            createdUser.OrganizationName = AppConstants.DefaultTenantId;
            dbContext.Users.Update(createdUser);
            dbContext.SaveChanges();
            
            var roleResult = userManager.AddToRolesAsync(user, new List<string> { RoleUtils.AppRoleSuper, RoleUtils.AppRoleAdmin }).Result;

            if (!roleResult.Succeeded) return;

            _ = userManager.AddClaimsAsync(user, new[]
            {
                new Claim(JwtClaimTypes.Name, "Des"),
                new Claim(JwtClaimTypes.GivenName, "KUma"),
                new Claim(JwtClaimTypes.FamilyName, "Admin")
            }).Result;
        }
        
        var defaultUser2 = dbContext.Users.IgnoreQueryFilters()
            .FirstOrDefault(item => item.Email == "douglassboakye22@gmail.com");
        
        if (defaultUser2 != null) return;
        
        var user2 = new User
        {
            UserName = "douglassboakye22@gmail.com",
            Email = "douglassboakye22@gmail.com",
            FirstName = "Doug",
            LastName = "Afford",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            OrganizationName = AppConstants.DefaultTenantId,
            CreatedAt = DateTime.UtcNow
        };

        var result2 = userManager.CreateAsync
            (user2, "Pass123$1").Result;
        
        if (!result2.Succeeded) return;
        {
            var createdUser = dbContext.Users.IgnoreQueryFilters()
                .FirstOrDefault(item => item.Email == "douglassboakye22@gmail.com");

            if (createdUser == null) return;

            createdUser.OrganizationName = AppConstants.DefaultTenantId;
            dbContext.Users.Update(createdUser);
            dbContext.SaveChanges();
            
            var roleResult = userManager.AddToRolesAsync(user, new List<string> { RoleUtils.AppRoleSuper, RoleUtils.AppRoleAdmin }).Result;

            if (!roleResult.Succeeded) return;

            _ = userManager.AddClaimsAsync(user, new[]
            {
                new Claim(JwtClaimTypes.Name, "Dog"),
                new Claim(JwtClaimTypes.GivenName, "Affordable"),
                new Claim(JwtClaimTypes.FamilyName, "Admin")
            }).Result;
        }
    }
    
    
    private static void SeedRoles
        (RoleManager<Role> roleManager, ApplicationDbContext dbContext)
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