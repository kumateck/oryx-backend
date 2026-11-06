using System.Reflection;
using API.Controllers;
using APP.Utils;
using INFRASTRUCTURE.Migrations;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace APP.Tests.Authorization;

/// <summary>HR and settings writes: each task needs its own key on the server (audit phase 4).</summary>
public sealed class HrPermissionTests
{
    private static IReadOnlyList<string> Policies(Type controller, string action) =>
        controller.GetMethods().Where(method => method.Name == action)
            .Select(method => Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy!).ToList();

    [Theory]
    [InlineData(typeof(DepartmentController), "CreateDepartment", PermissionKeys.CanCreateNewDepartment)]
    [InlineData(typeof(DesignationController), "DeleteDesignation", PermissionKeys.CanDeleteDesignation)]
    [InlineData(typeof(HolidayController), "UpdateHoliday", PermissionKeys.CanEditHoliday)]
    [InlineData(typeof(LeaveTypeController), "CreateLeaveType", PermissionKeys.CanCreateLeaveType)]
    [InlineData(typeof(LeaveEntitlementController), "UpdateLeaveEntitlement", PermissionKeys.CanEditLeaveEntitlement)]
    [InlineData(typeof(LeaveRequestController), "CreateLeaveOrAbsenceRequest", PermissionKeys.CanCreateLeaveRequest)]
    [InlineData(typeof(LeaveRequestController), "SubmitLeaveRequest", PermissionKeys.CanRecallLeave)]
    [InlineData(typeof(LeaveRequestController), "CancelLeaveRequest", PermissionKeys.CanCancelLeave)]
    [InlineData(typeof(LeaveRequestController), "DeleteLeaveRequest", PermissionKeys.CanDeleteLeaveRequest)]
    [InlineData(typeof(OvertimeRequestController), "DeleteOvertimeRequest", PermissionKeys.CanDeleteOvertimeManagement)]
    [InlineData(typeof(StaffRequisitionController), "UpdateStaffRequisition", PermissionKeys.CanEditStaffRequisition)]
    [InlineData(typeof(AttendanceRecordController), "UploadAttendance", PermissionKeys.CanSubmitAttendanceReportUpload)]
    [InlineData(typeof(EmployeeController), "ChangeEmployeeType", PermissionKeys.CanUpdateEmployeeInfo)]
    [InlineData(typeof(EmployeeController), "AssignEmployee", PermissionKeys.CanUpdateEmployeeInfo)]
    [InlineData(typeof(EmployeeController), "UpdateEmployeeEmail", PermissionKeys.CanUpdateEmployeeEmail)]
    [InlineData(typeof(EmployeeController), "UploadEmployees", PermissionKeys.CanRegisterEmployee)]
    [InlineData(typeof(EmployeeController), "DeleteEmployee", PermissionKeys.CanDeleteEmployee)]
    [InlineData(typeof(ConfigurationController), "UpdateConfiguration", PermissionKeys.CanEditCodeSettings)]
    [InlineData(typeof(ConfigurationController), "CreateThreshold", PermissionKeys.CanCreateThreshold)]
    public void A_write_needs_its_task_key(Type controller, string action, string key) =>
        Assert.All(Policies(controller, action), policy => Assert.Equal(key, policy));

    [Fact]
    public void Entitlement_keys_come_from_leave_type_keys_and_deletes_go_to_nobody()
    {
        var registered = PermissionUtils.GeneratePermissions().Select(permission => permission.Key).ToHashSet();
        foreach (var key in new[] { PermissionKeys.CanCreateLeaveEntitlement, PermissionKeys.CanEditLeaveEntitlement, PermissionKeys.CanDeleteLeaveEntitlement,
                     PermissionKeys.CanDeleteLeaveRequest, PermissionKeys.CanDeleteEmployee })
            Assert.Contains(key, registered);
        Assert.Contains(GrantLeaveEntitlementKeys.Grants, grant => grant == (PermissionKeys.CanEditLeaveEntitlement, PermissionKeys.CanEditLeaveType));
        Assert.DoesNotContain(GrantLeaveEntitlementKeys.Grants, grant => grant.Key is PermissionKeys.CanDeleteEmployee or PermissionKeys.CanDeleteLeaveRequest);
    }
}
