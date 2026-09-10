using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SHARED.Services.Identity;

namespace INFRASTRUCTURE.Context;

public sealed class DesignTimeDbContextFactory :
    IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("connectionString") ??
            "Host=127.0.0.1;Port=5432;Database=entrancedb;Username=postgres;Password=design-time";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connection)
            .Options;
        return new ApplicationDbContext(options, new DesignTimeCurrentUser());
    }

    private sealed class DesignTimeCurrentUser : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }
}
