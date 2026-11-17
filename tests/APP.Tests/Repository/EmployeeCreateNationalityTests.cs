using DOMAIN.Entities.Countries;
using DOMAIN.Entities.Employees;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.Repository;

public sealed partial class EmployeeAssignmentTests
{
    // Production request: staff number, email, type and every optional field null (nationality included).
    [Fact]
    public async Task Create_without_nationality_saves_instead_of_throwing()
    {
        await using var db = InMemory();
        var result = await Repository(db).CreateEmployee(new CreateEmployeeRequest
        {
            StaffNumber = "EPS312", Email = "eps312@example.test", Type = (EmployeeType)1, Nationality = null
        });
        Assert.True(result.IsSuccess);
        db.ChangeTracker.Clear();
        var saved = await db.Employees.SingleAsync();
        Assert.Equal("EPS312", saved.StaffNumber);
        Assert.Null(saved.Nationality);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("0f8fad5b-d9cb-469f-a165-70867728950e")]
    public async Task Create_with_unknown_nationality_is_a_validation_error(string nationality)
    {
        await using var db = InMemory();
        var result = await Repository(db).CreateEmployee(new CreateEmployeeRequest { StaffNumber = "EPS313", Nationality = nationality });
        Assert.True(result.IsFailure);
        Assert.Equal("Nationality.Invalid", result.Error.Code);
        Assert.Empty(await db.Employees.ToListAsync());
    }

    [Fact]
    public async Task Create_with_country_id_stores_the_country_name()
    {
        await using var db = InMemory();
        var ghana = new Country { Id = Guid.NewGuid(), Name = "Ghana", Nationality = "Ghanaian", Code = "GH" };
        db.Add(ghana);
        await db.SaveChangesAsync();
        var result = await Repository(db).CreateEmployee(new CreateEmployeeRequest { StaffNumber = "EPS314", Nationality = ghana.Id.ToString() });
        Assert.True(result.IsSuccess);
        Assert.Equal("Ghana", (await db.Employees.SingleAsync()).Nationality);
    }
}
