using API.Controllers;
using APP.Repository;
using APP.Services.Forms;
using APP.Services.NotificationService;
using APP.Utils;
using DOMAIN.Entities.Alerts;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.PendingCertificates;

public sealed class PendingCertificateTests
{
    [Theory]
    [InlineData(TemplateRepairTestKind.Product)]
    [InlineData(TemplateRepairTestKind.RawMaterial)]
    [InlineData(TemplateRepairTestKind.PackagingMaterial)]
    public void Each_endpoint_checks_its_own_key(TemplateRepairTestKind kind)
    {
        var controller = typeof(PendingCertificateController);
        var actions = new[]
        {
            ($"Save{kind}", PendingCertificateService.EditKey(kind)),
            ($"Request{kind}Review", PendingCertificateService.ReturnKey(kind, CertificateReturnKind.Review)),
            ($"Request{kind}Correction", PendingCertificateService.ReturnKey(kind, CertificateReturnKind.Correction))
        };
        foreach (var (name, key) in actions)
        {
            var method = controller.GetMethod(name)!;
            Assert.Equal(key, Assert.Single(method.GetCustomAttributes(typeof(AuthorizeAttribute), false).Cast<AuthorizeAttribute>()).Policy);
            Assert.Empty(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), false));
        }
    }

    [Theory]
    [InlineData(CertificateReturnKind.Review)]
    [InlineData(CertificateReturnKind.Correction)]
    public async Task Assigned_approver_can_pause_and_only_the_preparer_can_resubmit(CertificateReturnKind requestKind)
    {
        await using var db = CreateContext();
        var notices = new Notices();
        var service = new PendingCertificateService(db, notices);
        var key = PendingCertificateService.ReturnKey(TemplateRepairTestKind.Product, requestKind);
        var edit = PermissionKeys.CanEditProductCertificateOfAnalysis;
        var preparer = await User(db, edit);
        var approver = await User(db, key);
        var outsider = await User(db, key, edit);
        var response = await Certificate(db, preparer, approver);
        var request = new RequestCertificateChange(0, "Verify the compliance and notes.");

        Assert.Equal(ErrorType.Forbidden, (await service.Request(response.Id, preparer, TemplateRepairTestKind.Product, requestKind, request)).Errors[0].Type);
        Assert.Equal(ErrorType.Forbidden, (await service.Request(response.Id, outsider, TemplateRepairTestKind.Product, requestKind, request)).Errors[0].Type);
        Assert.True((await service.Request(response.Id, approver, TemplateRepairTestKind.Product, requestKind, request)).IsSuccess);
        Assert.Equal(requestKind, response.CoaReturnKind);
        Assert.Equal("COA.AwaitingResubmission", PendingCertificateService.DecisionError(response, 1)?.Code);
        Assert.Empty(await new PendingApprovalQueries(db, approver, []).Responses.ToListAsync());
        Assert.Equal(preparer, Assert.Single(Assert.Single(notices.Sent).Recipients).Id);

        Assert.Equal("COA.Changed", (await service.Save(response.Id, preparer, TemplateRepairTestKind.Product, new(0, "Verified", "", []))).Errors[0].Code);
        Assert.Equal(ErrorType.Forbidden, (await service.Save(response.Id, outsider, TemplateRepairTestKind.Product, new(1, "Verified", "", []))).Errors[0].Type);
        Assert.Equal(ErrorType.Forbidden, (await service.Save(response.Id, approver, TemplateRepairTestKind.Product, new(1, "Verified", "", []))).Errors[0].Type);
        var answer = Assert.Single(response.FormResponses);
        var section = answer.FormField.FormSectionId;
        var saved = await service.Save(response.Id, preparer, TemplateRepairTestKind.Product,
            new(1, "Verified", "Confirmed from the signed worksheet.", [new() { FormSectionId = section, Complies = true }]));
        Assert.True(saved.IsSuccess, saved.IsFailure ? saved.Errors[0].Description : null);
        Assert.Equal("Confirmed from the signed worksheet.", response.CertificateNotes);
        Assert.True(answer.Complies);
        Assert.Equal("5.8", answer.Value);
        Assert.Null(response.CoaReturnKind);
        Assert.Equal("COA.Changed", PendingCertificateService.DecisionError(response, 1)?.Code);
        Assert.Null(PendingCertificateService.DecisionError(response, 2));
        Assert.Equal(2, await db.Set<PendingCertificateChange>().CountAsync());
        Assert.Equal(2, Assert.Single(ResponseApprovalRoundManager.Current(response.Approvals)).ApprovalRound);
        Assert.Single(await new PendingApprovalQueries(db, approver, []).Responses.ToListAsync());
    }

    [Fact]
    public async Task Approved_certificate_wrong_kind_and_foreign_section_are_refused()
    {
        await using var db = CreateContext();
        var preparer = await User(db, PermissionKeys.CanEditProductCertificateOfAnalysis, PermissionKeys.CanEditRawMaterialCertificateOfAnalysis);
        var approver = await User(db, PermissionKeys.CanRequestProductCertificateReview);
        var response = await Certificate(db, preparer, approver);
        var service = new PendingCertificateService(db, new Notices());
        Assert.Equal(ErrorType.Forbidden, (await service.Save(response.Id, preparer, TemplateRepairTestKind.RawMaterial, new(0, "Verified", "", []))).Errors[0].Type);
        Assert.Equal("COA.Test", (await service.Save(response.Id, preparer, TemplateRepairTestKind.Product,
            new(0, "Verified", "", [new() { FormSectionId = Guid.NewGuid(), Complies = true }]))).Errors[0].Code);
        response.Approved = true;
        await db.SaveChangesAsync();
        Assert.Equal("COA.Closed", (await service.Save(response.Id, preparer, TemplateRepairTestKind.Product, new(0, "Verified", "", []))).Errors[0].Code);
        Assert.Equal("COA.Closed", (await service.Request(response.Id, approver, TemplateRepairTestKind.Product, CertificateReturnKind.Review, new(0, "Review"))).Errors[0].Code);
        Assert.Empty(await db.Set<PendingCertificateChange>().ToListAsync());
    }

    private static ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor());

    private static async Task<Guid> User(ApplicationDbContext db, params string[] keys)
    {
        var id = Guid.NewGuid();
        var role = Guid.NewGuid();
        db.Users.Add(new User { Id = id, UserName = id.ToString(), FirstName = "Test", LastName = "User" });
        db.Roles.Add(new Role { Id = role, Name = role.ToString() });
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = id, RoleId = role });
        foreach (var key in keys)
        {
            var claimId = db.RoleClaims.Local.Count + 1;
            db.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = claimId, RoleId = role, ClaimType = AppConstants.Permission, ClaimValue = key });
            db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), Key = key, RoleClaimId = claimId, Type = "View" });
        }
        await db.SaveChangesAsync();
        return id;
    }

    private static async Task<Response> Certificate(ApplicationDbContext db, Guid preparer, Guid approver)
    {
        var form = new Form { Id = Guid.NewGuid(), Name = "Assay worksheet" };
        var section = new FormSection { Id = Guid.NewGuid(), Form = form, Name = "pH" };
        var field = new FormField { Id = Guid.NewGuid(), FormSection = section };
        var response = new Response
        {
            Id = Guid.NewGuid(), Form = form, BatchManufacturingRecordId = Guid.NewGuid(), ProductionActivityStepId = Guid.NewGuid(),
            CheckedById = preparer, CheckedAt = DateTime.UtcNow, CreatedById = preparer,
            FormResponses = [new() { Id = Guid.NewGuid(), FormField = field, Value = "5.8", Complies = false }],
            Approvals = [new() { Id = Guid.NewGuid(), ApprovalId = Guid.NewGuid(), ApprovalRound = 1, UserId = approver, Required = true,
                ActivatedAt = DateTime.UtcNow, Status = ApprovalStatus.Pending }]
        };
        db.Responses.Add(response);
        await db.SaveChangesAsync();
        return response;
    }

    private sealed class Notices : INotificationService
    {
        public List<NotificationDto> Sent { get; } = [];
        public Task SendNotification(NotificationDto notification, List<AlertType> alertTypes)
        {
            Sent.Add(notification);
            return Task.CompletedTask;
        }
    }

    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string UserName => "";
        public bool IsAuthenticated => false;
        public string DepartmentType => "";
    }
}
