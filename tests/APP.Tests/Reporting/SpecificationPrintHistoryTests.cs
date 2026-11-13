using System.Reflection;
using API.Controllers;
using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using INFRASTRUCTURE.Migrations;
using INFRASTRUCTURE.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Reporting;

public class SpecificationPrintHistoryTests
{
    private sealed class Actor(Guid id) : ICurrentUserService
    {
        public Guid? UserId => id;
        public Guid? DepartmentId => null;
        public string DepartmentType => "NonProduction";
    }

    [Theory]
    [InlineData(nameof(SpecificationPrintController.RawMaterial), PermissionKeys.CanExportRawMaterialSpecification)]
    [InlineData(nameof(SpecificationPrintController.PackagingMaterial), PermissionKeys.CanExportPackagingMaterialSpecification)]
    [InlineData(nameof(SpecificationPrintController.Product), PermissionKeys.CanExportProductSpecification)]
    public void Endpoints_use_their_own_registered_export_permission(string action, string key)
    {
        var method = typeof(SpecificationPrintController).GetMethod(action)!;
        Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>(), attribute => attribute.Policy == key);
        Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.Single(PermissionUtils.GeneratePermissions(), permission => permission.Key == key);
    }

    [Theory]
    [InlineData(MaterialKind.Raw, PermissionKeys.CanViewRawMaterialSpecifications, PermissionKeys.CanExportPackagingMaterialSpecification)]
    [InlineData(MaterialKind.Package, PermissionKeys.CanViewPackagingMaterialSpecifications, PermissionKeys.CanExportRawMaterialSpecification)]
    public async Task Material_print_refuses_view_only_and_the_other_variants_export_permission(MaterialKind kind, string view, string wrongPrint)
    {
        var actor = new Actor(Guid.NewGuid());
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, actor);
        await Grant(context, actor.UserId!.Value, view, wrongPrint);
        var result = await new SpecificationPrintRepository(context, actor).Material(Guid.NewGuid(), kind, default);
        Assert.Equal("SpecificationPrint.Permission", result.Error.Code);
    }

    [Fact]
    public async Task Product_print_refuses_a_user_with_only_product_view_access()
    {
        var actor = new Actor(Guid.NewGuid());
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, actor);
        await Grant(context, actor.UserId!.Value, PermissionKeys.CanViewProductSpecifications);
        var result = await new SpecificationPrintRepository(context, actor).Product(Guid.NewGuid(), default);
        Assert.Equal("SpecificationPrint.Permission", result.Error.Code);
    }

    [LocalReportHistoryFact]
    public async Task Authorized_prints_return_the_stored_revision_and_exclude_approval_and_rolled_back_edits()
    {
        var connectionString = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ORYX_REPORT_HISTORY_TEST_CONNECTION"));
        Assert.Contains(connectionString.Host, new[] { "127.0.0.1", "localhost" });
        Assert.StartsWith("oryx_report_history_test", connectionString.Database);
        await using var connection = new NpgsqlConnection(connectionString.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await Execute(connection, """
            DROP SCHEMA public CASCADE; CREATE SCHEMA public;
            CREATE TABLE users ("Id" uuid PRIMARY KEY,"FirstName" text,"LastName" text,"IsDisabled" boolean,"DeletedAt" timestamptz);
            CREATE TABLE roles ("Id" uuid PRIMARY KEY,"DeletedAt" timestamptz);
            CREATE TABLE userroles ("UserId" uuid,"RoleId" uuid);
            CREATE TABLE roleclaims ("Id" integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,"RoleId" uuid,"ClaimType" text,"ClaimValue" text);
            CREATE TABLE "PermissionTypes" ("Id" uuid,"RoleClaimId" integer,"Key" text,"Type" text,"DeletedAt" timestamptz);
            CREATE TABLE "Materials" ("Id" uuid PRIMARY KEY,"Kind" integer,"DeletedAt" timestamptz);
            CREATE TABLE "MaterialSpecifications" ("Id" uuid PRIMARY KEY,"MaterialId" uuid,"DeletedAt" timestamptz,
                "SpecificationNumber" text,"RevisionNumber" text,"SupersedesNumber" text,"Description" text,
                "CreatedAt" timestamptz,"CreatedById" uuid,"UpdatedAt" timestamptz,"LastUpdatedById" uuid,"IsVerified" boolean);
            CREATE TABLE "ProductSpecifications" (LIKE "MaterialSpecifications" INCLUDING ALL);
            INSERT INTO users VALUES
              ('00000000-0000-0000-0000-000000000001','Ama','Owusu',false,NULL),
              ('00000000-0000-0000-0000-000000000002','Kofi','Mensah',false,NULL);
            INSERT INTO roles VALUES ('00000000-0000-0000-0000-000000000003',NULL);
            INSERT INTO userroles VALUES ('00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000000003');
            INSERT INTO roleclaims ("RoleId","ClaimType","ClaimValue")
              SELECT '00000000-0000-0000-0000-000000000003', 'permission', permission
              FROM unnest(ARRAY['CanViewRawMaterialSpecifications','CanViewPackagingMaterialSpecifications',
                'CanViewProductSpecifications']) AS keys(permission);
            INSERT INTO "PermissionTypes" SELECT gen_random_uuid(), "Id", "ClaimValue", 'View', NULL FROM roleclaims;
            INSERT INTO "Materials" VALUES ('00000000-0000-0000-0000-000000000004',0,NULL),('00000000-0000-0000-0000-000000000005',1,NULL);
            INSERT INTO "MaterialSpecifications" VALUES
              ('00000000-0000-0000-0000-000000000006','00000000-0000-0000-0000-000000000004',NULL,'SPEC-1','01','None','Initial',
               '2026-10-01T09:00:00Z','00000000-0000-0000-0000-000000000001',NULL,NULL,false),
              ('00000000-0000-0000-0000-000000000007','00000000-0000-0000-0000-000000000005',NULL,'PACK-1','01','None','Initial',
               '2026-10-01T09:00:00Z','00000000-0000-0000-0000-000000000001',NULL,NULL,false);
            INSERT INTO "ProductSpecifications" SELECT * FROM "MaterialSpecifications" WHERE "SpecificationNumber"='SPEC-1';
            """);
        var grantSql = Assert.Single(new GrantSpecificationPrintPermissions().UpOperations.OfType<SqlOperation>()).Sql;
        await Execute(connection, grantSql);
        await Execute(connection, grantSql);
        await Execute(connection, ReportHistorySchema.InstallSql);
        await Execute(connection, """
            SET CONSTRAINTS ALL IMMEDIATE;
            UPDATE "MaterialSpecifications" SET "RevisionNumber"='02',"SupersedesNumber"='01',"Description"='Revised',
              "UpdatedAt"='2026-11-12T15:10:00Z',"LastUpdatedById"='00000000-0000-0000-0000-000000000002' WHERE "SpecificationNumber"='SPEC-1';
            UPDATE "MaterialSpecifications" SET "IsVerified"=true,"UpdatedAt"='2026-11-12T17:00:00Z',
              "LastUpdatedById"='00000000-0000-0000-0000-000000000001' WHERE "SpecificationNumber"='SPEC-1';
            SAVEPOINT failed_edit;
            UPDATE "MaterialSpecifications" SET "RevisionNumber"='03' WHERE "SpecificationNumber"='SPEC-1';
            ROLLBACK TO SAVEPOINT failed_edit;
            """);

        var actor = new Actor(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connection).Options, actor);
        await context.Database.UseTransactionAsync(transaction);
        var repository = new SpecificationPrintRepository(context, actor);
        var rawId = Guid.Parse("00000000-0000-0000-0000-000000000006");
        var raw = (await repository.Material(rawId, MaterialKind.Raw, default)).Value;
        Assert.Equal("Kofi Mensah", raw.RevisedBy);
        Assert.Equal(new DateTime(2026, 11, 12, 15, 10, 0, DateTimeKind.Utc), raw.RevisedAt);
        Assert.Collection(raw.Revisions,
            revision => Assert.Equal(("02", "Revised"), (revision.RevisionNumber, revision.Event)),
            revision => Assert.Equal(("01", "History baseline"), (revision.RevisionNumber, revision.Event)));
        Assert.Equal("SpecificationPrint.NotFound", (await repository.Material(rawId, MaterialKind.Package, default)).Error.Code);
        var packagingId = Guid.Parse("00000000-0000-0000-0000-000000000007");
        Assert.Single((await repository.Material(packagingId, MaterialKind.Package, default)).Value.Revisions);
        Assert.Single((await repository.Product(rawId, default)).Value.Revisions);
    }

    private static async Task Execute(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task Grant(ApplicationDbContext context, Guid actor, params string[] keys)
    {
        var role = new Role { Id = Guid.NewGuid(), Name = "Specification viewer" };
        context.Users.Add(new User { Id = actor, UserName = "Specification viewer" });
        context.Roles.Add(role);
        context.UserRoles.Add(new IdentityUserRole<Guid> { UserId = actor, RoleId = role.Id });
        for (var index = 0; index < keys.Length; index++)
        {
            var id = index + 1;
            context.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = id, RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = keys[index] });
            context.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = id, Key = keys[index], Type = "View" });
        }
        await context.SaveChangesAsync();
    }
}
