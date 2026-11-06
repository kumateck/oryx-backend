using System.Reflection;
using API.Authorization;
using API.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace APP.Tests.Authorization;

/// <summary>HR and settings reads: each needs a key of a page that shows it, or says why it is open (audit phase 4).</summary>
public sealed class HrSettingsReadPermissionTests
{
    private static MethodInfo Action(Type controller, string name, string? template = null) =>
        Assert.Single(controller.GetMethods(), method => method.Name == name
            && (template is null || method.GetCustomAttributes<HttpMethodAttribute>().Any(http => http.Template == template)));

    [Theory]
    [InlineData(typeof(LeaveRequestController), "GetLeaveRequests", null, "CanViewLeaveRequests")]
    [InlineData(typeof(OvertimeRequestController), "GetOvertimeRequest", null, "CanViewPendingApprovals")]
    [InlineData(typeof(StaffRequisitionController), "GetStaffRequisitions", null, "CanViewStaffRequisition")]
    [InlineData(typeof(HolidayController), "GetHolidays", null, "CanViewHolidays")]
    [InlineData(typeof(CompanyWorkingDaysController), "GetWorkingDays", null, "CanViewWorkingDays")]
    [InlineData(typeof(ConfigurationController), "GetThreshold", null, "CanViewThreshold")]
    [InlineData(typeof(CustomerController), "GetCustomer", null, "CanViewCustomers")]
    [InlineData(typeof(ActivityLogController), "GetManufacturers", null, "CanViewAuditTrail")]
    public void A_read_takes_the_key_of_a_page_that_shows_it(Type controller, string name, string? template, string key) =>
        Assert.Contains(key, Assert.Single(Action(controller, name, template).GetCustomAttributes<AnyPermissionAttribute>()).Keys);

    [Theory]
    [InlineData(typeof(EmployeeController), "GetMyEmployee")]
    [InlineData(typeof(UserController), "GetUser")]
    [InlineData(typeof(AlertController), "GetNotificationsForUser")]
    [InlineData(typeof(DepartmentController), "GetDepartments")]
    [InlineData(typeof(PerformanceController), "GetMyReviews")]
    public void Own_data_and_reference_lookups_are_open_to_signed_in_users_with_a_reason(Type controller, string name) =>
        Assert.False(string.IsNullOrWhiteSpace(Action(controller, name).GetCustomAttribute<SignedInUserAttribute>()?.Reason));

    [Fact]
    public void Tickets_check_reporter_assignee_or_view_all_in_the_action() =>
        Assert.NotNull(Action(typeof(TicketController), "GetTickets").GetCustomAttribute<PermissionCheckedInActionAttribute>());
}
