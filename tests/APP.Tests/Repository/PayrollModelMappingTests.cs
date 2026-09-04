using DOMAIN.Entities.Payroll;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class PayrollMappingCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class PayrollModelMappingTests
{
    [Fact]
    public void PayrollRunUsesDedicatedHrTable()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=oryx_model_mapping;Username=unused")
            .Options;
        using var context = new ApplicationDbContext(
            options,
            new PayrollMappingCurrentUserService());

        var payrollRun = context.Model.FindEntityType(typeof(PayrollRun));

        Assert.NotNull(payrollRun);
        Assert.Equal("HrPayrollRuns", payrollRun.GetTableName());
    }
}
