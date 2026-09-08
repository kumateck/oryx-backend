using System.Security.Claims;

namespace APP.Services.StpDocuments;

public interface IStpDocumentAccessService
{
    Task<bool> CanAccessOwner(ClaimsPrincipal user, string ownerType, bool write);
    Task<bool> CanAccessDocument(ClaimsPrincipal user, Guid documentId, bool write);
    Task<bool> CanOpenEditor(ClaimsPrincipal user, Guid documentId);
}
