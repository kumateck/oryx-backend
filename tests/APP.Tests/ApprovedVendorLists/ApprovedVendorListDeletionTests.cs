using APP.Utils;
using APP.Services.Reviews;
using DOMAIN.Entities.ApprovedVendorLists;
using DOMAIN.Entities.Reviews;
using Microsoft.EntityFrameworkCore;
using SHARED;
using Xunit;

namespace APP.Tests.ApprovedVendorLists;

public partial class ApprovedVendorListTests
{
    [Fact]
    public async Task Deleting_a_superseded_list_needs_the_delete_key_even_for_its_editor()
    {
        var editor = await Person("Editor", PermissionKeys.CanViewApprovedVendorList, PermissionKeys.CanEditApprovedVendorList,
            PermissionKeys.CanReviseApprovedVendorList, PermissionKeys.CanApproveApprovedVendorList);
        var old = new ApprovedVendorList { Id = Guid.NewGuid(), Status = ApprovedVendorListStatus.Superseded };
        db.ApprovedVendorLists.Add(old);
        await db.SaveChangesAsync();

        var result = await Lists().Delete(old.Id, editor.User);

        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.Null((await db.ApprovedVendorLists.AsNoTracking().SingleAsync()).DeletedAt);
    }

    [Theory]
    [InlineData(ApprovedVendorListStatus.Pending)]
    [InlineData(ApprovedVendorListStatus.Approved)]
    [InlineData(ApprovedVendorListStatus.Rejected)]
    public async Task Other_list_states_cannot_be_deleted_even_with_the_key(ApprovedVendorListStatus status)
    {
        var remover = await Person("Remover", PermissionKeys.CanDeleteApprovedVendorList);
        var list = new ApprovedVendorList { Id = Guid.NewGuid(), Status = status };
        db.ApprovedVendorLists.Add(list);
        await db.SaveChangesAsync();

        Assert.Equal("ApprovedVendorList.NotSuperseded", (await Lists().Delete(list.Id, remover.User)).Error.Code);
        Assert.Null((await db.ApprovedVendorLists.AsNoTracking().SingleAsync()).DeletedAt);
    }

    [Fact]
    public async Task Deletion_retains_document_history_and_revision_links_and_keeps_the_current_list_in_force()
    {
        var f = await Masters();
        var first = await ApprovedList(f, Today.AddYears(1));
        var reviser = await Person("Reviser", PermissionKeys.CanReviseApprovedVendorList);
        var checker = await Person("Checker", PermissionKeys.CanReviewApprovedVendorList);
        var remover = await Person("Remover", PermissionKeys.CanDeleteApprovedVendorList);
        var second = await Lists().Revise(new() { AnnexureNumber = "QAD/SOP/007/A03-06", RevisionNumber = 18,
            Lines = First(f, Today.AddYears(3)).Lines }, reviser.User);
        Assert.True(second.IsSuccess);
        Assert.True((await Reviews().Confirm(ReviewDocumentTypes.ApprovedVendorList, second.Value, new(), checker.User)).IsSuccess);
        var reviewIds = await db.DocumentReviews.Select(item => item.Id).ToListAsync();
        var lineIds = await db.ApprovedVendorListLines.Select(item => item.Id).ToListAsync();
        var before = DateTime.UtcNow;

        Assert.True((await Lists().Delete(first, remover.User)).IsSuccess);
        db.ChangeTracker.Clear();

        Assert.Equal(second.Value, Assert.Single(await Lists().List()).Id);
        Assert.Equal(ErrorType.NotFound, (await Lists().Get(first)).Error.Type);
        Assert.Equal(ErrorType.NotFound, (await Lists().Delete(first, remover.User)).Error.Type);
        var saved = await db.ApprovedVendorLists.IgnoreQueryFilters().SingleAsync(item => item.Id == first);
        Assert.True(saved.DeletedAt >= before);
        Assert.Equal(remover.User, saved.LastDeletedById);
        Assert.Equal(ApprovedVendorListStatus.Superseded, saved.Status);
        Assert.Equal(lineIds.Order(), (await db.ApprovedVendorListLines.Select(item => item.Id).ToListAsync()).Order());
        Assert.Equal(reviewIds.Order(), (await db.DocumentReviews.Select(item => item.Id).ToListAsync()).Order());
        var current = (await Lists().Get(second.Value)).Value;
        Assert.Equal(first, current.PreviousRevisionId);
        Assert.Equal(ApprovedVendorListStatus.Approved, current.Status);
        var eligibility = await Lists().Eligibility(new() { MaterialIds = [f.Material] });
        Assert.True(Assert.Single(eligibility).Restricted);
        Assert.Equal(f.Supplier, Assert.Single(Assert.Single(eligibility).Approved).SupplierId);
    }

    [Fact]
    public async Task Deleted_or_unknown_lists_are_not_found_and_revoked_grants_are_refused()
    {
        var remover = await Person("Remover", PermissionKeys.CanDeleteApprovedVendorList);
        Assert.Equal(ErrorType.NotFound, (await Lists().Delete(Guid.NewGuid(), remover.User)).Error.Type);
        var list = new ApprovedVendorList { Id = Guid.NewGuid(), Status = ApprovedVendorListStatus.Superseded };
        db.ApprovedVendorLists.Add(list);
        await db.SaveChangesAsync();
        db.RoleClaims.RemoveRange(await db.RoleClaims.Where(item => item.RoleId == remover.Role).ToListAsync());
        await db.SaveChangesAsync();

        Assert.Equal(ErrorType.Forbidden, (await Lists().Delete(list.Id, remover.User)).Error.Type);
        Assert.Null((await db.ApprovedVendorLists.AsNoTracking().SingleAsync()).DeletedAt);
    }
}
