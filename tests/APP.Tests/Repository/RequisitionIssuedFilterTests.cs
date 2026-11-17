using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public sealed class RequisitionIssuedFilterTests
{
    [Fact]
    public async Task The_store_lists_issued_and_awaiting_requisitions_apart()
    {
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Reader());
        using var services = new ServiceCollection().AddSingleton(db).AddLogging()
            .AddAutoMapper(configure => { }, typeof(OryxMapper)).BuildServiceProvider();
        var store = Guid.NewGuid();
        var requester = new User { Id = Guid.NewGuid(), FirstName = "Eunice", LastName = "Adams" };
        db.Users.Add(requester);
        Requisition Raise(string code, RequestStatus status, bool approved) => new()
        {
            Id = Guid.NewGuid(), Code = code, DepartmentId = store, RequisitionType = RequisitionType.Stock, Status = status, Approved = approved, RequestedById = requester.Id
        };
        // RM/009 is issued but by another store, so it never appears.
        var otherStore = Raise("RM/009", RequestStatus.Completed, true);
        otherStore.DepartmentId = Guid.NewGuid();
        db.Requisitions.AddRange(Raise("RM/007", RequestStatus.Pending, false), Raise("RM/008", RequestStatus.New, true),
            Raise("RM/006", RequestStatus.Completed, true), Raise("RM/005", RequestStatus.Rejected, false), otherStore);
        await db.SaveChangesAsync();
        var repository = new RequisitionRepository(db, services.GetRequiredService<IMapper>(), null!, null!, null!, null!, null!, null!, null!, null!, identity: new Reader());

        async Task<string[]> Codes(bool? issued) => (await repository.GetRequisitions(1, 50, null, null, RequisitionType.Stock, store, null, issued))
            .Value.Data.Select(r => r.Code).OrderBy(code => code).ToArray();

        Assert.Equal(["RM/006"], await Codes(true));
        Assert.Equal(["RM/007", "RM/008"], await Codes(false));
        Assert.Equal(["RM/005", "RM/006", "RM/007", "RM/008"], await Codes(null));
    }

    private sealed class Reader : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => "";
    }
}
