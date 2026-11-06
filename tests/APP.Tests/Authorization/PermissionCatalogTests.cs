using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using API.Controllers;
using APP.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace APP.Tests.Authorization;

/// <summary>
/// One source of truth for permission keys, and a ratchet so no new endpoint ships without one. Regenerate the files
/// these tests compare against with <c>ORYX_WRITE_PERMISSION_FILES=1 dotnet test --filter PermissionCatalogTests</c>.
/// </summary>
public sealed class PermissionCatalogTests
{
    private static readonly bool Write = Environment.GetEnvironmentVariable("ORYX_WRITE_PERMISSION_FILES") == "1";

    private static string Repository()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (Directory.Exists(Path.Combine(directory.FullName, "API")) && Directory.Exists(Path.Combine(directory.FullName, "APP")))
                return directory.FullName;
        throw new InvalidOperationException("Repository root not found.");
    }

    private static void Compare(string relativePath, string expected)
    {
        var path = Path.Combine(Repository(), relativePath);
        if (Write) { File.WriteAllText(path, expected); return; }
        Assert.True(File.Exists(path), $"{relativePath} is missing. Run with ORYX_WRITE_PERMISSION_FILES=1 to create it.");
        Assert.True(File.ReadAllText(path) == expected,
            $"{relativePath} is out of date. Run `ORYX_WRITE_PERMISSION_FILES=1 dotnet test --filter PermissionCatalogTests` and commit the result.");
    }

    private static IReadOnlyList<string> Constants() => typeof(PermissionKeys)
        .GetFields(BindingFlags.Public | BindingFlags.Static).Where(field => field.IsLiteral && field.FieldType == typeof(string))
        .Select(field => (string)field.GetRawConstantValue()!).ToList();

    [Fact]
    public void Every_key_is_registered_exactly_once_and_every_constant_is_registered()
    {
        var catalog = PermissionUtils.GeneratePermissions().ToList();
        var duplicates = catalog.GroupBy(permission => permission.Key).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
        var added = duplicates.Where(key => !SharedToSplit.Contains(key)).ToList();
        Assert.True(added.Count == 0, "Registered more than once (one key per task: give each screen its own key): " + string.Join(", ", added));
        var split = SharedToSplit.Where(key => !duplicates.Contains(key)).ToList();
        Assert.True(split.Count == 0, "No longer shared; remove from SharedToSplit: " + string.Join(", ", split));
        var registered = catalog.Select(permission => permission.Key).ToHashSet(StringComparer.Ordinal);
        var unregistered = Constants().Where(key => !registered.Contains(key)).Distinct().ToList();
        Assert.True(unregistered.Count == 0, "Defined in PermissionKeys but never registered, so no role can hold them: " + string.Join(", ", unregistered));
    }

    /// <summary>
    /// Keys shown as tick boxes on several screens today (audit 3.2). Each is split into one key per screen in phases 2–3;
    /// this list may only shrink.
    /// </summary>
    private static readonly HashSet<string> SharedToSplit = new(StringComparer.Ordinal)
    {
        "CanViewForeignPurchaseOrder", "CanViewApprovedForeignPurchaseOrder", "CanCreateForeignPurchaseOrder", "CanViewLocalPurchaseOrder",
        "CanViewApprovedLocalPurchaseOrder", "CanCreateLocalPurchaseOrder", "CanViewRawMaterials", "CanViewPackagingMaterials",
        "CanAssignPackagingMaterialsStockToShelves",
        // Also the Extra Packaging Requisitions tick box; it gets its own key with the requisition work in phase 3.
        "CanViewPackagingMaterialRequisitions",
        "CanIssuePackagingMaterialRequisitions", "CanViewRawMaterialTransferList", "CanViewPackagingMaterialTransferList",
        "CanViewVendors", "CanCreateVendor", "CanUpdateVendorDetails", "CanDeleteVendor",
    };

    private sealed record Endpoint(string Name, string Verb, string Route, IReadOnlyList<string> Keys, bool Anonymous);

    /// <summary>Every key a product permission attribute can require, in either context.</summary>
    private static IEnumerable<string> ProductKeys(API.Authorization.ProductPermissionAttribute attribute)
    {
        var verbs = attribute.Verb == APP.Services.Authorization.ProductVerb.Save
            ? new[] { APP.Services.Authorization.ProductVerb.Add, APP.Services.Authorization.ProductVerb.Edit, APP.Services.Authorization.ProductVerb.Delete }
            : new[] { attribute.Verb };
        return Enum.GetValues<APP.Services.Authorization.ProductContext>().SelectMany(context => verbs
            .Select(verb => APP.Services.Authorization.ProductPermissions.Key(context, attribute.Section, verb))).Where(key => key is not null).Select(key => key!);
    }

    private static IEnumerable<Endpoint> Endpoints()
    {
        foreach (var controller in typeof(UserController).Assembly.GetTypes()
                     .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract).OrderBy(type => type.Name, StringComparer.Ordinal))
        {
            var prefix = controller.GetCustomAttributes<RouteAttribute>().Select(route => route.Template).OrderBy(template => template.Length).FirstOrDefault() ?? "";
            foreach (var action in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                         .Where(method => method.GetCustomAttributes<HttpMethodAttribute>().Any()).OrderBy(method => method.Name, StringComparer.Ordinal))
            {
                var http = action.GetCustomAttributes<HttpMethodAttribute>().First();
                var keys = controller.GetCustomAttributes<AuthorizeAttribute>().Concat(action.GetCustomAttributes<AuthorizeAttribute>())
                    .Select(attribute => attribute.Policy).Where(policy => !string.IsNullOrEmpty(policy)).Select(policy => policy!)
                    .Concat(action.GetCustomAttributes<API.Authorization.ProductPermissionAttribute>().SelectMany(ProductKeys))
                    .Concat(action.GetCustomAttributes<API.Authorization.MaterialPermissionAttribute>().SelectMany(attribute =>
                        Enum.GetValues<DOMAIN.Entities.Materials.MaterialKind>().Select(kind => APP.Services.Authorization.MaterialPermissions.Key(attribute.Task, kind))
                            .Where(key => key is not null).Select(key => key!)))
                    .Concat(action.GetCustomAttributes<API.Authorization.QcDocumentPermissionAttribute>()
                        .SelectMany(attribute => APP.Services.Authorization.QcDocumentPermissions.KeysFor(attribute.Document, attribute.Task)))
                    .Concat(action.GetCustomAttributes<API.Authorization.MaterialKindPermissionAttribute>()
                        .SelectMany(attribute => new[] { attribute.RawKey, attribute.PackagingKey }))
                    .Concat(action.GetCustomAttributes<API.Authorization.PurchaseOrderPermissionAttribute>()
                        .SelectMany(attribute => new[] { attribute.ForeignKey, attribute.LocalKey }))
                    .Concat(action.GetCustomAttributes<API.Authorization.AnyPermissionAttribute>().SelectMany(attribute => attribute.Keys))
                    .Concat(action.GetCustomAttributes<API.Authorization.SupplierTypePermissionAttribute>()
                        .SelectMany(attribute => new[] { attribute.ForeignKey, attribute.LocalKey }))
                    .Concat(action.GetCustomAttributes<API.Authorization.RequisitionCreatePermissionAttribute>()
                        .SelectMany(_ => API.Authorization.RequisitionCreatePermissionAttribute.AllKeys)).Distinct().ToList();
                var anonymous = action.GetCustomAttributes<AllowAnonymousAttribute>().Any() || controller.GetCustomAttributes<AllowAnonymousAttribute>().Any();
                var route = string.Join("/", new[] { prefix, http.Template }.Where(part => !string.IsNullOrEmpty(part)))
                    .Replace("api/v{version:apiVersion}/", "");
                yield return new Endpoint($"{controller.Name}.{action.Name}", http.HttpMethods.First(), route, keys, anonymous);
            }
        }
    }

    [Fact]
    public void Every_key_an_endpoint_requires_exists()
    {
        var registered = PermissionUtils.GeneratePermissions().Select(permission => permission.Key).ToHashSet(StringComparer.Ordinal);
        var unknown = Endpoints().SelectMany(endpoint => endpoint.Keys.Where(key => !registered.Contains(key)).Select(key => $"{endpoint.Name}: {key}")).ToList();
        Assert.True(unknown.Count == 0, "Endpoints require keys no role can hold: " + string.Join("; ", unknown));
    }

    /// <summary>Sign-in, password reset, invitation onboarding, the ONLYOFFICE callback and file reads (until signed links).</summary>
    private static readonly HashSet<string> AnonymousAllowed = new(StringComparer.Ordinal)
    {
        "AuthController.Login", "AuthController.SetPassword", "AuthController.ForgotPassword",
        "FileController.GetImage", "StpDocumentController.GetEditorVersionFile", "StpDocumentController.Callback",
    };

    /// <summary>The public onboarding form: every call carries and checks the emailed invitation token.</summary>
    private static bool Allowed(string name) => AnonymousAllowed.Contains(name) || name.StartsWith("OnboardingController.", StringComparison.Ordinal);

    [Fact]
    public void No_new_endpoint_ships_without_a_permission_and_anonymous_access_is_an_allow_list()
    {
        var endpoints = Endpoints().ToList();
        var anonymous = endpoints.Where(endpoint => endpoint.Anonymous).Select(endpoint => endpoint.Name).Distinct().ToList();
        var unexpected = anonymous.Where(name => !Allowed(name)).ToList();
        Assert.True(unexpected.Count == 0, "Anonymous endpoints outside the allow-list: " + string.Join(", ", unexpected));

        // Endpoints with no named permission (signed-in only). This list may only shrink: give a new endpoint its key.
        var lines = endpoints.Where(endpoint => !endpoint.Anonymous && endpoint.Keys.Count == 0)
            .Select(endpoint => $"{endpoint.Name} {endpoint.Verb} {endpoint.Route}").Distinct().OrderBy(line => line, StringComparer.Ordinal).ToList();
        var path = Path.Combine(Repository(), "docs/permission-endpoint-baseline.txt");
        if (Write) { File.WriteAllText(path, string.Join('\n', lines) + "\n"); return; }
        var baseline = File.ReadAllLines(path).Where(line => line.Length > 0).ToHashSet(StringComparer.Ordinal);
        var added = lines.Where(line => !baseline.Contains(line)).ToList();
        Assert.True(added.Count == 0, "New endpoints without a permission. Add [Authorize(PermissionKeys.X)]:\n" + string.Join('\n', added));
        var fixedNow = baseline.Where(line => !lines.Contains(line)).ToList();
        Assert.True(fixedNow.Count == 0, "These endpoints now have a permission; remove them from docs/permission-endpoint-baseline.txt (or regenerate):\n" + string.Join('\n', fixedNow));
    }

    [Fact]
    public void The_published_catalog_matches_the_registry()
    {
        var catalog = PermissionUtils.GeneratePermissions().Select(permission => new
        {
            module = permission.Module, submodule = permission.SubModule, key = permission.Key, name = permission.Name, description = permission.Description,
        });
        var json = JsonSerializer.Serialize(catalog, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }) + "\n";
        Compare("docs/permission-catalog.json", json);
    }
}
