using System.Text.Json;
using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Designations;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public sealed partial class EmployeeAssignmentTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(c => c.AddProfile<OryxMapper>()).CreateMapper();

    [Fact]
    public async Task AssignmentWithOmittedManagerSavesWithoutAnEmptyGuid()
    {
        await using var db = InMemory();
        var (employee, request) = await Seed(db);
        var result = await Repository(db).AssignEmployee(employee.Id, request);
        Assert.True(result.IsSuccess);
        db.ChangeTracker.Clear();
        var saved = await db.Employees.SingleAsync();
        Assert.Null(saved.ReportingManagerId);
        Assert.Equal(EmployeeStatus.Active, saved.Status);
        Assert.Equal(request.DepartmentId, saved.DepartmentId);
        Assert.Equal(request.DesignationId, saved.DesignationId);
        Assert.Equal("EBO", saved.StaffNumber);
        Assert.Equal(20, saved.AnnualLeaveDays);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("\"00000000-0000-0000-0000-000000000000\"")]
    public async Task NullOrEmptyManagerClearsTheOptionalLink(string jsonManager)
    {
        await using var db = InMemory();
        var (employee, request) = await Seed(db);
        var manager = new User { Id = Guid.NewGuid(), UserName = "manager" };
        db.Users.Add(manager);
        employee.ReportingManagerId = manager.Id;
        await db.SaveChangesAsync();
        request = WithManager(request, jsonManager);
        Assert.True((await Repository(db).AssignEmployee(employee.Id, request)).IsSuccess);
        db.ChangeTracker.Clear();
        Assert.Null((await db.Employees.SingleAsync()).ReportingManagerId);
    }

    [Fact]
    public async Task ExistingManagerUserIsAssigned()
    {
        await using var db = InMemory();
        var (employee, request) = await Seed(db);
        var manager = new User { Id = Guid.NewGuid(), UserName = "manager" };
        db.Users.Add(manager);
        await db.SaveChangesAsync();
        request.ReportingManagerId = manager.Id;
        Assert.True((await Repository(db).AssignEmployee(employee.Id, request)).IsSuccess);
        db.ChangeTracker.Clear();
        Assert.Equal(manager.Id, (await db.Employees.SingleAsync()).ReportingManagerId);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("employee-id")]
    [InlineData("deleted-user")]
    public async Task InvalidManagerReturnsValidationBeforeChangingAssignment(string kind)
    {
        await using var db = InMemory();
        var (employee, request) = await Seed(db);
        var invalid = kind == "employee-id" ? employee.Id : Guid.NewGuid();
        if (kind == "deleted-user")
        {
            db.Users.Add(new User { Id = invalid, UserName = "deleted", DeletedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }
        request.ReportingManagerId = invalid;
        var result = await Repository(db).AssignEmployee(employee.Id, request);
        Assert.True(result.IsFailure);
        Assert.Equal("Employee.ReportingManager", result.Error.Code);
        Assert.Equal(SHARED.ErrorType.Validation, result.Error.Type);
        Assert.Equal(EntityState.Unchanged, db.Entry(employee).State);
        db.ChangeTracker.Clear();
        var saved = await db.Employees.SingleAsync();
        Assert.Null(saved.ReportingManagerId);
        Assert.Null(saved.DepartmentId);
        Assert.Null(saved.DesignationId);
        Assert.Null(saved.StaffNumber);
        Assert.Equal(EmployeeStatus.New, saved.Status);
    }

    private static AssignEmployeeDto WithManager(AssignEmployeeDto request, string jsonManager)
    {
        var json = JsonSerializer.Serialize(request, JsonSerializerOptions.Web);
        using var document = JsonDocument.Parse(json);
        var fields = document.RootElement.EnumerateObject()
            .Where(property => property.Name != "reportingManagerId")
            .Select(property => JsonSerializer.Serialize(property.Name) + ":" + property.Value.GetRawText());
        return JsonSerializer.Deserialize<AssignEmployeeDto>("{" + string.Join(",", fields) + ",\"reportingManagerId\":" + jsonManager + "}", JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException("Fixture request is null.");
    }

    private static ApplicationDbContext InMemory() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new AnonymousActor());

    private static async Task<(Employee Employee, AssignEmployeeDto Request)> Seed(ApplicationDbContext db)
    {
        var department = new Department { Id = Guid.NewGuid(), Name = "Assignment test" };
        var designation = new Designation { Id = Guid.NewGuid(), Name = "Analyst", MaximumLeaveDays = 20 };
        var employee = new Employee { Id = Guid.NewGuid(), FirstName = "Fixture", LastName = "Employee", Email = "employee@example.test", Type = EmployeeType.Permanent };
        db.AddRange(department, designation, employee);
        await db.SaveChangesAsync();
        // Same request shape as the production error: no reportingManagerId member.
        var request = JsonSerializer.Deserialize<AssignEmployeeDto>(JsonSerializer.Serialize(new
        {
            departmentId = department.Id, designationId = designation.Id, staffNumber = "EBO",
            warehouseIds = Array.Empty<Guid>(), level = 1, startDate = "2015-11-01T00:00:00Z"
        }), JsonSerializerOptions.Web) ?? throw new InvalidOperationException("Fixture request is null.");
        return (employee, request);
    }

    private static EmployeeRepository Repository(ApplicationDbContext db)
    {
        var users = new UserManager<User>(new UserStore<User, Role, ApplicationDbContext, Guid>(db),
            Options.Create(new IdentityOptions()), new PasswordHasher<User>(), [], [],
            new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null,
            NullLogger<UserManager<User>>.Instance);
        return new EmployeeRepository(db, NullLogger<EmployeeRepository>.Instance, null!, Mapper, null!, users, null!, null!, null!);
    }

    private sealed class AnonymousActor : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }
}
