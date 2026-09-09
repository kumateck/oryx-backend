using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Instruments;
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

public class InstrumentCalibrationTests
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

    private static InstrumentRepository CreateRepository(ApplicationDbContext context) =>
        new(context, CreateMapper());

    private static async Task<Instrument> SeedInstrument(ApplicationDbContext context, DateTime? dueDate = null)
    {
        var instrument = new Instrument
        {
            Id = Guid.NewGuid(),
            Code = "INS-0001",
            Name = "Disintegration Tester",
            CalibrationDueDate = dueDate,
        };
        context.Instruments.Add(instrument);
        await context.SaveChangesAsync();
        return instrument;
    }

    [Fact]
    public async Task UpdateCalibration_updates_fields()
    {
        await using var context = CreateContext();
        var repository = CreateRepository(context);
        var instrument = await SeedInstrument(context);
        var dueDate = DateTime.UtcNow.AddMonths(6);

        var result = await repository.UpdateCalibration(
            instrument.Id,
            new UpdateInstrumentCalibrationRequest
            {
                CalibrationDueDate = dueDate,
                LastCalibratedAt = DateTime.UtcNow,
                QualificationStatus = InstrumentQualificationStatus.Qualified,
            },
            Guid.NewGuid()
        );

        Assert.True(result.IsSuccess);
        var updated = await context.Instruments.FirstAsync(i => i.Id == instrument.Id);
        Assert.Equal(dueDate, updated.CalibrationDueDate);
        Assert.Equal(InstrumentQualificationStatus.Qualified, updated.QualificationStatus);
    }

    [Fact]
    public async Task UpdateCalibration_returns_not_found_for_missing_instrument()
    {
        await using var context = CreateContext();
        var repository = CreateRepository(context);

        var result = await repository.UpdateCalibration(
            Guid.NewGuid(),
            new UpdateInstrumentCalibrationRequest(),
            Guid.NewGuid()
        );

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateCalibration_rejects_nonexistent_attachment()
    {
        await using var context = CreateContext();
        var repository = CreateRepository(context);
        var instrument = await SeedInstrument(context);

        var result = await repository.UpdateCalibration(
            instrument.Id,
            new UpdateInstrumentCalibrationRequest { CalibrationCertificateAttachmentId = Guid.NewGuid() },
            Guid.NewGuid()
        );

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateCalibration_accepts_existing_attachment()
    {
        await using var context = CreateContext();
        var repository = CreateRepository(context);
        var instrument = await SeedInstrument(context);
        var attachment = new Attachment { Id = Guid.NewGuid(), ModelId = instrument.Id, ModelType = nameof(Instrument), Name = "cert.pdf" };
        context.Attachments.Add(attachment);
        await context.SaveChangesAsync();

        var result = await repository.UpdateCalibration(
            instrument.Id,
            new UpdateInstrumentCalibrationRequest { CalibrationCertificateAttachmentId = attachment.Id },
            Guid.NewGuid()
        );

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetInstrumentsWithCalibrationDue_filters_by_day_boundary()
    {
        await using var context = CreateContext();
        var repository = CreateRepository(context);
        var asOf = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var withinRange = await SeedInstrument(context, asOf.AddDays(30));
        var atBoundary = await SeedInstrument(context, asOf.AddDays(30));
        await SeedInstrument(context, asOf.AddDays(31));
        await SeedInstrument(context, null);

        var result = await repository.GetInstrumentsWithCalibrationDue(30, asOf);

        Assert.True(result.IsSuccess);
        var ids = result.Value.Select(i => i.Id).ToList();
        Assert.Contains(withinRange.Id, ids);
        Assert.Contains(atBoundary.Id, ids);
        Assert.Equal(2, ids.Count);
    }
}
