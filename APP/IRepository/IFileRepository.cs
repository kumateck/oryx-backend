using Microsoft.AspNetCore.Http;
using SHARED;

namespace APP.IRepository;

public interface IFileRepository
{
    Task<Result> SaveBlobItem(string modelType, Guid modelId, string reference, IFormFile file,
        Guid? userId);
    Task<Result> SaveBlobItem(string modelType, Guid modelId, List<IFormFile> files,
        Guid? userId);
    Task<Result> DeleteAttachment(Guid modelId, Guid userId); 
    Task<Result> DeleteAttachment(Guid id, string reference, Guid userId);
}