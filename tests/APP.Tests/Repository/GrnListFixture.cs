using APP.Repository;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Grns;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Services.Identity;

namespace APP.Tests.Repository;

internal sealed class GrnListFixture : IAsyncDisposable
{
    public sealed class Actor(Guid departmentId) : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => departmentId;
        public string DepartmentType { get; set; } = "Production";
    }
    public Actor CurrentUser { get; }
    public ApplicationDbContext Context { get; }
    public Department Department { get; } = new() { Id = Guid.NewGuid(), Name = "Receiving" };
    public Department OtherDepartment { get; } = new() { Id = Guid.NewGuid(), Name = "Other receiving" };
    public User Creator { get; }
    public User OtherCreator { get; }
    public UnitOfMeasure Unit { get; } = new() { Id = Guid.NewGuid(), Symbol = "mg" };
    private readonly List<(Grn Note, Guid Creator)> notes = [];
    public WarehouseRepository Repository => new(Context, null!, null!, NullLogger<WarehouseRepository>.Instance);

    public GrnListFixture()
    {
        CurrentUser = new(Department.Id);
        Context = new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, CurrentUser);
        Creator = new() { Id = Guid.NewGuid(), DepartmentId = Department.Id, FirstName = "Receiver" };
        OtherCreator = new() { Id = Guid.NewGuid(), DepartmentId = OtherDepartment.Id, FirstName = "Other receiver" };
        Context.AddRange(Department, OtherDepartment, Creator, OtherCreator, Unit);
    }

    public (Grn Note, MaterialBatch[] Batches, Material Material) AddNote(string number,
        MaterialKind kind = MaterialKind.Raw, int count = 2, bool otherDepartment = false,
        Status status = Status.Pending, bool deleted = false)
    {
        var material = new Material { Id = Guid.NewGuid(), Name = "Material", Kind = kind };
        var note = new Grn { Id = Guid.NewGuid(), GrnNumber = number, CarrierName = "Carrier name",
            VehicleNumber = "GT-1", Status = status, DeletedAt = deleted ? DateTime.UtcNow : null };
        var batches = Enumerable.Range(0, count).Select(_ => new MaterialBatch {
            Id = Guid.NewGuid(), MaterialId = material.Id, GrnId = note.Id, UoMId = Unit.Id,
            BatchNumber = "BATCH", TotalQuantity = 100, Status = BatchStatus.Quarantine
        }).ToArray();
        notes.Add((note, otherDepartment ? OtherCreator.Id : Creator.Id));
        Context.AddRange(material, note); Context.AddRange(batches);
        return (note, batches, material);
    }

    public async Task Save()
    {
        await Context.SaveChangesAsync();
        foreach (var (note, creator) in notes)
        {
            note.CreatedById = creator;
            note.CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
    }

    public ValueTask DisposeAsync() => Context.DisposeAsync();
}
