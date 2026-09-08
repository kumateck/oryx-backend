using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.RndProjects;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class NoCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class RndProjectRepositoryTests
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
        services.AddHttpContextAccessor();
        services.AddAutoMapper(cfg => { }, typeof(OryxMapper));
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static ApprovalRepository CreateApprovalRepository(ApplicationDbContext context) =>
        new(
            context,
            null!,
            null!,
            null!,
            NullLogger<ApprovalRepository>.Instance,
            null!,
            new NoOpProductionActivityStepEventPublisher()
        );

    private static RndProjectRepository CreateRepository(ApplicationDbContext context) =>
        new(context, CreateMapper(), CreateApprovalRepository(context));

    private static async Task<Department> SeedDepartment(ApplicationDbContext context)
    {
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Code = "RND",
            Name = "Research & Development",
            Type = DepartmentType.NonProduction,
        };
        context.Departments.Add(department);
        await context.SaveChangesAsync();
        return department;
    }

    // RndProject.RequestedBy is a required relationship to User, and User carries a
    // global soft-delete query filter - any query touching that navigation (as
    // GetProject/GetProjects does) silently excludes rows whose RequestedBy doesn't
    // exist as a real row, so tests that read projects back need a real seeded User.
    private static async Task<User> SeedUser(ApplicationDbContext context)
    {
        var user = new User { Id = Guid.NewGuid() };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private static Approval ConfiguredApproval(Guid approverId)
    {
        var approval = new Approval
        {
            Id = Guid.NewGuid(),
            ItemType = nameof(RndProject),
            ApprovalStages = [],
        };
        approval.ApprovalStages.Add(new ApprovalStage
        {
            Id = Guid.NewGuid(),
            ApprovalId = approval.Id,
            Approval = approval,
            Order = 1,
            Required = true,
            UserId = approverId,
        });
        return approval;
    }

    [Fact]
    public async Task CreateProject_fails_when_department_not_seeded()
    {
        await using var context = CreateContext();
        var repository = CreateRepository(context);

        var result = await repository.CreateProject(
            new CreateRndProjectRequest { Title = "New capsule formulation" },
            Guid.NewGuid()
        );

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateProject_generates_sequential_code()
    {
        await using var context = CreateContext();
        await SeedDepartment(context);
        var repository = CreateRepository(context);

        var first = await repository.CreateProject(
            new CreateRndProjectRequest { Title = "Project One" },
            Guid.NewGuid()
        );
        var second = await repository.CreateProject(
            new CreateRndProjectRequest { Title = "Project Two" },
            Guid.NewGuid()
        );

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);

        var firstProject = await context.RndProjects.FirstAsync(p => p.Id == first.Value);
        var secondProject = await context.RndProjects.FirstAsync(p => p.Id == second.Value);

        var year = DateTime.UtcNow.Year;
        Assert.Equal($"RND-{year}-0001", firstProject.Code);
        Assert.Equal($"RND-{year}-0002", secondProject.Code);
    }

    [Fact]
    public async Task CreateProject_rejects_nonexistent_product()
    {
        await using var context = CreateContext();
        await SeedDepartment(context);
        var repository = CreateRepository(context);

        var result = await repository.CreateProject(
            new CreateRndProjectRequest { Title = "Project", ProductId = Guid.NewGuid() },
            Guid.NewGuid()
        );

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateProject_auto_approves_when_no_workflow_configured()
    {
        await using var context = CreateContext();
        await SeedDepartment(context);
        var repository = CreateRepository(context);

        var result = await repository.CreateProject(
            new CreateRndProjectRequest { Title = "Project" },
            Guid.NewGuid()
        );

        Assert.True(result.IsSuccess);
        var project = await context.RndProjects.FirstAsync(p => p.Id == result.Value);
        Assert.True(project.Approved);
        Assert.Equal(RndProjectStatus.InDevelopment, project.Status);
        Assert.Empty(context.RndProjectApprovals);
    }

    [Fact]
    public async Task CreateProject_creates_pending_stage_when_workflow_configured()
    {
        await using var context = CreateContext();
        await SeedDepartment(context);
        var approverId = Guid.NewGuid();
        context.Approvals.Add(ConfiguredApproval(approverId));
        await context.SaveChangesAsync();
        var repository = CreateRepository(context);

        var result = await repository.CreateProject(
            new CreateRndProjectRequest { Title = "Project" },
            Guid.NewGuid()
        );

        Assert.True(result.IsSuccess);
        var project = await context.RndProjects.FirstAsync(p => p.Id == result.Value);
        Assert.False(project.Approved);
        Assert.Equal(RndProjectStatus.Intake, project.Status);

        var stage = Assert.Single(context.RndProjectApprovals);
        Assert.Equal(ApprovalStatus.Pending, stage.Status);
        Assert.Equal(approverId, stage.UserId);
    }

    [Fact]
    public async Task ApproveItem_flips_project_to_approved_and_in_development()
    {
        await using var context = CreateContext();
        await SeedDepartment(context);
        var approverId = Guid.NewGuid();
        context.Approvals.Add(ConfiguredApproval(approverId));
        await context.SaveChangesAsync();
        var repository = CreateRepository(context);
        var approvalRepository = CreateApprovalRepository(context);

        var created = await repository.CreateProject(
            new CreateRndProjectRequest { Title = "Project" },
            Guid.NewGuid()
        );

        var approveResult = await approvalRepository.ApproveItem(
            nameof(RndProject),
            created.Value,
            approverId,
            [Guid.NewGuid()],
            "Looks good"
        );

        Assert.True(approveResult.IsSuccess);
        var project = await context.RndProjects.FirstAsync(p => p.Id == created.Value);
        Assert.True(project.Approved);
        Assert.Equal(RndProjectStatus.InDevelopment, project.Status);
    }

    [Fact]
    public async Task DeleteProject_soft_deletes_and_excludes_from_listing()
    {
        await using var context = CreateContext();
        await SeedDepartment(context);
        var user = await SeedUser(context);
        // Keep the project in Intake (rather than auto-approving to InDevelopment)
        // so the delete-while-Intake guard has something to actually test.
        context.Approvals.Add(ConfiguredApproval(Guid.NewGuid()));
        await context.SaveChangesAsync();
        var repository = CreateRepository(context);
        var created = await repository.CreateProject(
            new CreateRndProjectRequest { Title = "Project" },
            user.Id
        );

        var deleteResult = await repository.DeleteProject(created.Value, Guid.NewGuid());
        Assert.True(deleteResult.IsSuccess);

        var listing = await repository.GetProjects(1, 10, null, null);
        Assert.True(listing.IsSuccess);
        Assert.DoesNotContain(listing.Value.Data, p => p.Id == created.Value);
    }

    [Fact]
    public async Task UpdateStatus_rejects_transition_once_completed()
    {
        await using var context = CreateContext();
        await SeedDepartment(context);
        var repository = CreateRepository(context);
        var created = await repository.CreateProject(
            new CreateRndProjectRequest { Title = "Project" },
            Guid.NewGuid()
        );
        await repository.UpdateStatus(
            created.Value,
            new UpdateRndProjectStatusRequest { Status = RndProjectStatus.Completed },
            Guid.NewGuid()
        );

        var result = await repository.UpdateStatus(
            created.Value,
            new UpdateRndProjectStatusRequest { Status = RndProjectStatus.OnHold },
            Guid.NewGuid()
        );

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetProjects_filters_by_search_and_status()
    {
        await using var context = CreateContext();
        await SeedDepartment(context);
        var user = await SeedUser(context);
        var repository = CreateRepository(context);

        var capsule = await repository.CreateProject(
            new CreateRndProjectRequest { Title = "Capsule reformulation" },
            user.Id
        );
        var syrup = await repository.CreateProject(
            new CreateRndProjectRequest { Title = "Syrup stability study" },
            user.Id
        );
        Assert.True(capsule.IsSuccess);
        Assert.True(syrup.IsSuccess);

        await repository.UpdateStatus(
            capsule.Value,
            new UpdateRndProjectStatusRequest { Status = RndProjectStatus.OnHold },
            user.Id
        );

        var bySearch = await repository.GetProjects(1, 10, "Capsule", null);
        Assert.True(bySearch.IsSuccess);
        Assert.Single(bySearch.Value.Data);

        var byStatus = await repository.GetProjects(1, 10, null, RndProjectStatus.OnHold);
        Assert.True(byStatus.IsSuccess);
        Assert.Single(byStatus.Value.Data);
        Assert.Equal(capsule.Value, byStatus.Value.Data.First().Id);
    }
}
