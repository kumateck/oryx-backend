using System.Text.Json;
using APP.Services.Formulas;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public sealed class FormulaSnapshotExecutionOrderTests
{
    [Fact]
    public async Task Orders_source_before_dependent_formula()
    {
        var seeded = await SeedAsync(("dependent", Dependency("source")),
            ("source", "[]"));

        var result = await FormulaSnapshotExecutionOrder.OrderAsync(
            seeded.Context, seeded.ResponseId, seeded.Dtos, default);

        Assert.Equal(["source", "dependent"],
            result!.Select(item => item.PlacementKey).ToArray());
    }

    [Fact]
    public async Task Rejects_runtime_dependency_cycle()
    {
        var seeded = await SeedAsync(("first", Dependency("second")),
            ("second", Dependency("first")));

        var result = await FormulaSnapshotExecutionOrder.OrderAsync(
            seeded.Context, seeded.ResponseId, seeded.Dtos, default);

        Assert.Null(result);
    }

    [Fact]
    public async Task Rejects_reference_that_disagrees_with_dependency()
    {
        var seeded = await SeedAsync(
            ("dependent", Dependency("source", "different")), ("source", "[]"));

        var result = await FormulaSnapshotExecutionOrder.OrderAsync(
            seeded.Context, seeded.ResponseId, seeded.Dtos, default);

        Assert.Null(result);
    }

    private static string Dependency(
        string placementKey, string? referencedPlacementKey = null) => JsonSerializer.Serialize(
        new[] { new { variableKey = "input", source = new
        {
            sourceType = 8,
            reference = $"placement/{referencedPlacementKey ?? placementKey}/result/result",
            dependencies = new[] { new { placementKey, resultKey = "result" } }
        } } });

    private static async Task<Seeded> SeedAsync(
        params (string Key, string Bindings)[] configurations)
    {
        var actor = Guid.NewGuid();
        var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            new TestUser(actor));
        var response = new Response { Id = Guid.NewGuid(), CreatedById = actor };
        var snapshots = configurations.Select(item => Snapshot(
            response, item.Key, item.Bindings)).ToList();
        context.Add(response);
        context.AddRange(snapshots);
        await context.SaveChangesAsync();
        var dtos = snapshots.Select(item => new FormulaSnapshotDto(
            item.Id, item.ResponseId, item.PlacementKey, item.Sequence,
            item.FormulaRevisionId, item.DefinitionHash, item.ConfigurationHash,
            item.CapturedAt)).ToList();
        return new Seeded(context, response.Id, dtos);
    }

    private static ResponseFormulaSnapshot Snapshot(
        Response response, string key, string bindings) => new()
    {
        Id = Guid.NewGuid(), ResponseId = response.Id, Response = response,
        PlacementKey = key, Sequence = 1, FormulaRevisionId = Guid.NewGuid(),
        DefinitionHash = new string('a', 64), ConfigurationHash = new string('b', 64),
        ExecutableDefinitionJson = "{}", BindingsJson = bindings,
        ResultTargetsJson = "[]", TableShapeJson = "{}",
        CalculationPolicyJson = "{}", DisplayPolicyJson = "{}",
        MethodReference = "method", CapturedAt = DateTime.UtcNow
    };

    private sealed record Seeded(ApplicationDbContext Context, Guid ResponseId,
        IReadOnlyList<FormulaSnapshotDto> Dtos);

    private sealed class TestUser(Guid id) : ICurrentUserService
    {
        public Guid? UserId => id;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }
}
