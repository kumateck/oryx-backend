using System.Text.Json;
using DOMAIN.Entities.Grns;
using DOMAIN.Entities.Materials;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.Repository;

public class GrnListReadTests
{
    [Fact]
    public async Task List_preserves_counts_and_legacy_batch_array_without_mapper_tracking_or_inventory_mutation()
    {
        await using var fixture = new GrnListFixture();
        var raw = fixture.AddNote("WHD/RM/26/001", count: 47);
        fixture.AddNote("WHD/PM/26/001", MaterialKind.Package);
        await fixture.Save();
        var result = await fixture.Repository.GetGrns(1, 50, "", MaterialKind.Raw, null);
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalRecordCount);
        var row = Assert.Single(result.Value.Data);
        Assert.Equal(raw.Note.Id, row.Id);
        Assert.Equal(raw.Note.GrnNumber, row.GrnNumber);
        Assert.Equal("Carrier name", row.CarrierName);
        Assert.Equal("GT-1", row.VehicleNumber);
        Assert.Equal(47, row.MaterialBatches.Count);
        Assert.Equal(raw.Batches.Select(batch => batch.Id).Order(), row.MaterialBatches.Select(batch => batch.Id).Order());
        var json = JsonSerializer.Serialize(row, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        foreach (var forbidden in new[] { "createdBy", "sampledBy", "arNumber", "analysedBy", "material", "warehouses", "events" })
            Assert.DoesNotContain($"\"{forbidden}\"", json);
        Assert.Empty(fixture.Context.ChangeTracker.Entries());
        Assert.All(await fixture.Context.MaterialBatches.IgnoreAutoIncludes().AsNoTracking().ToListAsync(),
            batch => Assert.Equal(100m, batch.TotalQuantity));
    }

    [Fact]
    public async Task Department_and_grn_deletion_filters_stay_active()
    {
        await using var fixture = new GrnListFixture();
        var visible = fixture.AddNote("VISIBLE");
        fixture.AddNote("OTHER", otherDepartment: true);
        fixture.AddNote("DELETED", deleted: true);
        await fixture.Save();
        var result = await fixture.Repository.GetGrns(1, 50, null!, null, null);
        Assert.True(result.IsSuccess);
        Assert.Equal(visible.Note.Id, Assert.Single(result.Value.Data).Id);
        Assert.Equal(1, result.Value.TotalRecordCount);
        Assert.Empty(fixture.Context.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData("NonProduction")]
    [InlineData("RnD")]
    public async Task Existing_cross_department_policy_is_preserved_for_supported_department_types(string type)
    {
        await using var fixture = new GrnListFixture();
        fixture.CurrentUser.DepartmentType = type;
        fixture.AddNote("OWN"); fixture.AddNote("OTHER", otherDepartment: true);
        await fixture.Save();
        var result = await fixture.Repository.GetGrns(1, 50, null!, null, null);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalRecordCount);
    }

    [Fact]
    public async Task Deleted_batches_and_materials_are_excluded_without_dropping_zero_batch_notes_when_kind_is_absent()
    {
        await using var fixture = new GrnListFixture();
        var note = fixture.AddNote("PARTIAL", count: 2);
        note.Batches[0].DeletedAt = DateTime.UtcNow;
        var deletedMaterial = fixture.AddNote("DELETED MATERIAL");
        deletedMaterial.Material.DeletedAt = DateTime.UtcNow;
        await fixture.Save();
        var filtered = await fixture.Repository.GetGrns(1, 50, "", MaterialKind.Raw, null);
        Assert.True(filtered.IsSuccess);
        Assert.Single(Assert.Single(filtered.Value.Data).MaterialBatches);
        var all = await fixture.Repository.GetGrns(1, 50, "", null, null);
        Assert.True(all.IsSuccess);
        Assert.Empty(Assert.Single(all.Value.Data, row => row.Id == deletedMaterial.Note.Id).MaterialBatches);
        Assert.Empty(fixture.Context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Search_status_and_deterministic_pagination_preserve_total_count()
    {
        await using var fixture = new GrnListFixture();
        fixture.AddNote("WHD/RM/001", status: Status.Completed);
        fixture.AddNote("WHD/RM/002", status: Status.Completed);
        fixture.AddNote("OTHER"); fixture.AddNote("EMPTY", count: 0, status: Status.Completed);
        await fixture.Save();
        var first = await fixture.Repository.GetGrns(1, 1, "WHD/RM", null, Status.Completed);
        var second = await fixture.Repository.GetGrns(2, 1, "WHD/RM", null, Status.Completed);
        Assert.True(first.IsSuccess); Assert.True(second.IsSuccess);
        Assert.Equal(2, first.Value.TotalRecordCount); Assert.Equal(2, first.Value.PageCount);
        Assert.NotEqual(Assert.Single(first.Value.Data).Id, Assert.Single(second.Value.Data).Id);
        var emptyPage = await fixture.Repository.GetGrns(3, 1, "WHD/RM", null, Status.Completed);
        Assert.Empty(emptyPage.Value.Data); Assert.Equal(2, emptyPage.Value.TotalRecordCount);
        var carrierSearch = await fixture.Repository.GetGrns(1, 50, "Carrier name", null, null);
        Assert.Equal(4, carrierSearch.Value.TotalRecordCount);
        var completed = await fixture.Repository.GetGrns(1, 50, "", null, Status.Completed);
        Assert.Equal(2, completed.Value.TotalRecordCount); // Original status Any excludes zero-batch notes.
        Assert.Empty(fixture.Context.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(1, 0)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public async Task Invalid_pagination_fails_before_loading_data(int page, int size)
    {
        await using var fixture = new GrnListFixture();
        var result = await fixture.Repository.GetGrns(page, size, "", null, null);
        Assert.True(result.IsFailure);
        Assert.Equal("Grn.Pagination", result.Error.Code);
    }
}
