using System.Reflection;
using API.Authorization;
using API.Controllers;
using APP.Services.Stores;
using APP.Utils;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace APP.Tests.Authorization;

/// <summary>Service and maintenance workflows: fixed-key checks sit on the action, where the catalog sees them (audit phase 4).</summary>
public sealed class ServicePermissionTests
{
    private static string[] Policies(Type controller, string action) => controller.GetMethods().Where(method => method.Name == action)
        .SelectMany(method => method.GetCustomAttributes<AuthorizeAttribute>()).Select(attribute => attribute.Policy!).Order().ToArray();

    [Theory]
    [InlineData(typeof(JobRequestController), "CreateJobRequest", ServiceKeys.CreateJobRequest + "," + ServiceKeys.ViewJobRequests)]
    [InlineData(typeof(JobExecutionController), "StartJobExecution", ServiceKeys.StartJobRequest + "," + ServiceKeys.ViewJobRequests)]
    [InlineData(typeof(JobOrderController), "SelectQuotation", ServiceKeys.ApplyPricing + "," + ServiceKeys.ViewPricing)]
    [InlineData(typeof(ServiceMemoController), "MarkServiceMemoAsPaid", ServiceKeys.CreatePurchaseOrder + "," + ServiceKeys.ViewMemo)]
    [InlineData(typeof(ServiceProviderController), "DeleteServiceProvider", "CanDeleteContractors,CanViewContractors")]
    [InlineData(typeof(ServiceController), "UpdateService", "CanEditService,CanViewServices")]
    [InlineData(typeof(ServiceProformaInvoiceController), "ApproveServiceProformaInvoice", ServiceKeys.CreatePurchaseOrder + "," + ServiceKeys.ViewPurchaseOrders)]
    [InlineData(typeof(JobExecutionController), "ApproveJobExecution", ServiceKeys.ViewJobRequests)]
    [InlineData(typeof(JobRequestController), "UpdateJobRequestStatus", ServiceKeys.ViewJobRequests)]
    [InlineData(typeof(WorkOrderController), "DeleteWorkOrder", PermissionKeys.CanDeleteWorkOrder)]
    public void A_service_write_needs_its_view_and_task_keys(Type controller, string action, string keys) =>
        Assert.Equal(keys.Split(',').Order(), Policies(controller, action));

    [Fact]
    public void Shared_service_reads_take_any_of_the_pages_keys()
    {
        var keys = Assert.Single(typeof(ServiceController).GetMethod("GetServices")!.GetCustomAttributes<AnyPermissionAttribute>()).Keys;
        Assert.Equal(["CanViewServices", "CanCreateJobRequest", "CanEditJobRequest", "CanCreateJobOrder"], keys);
    }
}
