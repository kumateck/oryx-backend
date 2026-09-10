using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.RndFormulations;
using DOMAIN.Entities.RndProjects;
using DOMAIN.Entities.RndTechnologyTransfers;
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

public class RndTechnologyTransferRepositoryTests
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

    private static RndTechnologyTransferRepository CreateRepository(ApplicationDbContext context)
    {
        var mapper = CreateMapper();
        return new RndTechnologyTransferRepository(context, mapper, new BoMRepository(context, mapper));
    }

    private static async Task<(RndProject project, RndFormulation formulation, Guid materialId)> SeedFixture(
        ApplicationDbContext context,
        bool withProduct = true,
        RndFormulationStatus formulationStatus = RndFormulationStatus.Approved
    )
    {
        Product product = null;
        if (withProduct)
        {
            product = new Product { Id = Guid.NewGuid() };
            context.Products.Add(product);
        }

        var project = new RndProject
        {
            Id = Guid.NewGuid(),
            Code = "RND-2026-0001",
            Title = "Test project",
            DepartmentId = Guid.NewGuid(),
            RequestedById = Guid.NewGuid(),
            Status = RndProjectStatus.InDevelopment,
            Approved = true,
            ProductId = product?.Id,
        };
        var material = new Material { Id = Guid.NewGuid() };
        var formulation = new RndFormulation
        {
            Id = Guid.NewGuid(),
            RndProjectId = project.Id,
            Version = 2,
            Status = formulationStatus,
            Items =
            [
                new RndFormulationItem
                {
                    MaterialId = material.Id,
                    Order = 1,
                    BaseQuantity = 10,
                    PrescribedQuantity = 10,
                },
            ],
        };
        context.AddRange(project, material, formulation);
        await context.SaveChangesAsync();
        return (project, formulation, material.Id);
    }

    [Fact]
    public async Task CreateTransfer_rejects_unapproved_formulation()
    {
        await using var context = CreateContext();
        var (project, formulation, _) = await SeedFixture(context, formulationStatus: RndFormulationStatus.Draft);
        var repository = CreateRepository(context);

        var result = await repository.CreateTransfer(
            project.Id,
            new CreateRndTechnologyTransferRequest { RndFormulationId = formulation.Id },
            Guid.NewGuid()
        );

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateTransfer_starts_at_due_diligence()
    {
        await using var context = CreateContext();
        var (project, formulation, _) = await SeedFixture(context);
        var repository = CreateRepository(context);

        var result = await repository.CreateTransfer(
            project.Id,
            new CreateRndTechnologyTransferRequest { RndFormulationId = formulation.Id },
            Guid.NewGuid()
        );

        Assert.True(result.IsSuccess);
        var transfer = await context.RndTechnologyTransfers.SingleAsync(t => t.Id == result.Value);
        Assert.Equal(RndTechnologyTransferStatus.DueDiligence, transfer.Status);
    }

    [Fact]
    public async Task UpdateStatus_rejects_skipping_straight_to_protocol_approved()
    {
        await using var context = CreateContext();
        var (project, formulation, _) = await SeedFixture(context);
        var repository = CreateRepository(context);
        var created = await repository.CreateTransfer(
            project.Id,
            new CreateRndTechnologyTransferRequest { RndFormulationId = formulation.Id },
            Guid.NewGuid()
        );

        var result = await repository.UpdateStatus(
            created.Value,
            new UpdateRndTechnologyTransferStatusRequest { Status = RndTechnologyTransferStatus.ProtocolApproved },
            Guid.NewGuid()
        );

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task PromoteToProduction_rejects_when_not_protocol_approved()
    {
        await using var context = CreateContext();
        var (project, formulation, _) = await SeedFixture(context);
        var repository = CreateRepository(context);
        var created = await repository.CreateTransfer(
            project.Id,
            new CreateRndTechnologyTransferRequest { RndFormulationId = formulation.Id },
            Guid.NewGuid()
        );

        var result = await repository.PromoteToProduction(created.Value, Guid.NewGuid());

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task PromoteToProduction_rejects_when_project_has_no_product()
    {
        await using var context = CreateContext();
        var (project, formulation, _) = await SeedFixture(context, withProduct: false);
        var repository = CreateRepository(context);
        var created = await repository.CreateTransfer(
            project.Id,
            new CreateRndTechnologyTransferRequest { RndFormulationId = formulation.Id },
            Guid.NewGuid()
        );
        await repository.UpdateStatus(
            created.Value,
            new UpdateRndTechnologyTransferStatusRequest { Status = RndTechnologyTransferStatus.GapAnalysis },
            Guid.NewGuid()
        );
        await repository.UpdateStatus(
            created.Value,
            new UpdateRndTechnologyTransferStatusRequest { Status = RndTechnologyTransferStatus.ProtocolApproved },
            Guid.NewGuid()
        );

        var result = await repository.PromoteToProduction(created.Value, Guid.NewGuid());

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task PromoteToProduction_creates_bom_and_stamps_product()
    {
        await using var context = CreateContext();
        var (project, formulation, materialId) = await SeedFixture(context);
        var repository = CreateRepository(context);
        var created = await repository.CreateTransfer(
            project.Id,
            new CreateRndTechnologyTransferRequest { RndFormulationId = formulation.Id },
            Guid.NewGuid()
        );
        await repository.UpdateStatus(
            created.Value,
            new UpdateRndTechnologyTransferStatusRequest { Status = RndTechnologyTransferStatus.GapAnalysis },
            Guid.NewGuid()
        );
        await repository.UpdateStatus(
            created.Value,
            new UpdateRndTechnologyTransferStatusRequest { Status = RndTechnologyTransferStatus.ProtocolApproved },
            Guid.NewGuid()
        );

        var result = await repository.PromoteToProduction(created.Value, Guid.NewGuid());

        Assert.True(result.IsSuccess);
        var bom = await context
            .BillOfMaterials.Include(b => b.Items)
            .SingleAsync(b => b.Id == result.Value);
        Assert.Equal(project.ProductId, bom.ProductId);
        Assert.Single(bom.Items);
        Assert.Equal(materialId, bom.Items.First().MaterialId);

        var product = await context.Products.SingleAsync(p => p.Id == project.ProductId);
        Assert.Equal($"MF-{project.Code}-V{formulation.Version}", product.MasterFormulaNumber);
        Assert.Equal(formulation.Version, product.RevisionNumber);

        var transfer = await context.RndTechnologyTransfers.SingleAsync(t => t.Id == created.Value);
        Assert.Equal(RndTechnologyTransferStatus.Completed, transfer.Status);
        Assert.Equal(bom.Id, transfer.BillOfMaterialId);
        Assert.NotNull(transfer.CompletedAt);
    }
}
