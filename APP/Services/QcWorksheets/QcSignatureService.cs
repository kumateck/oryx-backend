using APP.IRepository;
using DOMAIN.Entities.QcWorksheets;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.QcWorksheets;

public interface IQcSignatureService
{
    Task<Result> VerifyAsync(Guid userId, string password);

    Task<Result> SignAndApproveAsync(
        string modelType, Guid entityId, Guid userId, List<Guid> roleIds, string password, string comments);

    Task<Result> SignAndRejectAsync(
        string modelType, Guid entityId, Guid userId, List<Guid> roleIds, string password, string comments);
}

/// <summary>
/// The QC re-authentication wrapper around the existing generic approval engine.
/// <para>
/// The sequence is exactly: verify the credential against the <b>current</b> user's own
/// credentials, mark this request re-authenticated, then call the same
/// <c>IApprovalRepository.ApproveItem</c>/<c>RejectItem</c> the generic
/// <c>ApprovalController</c> calls — so all stage routing and multi-approver logic is
/// reused with zero duplication. The <see cref="QcApproval"/> row, including
/// <c>ReauthConfirmedAt</c>, is written by the handler inside that call.
/// </para>
/// </summary>
public class QcSignatureService(
    ApplicationDbContext context,
    UserManager<User> userManager,
    IQcReauthContext reauthContext,
    IApprovalRepository approvalRepository) : IQcSignatureService
{
    /// <summary>
    /// Verifies the acting user's own password, the same way <c>AuthRepository</c> and
    /// <c>StpDocumentRepository</c> do. This is not a generic password check: it is the
    /// authenticated user re-proving they are present, which is what meaning-of-signature
    /// requires.
    /// </summary>
    public async Task<Result> VerifyAsync(Guid userId, string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return QcWorksheetErrors.ReauthenticationRequired;

        var user = await context.Users.FirstOrDefaultAsync(item => item.Id == userId);
        if (user?.PasswordHash is null)
            return QcWorksheetErrors.ReauthenticationFailed;

        var verification = userManager.PasswordHasher.VerifyHashedPassword(
            user, user.PasswordHash, password);

        if (verification == PasswordVerificationResult.Failed)
            return QcWorksheetErrors.ReauthenticationFailed;

        reauthContext.Confirm(userId);
        return Result.Success();
    }

    public async Task<Result> SignAndApproveAsync(
        string modelType, Guid entityId, Guid userId, List<Guid> roleIds, string password, string comments)
    {
        var verified = await VerifyAsync(userId, password);
        if (!verified.IsSuccess)
            return verified;

        return await approvalRepository.ApproveItem(modelType, entityId, userId, roleIds, comments);
    }

    public async Task<Result> SignAndRejectAsync(
        string modelType, Guid entityId, Guid userId, List<Guid> roleIds, string password, string comments)
    {
        var verified = await VerifyAsync(userId, password);
        if (!verified.IsSuccess)
            return verified;

        return await approvalRepository.RejectItem(modelType, entityId, userId, roleIds, comments);
    }
}
