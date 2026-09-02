using APP.Repository;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.Reports;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class ReportCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class ReportRepositoryGenderRatioTests
{
    private static ApplicationDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new ReportCurrentUserService()
        );

    private static ReportRepository CreateRepository(ApplicationDbContext context) =>
        new(context, null!, null!, NullLogger<ReportRepository>.Instance);

    [Fact]
    public async Task Gender_ratio_groups_active_staff_and_excludes_other_departments()
    {
        await using var context = CreateContext();
        var quality = new Department { Id = Guid.NewGuid(), Name = "Quality" };
        var production = new Department { Id = Guid.NewGuid(), Name = "Production" };
        context.Departments.AddRange(quality, production);
        context.Employees.AddRange(
            Employee(quality, EmployeeType.Permanent, Gender.Female),
            Employee(quality, EmployeeType.Casual, Gender.Male),
            Employee(quality, EmployeeType.Casual, Gender.Female, EmployeeStatus.Inactive),
            Employee(production, EmployeeType.Permanent, Gender.Male)
        );
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).GetStaffGenderRatioReport(
            new MovementReportFilter { DepartmentId = quality.Id }
        );

        Assert.True(result.IsSuccess);
        var qualityRatio = Assert.Single(result.Value.Departments);
        Assert.Equal("Quality", qualityRatio.Department);
        Assert.Equal(1, qualityRatio.NumberOfPermanentFemale);
        Assert.Equal(1, qualityRatio.NumberOfCasualMale);
        Assert.Equal(2, result.Value.Totals.Total);
    }

    [Fact]
    public async Task Gender_ratio_applies_an_inclusive_employment_date_range()
    {
        await using var context = CreateContext();
        var quality = new Department { Id = Guid.NewGuid(), Name = "Quality" };
        context.Departments.Add(quality);
        context.Employees.AddRange(
            Employee(quality, EmployeeType.Permanent, Gender.Male, date: new(2026, 8, 1)),
            Employee(quality, EmployeeType.Casual, Gender.Female, date: new(2026, 8, 31, 18, 0, 0)),
            Employee(quality, EmployeeType.Casual, Gender.Male, date: new(2026, 9, 1))
        );
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).GetStaffGenderRatioReport(
            new MovementReportFilter
            {
                StartDate = new DateTime(2026, 8, 1),
                EndDate = new DateTime(2026, 8, 31),
            }
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Totals.Total);
        Assert.Equal(1, result.Value.Totals.NumberOfPermanentMale);
        Assert.Equal(1, result.Value.Totals.NumberOfCasualFemale);
    }

    private static Employee Employee(
        Department department,
        EmployeeType type,
        Gender gender,
        EmployeeStatus status = EmployeeStatus.Active,
        DateTime? date = null
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Employee",
            DepartmentId = department.Id,
            Department = department,
            Type = type,
            Gender = gender,
            Status = status,
            DateEmployed = date ?? new DateTime(2026, 8, 15),
        };
}
