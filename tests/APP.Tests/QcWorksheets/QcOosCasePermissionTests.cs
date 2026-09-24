using System.Reflection;
using API.Controllers;
using APP.Utils;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace APP.Tests.QcWorksheets;

/// <summary>
/// The OOS module's permission surface, pinned action by action.
/// <para>
/// Reading a case used to sit behind <see cref="QcWorksheetPermissionKeys.CanInvestigateQcOosCase"/>,
/// which made the other two authorities unusable on their own: a QA Manager holding only
/// <see cref="QcWorksheetPermissionKeys.CanDispositionQcOosCase"/> could not load the case they
/// had to sign, and the QC Manager holding only
/// <see cref="QcWorksheetPermissionKeys.CanAuthorizeQcOosRetest"/> was shut out the same way.
/// </para>
/// <para>
/// These tests exist to keep the fix from eroding in either direction — the view key must cover
/// the reads, and must never spread onto an action that writes.
/// </para>
/// </summary>
public class QcOosCasePermissionTests
{
    /// <summary>
    /// Every endpoint on the controller, with the one key that gates it. Asserting the whole map
    /// rather than the two changed lines is the point: an action added later without a key, or
    /// gated on the view key, fails here.
    /// </summary>
    [Fact]
    public void Every_oos_case_endpoint_is_gated_on_exactly_the_expected_key()
    {
        var actual = typeof(QcOosCaseController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .ToDictionary(
                method => method.Name,
                method => Assert.Single(
                    method.GetCustomAttributes<AuthorizeAttribute>()
                        .Select(attribute => attribute.Policy)));

        Assert.Equal(new Dictionary<string, string>
        {
            // Reads. The whole point of the split: both acting roles below must be able to load
            // the case they are about to act on without holding an investigation permission.
            [nameof(QcOosCaseController.GetOosCases)] =
                QcWorksheetPermissionKeys.CanViewQcOosCases,
            [nameof(QcOosCaseController.GetOosCase)] =
                QcWorksheetPermissionKeys.CanViewQcOosCases,

            // Acting. Unchanged, and deliberately so — the view key widens reads only. A QC
            // Officer runs the Phase 1 lab-error check...
            [nameof(QcOosCaseController.StartInvestigation)] =
                QcWorksheetPermissionKeys.CanInvestigateQcOosCase,
            [nameof(QcOosCaseController.UpdateInvestigation)] =
                QcWorksheetPermissionKeys.CanInvestigateQcOosCase,

            // ...a QC Manager decides a retest is warranted, or escalates instead...
            [nameof(QcOosCaseController.AuthorizeRetest)] =
                QcWorksheetPermissionKeys.CanAuthorizeQcOosRetest,
            [nameof(QcOosCaseController.Escalate)] =
                QcWorksheetPermissionKeys.CanAuthorizeQcOosRetest,

            // ...and a QA Manager signs the disposition that rejects or releases a real batch.
            [nameof(QcOosCaseController.Dispose)] =
                QcWorksheetPermissionKeys.CanDispositionQcOosCase
        }, actual);
    }

    /// <summary>
    /// The new key is actually grantable, and filed under the OOS submodule rather than falling
    /// through the catalog's dispatch into the WorksheetTemplates default. A key filed under the
    /// wrong submodule is grantable but unfindable, which in practice is the same failure as not
    /// registering it at all.
    /// </summary>
    [Fact]
    public void The_oos_view_key_is_grantable_under_the_oos_submodule()
    {
        var permission = Assert.Single(
            PermissionUtils.GeneratePermissions()
                .Where(item => item.Key == QcWorksheetPermissionKeys.CanViewQcOosCases));

        Assert.Equal(QcWorksheetPermissionCatalog.Module, permission.Module);
        Assert.Equal(QcWorksheetPermissionCatalog.OosCases, permission.SubModule);

        // The three acting keys are filed alongside it, so the split did not strand the new key
        // in a submodule of its own.
        foreach (var key in new[]
                 {
                     QcWorksheetPermissionKeys.CanInvestigateQcOosCase,
                     QcWorksheetPermissionKeys.CanAuthorizeQcOosRetest,
                     QcWorksheetPermissionKeys.CanDispositionQcOosCase
                 })
        {
            var acting = Assert.Single(
                PermissionUtils.GeneratePermissions().Where(item => item.Key == key));
            Assert.Equal(QcWorksheetPermissionCatalog.OosCases, acting.SubModule);
        }
    }
}
