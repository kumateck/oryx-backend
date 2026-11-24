using System.Reflection;
using System.Data;
using System.Text.Json;
using API.Controllers;
using APP.Services.Forms;
using APP.Services.Formulas;
using APP.Utils;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.ProductSpecifications;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using INFRASTRUCTURE.Migrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public sealed class ProductAssayReportingTests
{
    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? DepartmentId { get; } = Guid.NewGuid();
        public string DepartmentType => "Production";
    }
    private sealed class Client : IFormulaCalculationClient
    {
        public bool Available = true;
        public Task<Result<bool>> IsReadyAsync(CancellationToken token = default) => Task.FromResult(Result.Success(Available));
        public Task<Result<FormulaServiceResponse>> ValidateAsync(JsonElement request, CancellationToken token = default) => throw new NotSupportedException();
        public Task<Result<FormulaServiceResponse>> EvaluateAsync(JsonElement request, CancellationToken token = default)
        {
            if (!Available) return Task.FromResult(Result.Failure<FormulaServiceResponse>(FormulaCalculationErrors.Unavailable));
            var inputs = JsonSerializer.Deserialize<Dictionary<string, string>>(request.GetProperty("resolvedInputs").GetRawText())!;
            var result = request.GetProperty("definition").GetProperty("results").EnumerateArray().Select(output => new {
                key = output.GetProperty("key").GetString(), exactResult = inputs["assay"], roundedResult = "101.39", displayedResult = "101.39", unit = (string?)null }).ToArray();
            return Task.FromResult(Result.Success(new FormulaServiceResponse("oryx-formula-service-response-v1", 1, "test", 5, "COMPLETED", "test", new string('a', 64),
                request.GetProperty("definitionHash").GetString(), request.GetProperty("definitionHash").GetString(),
                request.GetProperty("configurationHash").GetString(), request.GetProperty("configurationHash").GetString(),
                FormulaCanonicalJson.HashJson("oryx:formula-resolved-inputs:v1", JsonSerializer.Serialize(inputs), 262144),
                request.GetProperty("configuration").GetProperty("placementKey").GetString(), null, [], [], [], [],
                JsonSerializer.SerializeToElement(new { status = 0, results = result }), [])));
        }
    }
    private sealed class Fixture : IAsyncDisposable
    {
        public Actor Actor = new();
        public ApplicationDbContext Db;
        public ProductAssayReportingService Service;
        public Client Calculation = new();
        public Guid Form = Guid.NewGuid(), FirstField = Guid.NewGuid(), SecondField = Guid.NewGuid(), OtherField = Guid.NewGuid();
        public ProductSpecification Spec = null!, OtherSpec = null!;
        public Response Response = null!;
        public const string Definition = "{\"type\":0,\"expression\":\":assay\",\"decimalPlaces\":2}";
        public Fixture(string? connection = null)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>();
            if (connection is null) options.UseInMemoryDatabase(Guid.NewGuid().ToString()); else options.UseNpgsql(connection);
            Db = new(options.Options, Actor);
            Service = new(Db);
        }
        public async Task Seed(params string[] grants)
        {
            var role = Guid.NewGuid(); var product = Guid.NewGuid(); var other = Guid.NewGuid(); var batch = Guid.NewGuid(); var bmr = Guid.NewGuid(); var step = Guid.NewGuid();
            Db.Departments.Add(new Department { Id = Actor.DepartmentId!.Value, Name = "Liquids", Type = DepartmentType.Production });
            Db.Users.Add(new User { Id = Actor.UserId!.Value, DepartmentId = Actor.DepartmentId, UserName = "qc" });
            Db.Roles.Add(new Role { Id = role, Name = "QC" });
            Db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = Actor.UserId.Value, RoleId = role });
            for (var index = 0; index < grants.Length; index++)
            {
                Db.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = index + 1, RoleId = role, ClaimType = AppConstants.Permission, ClaimValue = grants[index] });
                Db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = index + 1, Key = grants[index], Type = "Access" });
            }
            Db.Products.AddRange(new Product { Id = product, Name = "Product A", DepartmentId = Actor.DepartmentId }, new Product { Id = other, Name = "Product B", DepartmentId = Actor.DepartmentId });
            Db.Forms.Add(new Form { Id = Form, Name = "Reusable assay" });
            foreach (var id in new[] { FirstField, SecondField, OtherField })
            {
                var section = Guid.NewGuid(); var question = Guid.NewGuid();
                Db.FormSections.Add(new FormSection { Id = section, FormId = Form, Name = "Assay", Description = "2.32–2.52% w/v" });
                Db.Questions.Add(new Question { Id = question, Type = QuestionType.Formula, Label = "Assay" });
                Db.QuestionOptions.Add(new QuestionOption { Id = Guid.NewGuid(), QuestionId = question, Name = Definition });
                Db.FormFields.Add(new FormField { Id = id, FormSectionId = section, QuestionId = question });
            }
            Spec = new() { Id = Guid.NewGuid(), FormId = Form, ProductId = product, SpecificationNumber = "SHARED", RevisionNumber = "01", IsVerified = true, TestStage = TestStage.Finished };
            OtherSpec = new() { Id = Guid.NewGuid(), FormId = Form, ProductId = other, SpecificationNumber = "SHARED", RevisionNumber = "01", IsVerified = true, TestStage = TestStage.Finished };
            Db.ProductSpecifications.AddRange(Spec, OtherSpec);
            Db.ProductionScheduleProducts.Add(new ProductionScheduleProduct { Id = batch, ProductId = product });
            Db.ProductionActivitySteps.Add(new ProductionActivityStep { Id = step });
            Db.BatchManufacturingRecords.Add(new BatchManufacturingRecord { Id = bmr, ProductionScheduleProductId = batch, ProductionActivityStepId = step, BatchNumber = "B1" });
            Db.AnalyticalTestRequests.Add(new AnalyticalTestRequest { Id = Guid.NewGuid(), ProductionScheduleProductId = batch,
                BatchManufacturingRecordId = bmr, ProductionActivityStepId = step, Stage = TestStage.Finished });
            Response = new() { Id = Guid.NewGuid(), FormId = Form, BatchManufacturingRecordId = bmr, ProductionActivityStepId = step };
            Db.Responses.Add(Response);
            foreach (var field in new[] { FirstField, SecondField, OtherField }) Db.FormResponses.Add(new FormResponse { Id = Guid.NewGuid(), ResponseId = Response.Id, FormFieldId = field, Value = "{\"assay\":101.39}" });
            await Db.SaveChangesAsync();
        }
        public AssayReportingRule Rule(Guid field, decimal nominal = 2.4m) => new(field, "sample1", LegacyFormulaRepairDefinition.Hash(Definition), nominal, 2.32m, 2.52m, 4);
        public async Task<SHARED.Result<AssayReportingConfigurationDto>> Save(params AssayReportingRule[] rules)
        {
            var configuration = await Service.Get(Spec.Id, Actor.UserId!.Value);
            return await Service.Save(Spec.Id, new(configuration.Value.Token, "Use the approved concentration basis.", rules.ToList()), Actor.UserId.Value);
        }
        public async ValueTask DisposeAsync() => await Db.DisposeAsync();
    }

    [Theory]
    [InlineData(nameof(ProductAssayReportingController.Get), PermissionKeys.CanViewProductAssayReporting)]
    [InlineData(nameof(ProductAssayReportingController.Save), PermissionKeys.CanEditProductAssayReporting)]
    public void Each_endpoint_has_its_own_registered_permission(string action, string key)
    {
        var method = typeof(ProductAssayReportingController).GetMethod(action)!;
        Assert.Equal(key, Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        Assert.Empty(method.GetCustomAttributes<AllowAnonymousAttribute>());
        Assert.Single(PermissionUtils.GeneratePermissions(), permission => permission.Key == key);
    }

    [Fact]
    public async Task Ordinary_specification_editing_does_not_grant_reporting_access()
    {
        await using var f = new Fixture(); await f.Seed(PermissionKeys.CanEditProductSpecification);
        Assert.Equal(ErrorType.Forbidden, (await f.Service.Get(f.Spec.Id, f.Actor.UserId!.Value)).Error.Type);
        Assert.Equal(ErrorType.Forbidden, (await f.Service.Save(f.Spec.Id, new("", "Reason", []), f.Actor.UserId.Value)).Error.Type);
        Assert.Null(f.Spec.AssayReportingRulesJson);
    }

    [Fact]
    public async Task Shared_templates_and_numbers_keep_each_product_and_each_same_named_assay_independent()
    {
        await using var f = new Fixture(); await f.Seed(PermissionKeys.CanViewProductAssayReporting, PermissionKeys.CanEditProductAssayReporting);
        var saved = await f.Save(f.Rule(f.FirstField), f.Rule(f.SecondField, 2.5m));
        Assert.True(saved.IsSuccess, saved.Error.Description);
        Assert.Equal(2, saved.Value.Rules.Count);
        Assert.Null(f.OtherSpec.AssayReportingRulesJson);
        Assert.False(f.Spec.IsVerified);
        Assert.All(await f.Db.QuestionOptions.ToListAsync(), option => Assert.Equal(Fixture.Definition, option.Name));
        f.Spec.IsVerified = true; await f.Db.SaveChangesAsync();
        var fields = await ProductAssayReportingRuntime.Build(f.Db, f.Response, f.Calculation);
        Assert.Equal(2, fields.Count);
        Assert.DoesNotContain(fields, field => field.FormFieldId == f.OtherField);
        Assert.All(fields, field => Assert.Null(field.Error));
        var first = Assert.Single(fields.Single(field => field.FormFieldId == f.FirstField).Results);
        Assert.Equal(2.43336m, first.Value); Assert.Equal("2.4334", first.DisplayValue); Assert.True(first.Complies);
        var second = Assert.Single(fields.Single(field => field.FormFieldId == f.SecondField).Results);
        Assert.Equal(2.53475m, second.Value); Assert.False(second.Complies);
    }

    [Fact]
    public async Task A_stale_token_wrong_output_or_removed_permission_refuses_the_write()
    {
        await using var f = new Fixture(); await f.Seed(PermissionKeys.CanViewProductAssayReporting, PermissionKeys.CanEditProductAssayReporting);
        var before = (await f.Service.Get(f.Spec.Id, f.Actor.UserId!.Value)).Value;
        Assert.True((await f.Save(f.Rule(f.FirstField))).IsSuccess);
        Assert.True((await f.Service.Save(f.Spec.Id, new(before.Token, "Concurrent update", []), f.Actor.UserId.Value)).IsFailure);
        Assert.True((await f.Save(f.Rule(Guid.NewGuid()))).IsFailure);
        Assert.True((await f.Save(f.Rule(f.FirstField) with { ResultKey = "another-assay" })).IsFailure);
        f.Db.RoleClaims.RemoveRange(await f.Db.RoleClaims.Where(claim => claim.ClaimValue == PermissionKeys.CanEditProductAssayReporting).ToListAsync());
        await f.Db.SaveChangesAsync();
        var current = (await f.Service.Get(f.Spec.Id, f.Actor.UserId.Value)).Value;
        Assert.Equal(ErrorType.Forbidden, (await f.Service.Save(f.Spec.Id, new(current.Token, "Disable", []), f.Actor.UserId.Value)).Error.Type);
    }

    [Fact]
    public async Task Captured_results_survive_later_settings_and_source_changes_and_historical_tests_are_unchanged()
    {
        await using var f = new Fixture(); await f.Seed(PermissionKeys.CanViewProductAssayReporting, PermissionKeys.CanEditProductAssayReporting);
        Assert.True((await f.Save(f.Rule(f.FirstField))).IsSuccess);
        f.Spec.IsVerified = true; await f.Db.SaveChangesAsync();
        Assert.True((await ProductAssayReportingRuntime.Capture(f.Db, f.Response, f.Actor.UserId!.Value, f.Calculation)).IsSuccess);
        f.Response.CheckedAt = DateTime.UtcNow; f.Response.Approved = true; await f.Db.SaveChangesAsync();
        var original = f.Response.AssayReportingSnapshotJson;
        Assert.True((await f.Save(f.Rule(f.FirstField, 2.5m))).IsSuccess);
        (await f.Db.FormResponses.FirstAsync()).Value = "{\"assay\":50}"; await f.Db.SaveChangesAsync();
        f.Calculation.Available = false;
        var frozen = Assert.Single(await ProductAssayReportingRuntime.Build(f.Db, f.Response, f.Calculation));
        Assert.Equal(2.43336m, Assert.Single(frozen.Results).Value);
        Assert.Equal(original, f.Response.AssayReportingSnapshotJson);
        f.Response.AssayReportingSnapshotJson = null;
        Assert.Empty(await ProductAssayReportingRuntime.Build(f.Db, f.Response, f.Calculation));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Pending_certificates_use_latest_reporting_including_previously_captured_empty_settings(bool captured)
    {
        await using var f = new Fixture(); await f.Seed(PermissionKeys.CanViewProductAssayReporting, PermissionKeys.CanEditProductAssayReporting);
        if (captured) Assert.True((await ProductAssayReportingRuntime.Capture(f.Db, f.Response, f.Actor.UserId!.Value, f.Calculation)).IsSuccess);
        f.Response.CheckedAt = DateTime.UtcNow;
        (await f.Db.FormResponses.SingleAsync(answer => answer.FormFieldId == f.FirstField)).Value = "{\"assay\":101.21}";
        await f.Db.SaveChangesAsync();
        var oldSnapshot = f.Response.AssayReportingSnapshotJson;
        Assert.True((await f.Save(f.Rule(f.FirstField))).IsSuccess);
        Assert.Contains("Approve", Assert.Single(await ProductAssayReportingRuntime.Build(f.Db, f.Response, f.Calculation)).Error);
        f.Spec.IsVerified = true; await f.Db.SaveChangesAsync();
        var result = Assert.Single(Assert.Single(await ProductAssayReportingRuntime.Build(f.Db, f.Response, f.Calculation)).Results);
        Assert.Equal(2.42904m, result.Value); Assert.Equal("2.4290", result.DisplayValue); Assert.True(result.Complies);
        Assert.Equal(oldSnapshot, f.Response.AssayReportingSnapshotJson); // Reading a pending COA never writes.
        Assert.True((await f.Save(f.Rule(f.FirstField, 2.5m))).IsSuccess);
        f.Spec.IsVerified = true; await f.Db.SaveChangesAsync();
        Assert.False(Assert.Single(Assert.Single(await ProductAssayReportingRuntime.Build(f.Db, f.Response, f.Calculation)).Results).Complies);
    }

    [Fact]
    public async Task Approval_requires_the_reviewed_reporting_token_and_freezes_the_latest_result_only_after_approval()
    {
        await using var f = new Fixture(); await f.Seed(PermissionKeys.CanViewProductAssayReporting, PermissionKeys.CanEditProductAssayReporting, PermissionKeys.CanApproveResponse);
        Assert.True((await f.Save(f.Rule(f.FirstField))).IsSuccess);
        f.Spec.IsVerified = true; f.Response.CheckedAt = DateTime.UtcNow; await f.Db.SaveChangesAsync();
        var fields = await ProductAssayReportingRuntime.Build(f.Db, f.Response, f.Calculation);
        var token = await ProductAssayReportingRuntime.ReviewToken(f.Db, f.Response, fields);
        Assert.True((await ProductAssayReportingRuntime.PrepareApproval(f.Db, f.Response, f.Actor.UserId!.Value, f.Calculation, null)).IsFailure);
        Assert.True((await ProductAssayReportingRuntime.PrepareApproval(f.Db, f.Response, f.Actor.UserId.Value, f.Calculation, "stale")).IsFailure);
        Assert.Null(f.Response.AssayReportingSnapshotJson);
        Assert.True((await ProductAssayReportingRuntime.PrepareApproval(f.Db, f.Response, f.Actor.UserId.Value, f.Calculation, token)).IsSuccess);
        Assert.True((await f.Db.FormResponses.SingleAsync(answer => answer.FormFieldId == f.FirstField)).Complies);
        f.Response.Approved = true; await f.Db.SaveChangesAsync();
        var frozen = f.Response.AssayReportingSnapshotJson;
        Assert.True((await f.Save(f.Rule(f.FirstField, 2.5m))).IsSuccess);
        f.Calculation.Available = false;
        Assert.Equal(2.43336m, Assert.Single(Assert.Single(await ProductAssayReportingRuntime.Build(f.Db, f.Response, f.Calculation)).Results).Value);
        Assert.True((await ProductAssayReportingRuntime.PrepareApproval(f.Db, f.Response, f.Actor.UserId.Value, f.Calculation, token)).IsFailure);
        Assert.Equal(frozen, f.Response.AssayReportingSnapshotJson);
    }

    [Fact]
    public async Task Reporting_changes_restart_only_unapproved_reviews_and_preserve_prior_decisions()
    {
        await using var f = new Fixture(); await f.Seed(PermissionKeys.CanViewProductAssayReporting, PermissionKeys.CanEditProductAssayReporting);
        f.Response.CheckedAt = DateTime.UtcNow;
        var approvalId = Guid.NewGuid();
        var first = new ResponseApproval { Id = Guid.NewGuid(), ResponseId = f.Response.Id, ApprovalId = approvalId, ApprovalRound = 1,
            Required = true, Order = 1, UserId = f.Actor.UserId, Status = DOMAIN.Entities.Approvals.ApprovalStatus.Approved, ActivatedAt = DateTime.UtcNow, ApprovedById = f.Actor.UserId };
        f.Db.ResponseApprovals.Add(first);
        f.Db.ResponseApprovals.Add(new() { Id = Guid.NewGuid(), ResponseId = f.Response.Id, ApprovalId = approvalId, ApprovalRound = 1,
            Required = true, Order = 2, UserId = Guid.NewGuid() });
        await f.Db.SaveChangesAsync();
        var before = await ProductAssayReportingRuntime.ReviewToken(f.Db, f.Response, []);
        var configuration = (await f.Service.Get(f.Spec.Id, f.Actor.UserId!.Value)).Value;
        Assert.True((await f.Service.Save(f.Spec.Id, new(configuration.Token, new string('R', 2000), [f.Rule(f.FirstField)]), f.Actor.UserId.Value)).IsSuccess);
        Assert.Equal(2000, f.Spec.RevisionReason.Length);
        var all = await f.Db.ResponseApprovals.Where(stage => stage.ResponseId == f.Response.Id).ToListAsync();
        Assert.Equal(4, all.Count);
        Assert.Equal(DOMAIN.Entities.Approvals.ApprovalStatus.Approved, first.Status);
        var current = APP.Repository.ResponseApprovalRoundManager.Current(all);
        Assert.All(current, stage => Assert.InRange(stage.Comments.Length, 1, 1000));
        Assert.All(current, stage => { Assert.Equal(2, stage.ApprovalRound); Assert.Equal(DOMAIN.Entities.Approvals.ApprovalStatus.Pending, stage.Status); });
        Assert.NotNull(current.Single(stage => stage.Order == 1).ActivatedAt);
        Assert.Null(current.Single(stage => stage.Order == 2).ActivatedAt);
        Assert.NotEqual(before, await ProductAssayReportingRuntime.ReviewToken(f.Db, f.Response, []));
        f.Response.Approved = true; await f.Db.SaveChangesAsync();
        Assert.True((await f.Save(f.Rule(f.FirstField, 2.5m))).IsSuccess);
        Assert.Equal(4, await f.Db.ResponseApprovals.CountAsync());
    }

    [Fact]
    public async Task Approval_repository_captures_current_reporting_with_the_existing_approval_permission()
    {
        await using var f = new Fixture(); await f.Seed(PermissionKeys.CanViewProductAssayReporting, PermissionKeys.CanEditProductAssayReporting, PermissionKeys.CanApproveResponse);
        Assert.True((await f.Save(f.Rule(f.FirstField))).IsSuccess);
        f.Spec.IsVerified = true; f.Response.CheckedAt = DateTime.UtcNow;
        foreach (var order in new[] { 1, 2 }) f.Db.ResponseApprovals.Add(new() { Id = Guid.NewGuid(), ResponseId = f.Response.Id,
            ApprovalId = Guid.NewGuid(), ApprovalRound = 1, Required = true, Order = order, UserId = order == 1 ? f.Actor.UserId : Guid.NewGuid(),
            ActivatedAt = order == 1 ? DateTime.UtcNow : null });
        await f.Db.SaveChangesAsync();
        var fields = await ProductAssayReportingRuntime.Build(f.Db, f.Response, f.Calculation);
        var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        http.HttpContext.Request.Headers.IfMatch = "\"" + await ProductAssayReportingRuntime.ReviewToken(f.Db, f.Response, fields) + "\"";
        var repo = new APP.Repository.ApprovalRepository(f.Db, null!, null!, null!, null!, null!, null!, f.Calculation, http);
        Assert.True((await repo.ApproveItem(nameof(Response), f.Response.Id, f.Actor.UserId!.Value, [])).IsSuccess);
        Assert.False(f.Response.Approved); // The next required approver still decides.
        Assert.NotNull(f.Response.AssayReportingSnapshotJson);
        Assert.True((await f.Db.FormResponses.SingleAsync(answer => answer.FormFieldId == f.FirstField)).Complies);
        f.Db.RoleClaims.RemoveRange(await f.Db.RoleClaims.Where(claim => claim.ClaimValue == PermissionKeys.CanApproveResponse).ToListAsync());
        await f.Db.SaveChangesAsync();
        Assert.Equal(ErrorType.Forbidden, (await ProductAssayReportingRuntime.PrepareApproval(f.Db, f.Response, f.Actor.UserId.Value, f.Calculation,
            await ProductAssayReportingRuntime.ReviewToken(f.Db, f.Response, fields))).Error.Type);
    }

    private sealed class Publisher : APP.Services.ProductionActivityStepEventPublisher.IProductionActivityStepEventPublisher
    {
        public Task PublishStatusChanged(Guid stepId, DOMAIN.Entities.Base.ProductionStatus status, Guid? actorId) => Task.CompletedTask;
    }

    [Fact]
    public async Task Final_approval_captures_the_latest_conversion_and_later_reporting_edits_leave_it_unchanged()
    {
        await using var f = new Fixture(); await f.Seed(PermissionKeys.CanViewProductAssayReporting, PermissionKeys.CanEditProductAssayReporting, PermissionKeys.CanApproveResponse);
        Assert.True((await f.Save(f.Rule(f.FirstField))).IsSuccess);
        f.Spec.IsVerified = true; f.Response.CheckedAt = DateTime.UtcNow;
        var stp = new DOMAIN.Entities.ProductStandardTestProcedures.ProductStandardTestProcedure { Id = Guid.NewGuid(), ProductId = f.Spec.ProductId, StpNumber = "STP" };
        f.Db.ProductStandardTestProcedures.Add(stp);
        var ard = new DOMAIN.Entities.ProductAnalyticalRawData.ProductAnalyticalRawData { Id = Guid.NewGuid(), FormId = f.Form, StpId = stp.Id,
            Stage = TestStage.Finished, AnalysisType = DOMAIN.Entities.QualityRoutines.AnalysisType.Chemical };
        f.Db.ProductAnalyticalRawData.Add(ard);
        var step = await f.Db.ProductionActivitySteps.IgnoreQueryFilters().SingleAsync(item => item.Id == f.Response.ProductionActivityStepId);
        step.ProductionActivity = new() { Id = Guid.NewGuid(), ProductionScheduleProductId = (await f.Db.BatchManufacturingRecords.SingleAsync()).ProductionScheduleProductId };
        step.Operation = new() { Id = Guid.NewGuid(), Name = "QC" };
        f.Db.ProductionActivities.Add(step.ProductionActivity); f.Db.Operations.Add(step.Operation);
        (await f.Db.AnalyticalTestRequests.SingleAsync()).ChemicalArdId = ard.Id;
        f.Db.ResponseApprovals.Add(new() { Id = Guid.NewGuid(), ResponseId = f.Response.Id, ApprovalId = Guid.NewGuid(),
            ApprovalRound = 1, Required = true, Order = 1, UserId = f.Actor.UserId, ActivatedAt = DateTime.UtcNow });
        await f.Db.SaveChangesAsync();
        var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        http.HttpContext.Request.Headers.IfMatch = await ProductAssayReportingRuntime.ReviewToken(f.Db, f.Response,
            await ProductAssayReportingRuntime.Build(f.Db, f.Response, f.Calculation));
        var repo = new APP.Repository.ApprovalRepository(f.Db, null!, null!, null!, null!, null!, new Publisher(), f.Calculation, http);
        var approved = await repo.ApproveItem(nameof(Response), f.Response.Id, f.Actor.UserId!.Value, []);
        Assert.True(approved.IsSuccess, approved.Error.Description); Assert.True(f.Response.Approved);
        var before = f.Response.AssayReportingSnapshotJson;
        Assert.True((await f.Save(f.Rule(f.FirstField, 2.5m))).IsSuccess);
        f.Calculation.Available = false;
        Assert.Equal(2.43336m, Assert.Single(Assert.Single(await ProductAssayReportingRuntime.Build(f.Db, f.Response, f.Calculation)).Results).Value);
        Assert.Equal(before, f.Response.AssayReportingSnapshotJson);
    }

    [Fact]
    public async Task Unapproved_changed_or_unavailable_results_cannot_be_captured()
    {
        await using var f = new Fixture(); await f.Seed(PermissionKeys.CanViewProductAssayReporting, PermissionKeys.CanEditProductAssayReporting);
        Assert.True((await f.Save(f.Rule(f.FirstField))).IsSuccess);
        Assert.True((await ProductAssayReportingRuntime.Capture(f.Db, f.Response, f.Actor.UserId!.Value, f.Calculation)).IsFailure);
        Assert.Null(f.Response.AssayReportingSnapshotJson);
        f.Spec.IsVerified = true; await f.Db.SaveChangesAsync(); f.Calculation.Available = false;
        Assert.True((await ProductAssayReportingRuntime.Capture(f.Db, f.Response, f.Actor.UserId.Value, f.Calculation)).IsFailure);
        f.Calculation.Available = true;
        var field = await f.Db.FormFields.Include(field => field.Question).ThenInclude(question => question.Options).SingleAsync(field => field.Id == f.FirstField);
        field.Question.Options[0].Name = Fixture.Definition.Replace(":assay", ":assay*2"); await f.Db.SaveChangesAsync();
        Assert.Contains("formula changed", Assert.Single(await ProductAssayReportingRuntime.Build(f.Db, f.Response, f.Calculation)).Error);
    }

    [Fact]
    public void Compliance_compares_the_full_converted_value_before_rounding()
    {
        var rule = new AssayReportingRule(Guid.NewGuid(), "average", "hash", 2.5m, 2.32m, 2.52m, 2);
        var result = ProductAssayReportingRuntime.Convert(rule, "Average", 100.8004m);
        Assert.Equal("2.52", result.DisplayValue); Assert.False(result.Complies);
        var small = ProductAssayReportingRuntime.Convert(rule with { NominalConcentration = 0.000001m }, "Average", 0.1m);
        Assert.NotEqual("0.00", small.DisplayValue);
    }

    public class CertificateApprovals : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args) =>
            method?.Name == nameof(APP.IRepository.IApprovalRepository.CreateInitialApprovalsAsync)
                ? Task.CompletedTask : throw new NotSupportedException();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    public async Task Certificate_submission_enforces_the_converted_verdict_even_when_the_client_omits_or_overrides_it(bool includeVerdict, bool clientVerdict)
    {
        await using var f = new Fixture(); await f.Seed(PermissionKeys.CanViewProductAssayReporting, PermissionKeys.CanEditProductAssayReporting, PermissionKeys.CanCheckProductTest);
        Assert.True((await f.Save(f.Rule(f.FirstField), f.Rule(f.SecondField, 2.5m))).IsSuccess);
        f.Spec.IsVerified = true;
        var atr = await f.Db.AnalyticalTestRequests.SingleAsync();
        atr.SampledAt = DateTime.UtcNow.Date.AddDays(-3); atr.AcknowledgedAt = DateTime.UtcNow.Date.AddDays(-2); atr.TestedAt = DateTime.UtcNow.Date.AddDays(-1);
        var approval = new DOMAIN.Entities.Approvals.Approval { Id = Guid.NewGuid(), ItemType = nameof(Response) };
        approval.ApprovalStages = [new() { Id = Guid.NewGuid(), ApprovalId = approval.Id, Order = 1, Required = true, UserId = f.Actor.UserId }];
        f.Db.Approvals.Add(approval); await f.Db.SaveChangesAsync();
        var sections = await f.Db.FormFields.Where(field => field.Id == f.FirstField || field.Id == f.SecondField).Select(field => field.FormSectionId).ToListAsync();
        var repository = new APP.Repository.FormRepository(f.Db, null!, null!, DispatchProxy.Create<APP.IRepository.IApprovalRepository, CertificateApprovals>(), null!, f.Calculation);
        var submitted = await repository.GenerateCertificateOfAnalysisForProduct(f.Response.BatchManufacturingRecordId!.Value,
            f.Response.ProductionActivityStepId!.Value, f.Actor.UserId!.Value,
            includeVerdict ? sections.Select(id => new CertificateOfAnalysisComplies { FormSectionId = id, Complies = clientVerdict }).ToList() : [], f.Response.Id);
        Assert.True(submitted.IsSuccess, submitted.Error.Description);
        Assert.True((await f.Db.FormResponses.SingleAsync(answer => answer.FormFieldId == f.FirstField)).Complies);
        Assert.False((await f.Db.FormResponses.SingleAsync(answer => answer.FormFieldId == f.SecondField)).Complies);
        Assert.NotNull(f.Response.CheckedAt); Assert.NotNull(f.Response.AssayReportingSnapshotJson);
    }

    [Fact]
    public async Task The_issued_product_certificate_uses_the_captured_conversion_and_keeps_its_existing_permission()
    {
        await using var f = new Fixture(); await f.Seed(PermissionKeys.CanViewProductAssayReporting, PermissionKeys.CanEditProductAssayReporting, PermissionKeys.CanCheckProductTest);
        Assert.True((await f.Save(f.Rule(f.FirstField))).IsSuccess);
        f.Spec.IsVerified = true; await f.Db.SaveChangesAsync();
        Assert.True((await ProductAssayReportingRuntime.Capture(f.Db, f.Response, f.Actor.UserId!.Value, f.Calculation)).IsSuccess);
        f.Response.Approved = true; f.Response.CheckedAt = DateTime.UtcNow;
        foreach (var answer in await f.Db.FormResponses.ToListAsync()) answer.Complies = true;
        var stp = new DOMAIN.Entities.ProductStandardTestProcedures.ProductStandardTestProcedure { Id = Guid.NewGuid(), ProductId = f.Spec.ProductId, StpNumber = "STP" };
        f.Db.ProductStandardTestProcedures.Add(stp);
        var ard = new DOMAIN.Entities.ProductAnalyticalRawData.ProductAnalyticalRawData { Id = Guid.NewGuid(), FormId = f.Form, StpId = stp.Id,
            Stage = TestStage.Finished, AnalysisType = DOMAIN.Entities.QualityRoutines.AnalysisType.Chemical,
            CoaItems = [new() { Id = Guid.NewGuid(), FormFieldId = f.FirstField, DisplayLabel = "Assay", SpecificationText = "98–102%", Unit = "%" }] };
        f.Db.ProductAnalyticalRawData.Add(ard);
        var atr = await f.Db.AnalyticalTestRequests.SingleAsync(); atr.ChemicalArdId = ard.Id;
        await f.Db.SaveChangesAsync();
        var assaySection = await f.Db.FormSections.SingleAsync(section => section.Fields.Any(field => field.Id == f.FirstField));
        assaySection.Value = "0"; await f.Db.SaveChangesAsync();
        var repository = new APP.Repository.CommercialCertificateRepository(f.Db);
        var issued = await repository.GenerateForProduct(atr.Id, f.Actor.UserId.Value);
        Assert.True(issued.IsSuccess, issued.Error.Description);
        var certificate = await f.Db.CommercialCertificates.SingleAsync();
        using var rows = JsonDocument.Parse(certificate.RowsJson);
        var row = rows.RootElement[0];
        Assert.Contains("2.32–2.52", row.GetProperty("SpecificationText").GetString());
        Assert.Equal("% w/v", row.GetProperty("Unit").GetString());
        Assert.Equal("BP", row.GetProperty("Reference").GetString());
        using var reported = JsonDocument.Parse(row.GetProperty("result").GetString()!);
        Assert.Equal(2.43336m, reported.RootElement.GetProperty("coaReportedResults")[0].GetProperty("value").GetDecimal());
        var before = certificate.RowsJson;
        Assert.True((await f.Save(f.Rule(f.FirstField, 2.5m))).IsSuccess);
        assaySection.Value = "1"; await f.Db.SaveChangesAsync();
        Assert.Equal(before, (await repository.Get(issued.Value)).Value.RowsJson);
        f.Db.RoleClaims.RemoveRange(await f.Db.RoleClaims.Where(claim => claim.ClaimValue == PermissionKeys.CanCheckProductTest).ToListAsync());
        await f.Db.SaveChangesAsync();
        Assert.Equal(ErrorType.Forbidden, (await repository.GenerateForProduct(atr.Id, f.Actor.UserId.Value)).Error.Type);
    }

    [Fact]
    public async Task Reused_template_references_follow_the_actual_product_specification_scope()
    {
        await using var f = new Fixture(); await f.Seed();
        var section = await f.Db.FormSections.FirstAsync();
        section.Value = "0"; section.ProductSpecificationId = f.OtherSpec.Id;
        await f.Db.SaveChangesAsync();
        // Products linked to the same specification number intentionally share test references.
        Assert.Equal("0", (await SpecificationTestReferences.Values(f.Db, f.Response))[section.Id]);
        f.OtherSpec.SpecificationNumber = "ANOTHER-SPECIFICATION";
        await f.Db.SaveChangesAsync();
        Assert.Null((await SpecificationTestReferences.Values(f.Db, f.Response))[section.Id]);
        section.ProductSpecificationId = f.Spec.Id; await f.Db.SaveChangesAsync();
        Assert.Equal("0", (await SpecificationTestReferences.Values(f.Db, f.Response))[section.Id]);
    }

    private sealed class PostgresFactAttribute : FactAttribute
    {
        public PostgresFactAttribute()
        { if (Environment.GetEnvironmentVariable("ORYX_ASSAY_REPORTING_TEST_CONNECTION") is null) Skip = "Set ORYX_ASSAY_REPORTING_TEST_CONNECTION to a disposable local PostgreSQL server."; }
    }
    private sealed class EngineFactAttribute : FactAttribute
    {
        public EngineFactAttribute()
        { if (Environment.GetEnvironmentVariable("ORYX_ASSAY_REPORTING_ENGINE_URL") is null) Skip = "Set ORYX_ASSAY_REPORTING_ENGINE_URL to an isolated local formula service."; }
    }

    [EngineFact]
    public async Task Real_engine_preserves_exact_assay_and_averages_percentage_scaled_samples_before_reporting()
    {
        await using var f = new Fixture(); await f.Seed(PermissionKeys.CanViewProductAssayReporting, PermissionKeys.CanEditProductAssayReporting);
        var url = new Uri(Environment.GetEnvironmentVariable("ORYX_ASSAY_REPORTING_ENGINE_URL")!);
        Assert.True(url.IsLoopback);
        using var http = new HttpClient { BaseAddress = url };
        http.DefaultRequestHeaders.Authorization = new("Bearer", "test-only");
        var client = new FormulaCalculationClient(http, new(true, url, "test-only", TimeSpan.FromSeconds(5)));
        Assert.True((await f.Save(f.Rule(f.FirstField))).IsSuccess);
        f.Spec.IsVerified = true; await f.Db.SaveChangesAsync();
        f.Response.CheckedAt = DateTime.UtcNow; await f.Db.SaveChangesAsync();
        var simple = Assert.Single(await ProductAssayReportingRuntime.Build(f.Db, f.Response, client));
        Assert.Null(simple.Error); Assert.Equal(2.43336m, Assert.Single(simple.Results).Value);

        const string definition = "{\"type\":3,\"expression\":\":assay\",\"numberOfSamples\":2,\"manualVarLabels\":{\"assay\":\"Assay\"},\"averageAllResults\":true,\"percentageResults\":true,\"decimalPlaces\":2}";
        var field = await f.Db.FormFields.Include(field => field.Question).ThenInclude(question => question.Options).SingleAsync(field => field.Id == f.FirstField);
        field.Question.Options[0].Name = definition;
        (await f.Db.FormResponses.SingleAsync(answer => answer.ResponseId == f.Response.Id && answer.FormFieldId == f.FirstField)).Value = "{\"manualVars\":{\"assay\":{\"value\":1.0139}}}";
        await f.Db.SaveChangesAsync();
        var configured = await f.Save(f.Rule(f.FirstField) with { ResultKey = "average", DefinitionHash = LegacyFormulaRepairDefinition.Hash(definition) });
        Assert.True(configured.IsSuccess, configured.Error.Description);
        f.Spec.IsVerified = true; await f.Db.SaveChangesAsync();
        var average = Assert.Single(await ProductAssayReportingRuntime.Build(f.Db, f.Response, client));
        Assert.Null(average.Error);
        Assert.Equal(101.39m, Assert.Single(average.Results).SourceValue);
        Assert.Equal(2.43336m, Assert.Single(average.Results).Value);
    }
    private sealed class ConcurrentWrites : SaveChangesInterceptor
    {
        private int arrived;
        private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<ProductSpecification>().Any(entry => entry.State == EntityState.Modified))
            {
                if (Interlocked.Increment(ref arrived) == 2) ready.TrySetResult();
                await ready.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
            }
            return result;
        }
    }

    [PostgresFact]
    public async Task Schema_and_grant_migrations_preserve_access_types_and_concurrent_edits_cannot_overwrite_each_other()
    {
        var admin = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ORYX_ASSAY_REPORTING_TEST_CONNECTION"));
        if (admin.Host is not ("127.0.0.1" or "localhost")) throw new InvalidOperationException("A disposable local PostgreSQL server is required.");
        var database = "oryx_assay_" + Guid.NewGuid().ToString("N");
        await using var connection = new NpgsqlConnection(admin.ConnectionString); await connection.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", connection)) await create.ExecuteNonQueryAsync();
        var target = new NpgsqlConnectionStringBuilder(admin.ConnectionString) { Database = database }.ConnectionString;
        try
        {
            await using var f = new Fixture(target); await f.Db.Database.EnsureCreatedAsync();
            await using (var seed = await f.Db.Database.BeginTransactionAsync())
            {
                await f.Db.Database.ExecuteSqlRawAsync("SET LOCAL session_replication_role = replica");
                await f.Seed(PermissionKeys.CanEditProductSpecification);
                f.Response.CheckedAt = DateTime.UtcNow;
                var approval = new DOMAIN.Entities.Approvals.Approval { Id = Guid.NewGuid(), ItemType = nameof(Response) };
                f.Db.Approvals.Add(approval);
                f.Db.ResponseApprovals.Add(new() { Id = Guid.NewGuid(), ResponseId = f.Response.Id, ApprovalId = approval.Id,
                    ApprovalRound = 1, Required = true, Order = 1, UserId = f.Actor.UserId, ActivatedAt = DateTime.UtcNow });
                await f.Db.SaveChangesAsync();
                await seed.CommitAsync();
            }
            await f.Db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Responses\" DROP COLUMN \"AssayReportingSnapshotJson\"; ALTER TABLE \"ProductSpecifications\" DROP COLUMN \"AssayReportingRulesJson\";");
            foreach (var command in f.Db.GetService<IMigrationsSqlGenerator>().Generate(new AddProductAssayReporting().UpOperations, f.Db.Model))
                await f.Db.Database.ExecuteSqlRawAsync(command.CommandText);
            await f.Db.Database.ExecuteSqlRawAsync("SELECT setval(pg_get_serial_sequence('\"roleclaims\"', 'Id'), (SELECT MAX(\"Id\") FROM \"roleclaims\"));");
            foreach (var command in f.Db.GetService<IMigrationsSqlGenerator>().Generate(new GrantProductAssayReportingKeys().UpOperations, f.Db.Model))
                await f.Db.Database.ExecuteSqlRawAsync(command.CommandText);
            Assert.True((await f.Service.Get(f.Spec.Id, f.Actor.UserId!.Value)).IsSuccess);
            Assert.Equal(2, await f.Db.PermissionTypes.CountAsync(type => type.Key == PermissionKeys.CanViewProductAssayReporting || type.Key == PermissionKeys.CanEditProductAssayReporting));
            var config = (await f.Service.Get(f.Spec.Id, f.Actor.UserId.Value)).Value;
            var barrier = new ConcurrentWrites();
            await using var first = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(target).AddInterceptors(barrier).Options, f.Actor);
            await using var second = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(target).AddInterceptors(barrier).Options, f.Actor);
            async Task<int?> Save(ApplicationDbContext db, decimal nominal)
            {
                var controller = new ProductAssayReportingController(new(db)) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
                controller.HttpContext.Items["Sub"] = f.Actor.UserId.Value.ToString();
                var result = await controller.Save(f.Spec.Id, new(config.Token, "Reviewed concurrent reporting change.", [f.Rule(f.FirstField, nominal)]));
                return Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode;
            }
            var results = await Task.WhenAll(Save(first, 2.4m), Save(second, 2.5m));
            Assert.Single(results, result => result == 200);
            Assert.Single(results, result => result == 409);
            Assert.Equal(2, await f.Db.ResponseApprovals.CountAsync(stage => stage.ResponseId == f.Response.Id));
            Assert.Equal(2, await f.Db.ResponseApprovals.Where(stage => stage.ResponseId == f.Response.Id).MaxAsync(stage => stage.ApprovalRound));
            Assert.Null((await f.Db.ProductSpecifications.AsNoTracking().SingleAsync(spec => spec.Id == f.OtherSpec.Id)).AssayReportingRulesJson);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)", connection); await drop.ExecuteNonQueryAsync();
        }
    }
}
