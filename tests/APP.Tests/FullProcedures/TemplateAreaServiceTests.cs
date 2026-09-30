using System.Security.Cryptography;
using System.Text;
using APP.Services.FullProcedures;
using DOMAIN.Entities.FullProcedures;
using DOMAIN.Entities.Roles;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.FullProcedures;

file sealed class TemplateAreaCurrentUser(Guid actorId) : ICurrentUserService
{
    public Guid? UserId => actorId;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public sealed class TemplateAreaServiceTests
{
    [Fact]
    public async Task Create_persists_governed_configuration_and_hashed_audit()
    {
        var actor = Guid.NewGuid();
        await using var context = Context(actor);
        var owner = await AddRole(context, "Production");
        var viewer = await AddRole(context, "Quality assurance");
        var service = new TemplateAreaService(context);

        var result = await service.CreateAsync(Draft(owner.Id, viewer.Id), actor, [owner.Id]);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Version);
        Assert.Contains("quality-gate", result.Value.CapabilityIds);
        var audit = await context.Set<TemplateAreaAudit>().SingleAsync();
        var hash = Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(audit.SnapshotJson))).ToLowerInvariant();
        Assert.Equal(hash, audit.SnapshotHash);
        Assert.Equal("Created", audit.Action);
    }

    [Fact]
    public async Task Duplicate_name_is_rejected_case_insensitively()
    {
        var actor = Guid.NewGuid();
        await using var context = Context(actor);
        var owner = await AddRole(context, "Production");
        var service = new TemplateAreaService(context);
        Assert.True((await service.CreateAsync(Draft(owner.Id), actor, [owner.Id])).IsSuccess);
        var duplicate = Draft(owner.Id);
        duplicate.Name = "  PRODUCTION PROCEDURES  ";

        var result = await service.CreateAsync(duplicate, actor, [owner.Id]);

        Assert.True(result.IsFailure);
        Assert.Equal("TemplateArea.DuplicateName", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Area_assignment_scopes_list_and_detail_access()
    {
        var actor = Guid.NewGuid();
        await using var context = Context(actor);
        var owner = await AddRole(context, "Production");
        var outsider = await AddRole(context, "Human resources");
        var service = new TemplateAreaService(context);
        var created = await service.CreateAsync(Draft(owner.Id), actor, [owner.Id]);

        var list = await service.ListAsync([outsider.Id], false);
        var detail = await service.GetAsync(created.Value.Id, [outsider.Id]);

        Assert.Empty(list.Value);
        Assert.Equal("TemplateArea.AccessDenied", Assert.Single(detail.Errors).Code);
    }

    [Fact]
    public async Task Viewer_grant_cannot_administer_an_area()
    {
        var actor = Guid.NewGuid();
        await using var context = Context(actor);
        var owner = await AddRole(context, "Production");
        var viewer = await AddRole(context, "Viewer");
        var service = new TemplateAreaService(context);
        var created = await service.CreateAsync(Draft(owner.Id, viewer.Id), actor, [owner.Id]);
        var update = UpdateFrom(Draft(owner.Id, viewer.Id), created.Value.Version);

        var result = await service.UpdateAsync(created.Value.Id, update, actor, [viewer.Id]);

        Assert.Equal("TemplateArea.AdministratorRequired", Assert.Single(result.Errors).Code);
        Assert.Single(await context.Set<TemplateAreaAudit>().ToListAsync());
    }

    [Fact]
    public async Task Update_replaces_bindings_increments_version_and_rejects_stale_write()
    {
        var actor = Guid.NewGuid();
        await using var context = Context(actor);
        var owner = await AddRole(context, "Production");
        var service = new TemplateAreaService(context);
        var created = await service.CreateAsync(Draft(owner.Id), actor, [owner.Id]);
        context.ChangeTracker.Clear();
        var update = UpdateFrom(Draft(owner.Id), created.Value.Version);
        update.Name = "Production batch procedures";
        update.CapabilityIds = ["capture-response", "approval"];
        update.Reason = "Refine the area after governance review.";

        var updated = await service.UpdateAsync(created.Value.Id, update, actor, [owner.Id]);
        var stale = await service.UpdateAsync(created.Value.Id, update, actor, [owner.Id]);

        Assert.True(updated.IsSuccess, string.Join(", ", updated.Errors.Select(item => item.Code)));
        Assert.Equal(2, updated.Value.Version);
        Assert.Equal(["approval", "capture-response"], updated.Value.CapabilityIds);
        Assert.Equal("TemplateArea.VersionConflict", Assert.Single(stale.Errors).Code);
        Assert.Equal(2, await context.Set<TemplateAreaAudit>().CountAsync());
    }

    [Fact]
    public async Task Deactivation_is_versioned_and_audited()
    {
        var actor = Guid.NewGuid();
        await using var context = Context(actor);
        var owner = await AddRole(context, "Production");
        var service = new TemplateAreaService(context);
        var created = await service.CreateAsync(Draft(owner.Id), actor, [owner.Id]);
        context.ChangeTracker.Clear();

        var result = await service.SetActiveAsync(created.Value.Id,
            new ChangeTemplateAreaActiveRequest
            {
                IsActive = false, ExpectedVersion = 1,
                Reason = "Temporarily retire this configuration area."
            }, actor, [owner.Id]);

        Assert.True(result.IsSuccess, string.Join(", ", result.Errors.Select(item => item.Code)));
        Assert.False(result.Value.IsActive);
        Assert.Equal(2, result.Value.Version);
        Assert.Contains(await context.Set<TemplateAreaAudit>().ToListAsync(),
            item => item.Action == "Deactivated" && item.Version == 2);
    }

    [Fact]
    public async Task Catalog_rejects_unreviewed_capability_and_unknown_grant_role()
    {
        var actor = Guid.NewGuid();
        await using var context = Context(actor);
        var owner = await AddRole(context, "Production");
        var service = new TemplateAreaService(context);
        var request = Draft(owner.Id, Guid.NewGuid());
        request.CapabilityIds.Add("run-arbitrary-code");

        var result = await service.CreateAsync(request, actor, [owner.Id]);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, item => item.Code == "TemplateArea.UnknownCapability");
        Assert.Contains(result.Errors, item => item.Code == "TemplateArea.UnknownOwnerGroup");
        Assert.Empty(await context.Set<TemplateArea>().ToListAsync());
    }

    [Fact]
    public async Task Area_cannot_remove_a_purpose_or_subject_used_by_a_question()
    {
        var actor = Guid.NewGuid();
        await using var context = Context(actor);
        var owner = await AddRole(context, "Production");
        var service = new TemplateAreaService(context);
        var created = await service.CreateAsync(Draft(owner.Id), actor, [owner.Id]);
        context.Add(new TemplateQuestion
        {
            Id = Guid.NewGuid(), TemplateAreaId = created.Value.Id,
            PurposeId = "production-procedure", SubjectTypeId = "product",
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var update = UpdateFrom(Draft(owner.Id), created.Value.Version);
        update.SubjectTypeIds = ["batch"];
        update.Reason = "Attempt to remove a referenced subject binding.";

        var result = await service.UpdateAsync(
            created.Value.Id, update, actor, [owner.Id]);

        Assert.Equal("TemplateArea.BindingInUse", Assert.Single(result.Errors).Code);
    }

    private static TemplateAreaDraftRequest Draft(Guid ownerRoleId, Guid? viewerRoleId = null) => new()
    {
        Name = "Production procedures", OwnerRoleId = ownerRoleId,
        PurposeIds = ["production-procedure"], SubjectTypeIds = ["product", "batch"],
        CapabilityIds = ["capture-response", "approval", "quality-gate"],
        ReviewPolicyId = "regulated-three-person",
        RoleGrants = viewerRoleId.HasValue
            ? [new TemplateAreaRoleGrantRequest
                { RoleId = viewerRoleId.Value, AccessLevel = TemplateAreaAccessLevel.Viewer }]
            : [],
        Reason = "Create a controlled template configuration area.",
    };

    private static UpdateTemplateAreaRequest UpdateFrom(TemplateAreaDraftRequest draft, int version) => new()
    {
        Name = draft.Name, OwnerRoleId = draft.OwnerRoleId,
        PurposeIds = [.. draft.PurposeIds], SubjectTypeIds = [.. draft.SubjectTypeIds],
        CapabilityIds = [.. draft.CapabilityIds], ReviewPolicyId = draft.ReviewPolicyId,
        RoleGrants = [.. draft.RoleGrants], Reason = draft.Reason, ExpectedVersion = version,
    };

    private static async Task<Role> AddRole(ApplicationDbContext context, string name)
    {
        var role = new Role
        {
            Id = Guid.NewGuid(), Name = name.Replace(" ", string.Empty),
            NormalizedName = name.Replace(" ", string.Empty).ToUpperInvariant(), DisplayName = name,
        };
        context.Add(role);
        await context.SaveChangesAsync();
        return role;
    }

    private static ApplicationDbContext Context(Guid actorId) => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new TemplateAreaCurrentUser(actorId));
}
