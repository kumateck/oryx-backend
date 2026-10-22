using DOMAIN.Entities.Employees;
using Microsoft.EntityFrameworkCore;

namespace INFRASTRUCTURE.Context;

public partial class ApplicationDbContext
{
    public DbSet<EmployeeInvitation> EmployeeInvitations { get; set; }

    private static void ConfigureEmployeeOnboarding(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmployeeInvitation>().HasIndex(i => i.TokenHash).IsUnique();
        modelBuilder.Entity<Employee>().HasIndex(e => e.UserId);
    }
}
