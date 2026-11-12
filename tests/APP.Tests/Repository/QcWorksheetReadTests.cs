using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Checklists;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Forms.Request;
using DOMAIN.Entities.Grns;
using DOMAIN.Entities.MaterialSampling;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public sealed class QcWorksheetReadTests
{
    [Fact]
    public async Task Atr_queue_and_details_show_the_replacement_from_the_exact_worksheet()
    {
        await using var db = NewContext();
        using var services = MapperServices(db);
        var mapper = services.GetRequiredService<IMapper>();
        var previous = new User { Id = Guid.NewGuid(), FirstName = "Previous", LastName = "Analyst" };
        var replacement = new User { Id = Guid.NewGuid(), FirstName = "Current", LastName = "Analyst" };
        var other = new User { Id = Guid.NewGuid(), FirstName = "Other", LastName = "Analyst" };
        var product = new Product { Id = Guid.NewGuid(), Name = "Product" };
        var schedule = new ProductionSchedule { Id = Guid.NewGuid() };
        var scheduleProduct = new ProductionScheduleProduct
        {
            Id = Guid.NewGuid(), Product = product, ProductionSchedule = schedule
        };
        var batch = new BatchManufacturingRecord { Id = Guid.NewGuid() };
        var step = new ProductionActivityStep { Id = Guid.NewGuid() };
        var atr = new AnalyticalTestRequest
        {
            Id = Guid.NewGuid(), Status = AnalyticalTestStatus.Testing, Stage = TestStage.Bulk,
            ProductionScheduleProduct = scheduleProduct, BatchManufacturingRecord = batch, ProductionActivityStep = step,
            Assignees = [new() { User = previous }]
        };
        var assignment = new FormAssignee
        {
            Id = Guid.NewGuid(), FormId = Guid.NewGuid(), BatchManufacturingRecordId = batch.Id,
            ProductionActivityStepId = step.Id, Stage = TestStage.Bulk,
            FieldAssignees =
            [
                new() { FormFieldId = Guid.NewGuid(), Assignee = previous },
                new() { FormFieldId = Guid.NewGuid(), Assignee = previous }
            ]
        };
        db.AddRange(atr, assignment, new FormAssignee
        {
            Id = Guid.NewGuid(), FormId = Guid.NewGuid(), BatchManufacturingRecordId = batch.Id,
            ProductionActivityStepId = Guid.NewGuid(), Stage = TestStage.Bulk,
            FieldAssignees = [new() { FormFieldId = Guid.NewGuid(), Assignee = other }]
        }, replacement);
        await db.SaveChangesAsync();
        var forms = new FormRepository(db, mapper, null!, null!, null!);
        var changed = await forms.ReassignFormAssignee(new ReassignFormAssigneeRequest
        {
            FormAssigneeId = assignment.Id, OldAssigneeId = previous.Id, NewAssigneeId = replacement.Id
        });
        Assert.True(changed.IsSuccess);

        db.ChangeTracker.Clear();
        var repository = new AnalyticalTestRequestRepository(db, mapper, null!);
        var queue = await repository.GetAnalyticalTestRequests(1, 10, "", AnalyticalTestStatus.Testing);
        var details = await repository.GetAnalyticalTestRequest(atr.Id);

        Assert.Equal(replacement.Id, Assert.Single(Assert.Single(queue.Value.Data).Assignees).Id);
        Assert.Equal(replacement.Id, Assert.Single(details.Value.Assignees).Id);
        Assert.Equal(AnalyticalTestStatus.Testing, details.Value.Status);
    }

    [Theory]
    [InlineData(MaterialKind.Raw)]
    [InlineData(MaterialKind.Package)]
    public async Task Received_batch_worksheet_uses_the_current_sample_response_and_assignments(MaterialKind kind)
    {
        await using var db = NewContext();
        using var services = MapperServices(db);
        var mapper = services.GetRequiredService<IMapper>();
        var material = new Material { Id = Guid.NewGuid(), Kind = kind };
        var note = new Grn { Id = Guid.NewGuid(), GrnNumber = "GRN-1" };
        var batch = new MaterialBatch
        {
            Id = Guid.NewGuid(), Material = material, Grn = note,
            Checklist = new Checklist { Id = Guid.NewGuid() },
            UoM = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Gram", Symbol = "g" }
        };
        var oldSample = new MaterialSampling
        {
            Id = Guid.NewGuid(), MaterialBatch = batch, Grn = note,
            ArNumber = "OLD", SampleDate = DateTime.UtcNow.AddDays(-1), ChemicalArdId = Guid.NewGuid()
        };
        var currentSample = new MaterialSampling
        {
            Id = Guid.NewGuid(), MaterialBatch = batch, Grn = note,
            ArNumber = "CURRENT", SampleDate = DateTime.UtcNow, ChemicalArdId = Guid.NewGuid()
        };
        var form = new Form { Id = Guid.NewGuid(), Name = "Worksheet" };
        var oldResponse = new Response
        {
            Id = Guid.NewGuid(), MaterialBatch = batch, MaterialSamplingId = oldSample.Id, Form = form
        };
        var oldAssignment = new FormAssignee
        {
            Id = Guid.NewGuid(), MaterialBatch = batch, MaterialSamplingId = oldSample.Id, Form = form
        };
        db.AddRange(oldSample, currentSample, oldResponse, oldAssignment);
        await db.SaveChangesAsync();

        var worksheet = new MaterialAnalyticalRawDataRepository(db, mapper);
        var forms = new FormRepository(db, mapper, null!, null!, null!);
        var before = await worksheet.GetRelevantMaterialInfoForArd(batch.Id);
        Assert.True(before.IsSuccess, string.Join(", ", before.Errors.Select(error => error.Description)));
        Assert.Equal("CURRENT", before.Value.ArNumber);
        Assert.Null(before.Value.WorksheetResponseId);
        Assert.True((await forms.GetFormAssigneeByBatch(batch.Id, null, null)).IsFailure);

        var currentResponse = new Response
        {
            Id = Guid.NewGuid(), MaterialBatch = batch, MaterialSamplingId = currentSample.Id, Form = form
        };
        var currentAssignment = new FormAssignee
        {
            Id = Guid.NewGuid(), MaterialBatch = batch, MaterialSamplingId = currentSample.Id, Form = form
        };
        db.AddRange(currentResponse, currentAssignment);
        await db.SaveChangesAsync();

        var current = (await worksheet.GetRelevantMaterialInfoForArd(batch.Id)).Value;
        Assert.Equal(currentResponse.Id, current.WorksheetResponseId);
        Assert.Equal(form.Id, current.WorksheetFormId);
        Assert.Equal(currentAssignment.Id, (await forms.GetFormAssigneeByBatch(batch.Id, null, null)).Value.Id);
    }

    private static ApplicationDbContext NewContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new QcReader());

    private static ServiceProvider MapperServices(ApplicationDbContext db) => new ServiceCollection()
        .AddSingleton(db).AddLogging().AddAutoMapper(configure => { }, typeof(OryxMapper)).BuildServiceProvider();

    private sealed class QcReader : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => "";
    }
}
