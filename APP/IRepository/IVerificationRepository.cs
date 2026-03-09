using SHARED;
using SHARED.Requests;

namespace APP.IRepository;

public interface IVerificationRepository
{
    Task<Result> VerifyEntity(VerifyRequest request, Guid userId);
}
