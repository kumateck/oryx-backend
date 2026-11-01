using API.Controllers;
using APP.Repository;
using APP.Services.Approvals;
using AutoMapper;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.ProductionOrders;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class ApprovalStatisticsTests
{
    [Fact]
    public async Task Stats_count_documents_without_materializing_their_entity_graphs()
    {
        var materializations = new Materializations();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(materializations).Options;
        var reviewer = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        await using (var seed = new ApplicationDbContext(options, new Actor()))
        {
            seed.ProductionOrders.Add(new ProductionOrder
            {
                Id = documentId, Code = "PO-STATS", CustomerId = Guid.NewGuid(),
                Approvals = [
                    new ProductionOrderApprovals { Id = Guid.NewGuid(), ApprovalId = Guid.NewGuid(),
                        UserId = reviewer, ActivatedAt = DateTime.UtcNow, Order = 1 },
                    new ProductionOrderApprovals { Id = Guid.NewGuid(), ApprovalId = Guid.NewGuid(),
                        UserId = reviewer, ActivatedAt = DateTime.UtcNow, Order = 1 },
                ],
            });
            await seed.SaveChangesAsync();
        }
        await using var db = new ApplicationDbContext(options, new Actor());
        var repository = Repository(db);
        var controller = new ApprovalController(repository, new ApprovalDelegationService(db), null!)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        controller.HttpContext.Items["Sub"] = reviewer.ToString();
        controller.HttpContext.Items["Roles"] = new List<Guid>();

        var result = Assert.IsType<Ok<Dictionary<string, int>>>(await controller.GetPendingApprovalStatistics());

        Assert.Equal(1, result.Value![nameof(ProductionOrder)]);
        Assert.Empty(materializations.Types);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public void PostgreSql_statistics_queries_do_not_load_auto_included_collections()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=sql_translation_only;Username=unused").Options, new Actor());
        var queries = new PendingApprovalQueries(db, Guid.NewGuid(), [Guid.NewGuid()]).Keys().ToList();
        Assert.Equal(26, queries.Count);
        foreach (var query in queries)
        {
            var sql = query.ToQueryString();
            Assert.Contains("EXISTS", sql);
            // Required joins in global soft-delete filters must remain. Collection loading must not.
            Assert.DoesNotContain("WarehouseLocations", sql);
            Assert.DoesNotContain("MaterialBatchEvents", sql);
            Assert.DoesNotContain("MaterialBatchReservedQuantities", sql);
            Assert.DoesNotContain("ApprovalActionLogs", sql);
        }
    }

    [Fact]
    public async Task Statistics_preserve_assignments_activation_deletion_and_document_counting()
    {
        await using var db = Context();
        var reviewer = Guid.NewGuid();
        var role = Guid.NewGuid();
        var other = Guid.NewGuid();
        ProductionOrder Order(Guid? assignedUser, Guid? assignedRole, bool active = true,
            ApprovalStatus status = ApprovalStatus.Pending, bool deleted = false) => new()
        {
            Id = Guid.NewGuid(), Code = "STAT", CustomerId = Guid.NewGuid(),
            DeletedAt = deleted ? DateTime.UtcNow : null,
            Approvals = [new ProductionOrderApprovals {
                Id = Guid.NewGuid(), ApprovalId = Guid.NewGuid(), UserId = assignedUser,
                RoleId = assignedRole, ActivatedAt = active ? DateTime.UtcNow : null, Status = status,
            }],
        };
        db.ProductionOrders.AddRange(Order(reviewer, null), Order(null, role), Order(other, null),
            Order(reviewer, null, active: false), Order(reviewer, null, status: ApprovalStatus.Approved),
            Order(reviewer, null, deleted: true));
        await db.SaveChangesAsync();
        var result = await Repository(db).GetStatisticsOfEntitiesRequiringApproval(reviewer, [Guid.NewGuid(), role]);
        Assert.Equal(2, result[nameof(ProductionOrder)]);
        Assert.Single(result);
        Assert.Empty(await Repository(db).GetStatisticsOfEntitiesRequiringApproval(Guid.NewGuid(), []));
    }

    [Fact]
    public async Task Statistics_preserve_requisition_types_and_latest_response_round()
    {
        await using var db = Context();
        var reviewer = Guid.NewGuid();
        foreach (var type in Enum.GetValues<DOMAIN.Entities.Requisitions.RequisitionType>())
            db.Requisitions.Add(new DOMAIN.Entities.Requisitions.Requisition {
                Id = Guid.NewGuid(), RequisitionType = type,
                Approvals = [new DOMAIN.Entities.Requisitions.RequisitionApproval {
                    Id = Guid.NewGuid(), ApprovalId = Guid.NewGuid(), UserId = reviewer,
                    ActivatedAt = DateTime.UtcNow,
                }],
            });
        var form = new DOMAIN.Entities.Forms.Form { Id = Guid.NewGuid(), Name = "Stats form" };
        var response = new DOMAIN.Entities.Forms.Response {
            Id = Guid.NewGuid(), FormId = form.Id, Form = form,
            Approvals = [
                new DOMAIN.Entities.Forms.ResponseApproval { Id = Guid.NewGuid(), ApprovalId = Guid.NewGuid(),
                    ApprovalRound = 1, UserId = reviewer, ActivatedAt = DateTime.UtcNow },
                new DOMAIN.Entities.Forms.ResponseApproval { Id = Guid.NewGuid(), ApprovalId = Guid.NewGuid(),
                    ApprovalRound = 2, UserId = Guid.NewGuid(), ActivatedAt = DateTime.UtcNow },
            ],
        };
        db.Responses.Add(response);
        await db.SaveChangesAsync();
        var result = await Repository(db).GetStatisticsOfEntitiesRequiringApproval(reviewer, []);
        Assert.Equal(3, result.Count);
        Assert.Equal(1, result["StockRequisition"]);
        Assert.Equal(1, result["TrialRequisition"]);
        Assert.Equal(1, result["PurchaseRequisition"]);
        Assert.False(result.ContainsKey("Response"));
        response.Approvals[1].UserId = reviewer;
        await db.SaveChangesAsync();
        Assert.Equal(1, (await Repository(db).GetStatisticsOfEntitiesRequiringApproval(reviewer, []))["Response"]);
    }

    private static ApplicationDbContext Context() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor());

    private static ApprovalRepository Repository(ApplicationDbContext db) => new(
        db, new MapperConfiguration(_ => { }).CreateMapper(), null!, null!,
        NullLogger<ApprovalRepository>.Instance, null!, null!);

    private sealed class Materializations : IMaterializationInterceptor
    {
        public List<Type> Types { get; } = [];
        public object InitializedInstance(MaterializationInterceptionData data, object entity)
        {
            Types.Add(entity.GetType());
            return entity;
        }
    }

    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => "";
    }
}
