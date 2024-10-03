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

    private static void SeedUsers(UserManager<User> userManager, ApplicationDbContext dbContext)
    {
        // Seed first user
        CreateUserIfNotExists(userManager, dbContext, 
            "dkadusei@kumateck.com", "Des", "Kumateck", "Pass123$1",
            [RoleUtils.AppRoleSuper, RoleUtils.AppRoleAdmin], 
            new Claim[] {
                new Claim(JwtClaimTypes.Name, "Des"),
                new Claim(JwtClaimTypes.GivenName, "KUma"),
                new Claim(JwtClaimTypes.FamilyName, "Admin")
            });

        // Seed second user
        CreateUserIfNotExists(userManager, dbContext, 
            "douglassboakye22@gmail.com", "Doug", "Afford", "Pass123$1",
            [RoleUtils.AppRoleSuper, RoleUtils.AppRoleAdmin], 
            new Claim[] {
                new Claim(JwtClaimTypes.Name, "Dog"),
                new Claim(JwtClaimTypes.GivenName, "Affordable"),
                new Claim(JwtClaimTypes.FamilyName, "Admin")
            });
    }

    private static void CreateUserIfNotExists(
        UserManager<User> userManager, ApplicationDbContext dbContext, 
        string email, string firstName, string lastName, string password, 
        List<string> roles, Claim[] claims)
    {
        var existingUser = dbContext.Users.IgnoreQueryFilters()
            .FirstOrDefault(item => item.Email == email);
        
        if (existingUser != null) return;

        var user = new User
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            OrganizationName = AppConstants.DefaultTenantId,
            CreatedAt = DateTime.UtcNow
        };

        var createUserResult = userManager.CreateAsync(user, password).GetAwaiter().GetResult();
        if (!createUserResult.Succeeded) return;

        AssignRolesAndClaims(userManager, dbContext, user, roles, claims);
    }

    private static void AssignRolesAndClaims(UserManager<User> userManager, ApplicationDbContext dbContext, User user, List<string> roles, Claim[] claims)
    {
        user.OrganizationName = AppConstants.DefaultTenantId;
        dbContext.Users.Update(user);
        dbContext.SaveChanges();

        var roleResult = userManager.AddToRolesAsync(user, roles).GetAwaiter().GetResult();
        if (!roleResult.Succeeded) return;

        userManager.AddClaimsAsync(user, claims).GetAwaiter().GetResult();
    }

    private static void SeedRoles(RoleManager<Role> roleManager, ApplicationDbContext dbContext)
    {
        foreach (var roleName in RoleUtils.AppRoles())
        {
            var existingRole = dbContext.Roles.IgnoreQueryFilters()
                .FirstOrDefault(item => item.Name == roleName);

            if (existingRole != null)
            {
                if (roleName == RoleUtils.AppRoleSuper)
                {
                    Debug.WriteLine("Super admin role already exists");
                }
                continue;
            }

            var newRole = new Role
            {
                Name = roleName,
                NormalizedName = roleName.Capitalize(),
                DisplayName = roleName.RemoveCharacter('.').Capitalize()
            };

            var roleResult = roleManager.CreateAsync(newRole).GetAwaiter().GetResult();
            if (!roleResult.Succeeded) continue;

            if (newRole.Name == RoleUtils.AppRoleSuper)
            {
                Debug.WriteLine("Super admin role seeded");
            }
        }
    }
}
