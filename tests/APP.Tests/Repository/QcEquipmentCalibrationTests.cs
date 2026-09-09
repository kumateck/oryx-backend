using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Products.Equipments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class NoCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class QcEquipmentCalibrationTests
{
    private static ApplicationDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new NoCurrentUserService()
        );

    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => { }, typeof(OryxMapper));
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static AnalyticalTestRequestRepository CreateRepository(ApplicationDbContext context) =>
        new(context, CreateMapper(), null!);

    private static async Task<QcEquipmentCategory> SeedCategory(ApplicationDbContext context)
    {
        var category = new QcEquipmentCategory { Id = Guid.NewGuid(), Name = "Balances" };
        context.QcEquipmentCategories.Add(category);
        await context.SaveChangesAsync();
        return category;
    }

    [Fact]
    public async Task CreateAndUpdate_round_trips_calibration_fields()
    {
        await using var context = CreateContext();
        var repository = CreateRepository(context);
        var category = await SeedCategory(context);
        var dueDate = DateTime.UtcNow.AddMonths(3);

        var createResult = await repository.CreateQcEquipment(
            new CreateQcEquipment
            {
                EquipmentId = "EQ-0001",
                Name = "Analytical Balance",
                QcEquipmentCategoryId = category.Id,
                CalibrationDueDate = dueDate,
                QualificationStatus = QcEquipmentQualificationStatus.Qualified,
            },
            Guid.NewGuid()
        );

        Assert.True(createResult.IsSuccess);
        var created = await context.QcEquipments.FirstAsync(e => e.Id == createResult.Value);
        Assert.Equal(dueDate, created.CalibrationDueDate);
        Assert.Equal(QcEquipmentQualificationStatus.Qualified, created.QualificationStatus);

        var newDueDate = dueDate.AddMonths(6);
        var updateResult = await repository.UpdateQcEquipment(
            new CreateQcEquipment
            {
                EquipmentId = created.EquipmentId,
                Name = created.Name,
                QcEquipmentCategoryId = category.Id,
                CalibrationDueDate = newDueDate,
                QualificationStatus = QcEquipmentQualificationStatus.DueForCalibration,
            },
            created.Id,
            Guid.NewGuid()
        );

        Assert.True(updateResult.IsSuccess);
        var updated = await context.QcEquipments.FirstAsync(e => e.Id == created.Id);
        Assert.Equal(newDueDate, updated.CalibrationDueDate);
        Assert.Equal(QcEquipmentQualificationStatus.DueForCalibration, updated.QualificationStatus);
    }

    [Fact]
    public async Task GetQcEquipmentsWithCalibrationDue_filters_by_day_boundary()
    {
        await using var context = CreateContext();
        var repository = CreateRepository(context);
        var category = await SeedCategory(context);
        var asOf = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var withinRange = new QcEquipment { Id = Guid.NewGuid(), EquipmentId = "EQ-A", Name = "A", QcEquipmentCategoryId = category.Id, CalibrationDueDate = asOf.AddDays(30) };
        var outOfRange = new QcEquipment { Id = Guid.NewGuid(), EquipmentId = "EQ-B", Name = "B", QcEquipmentCategoryId = category.Id, CalibrationDueDate = asOf.AddDays(31) };
        var noDueDate = new QcEquipment { Id = Guid.NewGuid(), EquipmentId = "EQ-C", Name = "C", QcEquipmentCategoryId = category.Id };
        context.QcEquipments.AddRange(withinRange, outOfRange, noDueDate);
        await context.SaveChangesAsync();

        var result = await repository.GetQcEquipmentsWithCalibrationDue(30, asOf);

        Assert.True(result.IsSuccess);
        var ids = result.Value.Select(e => e.Id).ToList();
        Assert.Single(ids);
        Assert.Contains(withinRange.Id, ids);
    }
}
