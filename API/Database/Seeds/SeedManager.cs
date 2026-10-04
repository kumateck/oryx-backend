using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace API.Database.Seeds;

public static class SeedManager
{
    public static IHost SeedData(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope
            .ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(SeedManager));
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        if (configuration.GetValue("Database:ApplyMigrationsOnStartup", true))
        {
            try
            {
                context.Database.Migrate();
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Failed to apply database migrations");
                throw;
            }
        }

        try
        {
            var databaseSeeder = new DatabaseSeeder(scope);
            databaseSeeder.SeedData();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed database");
            // ignored
        }
        return host;
    }
}
