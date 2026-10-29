using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

/// <summary>
/// Before anyone requests stock, a batch has no raw or packaging stock requisition. That is a normal
/// state, so the lookups answer "not found" (404), which the production board reads as "not raised yet",
/// instead of a null-value validation error (400) that made the board treat the step as broken.
/// </summary>
public class StockRequisitionLookupTests
{
    [Fact]
    public async Task Lookups_answer_not_found_before_stock_is_requested()
    {
        await using var fixture = await Fixture.Create();
        var batchId = Guid.NewGuid();

        var raw = await fixture.Repository.GetStockRequisitionForRaw(batchId);
        var packaging = await fixture.Repository.GetStockRequisitionForPackaging(batchId);

        Assert.True(raw.IsFailure);
        Assert.Equal(ErrorType.NotFound, raw.Errors.First().Type);
        Assert.Equal("StockRequisition.NotRaised", raw.Errors.First().Code);
        Assert.Contains("raw material", raw.Errors.First().Description);

        Assert.True(packaging.IsFailure);
        Assert.Equal(ErrorType.NotFound, packaging.Errors.First().Type);
        Assert.Contains("packaging material", packaging.Errors.First().Description);
    }

    [Fact]
    public async Task Each_lookup_finds_only_its_own_kind_of_stock_requisition()
    {
        await using var fixture = await Fixture.Create();
        var batchId = Guid.NewGuid();
        fixture.Requisition(batchId, "RM/N/26/001", RequisitionType.Stock);
        fixture.Requisition(batchId, "PM/N/26/001", RequisitionType.Purchase); // not a stock requisition
        await fixture.Context.SaveChangesAsync();

        var raw = await fixture.Repository.GetStockRequisitionForRaw(batchId);
        var packaging = await fixture.Repository.GetStockRequisitionForPackaging(batchId);

        Assert.True(raw.IsSuccess);
        Assert.Equal("RM/N/26/001", raw.Value.Code);
        Assert.True(packaging.IsFailure);
        Assert.Equal(ErrorType.NotFound, packaging.Errors.First().Type);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public ApplicationDbContext Context { get; }
        private ServiceProvider Services { get; }
        private User Requester { get; }
        private Department Department { get; } = new() { Id = Guid.NewGuid(), Name = "Production" };

        public ProductionScheduleRepository Repository =>
            new(Context, Services.GetRequiredService<IMapper>(), null!, null!, null!, null!, new NoOpProductionActivityStepEventPublisher());

        private Fixture()
        {
            Context = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
                new TestUser(Department.Id));
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(Context);
            var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
            http.HttpContext.Request.Host = new HostString("oryx.test");
            services.AddSingleton<IHttpContextAccessor>(http);
            services.AddAutoMapper(configuration => { }, typeof(OryxMapper));
            Services = services.BuildServiceProvider();
            Requester = new User { Id = Guid.NewGuid(), FirstName = "Akosua", LastName = "Akpene", Email = "akosua@oryx.test", UserName = "akosua@oryx.test" };
            Context.AddRange(Department, Requester);
        }

        public static async Task<Fixture> Create()
        {
            var fixture = new Fixture();
            await fixture.Context.SaveChangesAsync();
            return fixture;
        }

        public void Requisition(Guid batchId, string code, RequisitionType type) =>
            Context.Requisitions.Add(new Requisition
            {
                Id = Guid.NewGuid(), Code = code, RequisitionType = type, ProductionScheduleProductId = batchId,
                RequestedById = Requester.Id, RequestedBy = Requester, DepartmentId = Department.Id, Department = Department,
                Comments = "", Items = [], Approvals = [],
            });

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Services.DisposeAsync();
        }
    }

    private sealed record TestUser(Guid Department) : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => Department;
        public string DepartmentType => nameof(SHARED.DepartmentType.Production);
    }
}
