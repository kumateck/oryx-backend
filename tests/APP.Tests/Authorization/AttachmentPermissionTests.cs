using System.Reflection;
using API.Authorization;
using API.Controllers;
using APP.Utils;
using DOMAIN.Entities.Attachments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Authorization;

/// <summary>Adding or removing a record's files follows the page that works on that record (audit phase 4).</summary>
public sealed class AttachmentPermissionTests
{
    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => Guid.Empty;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }

    [Fact]
    public void Every_upload_and_removal_is_checked_and_every_key_exists()
    {
        foreach (var action in new[] { "UploadFile", "DeleteAttachments", "DeleteSpecificAttachment" })
            Assert.All(typeof(FileController).GetMethods().Where(method => method.Name == action),
                method => Assert.NotNull(method.GetCustomAttribute<AttachmentPermissionAttribute>()));
        var registered = PermissionUtils.GeneratePermissions().Select(permission => permission.Key).ToHashSet();
        Assert.All(AttachmentPermissionAttribute.AllKeys, key => Assert.Contains(key, registered));
        // Record types are matched whatever their case, as the desktop sends some in lower case.
        Assert.True(AttachmentPermissionAttribute.Keys.ContainsKey("signature"));
    }

    [Fact]
    public async Task A_removal_uses_the_type_of_the_stored_files()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor());
        var order = Guid.NewGuid();
        db.Attachments.AddRange(
            new Attachment { Id = Guid.NewGuid(), ModelId = order, ModelType = "PurchaseOrder", Reference = "invoice", Name = "a.pdf" },
            new Attachment { Id = Guid.NewGuid(), ModelId = order, ModelType = "PurchaseOrder", Reference = "packing", Name = "b.pdf" });
        await db.SaveChangesAsync();
        Assert.Equal(["PurchaseOrder"], await AttachmentPermissionAttribute.Types(db, new Dictionary<string, object?> { ["modelId"] = order, ["reference"] = "invoice" }));
        Assert.Equal(["Employee"], await AttachmentPermissionAttribute.Types(db, new Dictionary<string, object?> { ["modelType"] = "Employee", ["modelId"] = order }));
        Assert.Empty(await AttachmentPermissionAttribute.Types(db, new Dictionary<string, object?> { ["modelId"] = Guid.NewGuid() }));
    }
}
