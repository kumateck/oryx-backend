using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.ProductionOrders;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

/// <summary>A production role's approvals stay inside the holder's own production department.</summary>
public class ApprovalDepartmentScopeTests
{
    private readonly Department beta = new() { Id = Guid.NewGuid(), Name = "Beta", Type = DepartmentType.Production };
    private readonly Department syrup = new() { Id = Guid.NewGuid(), Name = "Syrup", Type = DepartmentType.Production };
    private readonly Department sales = new() { Id = Guid.NewGuid(), Name = "Sales", Type = DepartmentType.NonProduction };
    private readonly Role manager = new() { Id = Guid.NewGuid(), Name = "General Manager Production", Type = DepartmentType.Production };
    private readonly Role procurement = new() { Id = Guid.NewGuid(), Name = "Procurement Manager", Type = DepartmentType.NonProduction };

    [Fact]
    public async Task Production_role_sees_only_its_own_production_departments_documents()
    {
        await using var db = Context();
        var reviewer = User(beta);
        var betaAuthor = User(beta);
        var syrupAuthor = User(syrup);
        var salesAuthor = User(sales);
        var noDepartment = User(null);
        db.AddRange(beta, syrup, sales, manager, procurement, reviewer, betaAuthor, syrupAuthor, salesAuthor, noDepartment);
        var own = Order(betaAuthor, role: manager.Id);
        var otherDepartment = Order(syrupAuthor, role: manager.Id);
        var namedInOther = Order(syrupAuthor, user: reviewer.Id);
        var openRoleInOther = Order(syrupAuthor, role: procurement.Id);
        var fromSales = Order(salesAuthor, role: manager.Id);
        var unowned = Order(noDepartment, role: manager.Id);
        db.ProductionOrders.AddRange(own, otherDepartment, namedInOther, openRoleInOther, fromSales, unowned);
        await Save(db, (own, betaAuthor), (otherDepartment, syrupAuthor), (namedInOther, syrupAuthor),
            (openRoleInOther, syrupAuthor), (fromSales, salesAuthor), (unowned, noDepartment));

        var keys = await Repository(db).GetPendingApprovalKeys(reviewer.Id, [manager.Id, procurement.Id]);

        var ids = keys.Select(key => key.Id).ToHashSet();
        Assert.Contains(own.Id, ids);
        Assert.Contains(namedInOther.Id, ids);
        Assert.Contains(openRoleInOther.Id, ids);
        Assert.Contains(fromSales.Id, ids);
        Assert.Contains(unowned.Id, ids);
        Assert.DoesNotContain(otherDepartment.Id, ids);
        Assert.Equal(5, (await Repository(db).GetStatisticsOfEntitiesRequiringApproval(reviewer.Id, [manager.Id, procurement.Id]))[nameof(ProductionOrder)]);
    }

    [Fact]
    public async Task Holders_outside_production_departments_are_unaffected()
    {
        await using var db = Context();
        var head = User(sales);
        var syrupAuthor = User(syrup);
        db.AddRange(syrup, sales, manager, head, syrupAuthor);
        var order = Order(syrupAuthor, role: manager.Id);
        db.ProductionOrders.Add(order);
        await Save(db, (order, syrupAuthor));

        var keys = await Repository(db).GetPendingApprovalKeys(head.Id, [manager.Id]);

        Assert.Contains(keys, key => key.Id == order.Id);
    }

    [Fact]
    public void PostgreSql_translates_every_scoped_queue()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=sql_translation_only;Username=unused").Options, new Actor());
        var user = Guid.NewGuid();
        var scope = new ApprovalDepartmentScope(Guid.NewGuid(), [Guid.NewGuid()], [Guid.NewGuid()], user);
        var queries = new PendingApprovalQueries(db, user, [Guid.NewGuid()], scope).Keys().ToList();
        Assert.Equal(25, queries.Count);
        foreach (var query in queries) Assert.Contains("EXISTS", query.ToQueryString());
    }

    /// <summary>Saving stamps the signed-in user as author, so the real authors are set afterwards.</summary>
    private static async Task Save(ApplicationDbContext db, params (ProductionOrder order, User author)[] authors)
    {
        await db.SaveChangesAsync();
        foreach (var (order, author) in authors) order.CreatedById = author.Id;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    private static User User(Department department) => new()
    {
        Id = Guid.NewGuid(), UserName = Guid.NewGuid().ToString(), DepartmentId = department?.Id, Department = department,
    };

    private static ProductionOrder Order(User author, Guid? role = null, Guid? user = null) => new()
    {
        Id = Guid.NewGuid(), Code = "SCOPE", CustomerId = Guid.NewGuid(), CreatedById = author.Id,
        Approvals = [new ProductionOrderApprovals {
            Id = Guid.NewGuid(), ApprovalId = Guid.NewGuid(), RoleId = role, UserId = user,
            ActivatedAt = DateTime.UtcNow, Status = ApprovalStatus.Pending,
        }],
    };

    private static ApplicationDbContext Context() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor());

    private static ApprovalRepository Repository(ApplicationDbContext db) => new(
        db, new MapperConfiguration(_ => { }).CreateMapper(), null!, null!,
        NullLogger<ApprovalRepository>.Instance, null!, null!);

    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => "";
    }
}
