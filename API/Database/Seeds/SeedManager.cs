using Microsoft.EntityFrameworkCore;
using INFRASTRUCTURE.Context;

namespace API.Database.Seeds;

public static class SeedManager
{
    public static IHost SeedData(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var configuration = scope.ServiceProvider
            .GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        if (configuration.GetValue("Database:ApplyMigrationsOnStartup", true))
        {
            context.Database.Migrate();
        }

        try
        {
            var databaseSeeder = new DatabaseSeeder(scope);
            databaseSeeder.SeedData();
        }
        catch (Exception)
        {
            // ignored
        }
        return host;
    }
}
