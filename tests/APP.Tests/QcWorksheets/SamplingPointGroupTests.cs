using DOMAIN.Entities.QcWorksheets;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.QcWorksheets;

/// <summary>
/// The master-data entity introduced in Milestone 2 ahead of Milestone 6's MonitoringProgram.
/// Plain CRUD with no lifecycle — these tests pin the few rules it does have.
/// </summary>
public class SamplingPointGroupTests
{
    [Fact]
    public async Task Groups_are_created_and_listed_alphabetically()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = Guid.NewGuid();

        foreach (var name in new[] { "General Rooms", "Dispensing Booth", "Sterile Core" })
        {
            var created = await harness.SamplingPointGroups.CreateSamplingPointGroup(
                new CreateSamplingPointGroupRequest { Name = name }, userId);
            Assert.True(created.IsSuccess);
        }

        var listed = await harness.SamplingPointGroups.GetSamplingPointGroups(null);

        Assert.True(listed.IsSuccess);
        Assert.Equal(
            ["Dispensing Booth", "General Rooms", "Sterile Core"],
            listed.Value.Select(group => group.Name).ToArray());
    }

    /// <summary>
    /// Names are unique. Two groups with the same name would defeat the point of picking one
    /// from a dropdown — the author could not tell which tier they were selecting.
    /// </summary>
    [Fact]
    public async Task Duplicate_group_names_are_rejected()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = Guid.NewGuid();

        var first = await harness.SamplingPointGroups.CreateSamplingPointGroup(
            new CreateSamplingPointGroupRequest { Name = "General Rooms" }, userId);
        Assert.True(first.IsSuccess);

        var duplicate = await harness.SamplingPointGroups.CreateSamplingPointGroup(
            new CreateSamplingPointGroupRequest { Name = "general rooms" }, userId);

        Assert.False(duplicate.IsSuccess);
        Assert.Equal("QcSamplingPointGroup.DuplicateName", duplicate.Error.Code);
    }

    [Fact]
    public async Task A_group_can_be_renamed_without_colliding_with_itself()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = Guid.NewGuid();

        var created = await harness.SamplingPointGroups.CreateSamplingPointGroup(
            new CreateSamplingPointGroupRequest { Name = "General Rooms" }, userId);

        var renamed = await harness.SamplingPointGroups.UpdateSamplingPointGroup(
            created.Value.Id,
            new UpdateSamplingPointGroupRequest
            {
                Name = "General Rooms",
                Description = "Alert 80 / Action 100 CFU/4Hrs"
            },
            userId);

        Assert.True(renamed.IsSuccess);
        Assert.Equal("Alert 80 / Action 100 CFU/4Hrs", renamed.Value.Description);
    }

    [Fact]
    public async Task An_unused_group_is_deleted()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = Guid.NewGuid();

        var created = await harness.SamplingPointGroups.CreateSamplingPointGroup(
            new CreateSamplingPointGroupRequest { Name = "Obsolete Area" }, userId);

        var deleted = await harness.SamplingPointGroups.DeleteSamplingPointGroup(
            created.Value.Id, userId);

        Assert.True(deleted.IsSuccess);
        Assert.Empty((await harness.SamplingPointGroups.GetSamplingPointGroups(null)).Value);
    }

    /// <summary>
    /// A group still carrying a characteristic's Alert/Action tier cannot be deleted —
    /// deleting it would leave that characteristic's limits unattributable.
    /// </summary>
    [Fact]
    public async Task A_group_in_use_by_a_characteristic_cannot_be_deleted()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = Guid.NewGuid();

        var template = await harness.SeedEffectiveTemplate(
            "WS/EM", WorksheetCategory.Microbial, "airborne_viables");

        var group = await harness.SamplingPointGroups.CreateSamplingPointGroup(
            new CreateSamplingPointGroupRequest { Name = "General Rooms" }, userId);

        var specification = await harness.Specifications.CreateSpecification(
            new CreateSpecificationRequest
            {
                Code = "QCD/SPEC/EM/001",
                Name = "Environmental Monitoring",
                AppliesTo = SpecificationAppliesTo.RoutineEnvironmental,
                RetestPolicy = QcRetestPolicy.FreshResample,
                WorksheetLinks =
                [
                    new CreateSpecificationWorksheetLinkRequest
                    {
                        WorksheetTemplateId = template.Id,
                        AnalysisType = SpecificationAnalysisType.Microbial
                    }
                ],
                Characteristics =
                [
                    new CreateSpecificationCharacteristicRequest
                    {
                        TestName = "Airborne Viables",
                        AcceptanceCriteria = "Within limits",
                        ActionLimit = "NMT 100 CFU/4Hrs",
                        SamplingPointGroupId = group.Value.Id,
                        SourceWorksheetTemplateId = template.Id,
                        SourceFieldKey = "airborne_viables",
                        DisplayOrder = 1
                    }
                ]
            },
            userId);

        Assert.True(specification.IsSuccess);

        var deleted = await harness.SamplingPointGroups.DeleteSamplingPointGroup(
            group.Value.Id, userId);

        Assert.False(deleted.IsSuccess);
        Assert.Equal("QcSamplingPointGroup.InUse", deleted.Error.Code);

        // Still there, and still attached to the characteristic.
        Assert.True(await harness.Db.QcSamplingPointGroups.AnyAsync(row => row.Id == group.Value.Id));
    }

    /// <summary>
    /// The same table Milestone 6's MonitoringProgram will reference, so it is deliberately
    /// not scoped to Specification in any way.
    /// </summary>
    [Fact]
    public async Task Groups_are_standalone_master_data_with_no_specification_scope()
    {
        using var harness = new QcWorksheetTestContext();

        var created = await harness.SamplingPointGroups.CreateSamplingPointGroup(
            new CreateSamplingPointGroupRequest { Name = "Water Loop" }, Guid.NewGuid());

        Assert.True(created.IsSuccess);

        var properties = typeof(SamplingPointGroup).GetProperties().Select(p => p.Name).ToList();
        Assert.DoesNotContain("SpecificationId", properties);
        Assert.Contains("Name", properties);
        Assert.Contains("Description", properties);
    }
}
