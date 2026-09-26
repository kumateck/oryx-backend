using System.Reflection;
using API.Controllers;
using APP.Utils;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace APP.Tests.QcWorksheets.WorksheetImport;

/// <summary>
/// The worksheet import has its own key, mirroring <see cref="QcWorksheetPermissionKeys.CanImportQcStp"/>:
/// grantable, filed with the other worksheet-template keys, and the only key on the endpoint.
/// </summary>
public class WorksheetImportPermissionTests
{
    [Fact]
    public void The_import_key_is_grantable_under_the_worksheet_template_submodule()
    {
        var permission = Assert.Single(PermissionUtils.GeneratePermissions(),
            item => item.Key == QcWorksheetPermissionKeys.CanImportQcWorksheetTemplates);

        Assert.Equal(QcWorksheetPermissionCatalog.Module, permission.Module);
        Assert.Equal(QcWorksheetPermissionCatalog.WorksheetTemplates, permission.SubModule);
    }

    [Fact]
    public void The_import_endpoint_is_gated_on_the_import_key_alone()
    {
        var import = typeof(QcWorksheetTemplateController).GetMethod(nameof(QcWorksheetTemplateController.Import))!;

        var policy = Assert.Single(import.GetCustomAttributes<AuthorizeAttribute>().Select(attribute => attribute.Policy));
        Assert.Equal(QcWorksheetPermissionKeys.CanImportQcWorksheetTemplates, policy);
    }
}
