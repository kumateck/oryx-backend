using System.Reflection;
using API.Authorization;
using API.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace APP.Tests.Authorization;

/// <summary>Forms (templates, questions, assignments, responses): each task needs a key of a page that does it (audit phase 4).</summary>
public sealed class FormPermissionTests
{
    private static IEnumerable<AnyPermissionAttribute> Rules(string name) =>
        typeof(FormController).GetMethods().Where(method => method.Name == name).SelectMany(method => method.GetCustomAttributes<AnyPermissionAttribute>());

    [Theory]
    [InlineData("CreateForm", "CanCreateTemplate")]
    [InlineData("UpdateFormSection", "CanEditTemplate")]
    [InlineData("DeleteForm", "CanDeleteTemplate")]
    [InlineData("CreateQuestion", "CanCreateQuestions")]
    [InlineData("DeleteQuestion", "CanDeleteQuestions")]
    [InlineData("SubmitFormAssignee", "CanAssignProductTest")]
    [InlineData("ReassignFormAssignee", "CanReassignRawMaterialTest")]
    [InlineData("SubmitFormSectionResponse", "CanStartPackagingMaterialTest")]
    [InlineData("GenerateCertificateOfAnalysisForProduct", "CanCheckProductTest")]
    [InlineData("GetForms", "CanViewTemplate")]
    public void A_form_task_needs_a_key_of_a_page_that_does_it(string name, string key) =>
        Assert.Contains(Rules(name), rule => rule.Keys.Contains(key));

    [Fact]
    public void Every_form_endpoint_is_covered()
    {
        var actions = typeof(FormController).GetMethods().Where(method => method.GetCustomAttributes<HttpMethodAttribute>().Any());
        Assert.All(actions, method => Assert.True(
            method.GetCustomAttributes<AnyPermissionAttribute>().Any() || method.GetCustomAttribute<PermissionCheckedInActionAttribute>() is not null
                || method.GetCustomAttributes<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Any(attribute => attribute.Policy is not null),
            method.Name));
    }
}
